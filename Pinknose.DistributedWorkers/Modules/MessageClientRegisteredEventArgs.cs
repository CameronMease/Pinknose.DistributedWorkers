using Pinknose.DistributedWorkers.Clients;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pinknose.DistributedWorkers.Modules
{
    public class MessageClientRegisteredEventArgs : EventArgs
    {
        public MessageClientRegisteredEventArgs(MessageClientBase messageClient)
        {
            MessageClient = messageClient;
        }

        public MessageClientBase MessageClient { get; private set; }
    }
}
