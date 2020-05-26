///////////////////////////////////////////////////////////////////////////////////
// MIT License
//
// Copyright(c) 2020 Cameron Mease
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
///////////////////////////////////////////////////////////////////////////////////

using Pinknose.DistributedWorkers.Exceptions;
using Pinknose.DistributedWorkers.Keystore;
using Pinknose.DistributedWorkers.MessageQueues;
using Pinknose.DistributedWorkers.Messages;
using Pinknose.DistributedWorkers.MessageTags;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Pinknose.DistributedWorkers.Clients
{
    public enum EncryptionOption { None = 0, EncryptWithPrivateKey = 1, EncryptWithSystemSharedKey = 2 }

    public enum RpcCallResult { Success, Timeout, BadSignature }

    public enum SignatureVerificationStatus { SignatureValid, SignatureValidButUntrusted, SignatureNotValid, NoValidClientInfo, SignatureUnverified }

    /// <summary>
    /// The base class for message clients and servers.
    /// </summary>
    public abstract partial class MessageClientBase : IDisposable
    {
        #region Fields

        /// <summary>
        /// Heartbeat time between clients/server in milliseconds;
        /// </summary>
        protected const int HeartbeatTime = 10000;

        protected static readonly int SharedKeyByteSize = 32;

        protected readonly Dictionary<string, RpcCallWaitInfo> rpcCallWaitInfo = new Dictionary<string, RpcCallWaitInfo>();

        private string _password;

        private string _rabbitMqServerHostName;

        private string _userName;

        private IConnection connection;

        private bool disposedValue = false;

        private System.Timers.Timer heartbeatTimer = new System.Timers.Timer()
        {
            Interval = HeartbeatTime,
            AutoReset = true
        };

        #endregion Fields

        #region Constructors

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

        #endregion Constructors

        #region Events

        public event EventHandler<AsynchronousExceptionEventArgs> AsynchronousException;

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        #endregion Events

        #region Properties

        public string ClientName => ClientInfo.Name;

        public string InternalId { get; private set; }

        public bool IsConnected { get; protected set; } = false;

        public string SystemName => ClientInfo.SystemName;

        protected string BroadcastExchangeName => NameHelper.GetBroadcastExchangeName(SystemName);

        protected IModel Channel { get; private set; }

        protected MessageClientInfo ClientInfo { get; private set; }

        protected string DedicatedQueueName => NameHelper.GetDedicatedQueueName(SystemName, ClientName);

        protected PublicKeystore PublicKeystore { get; }

        protected string ServerQueueName => NameHelper.GetServerQueueName(SystemName);

        protected string SubscriptionExchangeName => NameHelper.GetSubscriptionExchangeName(SystemName);

        protected ReadableMessageQueue SubscriptionQueue { get; private set; }

        protected string SubscriptionQueueName => NameHelper.GetSubscriptionQueueName(SystemName, ClientName);

        protected ReadableMessageQueue WorkQueue { get; private set; }

        protected string WorkQueueName => NameHelper.GetWorkQueueName(SystemName);

        #endregion Properties

        #region Methods

        public void BeginFullWorkConsume(bool autoAcknowledge) => WorkQueue.BeginFullConsume(autoAcknowledge);

        public void BeginLimitedWorkConsume(int maxActiveMessages, bool autoAcknowledge) => WorkQueue.BeginLimitedConsume(maxActiveMessages, autoAcknowledge);

        /// <summary>
        /// Sends the message to all clients.
        /// </summary>
        /// <param name="message"></param>
        public void BroadcastToAllClients(MessageBase message, EncryptionOption encryptionOption)
        {
            if (encryptionOption == EncryptionOption.EncryptWithPrivateKey)
            {
                throw new ArgumentOutOfRangeException(nameof(encryptionOption), "Broadcast messages cannot be encrypted with private keys.");
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

        public abstract void Disconnect();

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        public async Task<RpcCallWaitInfo> WriteToClient(MessageClientInfo clientInfo, MessageBase message, int waitTime, EncryptionOption encryptionOptions)
        {
            if (clientInfo == null)
            {
                throw new ArgumentNullException(nameof(clientInfo));
            }

            return await WriteToClient(clientInfo.Name, message, waitTime, encryptionOptions).ConfigureAwait(false);
        }

        public void WriteToClientNoWait(MessageClientInfo clientInfo, MessageBase message, EncryptionOption encryptionOptions)
        {
            if (clientInfo == null)
            {
                throw new ArgumentNullException(nameof(clientInfo));
            }

            WriteToClientNoWait(clientInfo.Name, message, encryptionOptions);
        }

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

        protected (byte[] SignedMessage, IBasicProperties BasicProperties) ConfigureUnicastMessageForSend(MessageBase message, string recipientName, EncryptionOption encryptionOptions)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            string correlationId = Guid.NewGuid().ToString();

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

        // To detect redundant calls
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

        protected void FireAsynchronousExceptionEvent(object sender, AsynchronousExceptionEventArgs eventArgs) => AsynchronousException?.Invoke(this, eventArgs);

        //protected string LogQueueName => $"{SystemName}-queue-log".ToLowerInvariant();
        protected void FireMessageReceivedEvent(MessageReceivedEventArgs eventArgs)
        {
            MessageReceived?.Invoke(this, eventArgs);
        }

        protected abstract void SendHeartbeat();

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

            var tempTags = subscriptionTags.ToList();
            tempTags.Add(SystemTags.BroadcastTag);

            SubscriptionQueue = MessageQueue.CreateExchangeBoundMessageQueue<ReadableMessageQueue>(this, Channel, ClientName, SubscriptionExchangeName, SubscriptionQueueName, tempTags.ToArray());
            SubscriptionQueue.MessageReceived += Queue_MessageReceived;
            SubscriptionQueue.AsynchronousException += (sender, eventArgs) => FireAsynchronousExceptionEvent(sender, eventArgs);

            //TODO: SHould ACK be required?
            SubscriptionQueue.BeginFullConsume(false);

            heartbeatTimer.Elapsed += HeartbeatTimer_Elapsed;
            heartbeatTimer.Start();
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

        private void HeartbeatTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            SendHeartbeat();
        }

        private void Queue_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            MessageReceived?.Invoke(this, e);
        }

        #endregion Methods

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~MessageClientBase()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }
    }
}