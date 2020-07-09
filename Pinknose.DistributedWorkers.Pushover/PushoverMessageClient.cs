using Pinknose.DistributedWorkers.Clients;
using Pinknose.DistributedWorkers.Messages;
using PushoverClient;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Transactions;

namespace Pinknose.DistributedWorkers.Pushover
{
    public class PushoverMessageClient : MessageClient
    {
        private PushoverClient.Pushover pushoverClient;
        private string pushoverUserKey;

        private Dictionary<Type, List<Func<MessageBase, string>>> transforms = new Dictionary<Type, List<Func<MessageBase, string>>>();

        public PushoverMessageClient(MessageClientIdentity identity, MessageClientIdentity serverIdentity, string rabbitMqServerHostName, string userName, string password, string pushoverAppApiKey, string pushoverUserKey, bool autoDeleteQueuesOnClose, bool queuesAreDurable, int heartbeatInterval, params MessageClientIdentity[] clientIdentities) :
            base(identity, serverIdentity, rabbitMqServerHostName, userName, password, autoDeleteQueuesOnClose, queuesAreDurable, heartbeatInterval, clientIdentities)
        {
            if (string.IsNullOrEmpty(pushoverAppApiKey))
            {
                throw new ArgumentException("A Pushover application API key must be supplied", nameof(pushoverAppApiKey));
            }

            if (string.IsNullOrEmpty(pushoverUserKey))
            {
                throw new ArgumentException("A Pushover user key must be supplied", nameof(pushoverUserKey));
            }

            pushoverClient = new PushoverClient.Pushover(pushoverAppApiKey);
            this.pushoverUserKey = pushoverUserKey;

            this.MessageReceived += PushoverMessageClient_MessageReceived;
        }

        private void PushoverMessageClient_MessageReceived(object sender, MessageQueues.MessageReceivedEventArgs e)
        {
            List<Func<MessageBase, string>> funcs;

            if (transforms.TryGetValue(e.MessageEnevelope.Message.GetType(), out funcs))
            {
                foreach (var func in funcs)
                {
                    string result = func(e.MessageEnevelope.Message);

                    if (!string.IsNullOrEmpty(result))
                    {
                        pushoverClient.Push("", result, pushoverUserKey);
                    }
                }
            }

            e.Response = MessageQueues.MessageResponse.Ack;
        }

        /// <summary>
        /// Adds a transform that will take an message of a particular type and formats it for sending to Pushover.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="transform"></param>
        public void AddTransform<TMessage>(Func<TMessage, string> transform) where TMessage : MessageBase
        {
            List<Func<MessageBase, string>> funcList;

            if (!transforms.ContainsKey(typeof(TMessage)))
            {
                transforms.Add(typeof(TMessage), new List<Func<MessageBase, string>>());
            }

            funcList = transforms[typeof(TMessage)];

            Func<MessageBase, string> tempTransform = (arg) => transform((TMessage)arg);

            funcList.Add(tempTransform);
        }

    }
}
