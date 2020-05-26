using EasyNetQ.Management.Client.Model;
using Pinknose.DistributedWorkers.MessageTags;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Pinknose.DistributedWorkers.Messages
{
    [Serializable]
    public class SystemSharedKeyUpdate : MessageBase
    {
        public SystemSharedKeyUpdate(byte[] key) : base()
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            AesKey = key;
        }

        public override Guid MessageTypeGuid => new Guid("A3AB5084-18F5-4C7F-922A-FAC79142F605");

        public byte[] AesKey { get; private set; }

    }
}
