using Pinknose.DistributedWorkers.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace DistributedWorkersTestApp
{
    [Serializable]
    public class IntMessage : PayloadMessage<int>
    {
        public IntMessage(int payload) : base(payload, false, false, false)
        {
        }

        public override Guid MessageTypeGuid => new Guid("{2DD8CDA5-FC5F-41B7-BD01-7F994EC6D257}");
    }
}
