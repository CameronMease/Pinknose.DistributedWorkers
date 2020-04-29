using Pinknose.DistributedWorkers.Messages;
using Pinknose.Utilities;
using RabbitMQ.Client;
using Serilog;
using System;
using System.Collections.Generic;
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

        private ReusableThreadSafeTimer serverHeartbeatTimer = new ReusableThreadSafeTimer()
        {
            Interval = HeartbeatTime,
            AutoReset = false
        };

        public MessageClient(string clientName, string systemName, string rabbitMqServerHostName, string userName, string password) :
            base(clientName, systemName, rabbitMqServerHostName, userName, password)
        {
            DedicatedQueue = MessageQueue.CreateExchangeBoundMessageQueue<ReadableMessageQueue>(Channel, clientName, BroadcastExchangeName, DedicatedQueueName);

            DedicatedQueue.MessageReceived += DedicatedQueue_MessageReceived;
            DedicatedQueue.BeginFullConsume(true);

            // Announce client to server
            ClientAnnounceMessage message = new ClientAnnounceMessage(false);

            MessageBase response;

            while (this.WriteRpcCall(message, out response, 10000) == RpcCallResult.Timeout)
            {
                Log.Warning("Timeout trying to communicate with the server.");
            }

            switch (((ClientAnnounceResponseMessage)response).Response)
            {
                case AnnounceResponse.Accepted:
                    break;

                case AnnounceResponse.Rejected:
                    throw new Exception($"Client rejected by server with the following message: {response.MessageText}.");
                    break;
            }

            serverHeartbeatTimer.Elapsed += ServerHeartbeatTimer_Elapsed;
            serverHeartbeatTimer.Start();
        }

        private void ServerHeartbeatTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Log.Warning("Server timeout.");
        }

        protected sealed override void SendHeartbeat()
        {
            this.WriteRpcCallNoWait(new HeartbeatMessage(false));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="replyToQueue"></param>
        /// <returns>Returns Correlation ID for the message.</returns>
        public RpcCallResult WriteRpcCall(MessageBase message, out MessageBase response, int waitTime, bool broadcastResults = false)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

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

                Channel.BasicPublish(
                    exchange: "",
                    routingKey: RpcQueue.Name,
                    basicProperties: basicProperties,
                    message.Serialize());

                RpcCallResult result;

                if (rpcCallWaitHandle.WaitOne(waitTime))
                {
                    result = RpcCallResult.Success;
                    response = lastRpcMessageReceived;
                }
                else
                {
                    result = RpcCallResult.Timeout;
                    response = null;
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

            Channel.BasicPublish(
                exchange: "",
                routingKey: RpcQueue.Name,
                basicProperties: basicProperties,
                message.Serialize());
        }

        public ReadableMessageQueue DedicatedQueue { get; private set; }

        private void DedicatedQueue_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            if (e.Message.BasicProperties.CorrelationId == rpcCallCorrelationId)
            {
                lastRpcMessageReceived = e.Message;
                rpcCallWaitHandle.Set();
            }
            else if (e.Message.GetType() == typeof(ClientReannounceRequestMessage))
            {
                Log.Information("Server requested reannouncement of clients.");
                var message = new ClientAnnounceMessage(false);
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
