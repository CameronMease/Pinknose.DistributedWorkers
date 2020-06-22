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
    public sealed class MessageClient : MessageClientBase<MessageQueue>
    {
        #region Fields

        private bool clientServerHandshakeComplete = false;
        //private string serverName = "server";

        private ReusableThreadSafeTimer serverHeartbeatTimer = new ReusableThreadSafeTimer()
        {
            Interval = HeartbeatTime,
            AutoReset = false
        };

        #endregion Fields

        #region Constructors

        internal MessageClient(MessageClientIdentity clientInfo, MessageClientIdentity serverInfo, string rabbitMqServerHostName, string userName, string password) :
            base(clientInfo, rabbitMqServerHostName, userName, password)
        {
            this.PublicKeystore.Add(serverInfo);
        }

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
            ClientAnnounceMessage message = new ClientAnnounceMessage(PublicKeystore.ParentClientInfo.ECKey);

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

                serverHeartbeatTimer.Elapsed += ServerHeartbeatTimer_Elapsed;
                serverHeartbeatTimer.Start();
            }

            IsConnected = true;
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
            //todo: reenable  this.WriteRpcCallNoWait(new HeartbeatMessage(false));
        }

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
                    var message = new ClientAnnounceMessage(PublicKeystore.ParentClientInfo.ECKey);

                    //todo: Encrypt?
                    WriteToServerNoWait(message, false);
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
                    Log.Verbose($"Received public key '{tempMessage.ClientInfo.ECKey.GetPublicKeyHash()}' for client '{tempMessage.ClientInfo.Name}'.");
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
            //TODO: Get timeouts working Log.Warning("Server timeout.");
        }

        #endregion Methods
    }
}