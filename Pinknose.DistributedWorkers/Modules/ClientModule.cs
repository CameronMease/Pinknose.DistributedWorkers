using EasyNetQ.Management.Client.Model;
using Pinknose.DistributedWorkers.Clients;
using Pinknose.DistributedWorkers.MessageTags;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pinknose.DistributedWorkers.Modules
{
    public abstract class ClientModule
    {
        public event EventHandler<MessageClientRegisteredEventArgs> MessageClientRegistered;

        public ClientModule(MessageTagCollection tags) : this(tags.ToArray())
        {
        }

        public ClientModule(params MessageTag[] tags)
        {
            //TODO: Add subscription to tags
        }

        internal void RegisterClient(MessageClientBase client)
        {
            MessageClient = client;

            MessageClientRegistered?.Invoke(this, new MessageClientRegisteredEventArgs(client));
        }

        protected MessageClientBase MessageClient { get; private set; }
    }
}
