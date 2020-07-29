using Pinknose.DistributedWorkers.Clients;
using Pinknose.DistributedWorkers.Messages;
using Pinknose.DistributedWorkers.MessageTags;
using Pinknose.DistributedWorkers.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pinknose.DistributedWorkers.Pushover
{
    public class PushoverModule : ClientModule
    {
        private PushoverClient.Pushover pushoverClient;
        private string pushoverUserKey;

        private Dictionary<Type, List<Func<MessageBase, string>>> transforms = new Dictionary<Type, List<Func<MessageBase, string>>>();

        public PushoverModule(string pushoverAppApiKey, string pushoverUserKey,  MessageTagCollection tags) : this(pushoverAppApiKey, pushoverAppApiKey, tags.ToArray())
        {
        }

        public PushoverModule(string pushoverAppApiKey, string pushoverUserKey, params MessageTag[] tags) : base(tags)
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

            this.MessageClientRegistered += (sender, e) => e.MessageClient.MessageReceived += MessageClient_MessageReceived;
        }

        private void MessageClient_MessageReceived(object sender, MessageQueues.MessageReceivedEventArgs e)
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
