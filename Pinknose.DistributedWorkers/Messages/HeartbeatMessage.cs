using Pinknose.DistributedWorkers.MessageTags;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pinknose.DistributedWorkers.Messages
{
    [Serializable]
    public sealed class HeartbeatMessage : MessageBase
    {
        public HeartbeatMessage(bool encryptMessage, params MessageTag[] tags) : base(encryptMessage, tags)
        {

        }

        public override Guid MessageTypeGuid => new Guid("EAED4920-EA19-4F00-AC5B-7D2BCCD2AE5D");
    }
}
