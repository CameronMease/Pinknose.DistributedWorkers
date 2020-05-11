using Pinknose.DistributedWorkers.Messages;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Pinknose.DistributedWorkers
{
    public enum RpcCallResult { Success, Timeout, BadSignature }

    /// <summary>
    /// The base class for message clients and servers.
    /// </summary>
    /// <typeparam name="TServerQueue"></typeparam>
    public abstract class MessageClientBase<TServerQueue> : IDisposable, IMessageClient where TServerQueue : MessageQueue, new()
    {
        protected CngKey CngKey { get; private set; }
        ECDsaCng dsa;

        private string _userName;
        private string _password;

        protected string WorkQueueName => $"{SystemName}-queue-work".ToLowerInvariant();
        protected string BroadcastExchangeName => $"{SystemName}-exchange-broadcast".ToLowerInvariant();
        protected string SubscriptionExchangeName => $"{SystemName}-exchange-subscription".ToLowerInvariant();
        protected string DedicatedQueueName => $"{SystemName}-{ClientName}-queue-dedicated".ToLowerInvariant();
        protected string ServerQueueName => $"{SystemName}-queue-server".ToLowerInvariant();

        protected string SubscriptionQueueName => $"{SystemName}-{ClientName}-queue-subscription".ToLowerInvariant();

        protected string LogQueueName => $"{SystemName}-queue-log".ToLowerInvariant();

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

            CngKey = key;
            dsa = new ECDsaCng(CngKey);

            _userName = userName;
            _password = password;
            ClientName = clientName;

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

            Channel.ExchangeDeclare(
                BroadcastExchangeName,
                ExchangeType.Fanout,
                false,
                false,
                new Dictionary<string, object>());

            ServerQueue = MessageQueue.CreateMessageQueue<TServerQueue>(this, Channel, ClientName, ServerQueueName);
            LogQueue = MessageQueue.CreateMessageQueue<TServerQueue>(this, Channel, ClientName, LogQueueName);

            Channel.ExchangeDeclare(
                SubscriptionExchangeName,
                ExchangeType.Headers,
                false,
                false,
                new Dictionary<string, object>());

            SubscriptionQueue = MessageQueue.CreateExchangeBoundMessageQueue<ReadableMessageQueue>(this, Channel, ClientName, SubscriptionExchangeName, SubscriptionQueueName, subscriptionTags);
            
            //TODO: SHould ACK be required?
            SubscriptionQueue.BeginFullConsume(false);

            heartbeatTimer.Elapsed += HeartbeatTimer_Elapsed;
            heartbeatTimer.Start();
        }

        private void HeartbeatTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            SendHeartbeat();
        }

        public byte[] AddSignature(byte[] message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            byte[] signature = dsa.SignData(message);

            byte[] output = new byte[message.Length + signature.Length];

            message.CopyTo(output, 0);
            signature.CopyTo(output, message.Length);
            
            return output;
        }

        public bool SignatureIsValid(byte[] message, ECDsaCng dsa)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            // TODO: Figure this out programmatically
            int hashLength = 64;

            return dsa.VerifyData(message[..^hashLength], message[^hashLength..]);
        }

        /// <summary>
        /// Sends the message to all clients.
        /// </summary>
        /// <param name="message"></param>
        public void BroacastToAllClients(MessageBase message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            byte[] hashedMessage = AddSignature(message.Serialize());

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

        public ReadableMessageQueue WorkQueue { get; private set; }

        internal TServerQueue LogQueue { get; private set; }

        /// <summary>
        /// Remote Procedure Call queue (in to master).
        /// </summary>
        public TServerQueue ServerQueue { get; private set; }

        public ReadableMessageQueue SubscriptionQueue { get; private set; }

        public string SystemName { get; private set; }

        public string InternalId { get; private set; }


        public string ClientName { get; private set; }


        protected abstract void SendHeartbeat();

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
