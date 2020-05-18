using Pinknose.DistributedWorkers.Extensions;
using Pinknose.DistributedWorkers.Messages;
using Pinknose.DistributedWorkers.MessageTags;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Pinknose.DistributedWorkers
{
    public enum RpcCallResult { Success, Timeout, BadSignature }

    public enum SignatureVerificationStatus { SignatureValid, SignatureNotValid, NoValidClientInfo, SignatureUnverified}

    public abstract class MessageClientBase<TServerQueue> : MessageClientBase where TServerQueue : MessageQueue, new()
    {
        //internal TServerQueue LogQueue { get; private set; }

        /// <summary>
        /// Remote Procedure Call queue (in to master).
        /// </summary>
        protected TServerQueue ServerQueue { get; private set; }

        protected MessageClientBase(string clientName, string systemName, string rabbitMqServerHostName, CngKey key, string userName, string password, params MessageTag[] subscriptionTags) :
            this(clientName, systemName, rabbitMqServerHostName, key, userName, password, new MessageTagCollection(subscriptionTags))
        {

        }

        protected MessageClientBase(string clientName, string systemName, string rabbitMqServerHostName, CngKey key, string userName, string password, MessageTagCollection subscriptionTags) :
            base(clientName, systemName, rabbitMqServerHostName, key, userName, password, subscriptionTags)
        {
            ServerQueue = MessageQueue.CreateMessageQueue<TServerQueue>(this, Channel, ClientName, ServerQueueName);
        }
    }

    /// <summary>
    /// The base class for message clients and servers.
    /// </summary>
    public abstract class MessageClientBase : IDisposable
    {
        protected CngKey CngKey { get; private set; }
        protected internal ECDsaCng Dsa { get; private set; }

        protected PublicKeystore PublicKeystore { get; } = new PublicKeystore();

        private string _userName;
        private string _password;

#pragma warning disable CA1308 // Normalize strings to uppercase
        protected string WorkQueueName => $"{SystemName}-queue-work".ToLowerInvariant();
        protected string BroadcastExchangeName => $"{SystemName}-exchange-broadcast".ToLowerInvariant();
        protected string SubscriptionExchangeName => $"{SystemName}-exchange-subscription".ToLowerInvariant();
        protected string DedicatedQueueName => $"{SystemName}-{ClientName}-queue-dedicated".ToLowerInvariant();
        protected string ServerQueueName => $"{SystemName}-queue-server".ToLowerInvariant();
        protected string SubscriptionQueueName => $"{SystemName}-{ClientName}-queue-subscription".ToLowerInvariant();
        protected string LogQueueName => $"{SystemName}-queue-log".ToLowerInvariant();
#pragma warning restore CA1308 // Normalize strings to uppercase

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        protected void FireMessageReceivedEvent(MessageReceivedEventArgs eventArgs)
        {
            MessageReceived?.Invoke(this, eventArgs);
        }

        /// <summary>
        /// Heartbeat time between clients/server in milliseconds;
        /// </summary>
        protected const int HeartbeatTime = 10000;

        private System.Timers.Timer heartbeatTimer = new System.Timers.Timer()
        {
            Interval = HeartbeatTime,
            AutoReset = true
        };

        private IConnection connection;
        protected IModel Channel { get; private set; }

        protected MessageClientBase(string clientName, string systemName, string rabbitMqServerHostName, CngKey key, string userName, string password, params MessageTag[] subscriptionTags) :
            this(clientName, systemName, rabbitMqServerHostName, key, userName, password, new MessageTagCollection(subscriptionTags))
        {
            
        }


        protected MessageClientBase(string clientName, string systemName, string rabbitMqServerHostName, CngKey key, string userName, string password, MessageTagCollection subscriptionTags )
        {
            if (string.IsNullOrEmpty(clientName))
            {
                throw new ArgumentNullException(nameof(clientName));
            }

            if (string.IsNullOrEmpty(systemName))
            {
                throw new ArgumentNullException(nameof(systemName));
            }

            if (string.IsNullOrEmpty(rabbitMqServerHostName))
            {
                throw new ArgumentNullException(nameof(rabbitMqServerHostName));
            }

            if (string.IsNullOrEmpty(userName))
            {
                throw new ArgumentNullException(nameof(userName));
            }

            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException(nameof(password));
            }

            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (subscriptionTags == null)
            {
                throw new ArgumentNullException(nameof(subscriptionTags));
            }

            CngKey = key;
            Dsa = new ECDsaCng(CngKey);

            _userName = userName;
            _password = password;
            ClientName = clientName;

            Log.Verbose($"Client '{clientName}' has public key '{key.GetPublicKeyHash()}'.");

            InternalId = Guid.NewGuid().ToString();
            SystemName = systemName;

            ConnectionFactory factory = new ConnectionFactory()
            {
                HostName = rabbitMqServerHostName,
                UserName = userName,
                Password = password,
                //VirtualHost = systemName,
                RequestedHeartbeat = TimeSpan.FromSeconds(10) //todo: Need to set elsewhere, be configurable
            };

            connection = factory.CreateConnection();
            Channel = connection.CreateModel();

            WorkQueue = MessageQueue.CreateMessageQueue<ReadableMessageQueue>(this, Channel, ClientName, WorkQueueName);
            WorkQueue.MessageReceived += Queue_MessageReceived;

            Channel.ExchangeDeclare(
                BroadcastExchangeName,
                ExchangeType.Fanout,
                false,
                true,
                new Dictionary<string, object>());

            
            //LogQueue = MessageQueue.CreateMessageQueue<TServerQueue>(this, Channel, ClientName, LogQueueName);

            Channel.ExchangeDeclare(
                SubscriptionExchangeName,
                ExchangeType.Headers,
                false,
                true,
                new Dictionary<string, object>());

            SubscriptionQueue = MessageQueue.CreateExchangeBoundMessageQueue<ReadableMessageQueue>(this, Channel, ClientName, SubscriptionExchangeName, SubscriptionQueueName, subscriptionTags);
            SubscriptionQueue.MessageReceived += Queue_MessageReceived;
        }

        private void Queue_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            MessageReceived?.Invoke(this, e);
        }

        private void HeartbeatTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            SendHeartbeat();
        }

        /*
        public byte[] AddSignature(byte[] message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            byte[] signature = Dsa.SignData(message);

            if (signature.Length > 256)
            {
                throw new Exception("Signature too long.");
            }

            byte[] output = new byte[message.Length + signature.Length + 1];

            message.CopyTo(output, 0);
            signature.CopyTo(output, message.Length);
            output[output.Length-1] = (byte)signature.Length;
                        
            return output;
        }
        */



        public SignatureVerificationStatus ValidateSignature(byte[] message, byte[] signature, string clientName)
        {
            if (!PublicKeystore.ContainsKey(clientName))
            {
                return SignatureVerificationStatus.NoValidClientInfo;
            }
            else if (PublicKeystore[clientName].Dsa.VerifyData(message, signature))
            {
                return SignatureVerificationStatus.SignatureValid;
            }
            else
            {
                return SignatureVerificationStatus.SignatureNotValid;
            }
        }

        /*
        public static bool SignatureIsValid(byte[] message, byte[] signature, ECDsaCng dsa)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (dsa == null)
            {
                throw new ArgumentNullException(nameof(dsa));
            }

            // The last byte of the message is the length of the signature
            int signatureIndex = (int)message[^1] + 1;

            // First portion of the messages is the binary serialized message, the next portion is
            // the signature
            return dsa.VerifyData(message, signature);
        }
        */

        /// <summary>
        /// Sends the message to all clients.
        /// </summary>
        /// <param name="message"></param>
        public void BroadcastToAllClients(MessageBase message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            message.ClientName = this.ClientName;

            byte[] hashedMessage = message.Serialize(this);            

            try
            {
                Channel.BasicPublish(
                    exchange: BroadcastExchangeName,
                    routingKey: "",
                    mandatory: true,
                    basicProperties: null,
                    body: hashedMessage);
            }
            catch (AlreadyClosedException e)
            {
                Log.Warning(e, $"Tried to send data to the closed exchange '{BroadcastExchangeName}'.");
            }
            
        }

        public virtual void Connect(TimeSpan timeout)
        {
            //TODO: SHould ACK be required?
            SubscriptionQueue.BeginFullConsume(false);

            heartbeatTimer.Elapsed += HeartbeatTimer_Elapsed;
            heartbeatTimer.Start();
        }

        public abstract void Disconnect();

        public bool IsConnected { get; protected set; } = false;

        protected ReadableMessageQueue WorkQueue { get; private set; }

        

        protected ReadableMessageQueue SubscriptionQueue { get; private set; }

        public string SystemName { get; private set; }

        public string InternalId { get; private set; }


        public string ClientName { get; private set; }


        public void WriteToSubscriptionQueues(MessageBase message) => SubscriptionQueue.WriteToBoundExchange(message);

        protected abstract void SendHeartbeat();

        public void BeginFullWorkConsume(bool autoAcknowledge) => WorkQueue.BeginFullConsume(autoAcknowledge);
        public void BeginLimitedWorkConsume(int maxActiveMessages, bool autoAcknowledge) => WorkQueue.BeginLimitedConsume(maxActiveMessages, autoAcknowledge);

        internal byte[] SignData(byte[] data)
        {
            return Dsa.SignData(data);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    heartbeatTimer?.Dispose();
                    connection?.Dispose();
                    Dsa?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~MessageClientBase()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }


        #endregion
    }
}
