using Pinknose.DistributedWorkers.Messages;
using Pinknose.Utilities;
using RabbitMQ.Client;
using Serilog;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Pinknose.DistributedWorkers
{
    public sealed class MessageClient : MessageClientBase<MessageQueue>
    {
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        private EventWaitHandle rpcCallWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
        private object rpcLock = new object();
        private string rpcCallCorrelationId;
        private MessageBase lastRpcMessageReceived;
        private byte[] lastRpcRawDataReceived;

        private ECDsaCng serverDsa = null;

        private ReusableThreadSafeTimer serverHeartbeatTimer = new ReusableThreadSafeTimer()
        {
            Interval = HeartbeatTime,
            AutoReset = false
        };

        public MessageClient(string clientName, string systemName, string rabbitMqServerHostName, CngKey key, string userName, string password, params MessageTag[] subscriptionTags) :
            this(clientName, systemName, rabbitMqServerHostName, key, userName, password, new MessageTagCollection(subscriptionTags))
        {

        }

        public MessageClient(string clientName, string systemName, string rabbitMqServerHostName, CngKey key, string userName, string password, MessageTagCollection subscriptionTags) :
            base(clientName, systemName, rabbitMqServerHostName, key, userName, password, subscriptionTags)
        {
            DedicatedQueue = MessageQueue.CreateExchangeBoundMessageQueue<ReadableMessageQueue>(this, Channel, clientName, BroadcastExchangeName, DedicatedQueueName);

            DedicatedQueue.MessageReceived += DedicatedQueue_MessageReceived;
            DedicatedQueue.BeginFullConsume(true);

            // Announce client to server
            ClientAnnounceMessage message = new ClientAnnounceMessage(CngKey, false);

            MessageBase response;
            byte[] rawResponse;

            while (this.WriteRpcCall(message, out response, out rawResponse, 10000) == RpcCallResult.Timeout)
            {
                Log.Warning("Timeout trying to communicate with the server.");
            }
            
            switch (((ClientAnnounceResponseMessage)response).Response)
            {
                case AnnounceResponse.Accepted:
                    break;

                case AnnounceResponse.Rejected:
                    throw new Exception($"Client rejected by server with the following message: {response.MessageText}.");
            }

            // Verify server's public key
            var tempKey = CngKey.Import(((ClientAnnounceResponseMessage)response).ServerPublicKey, CngKeyBlobFormat.EccFullPublicBlob);
            var tempDsa = new ECDsaCng(tempKey);

            if (!SignatureIsValid(rawResponse, tempDsa))
            {
                throw new Exception("Bad server key.");
            }
            else
            {
                serverDsa = tempDsa;

                serverHeartbeatTimer.Elapsed += ServerHeartbeatTimer_Elapsed;
                serverHeartbeatTimer.Start();
            }
            
        }

        private void ServerHeartbeatTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Log.Warning("Server timeout.");
        }

        protected sealed override void SendHeartbeat()
        {
            //todo: reenable  this.WriteRpcCallNoWait(new HeartbeatMessage(false));
        }


        public RpcCallResult WriteRpcCall(MessageBase message, out MessageBase response, int waitTime, bool broadcastResults = false)
        {
            byte[] rawMessage;

            return WriteRpcCall(message, out response, out rawMessage, waitTime, broadcastResults);
        }

        public RpcCallResult WriteRpcCall(MessageBase message, out MessageBase response, out byte[] rawResponse, int waitTime, bool broadcastResults = false)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            // Only one RPC call can be done at a time.
            lock (rpcLock)
            {
                rpcCallCorrelationId = Guid.NewGuid().ToString();

                message.ClientName = ClientName;

                IBasicProperties basicProperties = this.Channel.CreateBasicProperties();
                basicProperties.CorrelationId = rpcCallCorrelationId;

                string exchangeName;
                string routingKey;

                if (!broadcastResults)
                {
                    exchangeName = "";
                    routingKey = DedicatedQueue.Name;
                }
                else
                {
                    exchangeName = BroadcastExchangeName;
                    routingKey = "";
                }

                basicProperties.ReplyTo = $"exchangeName:{exchangeName},routingKey:{routingKey}";

                byte[] hashedMessage = AddSignature(message.Serialize());

                Channel.BasicPublish(
                    exchange: "",
                    routingKey: ServerQueue.Name,
                    basicProperties: basicProperties,
                    hashedMessage);

                RpcCallResult result;

                if (rpcCallWaitHandle.WaitOne(waitTime))
                {
                    response = lastRpcMessageReceived;
                    rawResponse = lastRpcRawDataReceived;

                    if (serverDsa == null || SignatureIsValid(rawResponse, serverDsa))
                    {
                        result = RpcCallResult.Success;
                    }
                    else
                    {
                        result = RpcCallResult.BadSignature;
                    }
                }
                else
                {
                    result = RpcCallResult.Timeout;
                    response = null;
                    rawResponse = null;
                    Log.Warning($"RPC call of type '{message.GetType()}' timed out.");
                }

                rpcCallCorrelationId = "";

                return result;
            }
        }

        public void WriteRpcCallNoWait(MessageBase message, bool broadcastResults = false)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            string correlationId = Guid.NewGuid().ToString();

            message.ClientName = ClientName;


            IBasicProperties basicProperties = this.Channel.CreateBasicProperties();
            basicProperties.CorrelationId = correlationId;

            string exchangeName;
            string routingKey;

            if (!broadcastResults)
            {
                exchangeName = "";
                routingKey = DedicatedQueueName;
            }
            else
            {
                exchangeName = BroadcastExchangeName;
                routingKey = "";
            }

            basicProperties.ReplyTo = $"exchangeName:{exchangeName},routingKey:{routingKey}";

            byte[] signedMessage = AddSignature(message.Serialize());

            Channel.BasicPublish(
                exchange: "",
                routingKey: ServerQueue.Name,
                basicProperties: basicProperties,
                signedMessage);
        }

        public ReadableMessageQueue DedicatedQueue { get; private set; }

        private void DedicatedQueue_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            if (e.Message.BasicProperties.CorrelationId == rpcCallCorrelationId)
            {
                lastRpcMessageReceived = e.Message;
                lastRpcRawDataReceived = e.RawData;
                rpcCallWaitHandle.Set();
            }
            else if (e.Message.GetType() == typeof(ClientReannounceRequestMessage))
            {
                Log.Information("Server requested reannouncement of clients.");
                var message = new ClientAnnounceMessage(CngKey, false);
                WriteRpcCallNoWait(message);
            }
            else if (e.Message.GetType() == typeof(HeartbeatMessage))
            {
                if (!serverHeartbeatTimer.Enabled)
                {
                    Log.Information("Server heartbeat re-established.");
                }

                serverHeartbeatTimer.Restart();
            }
            else
            {
                MessageReceived?.Invoke(this, e);
            }
        }

        
    }
}
