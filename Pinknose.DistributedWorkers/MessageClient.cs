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
        private bool clientServerHandshakeComplete = false;
        //private string serverName = "server";


        private ReusableThreadSafeTimer serverHeartbeatTimer = new ReusableThreadSafeTimer()
        {
            Interval = HeartbeatTime,
            AutoReset = false
        };


        public MessageClient(MessageClientInfo clientInfo, MessageClientInfo serverInfo, string rabbitMqServerHostName, string userName, string password) :
            base(clientInfo, rabbitMqServerHostName, userName, password)
        {


            this.PublicKeystore.Add(serverInfo);
        }

        public void Connect(TimeSpan timeout, params MessageTag[] subscriptionTags) => Connect(timeout, new MessageTagCollection(subscriptionTags));

        public void Connect(TimeSpan timeout, MessageTagCollection subscriptionTags)
        {
            SetupConnections(timeout, subscriptionTags);

            DedicatedQueue = MessageQueue.CreateExchangeBoundMessageQueue<ReadableMessageQueue>(this, Channel, ClientName, BroadcastExchangeName, DedicatedQueueName);
            DedicatedQueue.MessageReceived += DedicatedQueue_MessageReceived;
            DedicatedQueue.AsynchronousException += (sender, eventArgs) =>FireAsynchronousExceptionEvent(this, eventArgs);

            DedicatedQueue.BeginFullConsume(true);

            // Announce client to server
            ClientAnnounceMessage message = new ClientAnnounceMessage(PublicKeystore.ParentClientInfo.PublicKey);

            var result = this.WriteToServer(message, (int)timeout.TotalMilliseconds, EncryptionOption.None).Result;

            //var result = this.WriteToServer(message, out response, out rawResponse, (int)timeout.TotalMilliseconds);
            
            if (result.CallResult == RpcCallResult.Timeout)
            {
                throw new ConnectionException("Timeout trying to communicate with the server.");
            }

            switch (((ClientAnnounceResponseMessage)result.ResponseMessageEnvelope.Message).Response)
            {
                case AnnounceResponse.Accepted:
                    clientServerHandshakeComplete = true;
                    //serverName = result.ResponseMessageEnvelope.SenderName;
                    CurrentSystemSharedKey = ((ClientAnnounceResponseMessage)result.ResponseMessageEnvelope.Message).SystemSharedKey;
                    break;

                case AnnounceResponse.Rejected:
                    throw new ConnectionException($"Client rejected by server with the following message: {result.ResponseMessageEnvelope.Message.MessageText}.");
            }

            //result.ResponseMessageEnvelope.ReverifySignature(PublicKeystore[result.ResponseMessageEnvelope.SenderName].Dsa);

            //TODO: This is where we need to determine what to do with a new unstrusted signature

            if (result.ResponseMessageEnvelope.SignatureVerificationStatus != SignatureVerificationStatus.SignatureValid &&
                result.ResponseMessageEnvelope.SignatureVerificationStatus != SignatureVerificationStatus.SignatureValidButUntrusted)
            {
                throw new Exception("Bad server key.");
            }
            else
            {
                PublicKeystore.Merge(((ClientAnnounceResponseMessage)result.ResponseMessageEnvelope.Message).PublicKeystore);
                //TODO: Fix PublicKeystore[result.ResponseMessageEnvelope.SenderName].Iv = ((ClientAnnounceResponseMessage)result.ResponseMessageEnvelope.Message).Iv;

                serverHeartbeatTimer.Elapsed += ServerHeartbeatTimer_Elapsed;
                serverHeartbeatTimer.Start();
            }



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

        public async Task<RpcCallWaitInfo> WriteToServer(MessageBase message, int waitTime, EncryptionOption encryptionOptions)
        {
            if (encryptionOptions == EncryptionOption.EncryptWithSystemSharedKey)
            {
                throw new ArgumentOutOfRangeException(nameof(encryptionOptions));
            }

            return await WriteToClient(
                NameHelper.GetServerName(),
                message,
                waitTime,
                encryptionOptions);
        }



        public void WriteToServerNoWait(MessageBase message, EncryptionOption encryptionOptions)
        {
            WriteToClientNoWait(
                NameHelper.GetServerName(),
                message,
                encryptionOptions);
        }

        

        

        private ReadableMessageQueue DedicatedQueue { get; set; }

        private void DedicatedQueue_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            //var signatureIsValid = clientServerHandshakeComplete && SignatureIsValid(e.RawData, PublicKeystore[e.Message.ClientName].Dsa);

            Log.Verbose(e.MessageEnevelope.GetType().ToString());

            if (e.MessageEnevelope.BasicProperties.CorrelationId != null &&
                rpcCallWaitInfo.ContainsKey(e.MessageEnevelope.BasicProperties.CorrelationId))
            { 
                var waitInfo = rpcCallWaitInfo[e.MessageEnevelope.BasicProperties.CorrelationId];
                waitInfo.ResponseMessageEnvelope = e.MessageEnevelope;
                //waitInfo.RawResponse = e.RawData;
                waitInfo.WaitHandle.Set();           
            }
            else if (e.MessageEnevelope.SignatureVerificationStatus == SignatureVerificationStatus.SignatureValid)
            {
                if (e.MessageEnevelope.GetType() == typeof(ClientReannounceRequestMessage))
                {
                    Log.Information("Server requested reannouncement of clients.");
                    var message = new ClientAnnounceMessage(PublicKeystore.ParentClientInfo.PublicKey);
                    WriteToServerNoWait(message, EncryptionOption.None);
                }
                else if (e.MessageEnevelope.GetType() == typeof(HeartbeatMessage))
                {
                    if (!serverHeartbeatTimer.Enabled)
                    {
                        Log.Information("Server heartbeat re-established.");
                    }

                    serverHeartbeatTimer.Restart();
                }
                else if (e.MessageEnevelope.GetType() == typeof(PublicKeyUpdate))
                {
                    var tempMessage = (PublicKeyUpdate)e.MessageEnevelope.Message;
                    PublicKeystore.Add(tempMessage.ClientInfo);
                    Log.Verbose($"Received public key '{tempMessage.ClientInfo.PublicKey.GetPublicKeyHash()}' for client '{tempMessage.ClientInfo.Name}'.");
                }
                else if (e.MessageEnevelope.GetType() == typeof(SystemSharedKeyUpdate))
                {
                    throw new NotImplementedException();
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
