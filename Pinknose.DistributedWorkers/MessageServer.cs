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

        public MessageServer(string serverName, string systemName, string rabbitMqServerHostName, CngKey key, string userName, string password, params MessageTag[] subscriptionTags) : base(serverName, systemName, rabbitMqServerHostName, key, userName, password, subscriptionTags)
        {
            ServerQueue.MessageReceived += ServerQueue_MessageReceived;

            PublicKeystore.Add(this.ClientName, key);
        }

        private void ServerQueue_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            if (e.Message.GetType() == typeof(ClientAnnounceMessage))
            {
                if (!clients.ContainsKey(e.Message.ClientName))
                {
                    ReusableThreadSafeTimer timeoutTimer = new ReusableThreadSafeTimer()
                    {
                        AutoReset = false,
                        Interval = HeartbeatTime * 2,
                        Enabled = true,
                        Tag = e.Message.ClientName
                    };

                    timeoutTimer.Elapsed += TimeoutTimer_Elapsed;

                    var clientInfo = new MessageClientInfo(e.Message.ClientName, ((ClientAnnounceMessage)e.Message).PublicKey, CngKeyBlobFormat.EccFullPublicBlob);

                    e.Message.ReverifySignature(clientInfo.Dsa);

                    AnnounceResponse response;
                    string message;
                    if (e.Message.SignatureVerificationStatus == SignatureVerificationStatus.SignatureValid)
                    {
                        response = AnnounceResponse.Accepted;
                        Log.Information($"Client '{e.Message.ClientName}' announced and accepted.");
                        message = "";
                        clients.Add(e.Message.ClientName, (DateTime.Now, DateTime.Now, "duhh", timeoutTimer));

                        PublicKeystore[e.Message.ClientName] = clientInfo;

                        using ECDiffieHellmanCng ecdh = new ECDiffieHellmanCng(this.CngKey);
                        ecdh.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
                        ecdh.HashAlgorithm = CngAlgorithm.Sha256;
                        clientInfo.SymmetricKey =  ecdh.DeriveKeyMaterial(clientInfo.PublicKey);

                        var rand = new Random();
                        clientInfo.Iv = new byte[16];
                        rand.NextBytes(clientInfo.Iv);

                        var keyMessage = new PublicKeyUpdate(PublicKeystore[e.Message.ClientName]);
                        this.BroadcastToAllClients(keyMessage);
                    }
                    else
                    {
                        response = AnnounceResponse.Rejected;
                        Log.Information($"Client '{e.Message.ClientName}' announced and rejected for invalid signature.");
                        message = "Signature was invalid.";
                    }

                    ((MessageQueue)sender).RespondToMessage(
                        e.Message,
                        new ClientAnnounceResponseMessage(response, this.CngKey, clientInfo.Iv, this.PublicKeystore)
                        {
                            MessageText = message
                        });
                }
                else
                {
                    ((MessageQueue)sender).RespondToMessage(e.Message,
                        new ClientAnnounceResponseMessage(AnnounceResponse.Rejected, this.CngKey, null, this.PublicKeystore) { MessageText = $"The client {e.Message.ClientName} already exists." });

                    Log.Warning($"Client '{e.Message.ClientName}' announced and rejected (name already exists).");
                }
            }
            else
            {
                if (e.Message.SignatureVerificationStatus != SignatureVerificationStatus.SignatureValid)
                {
                    throw new Exception();
                }

                if (e.Message.GetType() == typeof(HeartbeatMessage))
                {
                    if (!clients.ContainsKey(e.Message.ClientName))
                    {
                        Log.Warning($"Heartbeat from unknown client '{e.Message.ClientName}'.");

                        foreach (var clientInfo in clients.Values)
                        {
                            clientInfo.TimeoutTimer?.Dispose();
                        }

                        clients.Clear();

                        BroadcastToAllClients(new ClientReannounceRequestMessage(false));
                    }
                    else
                    {
                        var clientInfo = clients[e.Message.ClientName];
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
            var message = new HeartbeatMessage(false);

            // TODO: Re-enabled BroacastToAllClients(message);
        }



        public override void Connect(TimeSpan timeout)
        {
            base.Connect(timeout);

            WorkQueue.Purge();

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
