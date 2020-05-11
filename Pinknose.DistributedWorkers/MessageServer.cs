using Pinknose.DistributedWorkers.Messages;
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

        private Dictionary<string, ECDsaCng> clientDsa = new Dictionary<string, ECDsaCng>();

        public MessageServer(string serverName, string systemName, string rabbitMqServerHostName, CngKey key, string userName, string password, params MessageTagValue[] subscriptionTags) : base(serverName, systemName, rabbitMqServerHostName, key, userName, password, subscriptionTags)
        {
            ServerQueue.MessageReceived += ServerQueue_MessageReceived;
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

                    var key = CngKey.Import(((ClientAnnounceMessage)e.Message).PublicKey, CngKeyBlobFormat.EccFullPublicBlob);
                    var dsa = new ECDsaCng(key);

                    AnnounceResponse response;
                    string message;
                    if (SignatureIsValid(e.RawData, dsa))
                    {
                        response = AnnounceResponse.Accepted;
                        Log.Information($"Client '{e.Message.ClientName}' announced and accepted.");
                        message = "";
                        clients.Add(e.Message.ClientName, (DateTime.Now, DateTime.Now, "duhh", timeoutTimer));
                        clientDsa.Add(e.Message.ClientName, dsa);
                    }
                    else
                    {
                        response = AnnounceResponse.Rejected;
                        Log.Information($"Client '{e.Message.ClientName}' announced and rejected for invalid signature.");
                        message = "Signature was invalid.";
                    }

                    ((MessageQueue)sender).RespondToMessage(
                        e.Message,
                        new ClientAnnounceResponseMessage(response, this.CngKey, false)
                        {
                            MessageText = message
                        });
                }
                else
                {
                    ((MessageQueue)sender).RespondToMessage(e.Message,
                        new ClientAnnounceResponseMessage(AnnounceResponse.Rejected, this.CngKey, false) { MessageText = $"The client {e.Message.ClientName} already exists." });

                    Log.Warning($"Client '{e.Message.ClientName}' announced and rejected (name already exists).");
                }
            }
            else
            {
                // TODO: Check message signature
                var signatureIsValid = SignatureIsValid(e.RawData, clientDsa[e.Message.ClientName]);

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

                        BroacastToAllClients(new ClientReannounceRequestMessage(false));
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

        

        public void Start(bool purgeWorkQueue = false)
        {
            if (purgeWorkQueue)
            {
                WorkQueue.Purge();
            }

            ServerQueue.BeginFullConsume(true);
            LogQueue.BeginFullConsume(true);

        }

        Dictionary<string, (DateTime AnnouncementTime, DateTime LastSeen, string PrivateQueueName, ReusableThreadSafeTimer TimeoutTimer)> clients =
            new Dictionary<string, (DateTime AnnouncementTime, DateTime LastSeen, string PrivateQueueName, ReusableThreadSafeTimer TimeoutTimer)>();

    }
}
