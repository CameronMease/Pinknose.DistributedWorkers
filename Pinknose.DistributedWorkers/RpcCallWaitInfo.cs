using Pinknose.DistributedWorkers.Messages;
using Pinknose.DistributedWorkers.MessageTags;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Pinknose.DistributedWorkers
{
    public class RpcCallWaitInfo
    {
        public RpcCallResult CallResult { get; set; }

        internal EventWaitHandle WaitHandle { get; set;  }

        public MessageEnvelope ResponseMessageEnvelope { get; set; }
    }
}
