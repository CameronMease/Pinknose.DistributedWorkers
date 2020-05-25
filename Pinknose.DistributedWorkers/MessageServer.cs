using Pinknose.DistributedWorkers.Messages;
using Pinknose.DistributedWorkers.MessageTags;
using Pinknose.Utilities;
using RabbitMQ.Client.Exceptions;
using Serilog;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Pinknose.DistributedWorkers
{
    public sealed class MessageServer : MessageClientBase<ReadableMessageQueue>
    {
        public event EventHandler<MessageReceivedEventArgs> RpcMessageReceived;

        public MessageServer(MessageClientInfo serverInfo, string rabbitMqServerHostName, string userName, string password, params MessageClientInfo[] clientInfos) :
            base(serverInfo, rabbitMqServerHostName, userName, password)
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

            byte[] key = new byte[SharedKeyByteSize];
            Rand.GetBytes(key);
            PreviousSystemSharedKey = CurrentSystemSharedKey;
            CurrentSystemSharedKey = key;
        }


        private void ServerQueue_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            if (e.MessageEnevelope.Message.GetType() == typeof(ClientAnnounceMessage))
            {
                if (!clients.ContainsKey(e.MessageEnevelope.SenderName))
                {
                    ReusableThreadSafeTimer timeoutTimer = new ReusableThreadSafeTimer()
                    {
                        AutoReset = false,
                        Interval = HeartbeatTime * 2,
                        Enabled = true,
                        Tag = e.MessageEnevelope.SenderName
                    };

                    timeoutTimer.Elapsed += TimeoutTimer_Elapsed;

                    var clientInfo = new MessageClientInfo(this.SystemName, e.MessageEnevelope.SenderName, ((ClientAnnounceMessage)e.MessageEnevelope.Message).PublicKey, CngKeyBlobFormat.EccFullPublicBlob);

                    //e.MessageEnevelope.ReverifySignature(clientInfo.Dsa);

                    string message;
                    if (e.MessageEnevelope.SignatureVerificationStatus == SignatureVerificationStatus.SignatureValid)
                    {
                        //TODO: This is where we need to determine what to do with a new unstrusted signature

                        Log.Information($"Client '{e.MessageEnevelope.SenderName}' announced and accepted.");
                        message = "";
                        clients.Add(e.MessageEnevelope.SenderName, (DateTime.Now, DateTime.Now, "duhh", timeoutTimer));

                        //PublicKeystore[e.MessageEnevelope.SenderName] = clientInfo;

                        //clientInfo.GenerateSymmetricKey(this.CngKey);

                        ((MessageQueue)sender).RespondToMessage(
                        e.MessageEnevelope,
                        new ClientAnnounceResponseMessage(AnnounceResponse.Accepted, CurrentSystemSharedKey.ToArray(), this.PublicKeystore)
                        {
                            MessageText = message
                        },
                        EncryptionOption.EncryptWithPrivateKey);


                        var keyMessage = new PublicKeyUpdate(PublicKeystore[e.MessageEnevelope.SenderName]);
                        //TODO: How to determine when to encyrpt
                        this.BroadcastToAllClients(keyMessage, EncryptionOption.None);

                        // Send the system AES key to the new client
                        //var aesKeyMessage = new SystemSharedKeyUpdate(CurrentSystemSharedKey.ToArray());
                        //this.WriteToClientNoWait(clientInfo, aesKeyMessage, EncryptionOption.EncryptWithPrivateKey);

                    }
                    else
                    {
                        Log.Information($"Client '{e.MessageEnevelope.SenderName}' announced and rejected for invalid signature.");
                        message = "Signature was invalid.";

                        ((MessageQueue)sender).RespondToMessage(
                        e.MessageEnevelope,
                        new ClientAnnounceResponseMessage(AnnounceResponse.Rejected, Array.Empty<byte>(), this.PublicKeystore)
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
                    if (!clients.ContainsKey(e.MessageEnevelope.SenderName))
                    {
                        Log.Warning($"Heartbeat from unknown client '{e.MessageEnevelope.SenderName}'.");

                        foreach (var clientInfo in clients.Values)
                        {
                            clientInfo.TimeoutTimer?.Dispose();
                        }

                        clients.Clear();
                        
                        //TODO: How to determine when to encyrpt
                        BroadcastToAllClients(new ClientReannounceRequestMessage(false), EncryptionOption.None);
                    }
                    else
                    {
                        var clientInfo = clients[e.MessageEnevelope.SenderName];
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

            // TODO: Re-enabled BroacastToAllClients(message);
        }



        public void Connect(TimeSpan timeout)
        {
            SetupConnections(timeout, new MessageTagCollection());

            ServerQueue.MessageReceived += ServerQueue_MessageReceived;
            ServerQueue.AsynchronousException += (sender, eventArgs) => this.FireAsynchronousExceptionEvent(sender, eventArgs);

            //WorkQueue.Purge();

            ServerQueue.BeginFullConsume(true);
            //LogQueue.BeginFullConsume(true);
        }

        public override void Disconnect()
        {
            throw new NotImplementedException();
        }

        Dictionary<string, (DateTime AnnouncementTime, DateTime LastSeen, string PrivateQueueName, ReusableThreadSafeTimer TimeoutTimer)> clients =
            new Dictionary<string, (DateTime AnnouncementTime, DateTime LastSeen, string PrivateQueueName, ReusableThreadSafeTimer TimeoutTimer)>();

    }
}
