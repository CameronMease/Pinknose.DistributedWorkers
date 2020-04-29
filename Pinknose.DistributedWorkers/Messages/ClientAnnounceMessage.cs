using System;
using System.Collections.Generic;
using System.Text;

namespace Pinknose.DistributedWorkers.Messages
{
    [Serializable]
    internal sealed class ClientAnnounceMessage : MessageBase
    {
        public override Guid MessageTypeGuid =>new Guid("B1C8E58E-DBEB-4D13-98F0-D73D4EE9A643");

        internal ClientAnnounceMessage(bool encryptMessage) : base(encryptMessage)
        {
        }
    }
}
