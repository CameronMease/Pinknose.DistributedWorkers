using Pinknose.DistributedWorkers.MessageTags;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Pinknose.DistributedWorkers.Messages
{
    internal enum AnnounceResponse
    {
        Accepted,
        Rejected
    }

    [Serializable]
    internal sealed class ClientAnnounceResponseMessage : MessageBase
    {
        internal ClientAnnounceResponseMessage(AnnounceResponse response, CngKey key, byte[] iv, PublicKeystore publicKeystore, params MessageTag[] tags) : base(false, tags)
        {
            Response = response;
            ServerPublicKey = key.Export(CngKeyBlobFormat.EccFullPublicBlob);
            PublicKeystore = publicKeystore;
        }

        public override Guid MessageTypeGuid => new Guid("6B6B9D9B-2B78-425B-91DE-7FCEFADD757C");

        public AnnounceResponse Response { get; private set; }

        public byte[] ServerPublicKey { get; private set; }

        // Initialization Vector for asymmetric encryption.
        public byte[] Iv { get; private set; }

        public PublicKeystore PublicKeystore { get; private set; }
    }
}
