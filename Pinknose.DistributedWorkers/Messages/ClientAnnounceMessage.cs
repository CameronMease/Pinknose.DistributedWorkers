using Pinknose.DistributedWorkers.MessageTags;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Pinknose.DistributedWorkers.Messages
{
    [Serializable]
    internal sealed class ClientAnnounceMessage : MessageBase
    {
        public override Guid MessageTypeGuid =>new Guid("B1C8E58E-DBEB-4D13-98F0-D73D4EE9A643");

        internal ClientAnnounceMessage(CngKey key) : base()
        {
            PublicKey = key.Export(CngKeyBlobFormat.EccFullPublicBlob);
        }

        public byte[] PublicKey { get; private set; }
    }
}
