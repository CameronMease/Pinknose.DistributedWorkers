﻿///////////////////////////////////////////////////////////////////////////////////
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
using Pinknose.DistributedWorkers.MessageQueues;
using Pinknose.DistributedWorkers.Messages;
using Pinknose.DistributedWorkers.MessageTags;
using Pinknose.Utilities;
using Serilog;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Pinknose.DistributedWorkers.Clients
{
    public sealed class MessageServer : MessageClientBase<ReadableMessageQueue>
    {
        public event EventHandler<MessageReceivedEventArgs> RpcMessageReceived;

        internal MessageServer(MessageClientIdentity serverInfo, string rabbitMqServerHostName, string userName, string password, bool autoDeleteQueuesOnClose, bool queuesAreDurable, params MessageClientIdentity[] clientInfos) :
            base(serverInfo, rabbitMqServerHostName, userName, password, autoDeleteQueuesOnClose, queuesAreDurable)
        {
            if (serverInfo is null)
            {
                throw new ArgumentNullException(nameof(serverInfo));
            }

            if (serverInfo.Name != NameHelper.GetServerName())
            {
                throw new Exception();
            }

            //PublicKeystore.Add(this.SystemName, this.ClientName, key);
            PublicKeystore.AddRange(clientInfos);

            //TODO: Add persistent key support?

            byte[] key = GetRandomBytes(SharedKeyByteSize);

            PublicKeystore.SystemSharedKeys[PublicKeystore.CurrentSharedKeyId] = key;
        }

        private void ServerQueue_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            if (e.MessageEnevelope.Message.GetType() == typeof(ClientAnnounceMessage))
            {
                if (!clients.ContainsKey(e.MessageEnevelope.SenderIdentityHash))
                {
                    var announceMessage = (ClientAnnounceMessage)e.MessageEnevelope.Message;

                    ReusableThreadSafeTimer timeoutTimer = null;
                    ReusableThreadSafeTimer heartbeatSendTimer = null;

                    if (announceMessage.HeartbeatInterval > 0)
                    {
                        timeoutTimer = new ReusableThreadSafeTimer()
                        {
                            AutoReset = false,
                            Interval = announceMessage.HeartbeatInterval * 2,
                            Enabled = true,
                            Tag = e.MessageEnevelope.SenderIdentityHash
                        };

                        timeoutTimer.Elapsed += TimeoutTimer_Elapsed;

                        heartbeatSendTimer = new ReusableThreadSafeTimer()
                        {
                            AutoReset = true,
                            Interval = announceMessage.HeartbeatInterval,
                            Enabled = true,
                            Tag = e.MessageEnevelope.SenderIdentityHash
                        };

                        heartbeatSendTimer.Elapsed += HeartbeatSendTimer_Elapsed;
                    }

                    var clientInfo = announceMessage.Payload;  // new MessageClientIdentity(this.SystemName, e.MessageEnevelope.SenderName, announceMessage.Payload, CngKeyBlobFormat.EccFullPublicBlob);

                    //e.MessageEnevelope.ReverifySignature(clientInfo.Dsa);

                    string message;
                    if (e.MessageEnevelope.SignatureVerificationStatus == SignatureVerificationStatus.SignatureValid)
                    {
                        //TODO: This is where we need to determine what to do with a new unstrusted signature

                        Log.Information($"Client '{e.MessageEnevelope.SenderIdentityHash}' announced and accepted.");
                        message = "";
                        clients.Add(e.MessageEnevelope.SenderIdentityHash, (DateTime.Now, DateTime.Now, "duhh", timeoutTimer, heartbeatSendTimer));

                        //PublicKeystore[e.MessageEnevelope.SenderName] = clientInfo;

                        //clientInfo.GenerateSymmetricKey(this.CngKey);

                        ((MessageQueue)sender).RespondToMessage(
                        e.MessageEnevelope,
                        new ClientAnnounceResponseMessage(AnnounceResponse.Accepted, PublicKeystore.CurrentSharedKeyId, PublicKeystore.SystemSharedKeys[PublicKeystore.CurrentSharedKeyId])
                        {
                            MessageText = message
                        },
                        EncryptionOption.EncryptWithPrivateKey);

                        //var keyMessage = new PublicKeyUpdate(PublicKeystore[e.MessageEnevelope.SenderName]);
                        //TODO: How to determine when to encyrpt
                        //this.BroadcastToAllClients(keyMessage, EncryptionOption.None);

                        // Send the system AES key to the new client
                        //var aesKeyMessage = new SystemSharedKeyUpdate(CurrentSystemSharedKey.ToArray());
                        //this.WriteToClientNoWait(clientInfo, aesKeyMessage, EncryptionOption.EncryptWithPrivateKey);
                    }
                    else
                    {
                        Log.Information($"Client '{e.MessageEnevelope.SenderIdentityHash}' announced and rejected for invalid signature.");
                        message = "Signature was invalid.";

                        ((MessageQueue)sender).RespondToMessage(
                        e.MessageEnevelope,
                        new ClientAnnounceResponseMessage(AnnounceResponse.Rejected, 0, Array.Empty<byte>())
                        {
                            MessageText = message
                        },
                        EncryptionOption.EncryptWithPrivateKey);
                    }
                }
                else
                {
                    //TODO: What to do here?
#if false
                    ((MessageQueue)sender).RespondToMessage(
                        e.MessageEnevelope,
                        new ClientAnnounceResponseMessage(AnnounceResponse.Rejected, PublicKeystore.ParentClientInfo.PublicKey, null, this.PublicKeystore) { MessageText = $"The client {e.MessageEnevelope.SenderName} already exists." },
                        EncryptionOption.None);

                    Log.Warning($"Client '{e.MessageEnevelope.SenderName}' announced and rejected (name already exists).");
#endif
                }
            }
            else
            {
                if (e.MessageEnevelope.SignatureVerificationStatus != SignatureVerificationStatus.SignatureValid)
                {
                    throw new Exception();
                }

                if (e.MessageEnevelope.GetType() == typeof(HeartbeatMessage))
                {
                    if (!clients.ContainsKey(e.MessageEnevelope.SenderIdentityHash))
                    {
                        Log.Warning($"Heartbeat from unknown client '{e.MessageEnevelope.SenderIdentityHash}'.");

                        foreach (var clientInfo in clients.Values)
                        {
                            clientInfo.TimeoutTimer?.Dispose();
                        }

                        clients.Clear();

                        //TODO: How to determine when to encyrpt
                        BroadcastToAllClients(new ClientReannounceRequestMessage(false), false);
                    }
                    else
                    {
                        var clientInfo = clients[e.MessageEnevelope.SenderIdentityHash];
                        clientInfo.TimeoutTimer.Restart();
                        clientInfo.LastSeen = DateTime.Now;
                    }
                }
                else
                {
                    RpcMessageReceived?.Invoke(this, e);
                }
            }
        }

        private void HeartbeatSendTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            this.WriteToClientNoWait((string)((ReusableThreadSafeTimer)sender).Tag, new HeartbeatMessage(), false);
        }

        private void TimeoutTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            string clientName = (string)((ReusableThreadSafeTimer)sender).Tag;

            if (clients.ContainsKey(clientName))
            {
                Log.Warning($"Client '{clientName}' timeout.");
                clients.Remove(clientName);
            }

            ((ReusableThreadSafeTimer)sender)?.Dispose();
        }

        protected sealed override void SendHeartbeat()
        {
            var message = new HeartbeatMessage();

            BroadcastToAllClients(message, false);
        }

        /// <summary>
        /// Connects the server's queues to the RabbitMQ server and begins processing of messages.
        /// </summary>
        /// <param name="timeout">The maximum amount of milliseconds to wait for a successful connection.  An exception occurs upon timeout.</param>
        public void Connect(int timeout) => Connect(TimeSpan.FromMilliseconds(timeout));

        /// <summary>
        /// Connects the server's queues to the RabbitMQ server and begins processing of messages.
        /// </summary>
        /// <param name="timeout">The maximum amount of time to wait for a successful connection.  An exception occurs upon timeout.</param>
        public void Connect(TimeSpan timeout)
        {
            SetupConnections(timeout, new MessageTagCollection());

            ServerQueue.MessageReceived += ServerQueue_MessageReceived;
            ServerQueue.AsynchronousException += ServerQueue_AsynchronousException;

            //WorkQueue.Purge();

            ServerQueue.BeginFullConsume(true);
            //LogQueue.BeginFullConsume(true);

            IsConnected = true;
        }

        private void ServerQueue_AsynchronousException(object sender, Exceptions.AsynchronousExceptionEventArgs e)
        {
            // Special case: If this is a client announcement and the identity is not valid, send a message back to the client
            if (e.Exception.GetType() == typeof(IdentityException))
            {
                /*
                ((MessageQueue)sender).RespondToMessage(
                        ((IdentityException)e.Exception).MessageEnevelope,
                        new ClientAnnounceResponseMessage(AnnounceResponse.Rejected, -1, null)
                        {
                            MessageText = "Identity is not valid."
                        }, 
                        EncryptionOption.None;
                */
            }

            // Fire asyncrhonouse exception event
            this.FireAsynchronousExceptionEvent(sender, e);
        }

        public override void Disconnect()
        {
            throw new NotImplementedException();
        }

        private SortedDictionary<string, (DateTime AnnouncementTime, DateTime LastSeen, string PrivateQueueName, ReusableThreadSafeTimer TimeoutTimer, ReusableThreadSafeTimer HeatbeatSendTiemr)> clients =
            new SortedDictionary<string, (DateTime AnnouncementTime, DateTime LastSeen, string PrivateQueueName, ReusableThreadSafeTimer TimeoutTimer, ReusableThreadSafeTimer HeatbeatSendTiemr)>();
    }
}