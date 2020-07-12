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
        public ClientModule(MessageTagCollection tags) : this(tags.ToArray())
        {

        }

        public ClientModule(params MessageTag[] tags)
        {

        }

        internal void RegisterClient(MessageClientBase client)
        {
            MessageClient = client;
        }

        protected MessageClientBase MessageClient { get; private set; }

    }
}
