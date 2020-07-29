using Pinknose.DistributedWorkers.MessageTags;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pinknose.DistributedWorkers.XBee.Messages
{
    [Serializable]
    public class XBeeReceivedDataTag : MessageTagValue
    {
        public XBeeReceivedDataTag() : base("XBee", "ReceivedData")
        {
        }
    }
}
