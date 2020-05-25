using Pinknose.DistributedWorkers.Exceptions;
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
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pinknose.DistributedWorkers
{
    public enum RpcCallResult { Success, Timeout, BadSignature }

    public enum SignatureVerificationStatus { SignatureValid, SignatureValidButUntrusted, SignatureNotValid, NoValidClientInfo, SignatureUnverified}

    public enum EncryptionOption { None = 0, EncryptWithPrivateKey=1, EncryptWithSystemSharedKey=2}

    public abstract class MessageClientBase<TServerQueue> : MessageClientBase where TServerQueue : MessageQueue, new()
    {

        //internal TServerQueue LogQueue { get; private set; }

        /// <summary>
        /// Remote Procedure Call queue (in to master).
        /// </summary>
        protected TServerQueue ServerQueue { get; private set; }

        protected MessageClientBase(MessageClientInfo clientInfo, string rabbitMqServerHostName, string userName, string password) :
            base(clientInfo, rabbitMqServerHostName, userName, password)
        {
            
        }

        protected override void SetupConnections(TimeSpan timeout, MessageTagCollection subscriptionTags)
        {
            base.SetupConnections(timeout, subscriptionTags);

            ServerQueue = MessageQueue.CreateMessageQueue<TServerQueue>(this, Channel, ClientName, ServerQueueName);
        }
    }

    /// <summary>
    /// The base class for message clients and servers.
    /// </summary>
    public abstract partial class MessageClientBase : IDisposable
    {
        protected static readonly int SharedKeyByteSize = 32;

        public event EventHandler<AsynchronousExceptionEventArgs> AsynchronousException;
        protected void FireAsynchronousExceptionEvent(object sender, AsynchronousExceptionEventArgs eventArgs) => AsynchronousException?.Invoke(this, eventArgs);

        protected readonly Dictionary<string, RpcCallWaitInfo> rpcCallWaitInfo = new Dictionary<string, RpcCallWaitInfo>();

        // TODO: Change to byte[]?
        protected ReadOnlyMemory<byte> PreviousSystemSharedKey { get; set; }  = null;
        protected ReadOnlyMemory<byte> CurrentSystemSharedKey { get; set; } = null;

        //protected CngKey CngKey { get; private set; }
        //protected internal ECDsaCng Dsa { get; private set; }

        protected MessageClientInfo ClientInfo { get; private set; }

        protected PublicKeystore PublicKeystore { get; }

        private string _userName;
        private string _password;
        private string _rabbitMqServerHostName;

        protected string WorkQueueName => NameHelper.GetWorkQueueName(SystemName);
        protected string BroadcastExchangeName => NameHelper.GetBroadcastExchangeName(SystemName);
        protected string SubscriptionExchangeName => NameHelper.GetSubscriptionExchangeName(SystemName);
        protected string DedicatedQueueName => NameHelper.GetDedicatedQueueName(SystemName, ClientName);
        protected string ServerQueueName => NameHelper.GetServerQueueName(SystemName);
        protected string SubscriptionQueueName => NameHelper.GetSubscriptionQueueName(SystemName, ClientName);
        //protected string LogQueueName => $"{SystemName}-queue-log".ToLowerInvariant();

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


        protected MessageClientBase(MessageClientInfo clientInfo, string rabbitMqServerHostName, string userName, string password)
        {
             if (clientInfo is null)
            {
                throw new ArgumentNullException(nameof(clientInfo));
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


            ClientInfo = clientInfo;

            PublicKeystore = new PublicKeystore(clientInfo);

            _userName = userName;
            _password = password;
            _rabbitMqServerHostName = rabbitMqServerHostName;

            //Log.Verbose($"Client '{ClientInfo.Name}' has public key '{ClientInfo.k.GetPublicKeyHash()}'.");

            InternalId = Guid.NewGuid().ToString();

           
        }

        private void Queue_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            MessageReceived?.Invoke(this, e);
        }

        private void HeartbeatTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            SendHeartbeat();
        }

        protected (byte[] SignedMessage, IBasicProperties BasicProperties) ConfigureUnicastMessageForSend(MessageBase message, string recipientName, EncryptionOption encryptionOptions)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            string correlationId = Guid.NewGuid().ToString();

            //message.ClientName = ClientName;

            IBasicProperties basicProperties = this.Channel.CreateBasicProperties();
            basicProperties.CorrelationId = correlationId;

            string exchangeName;
            string routingKey;

            exchangeName = "";
            routingKey = DedicatedQueueName;
            
            basicProperties.ReplyTo = $"exchangeName:{exchangeName},routingKey:{routingKey}";

            var wrapper = MessageEnvelope.WrapMessage(message, recipientName, this, encryptionOptions);
            //byte[] signedMessage = message.Serialize(this, clientName, encryptionOptions);

            return (wrapper.Serialize(), basicProperties);
        }

        public async Task<RpcCallWaitInfo> WriteToClient(MessageClientInfo clientInfo, MessageBase message, int waitTime, EncryptionOption encryptionOptions)
        {
            if (clientInfo == null)
            {
                throw new ArgumentNullException(nameof(clientInfo));
            }

            return await WriteToClient(clientInfo.Name, message, waitTime, encryptionOptions).ConfigureAwait(false);
        }

        protected async Task<RpcCallWaitInfo> WriteToClient(string clientName, MessageBase message, int waitTime, EncryptionOption encryptionOptions)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var formattedMessage = ConfigureUnicastMessageForSend(message, clientName, encryptionOptions);

            Channel.BasicPublish(
                exchange: "",
                routingKey: NameHelper.GetDedicatedQueueName(SystemName, clientName),
                basicProperties: formattedMessage.BasicProperties,
                formattedMessage.SignedMessage);

            RpcCallWaitInfo response = new RpcCallWaitInfo();
            response.WaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

            rpcCallWaitInfo.Add(formattedMessage.BasicProperties.CorrelationId, response);

            Task task = new Task(() =>
            {

                if (!response.WaitHandle.WaitOne(waitTime))
                {
                    response.CallResult = RpcCallResult.Timeout;
                }
                else
                {
                    if (response.ResponseMessageEnvelope.SignatureVerificationStatus == SignatureVerificationStatus.SignatureValid)
                    {
                        response.CallResult = RpcCallResult.Success;
                    }
                    else
                    {
                        response.CallResult = RpcCallResult.BadSignature;
                    }
                }

                rpcCallWaitInfo.Remove(formattedMessage.BasicProperties.CorrelationId);
                response.WaitHandle.Dispose();
            });

            task.Start();

            await task.ConfigureAwait(false);

            return response;
        }

        public void WriteToClientNoWait(MessageClientInfo clientInfo, MessageBase message, EncryptionOption encryptionOptions)
        {
            if (clientInfo == null)
            {
                throw new ArgumentNullException(nameof(clientInfo));
            }

            WriteToClientNoWait(clientInfo.Name, message, encryptionOptions);
        }

        protected void WriteToClientNoWait(string clientName, MessageBase message, EncryptionOption encryptionOptions)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var formattedMessage = ConfigureUnicastMessageForSend(message, clientName, encryptionOptions);

            Channel.BasicPublish(
                exchange: "",
                routingKey: NameHelper.GetDedicatedQueueName(SystemName, clientName),
                basicProperties: formattedMessage.BasicProperties,
                formattedMessage.SignedMessage);
        }

        


        /// <summary>
        /// Sends the message to all clients.
        /// </summary>
        /// <param name="message"></param>
        public void BroadcastToAllClients(MessageBase message, EncryptionOption encryptionOption)
        {
            if (encryptionOption == EncryptionOption.EncryptWithPrivateKey)
            {
                //TODO: Add message
                throw new ArgumentOutOfRangeException(nameof(encryptionOption));
            }

            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            //message.ClientName = this.ClientName;

            var wrapper = MessageEnvelope.WrapMessage(message, "", this, encryptionOption);

            try
            {
                Channel.BasicPublish(
                    exchange: BroadcastExchangeName,
                    routingKey: "",
                    mandatory: true,
                    basicProperties: null,
                    body: wrapper.Serialize());
            }
            catch (AlreadyClosedException e)
            {
                Log.Warning(e, $"Tried to send data to the closed exchange '{BroadcastExchangeName}'.");
            }
            
        }

        protected virtual void SetupConnections(TimeSpan timeout, MessageTagCollection subscriptionTags)
        {
            ConnectionFactory factory = new ConnectionFactory()
            {
                HostName = _rabbitMqServerHostName,
                UserName = _userName,
                Password = _password,
                //VirtualHost = systemName,
                RequestedHeartbeat = TimeSpan.FromSeconds(10) //todo: Need to set elsewhere, be configurable
            };

            connection = factory.CreateConnection();
            Channel = connection.CreateModel();

            WorkQueue = MessageQueue.CreateMessageQueue<ReadableMessageQueue>(this, Channel, ClientName, WorkQueueName);
            WorkQueue.MessageReceived += Queue_MessageReceived;
            WorkQueue.AsynchronousException += (sender, eventArgs) => this.AsynchronousException?.Invoke(this, eventArgs);

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
            SubscriptionQueue.AsynchronousException += (sender, eventArgs) => FireAsynchronousExceptionEvent(sender, eventArgs);

            //TODO: SHould ACK be required?
            SubscriptionQueue.BeginFullConsume(false);

            heartbeatTimer.Elapsed += HeartbeatTimer_Elapsed;
            heartbeatTimer.Start();
        }

        public abstract void Disconnect();

        public bool IsConnected { get; protected set; } = false;

        protected ReadableMessageQueue WorkQueue { get; private set; }

        

        protected ReadableMessageQueue SubscriptionQueue { get; private set; }

        public string SystemName => ClientInfo.SystemName;

        public string InternalId { get; private set; }


        public string ClientName => ClientInfo.Name;

        public void WriteToSubscriptionQueues(MessageBase message, EncryptionOption encryptionOption, params MessageTag[] tags) =>
            WriteToSubscriptionQueues(message, encryptionOption, new MessageTagCollection(tags));

        public void WriteToSubscriptionQueues(MessageBase message, EncryptionOption encryptionOption, MessageTagCollection tags)
        {
            if (encryptionOption == EncryptionOption.EncryptWithPrivateKey)
            {
                //TODO: Add message
                throw new ArgumentOutOfRangeException(nameof(encryptionOption));
            }
            SubscriptionQueue.WriteToBoundExchange(message, encryptionOption, tags);
        }

        protected abstract void SendHeartbeat();

        public void BeginFullWorkConsume(bool autoAcknowledge) => WorkQueue.BeginFullConsume(autoAcknowledge);
        public void BeginLimitedWorkConsume(int maxActiveMessages, bool autoAcknowledge) => WorkQueue.BeginLimitedConsume(maxActiveMessages, autoAcknowledge);



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
