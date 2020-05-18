using EasyNetQ.Management.Client.Model;
using Newtonsoft.Json.Serialization;
using Pinknose.DistributedWorkers.Extensions;
using Pinknose.DistributedWorkers.Messages;
using Pinknose.DistributedWorkers.MessageTags;
using Pinknose.Utilities;
using RabbitMQ.Client;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pinknose.DistributedWorkers
{
    public sealed class MessageClient : MessageClientBase<MessageQueue>
    {
        //private EventWaitHandle rpcCallWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
        //private object rpcLock = new object();

        private Dictionary<string, RpcCallWaitInfo> rpcCallWaitInfo = new Dictionary<string, RpcCallWaitInfo>();

        //private string rpcCallCorrelationId;
        //private MessageBase lastRpcMessageReceived;
        //private byte[] lastRpcRawDataReceived;

        private bool clientServerHandshakeComplete = false;
        private string serverName = "server";


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
        }

        public override void Connect(TimeSpan timeout)
        {
            DedicatedQueue.BeginFullConsume(true);

            // Announce client to server
            ClientAnnounceMessage message = new ClientAnnounceMessage(CngKey, false);

            var result = this.WriteToServer(message, (int)timeout.TotalMilliseconds, false).Result;

            //var result = this.WriteToServer(message, out response, out rawResponse, (int)timeout.TotalMilliseconds);
            
            if (result.CallResult == RpcCallResult.Timeout)
            {
                throw new ConnectionException("Timeout trying to communicate with the server.");
            }

            switch (((ClientAnnounceResponseMessage)result.ResponseMessage).Response)
            {
                case AnnounceResponse.Accepted:
                    clientServerHandshakeComplete = true;
                    serverName = ((ClientAnnounceResponseMessage)result.ResponseMessage).ClientName;
                    break;

                case AnnounceResponse.Rejected:
                    throw new ConnectionException($"Client rejected by server with the following message: {result.ResponseMessage.MessageText}.");
            }

            PublicKeystore.Merge(((ClientAnnounceResponseMessage)result.ResponseMessage).PublicKeystore);
            PublicKeystore[result.ResponseMessage.ClientName].Iv = ((ClientAnnounceResponseMessage)result.ResponseMessage).Iv;

            result.ResponseMessage.ReverifySignature(PublicKeystore[result.ResponseMessage.ClientName].Dsa);

            if (result.ResponseMessage.SignatureVerificationStatus != SignatureVerificationStatus.SignatureValid)
            {
                throw new Exception("Bad server key.");
            }
            else
            {
                serverHeartbeatTimer.Elapsed += ServerHeartbeatTimer_Elapsed;
                serverHeartbeatTimer.Start();
            }

            base.Connect(timeout);

            IsConnected = true; 
        }

        public override void Disconnect()
        {
            throw new NotImplementedException();
        }

        private void ServerHeartbeatTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Log.Warning("Server timeout.");
        }

        protected sealed override void SendHeartbeat()
        {
            //todo: reenable  this.WriteRpcCallNoWait(new HeartbeatMessage(false));
        }

        public async Task<RpcCallWaitInfo> WriteToServer(MessageBase message, int waitTime, bool broadcastResults=false)
        {
            return await WriteToClient(
                ServerQueueName,  
                PublicKeystore.ContainsKey(serverName) ? PublicKeystore[serverName].Dsa : null,
                message, 
                waitTime, 
                broadcastResults).ConfigureAwait(false);
        }

        public async Task<RpcCallWaitInfo> WriteToClient(MessageClientInfo clientInfo, MessageBase message, int waitTime, bool broadcastResults = false)
        {
            if (clientInfo == null)
            {
                throw new ArgumentNullException(nameof(clientInfo));
            }

            return await WriteToClient(clientInfo.DedicatedQueueName, clientInfo.Dsa, message, waitTime, broadcastResults).ConfigureAwait(false);
        }

        private async Task<RpcCallWaitInfo> WriteToClient(string queueName, ECDsaCng clientDsa, MessageBase message, int waitTime, bool broadcastResults=false)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var formattedMessage = ConfigureRpcMessageForSend(message, broadcastResults);

            Channel.BasicPublish(
                exchange: "",
                routingKey: queueName,
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
                    if (response.ResponseMessage.SignatureVerificationStatus == SignatureVerificationStatus.SignatureValid)
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

        public void WriteToServerNoWait(MessageBase message, bool broadcastResults = false)
        {
            WriteToClientNoWait(ServerQueueName, PublicKeystore[serverName].Dsa, message, broadcastResults);
        }

        public void WriteToClientNoWait(MessageClientInfo clientInfo, MessageBase message, bool broadcastResults = false)
        {
            if (clientInfo == null)
            {
                throw new ArgumentNullException(nameof(clientInfo));
            }

            WriteToClientNoWait(clientInfo.DedicatedQueueName, clientInfo.Dsa, message, broadcastResults);
        }

        private void WriteToClientNoWait(string queueName, ECDsaCng publicDsa, MessageBase message, bool broadcastResults)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var formattedMessage = ConfigureRpcMessageForSend(message, broadcastResults);

            Channel.BasicPublish(
                exchange: "",
                routingKey: queueName,
                basicProperties: formattedMessage.BasicProperties,
                formattedMessage.SignedMessage);
        }

        private (byte[] SignedMessage, IBasicProperties BasicProperties) ConfigureRpcMessageForSend(MessageBase message, bool broadcastResults)
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

            byte[] signedMessage = message.Serialize(this);

            return (signedMessage, basicProperties);
        }

        private ReadableMessageQueue DedicatedQueue { get; set; }

        private void DedicatedQueue_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            //var signatureIsValid = clientServerHandshakeComplete && SignatureIsValid(e.RawData, PublicKeystore[e.Message.ClientName].Dsa);

            if (e.Message.BasicProperties.CorrelationId != null &&
                rpcCallWaitInfo.ContainsKey(e.Message.BasicProperties.CorrelationId))
            { 
                var waitInfo = rpcCallWaitInfo[e.Message.BasicProperties.CorrelationId];
                waitInfo.ResponseMessage = e.Message;
                //waitInfo.RawResponse = e.RawData;
                waitInfo.WaitHandle.Set();           
            }
            else if (e.Message.SignatureVerificationStatus == SignatureVerificationStatus.SignatureValid)
            {
                if (e.Message.GetType() == typeof(ClientReannounceRequestMessage))
                {
                    Log.Information("Server requested reannouncement of clients.");
                    var message = new ClientAnnounceMessage(CngKey, false);
                    WriteToServerNoWait(message);
                }
                else if (e.Message.GetType() == typeof(HeartbeatMessage))
                {
                    if (!serverHeartbeatTimer.Enabled)
                    {
                        Log.Information("Server heartbeat re-established.");
                    }

                    serverHeartbeatTimer.Restart();
                }
                else if (e.Message.GetType() == typeof(PublicKeyUpdate))
                {
                    var tempMessage = (PublicKeyUpdate)e.Message;
                    PublicKeystore[tempMessage.ClientInfo.Name] = tempMessage.ClientInfo;
                    Log.Verbose($"Received public key '{tempMessage.ClientInfo.PublicKey.GetPublicKeyHash()}' for client '{tempMessage.ClientInfo.Name}'.");
                }
                else
                {
                    FireMessageReceivedEvent(e);
                }
            }
            else if (clientServerHandshakeComplete)
            {
                throw new Exception();
            }
        }        
    }
}
