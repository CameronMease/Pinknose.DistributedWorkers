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

using Pinknose.DistributedWorkers.Extensions;
using Pinknose.DistributedWorkers.MessageQueues;
using Pinknose.DistributedWorkers.Messages;
using Pinknose.DistributedWorkers.MessageTags;
using Pinknose.Utilities;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Pinknose.DistributedWorkers.Clients
{
    public class MessageClient : MessageClientBase<MessageQueue>
    {
        #region Fields

        /// <summary>
        /// The hearbteat send/receive interval in milliseconds.
        /// </summary>
        public int HeartbeatInterval { get; private set; } = 1000;

        private bool clientServerHandshakeComplete = false;
        //private string serverName = "server";

        private ReusableThreadSafeTimer serverHeartbeatTimer = new ReusableThreadSafeTimer()
        {
            AutoReset = false
        };

        private ReusableThreadSafeTimer heartbeatSendTimer = new ReusableThreadSafeTimer()
        {
            AutoReset = true
        };

        #endregion Fields

        #region Constructors

        public MessageClient(MessageClientIdentity identity, MessageClientIdentity serverIdentity, string rabbitMqServerHostName, string userName, string password, bool autoDeleteQueuesOnClose, bool queuesAreDurable, int heartbeatInterval, params MessageClientIdentity[] clientInfos) :
             base(identity, rabbitMqServerHostName, userName, password, autoDeleteQueuesOnClose, queuesAreDurable)
        {
            this.PublicKeystore.Add(serverIdentity);
            this.PublicKeystore.AddRange(clientInfos);
            HeartbeatInterval = heartbeatInterval;
        }

#if false
        public MessageClient(MessageClientIdentity identity, MessageClientIdentity serverIdentity, string rabbitMqServerHostName, string userName, string password, bool autoDeleteQueuesOnClose, bool queuesAreDurable, int heartbeatInterval) :
            this(identity, serverIdentity, rabbitMqServerHostName, userName, password, autoDeleteQueuesOnClose, queuesAreDurable, heartArray.Empty<MessageClientIdentity>())
        {
        }
#endif 

#endregion Constructors

#region Properties

        private ReadableMessageQueue DedicatedQueue { get; set; }

#endregion Properties

#region Methods

        /// <summary>
        /// Connects the server's queues to the RabbitMQ server and begins processing of messages.
        /// </summary>
        /// <param name="timeout">The maximum amount of milliseconds to wait for a successful connection.  An exception occurs upon timeout.</param>
        /// <param name="subscriptionTags"></param>
        public void Connect(int timeout, params MessageTag[] subscriptionTags) => Connect(TimeSpan.FromMilliseconds(timeout), subscriptionTags);

        /// <summary>
        /// Connects the server's queues to the RabbitMQ server and begins processing of messages.
        /// </summary>
        /// <param name="timeout">The maximum amount of milliseconds to wait for a successful connection.  An exception occurs upon timeout.</param>
        /// <param name="subscriptionTags"></param>
        public void Connect(TimeSpan timeout, params MessageTag[] subscriptionTags) => Connect(timeout, new MessageTagCollection(subscriptionTags));

        /// <summary>
        /// Connects the server's queues to the RabbitMQ server and begins processing of messages.
        /// </summary>
        /// <param name="timeout">The maximum amount of time to wait for a successful connection.  An exception occurs upon timeout.</param>
        /// <param name="subscriptionTags"></param>
        public void Connect(TimeSpan timeout, MessageTagCollection subscriptionTags)
        {
            //TODO: Add subscription of broadcast tags

            SetupConnections(timeout, subscriptionTags);

            DedicatedQueue = MessageQueue.CreateExchangeBoundMessageQueue<ReadableMessageQueue>(this, Channel, ClientName, BroadcastExchangeName, DedicatedQueueName, this.QueuesAreDurable, this.AutoDeleteQueuesOnClose);
            DedicatedQueue.MessageReceived += DedicatedQueue_MessageReceived;
            DedicatedQueue.AsynchronousException += (sender, eventArgs) => FireAsynchronousExceptionEvent(this, eventArgs);

            DedicatedQueue.BeginFullConsume(true);

            // Announce client to server
            ClientAnnounceMessage message = new ClientAnnounceMessage(PublicKeystore.ParentClientInfo);

            //TODO: Encrypt this?
            var result = this.WriteToServer(message, (int)timeout.TotalMilliseconds, false).Result;

            //var result = this.WriteToServer(message, out response, out rawResponse, (int)timeout.TotalMilliseconds);

            if (result.CallResult == RpcCallResult.Timeout)
            {
                throw new ConnectionException("Timeout trying to communicate with the server.");
            }

            var resultMessage = (ClientAnnounceResponseMessage)result.ResponseMessageEnvelope.Message;

            switch (resultMessage.Response)
            {
                case AnnounceResponse.Accepted:
                    clientServerHandshakeComplete = true;
                    //serverName = result.ResponseMessageEnvelope.SenderName;
                    PublicKeystore.SystemSharedKeys[resultMessage.SystemSharedKeyId] = resultMessage.SystemSharedKey;
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
                //PublicKeystore.Merge(((ClientAnnounceResponseMessage)result.ResponseMessageEnvelope.Message).PublicKeystore);
                //TODO: Fix PublicKeystore[result.ResponseMessageEnvelope.SenderName].Iv = ((ClientAnnounceResponseMessage)result.ResponseMessageEnvelope.Message).Iv;

                if (HeartbeatInterval > 0)
                {
                    serverHeartbeatTimer.Interval = HeartbeatInterval * 2;
                    serverHeartbeatTimer.Elapsed += ServerHeartbeatTimer_Elapsed;
                    serverHeartbeatTimer.Start();

                    heartbeatSendTimer.Interval = HeartbeatInterval;
                    heartbeatSendTimer.Elapsed += HeartbeatSendTimer_Elapsed;
                    heartbeatSendTimer.Start();
                }
            }

            IsConnected = true;
        }

        private void HeartbeatSendTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            SendHeartbeat();
        }

        public override void Disconnect()
        {
            throw new NotImplementedException();
        }

        public async Task<RpcCallWaitInfo> WriteToServer(MessageBase message, int waitTime, bool encryptMessage)
        {
            return await WriteToClient(
                NameHelper.GetServerName(),
                message,
                waitTime,
                encryptMessage);
        }

        public void WriteToServerNoWait(MessageBase message, bool encryptMessage)
        {
            WriteToClientNoWait(
                NameHelper.GetServerName(),
                message,
                encryptMessage);
        }

        protected sealed override void SendHeartbeat()
        {
            this.WriteToClientNoWait(NameHelper.GetServerName(), new HeartbeatMessage(), false);
        }

        private void DedicatedQueue_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            //var signatureIsValid = clientServerHandshakeComplete && SignatureIsValid(e.RawData, PublicKeystore[e.Message.ClientName].Dsa);

            if (e.MessageEnevelope.BasicProperties.CorrelationId != null &&
                RpcCallWaitInfo.ContainsKey(e.MessageEnevelope.BasicProperties.CorrelationId))
            {
                var waitInfo = RpcCallWaitInfo[e.MessageEnevelope.BasicProperties.CorrelationId];
                waitInfo.ResponseMessageEnvelope = e.MessageEnevelope;
                //waitInfo.RawResponse = e.RawData;
                waitInfo.WaitHandle.Set();
            }
            else if (e.MessageEnevelope.SignatureVerificationStatus == SignatureVerificationStatus.SignatureValid)
            {
                if (e.MessageEnevelope.Message.GetType() == typeof(ClientReannounceRequestMessage))
                {
                    Log.Information("Server requested reannouncement of clients.");
                    var message = new ClientAnnounceMessage(PublicKeystore.ParentClientInfo);

                    //todo: Encrypt?
                    WriteToServerNoWait(message, false);
                }
                else if (e.MessageEnevelope.Message.GetType() == typeof(HeartbeatMessage))
                {
                    if (!serverHeartbeatTimer.Enabled)
                    {
                        Log.Information("Server heartbeat re-established.");
                    }

                    serverHeartbeatTimer.Restart();
                }
                else if (e.MessageEnevelope.Message.GetType() == typeof(PublicKeyUpdate))
                {
                    var tempMessage = (PublicKeyUpdate)e.MessageEnevelope.Message;
                    PublicKeystore.Add(tempMessage.ClientInfo);
                    //TODO: Re-enable Log.Verbose($"Received public key '{tempMessage.ClientInfo.ECKey.GetPublicKeyHash()}' for client '{tempMessage.ClientInfo.Name}'.");
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

        private void ServerHeartbeatTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Log.Warning("Server timeout.");
        }

#endregion Methods
    }
}