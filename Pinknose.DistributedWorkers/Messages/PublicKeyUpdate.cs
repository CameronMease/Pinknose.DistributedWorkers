using Pinknose.DistributedWorkers.MessageTags;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Pinknose.DistributedWorkers.Messages
{
    [Serializable]
    public class PublicKeyUpdate : MessageBase
    {
        public PublicKeyUpdate(MessageClientInfo clientInfo, bool encryptMessage=false, params MessageTag[] tags) : base(encryptMessage, tags)
        {
            ClientInfo = clientInfo;
        }

        public override Guid MessageTypeGuid => new Guid("975E14B4-87B8-4B42-889F-A2E40A521BC6");

        public MessageClientInfo ClientInfo { get; private set; }
    }
}
