using Pinknose.DistributedWorkers.Messages;
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

        public MessageBase ResponseMessage { get; set; }
    }
}
