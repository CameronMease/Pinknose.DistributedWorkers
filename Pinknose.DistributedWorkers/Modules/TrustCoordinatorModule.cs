using Pinknose.DistributedWorkers.Clients;
using Pinknose.DistributedWorkers.Keystore;
using Pinknose.DistributedWorkers.Messages;
using Pinknose.DistributedWorkers.MessageTags;
using Pinknose.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Timers;

namespace Pinknose.DistributedWorkers.Modules
{
    /// <summary>
    /// Manages the shared key for a trust zone.  Ensures each client in a trust zone has the current shared key.
    /// </summary>
    public sealed class TrustCoordinatorModule : ClientModule
    {
        /// <summary>
        /// The current shared key for the trust zone.  All encypted broadcast messages use this key.
        /// </summary>
        private TrustZoneSharedKey currentKey;

        private ReusableThreadSafeTimer keyUpdateTimer;

        public TrustCoordinatorModule(TimeSpan keyRotationTime, MessageTagCollection tags) : this(keyRotationTime, tags.ToArray())
        {
            // No implementation here.  Use the other constructor.
        }

        public TrustCoordinatorModule(TimeSpan keyRotationTime, params MessageTag[] tags) : base(tags)
        {
            KeyRotationTime = keyRotationTime;

            this.MessageClientRegistered += TrustCoordinatorModule_MessageClientRegistered;
            
            keyUpdateTimer = new ReusableThreadSafeTimer()
            {
                Interval = (KeyRotationTime - UpdateLeadTime).TotalMilliseconds,
                AutoReset = false
            };

            keyUpdateTimer.Elapsed += KeyUpdateTimer_Elapsed;
            keyUpdateTimer.Start();
        }

        /// <summary>
        /// When called, updates the trust zone's shared key.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void KeyUpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var newKey = CreateNewKey(currentKey.TrustZoneName, currentKey.ValidTo);

            var updateMessage = new TrustZoneSharedKeyUpdate(newKey);

            this.MessageClient.BroadcastToAllClients(updateMessage, true);

            keyUpdateTimer.Interval = (newKey.ValidTo - DateTime.Now - UpdateLeadTime).TotalMilliseconds;
            keyUpdateTimer.Start();

            Timer tempTimer = new Timer()
            {
                Interval = (newKey.ValidFrom - DateTime.Now).TotalMilliseconds,
                Enabled = true
            };

            tempTimer.Elapsed += (sender, e) =>
            {
                currentKey = newKey;
                tempTimer?.Dispose();
            };
        }

        private void TrustCoordinatorModule_MessageClientRegistered(object sender, MessageClientRegisteredEventArgs e)
        {
            e.MessageClient.MessageReceived += MessageClient_MessageReceived;
            // Create first key
            //TODO: How to pick validity.
            currentKey = CreateNewKey(e.MessageClient.SystemName, DateTime.Now);
            e.MessageClient.PublicKeystore.TrustZoneSharedKeys.Add(currentKey);
        }

        /// <summary>
        /// Handles a received message.  If the message is a Client Announce Message, it send the client the current shared key.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MessageClient_MessageReceived(object sender, MessageQueues.MessageReceivedEventArgs e)
        {
            if (e.MessageEnevelope.Message.GetType() == typeof(ClientAnnounceMessage))
            {
                //TODO: Check validity???
                this.MessageClient.RespondToMessage(
                        e.MessageEnevelope,
                        new TrustZoneSharedKeyUpdate(this.MessageClient.PublicKeystore.CurrentSharedKey),
                        EncryptionOption.EncryptWithPrivateKey); ;
            }
        }

        private TrustZoneSharedKey CreateNewKey(string trustZoneName, DateTime validFrom)
        {
            return new TrustZoneSharedKey(trustZoneName, validFrom, validFrom + KeyRotationTime);
        }

        public TimeSpan KeyRotationTime { get; private set; }

        // The amount of time before key expiry when a new key should be created and sent out.
        public TimeSpan UpdateLeadTime { get; set; } = TimeSpan.FromSeconds(30);
    }
}
