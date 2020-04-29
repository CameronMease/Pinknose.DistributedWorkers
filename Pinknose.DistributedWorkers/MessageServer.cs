using Pinknose.DistributedWorkers.Messages;
using Pinknose.Utilities;
using RabbitMQ.Client.Exceptions;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pinknose.DistributedWorkers
{
    public sealed class MessageServer : MessageClientBase<ReadableMessageQueue>
    {
        public event EventHandler<MessageReceivedEventArgs> RpcMessageReceived;

        public MessageServer(string serverName, string systemName, string rabbitMqServerHostName, string userName, string password) : base(serverName, systemName, rabbitMqServerHostName, userName, password)
        {
            RpcQueue.MessageReceived += RpcQueue_MessageReceived;
        }

        private void RpcQueue_MessageReceived(object sender, MessageReceivedEventArgs e)
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

                    clients.Add(
                        e.Message.ClientName,
                        (DateTime.Now, DateTime.Now, "duhh", timeoutTimer));
                    ((MessageQueue)sender).RespondToMessage(e.Message, new ClientAnnounceResponseMessage(AnnounceResponse.Accepted, false));
                    Log.Information($"Client '{e.Message.ClientName}' announced and accepted.");
                }
                else
                {
                    ((MessageQueue)sender).RespondToMessage(e.Message,
                        new ClientAnnounceResponseMessage(AnnounceResponse.Rejected, false) { MessageText = $"The client {e.Message.ClientName} already exists." }); 

                    Log.Warning($"Client '{e.Message.ClientName}'  announced and rejected (name already exists).");
                }
            }
            else if (e.Message.GetType() == typeof(HeartbeatMessage))
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

            BroacastToAllClients(message);
        }

        public void BroacastToAllClients(MessageBase message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            try
            {
                Channel.BasicPublish(
                    exchange: BroadcastExchangeName,
                    routingKey: "",
                    mandatory: true,
                    basicProperties: null,
                    body: message.Serialize());
            }
            catch (AlreadyClosedException e)
            {
                Log.Warning(e, $"Tried to send data to the closed exchange '{BroadcastExchangeName}'.");
            }
        }

        public void Start(bool purgeWorkQueue = false)
        {
            if (purgeWorkQueue)
            {
                WorkQueue.Purge();
            }

            RpcQueue.BeginFullConsume(true);
            LogQueue.BeginFullConsume(true);

        }

        Dictionary<string, (DateTime AnnouncementTime, DateTime LastSeen, string PrivateQueueName, ReusableThreadSafeTimer TimeoutTimer)> clients =
            new Dictionary<string, (DateTime AnnouncementTime, DateTime LastSeen, string PrivateQueueName, ReusableThreadSafeTimer TimeoutTimer)>();

    }
}
