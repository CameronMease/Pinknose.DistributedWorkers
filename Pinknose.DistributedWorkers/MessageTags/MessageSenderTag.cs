using System;
using System.Collections.Generic;
using System.Text;

namespace Pinknose.DistributedWorkers.MessageTags
{
    public sealed class MessageSenderTag : MessageTagValue
    {
        public MessageSenderTag(string senderName) : base("MessageSender", senderName)
        {
        }
    }
}
