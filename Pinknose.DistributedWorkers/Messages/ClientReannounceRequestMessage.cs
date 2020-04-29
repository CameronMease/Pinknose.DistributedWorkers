﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Pinknose.DistributedWorkers.Messages
{
    [Serializable]
    internal sealed class ClientReannounceRequestMessage : MessageBase
    {
        public ClientReannounceRequestMessage(bool encryptMessage) : base(encryptMessage)
        {

        }

        public override Guid MessageTypeGuid => new Guid("2494FCA6-8F4E-494B-AFA0-1A46FBE17B13");
    }
}
