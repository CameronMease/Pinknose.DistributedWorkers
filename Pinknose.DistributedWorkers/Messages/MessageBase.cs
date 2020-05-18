using Newtonsoft.Json;
using Pinknose.DistributedWorkers.MessageTags;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Pinknose.DistributedWorkers.Messages
{
    /// <summary>
    /// A message which is passed between client and server.
    /// </summary>
    [Serializable]
    public abstract partial class MessageBase
    {

        // Temporary to create AES encryption keys
        private static byte[] iv = new byte[] { 71, 120, 112, 163, 182, 229, 14, 24, 175, 168, 92, 79, 86, 30, 154, 197 };
        private static byte[] key = new byte[] { 180, 214, 175, 230, 229, 198, 219, 236, 136, 69, 104, 206, 171, 64, 247, 0, 247, 106, 127, 6, 72, 133, 211, 252, 188, 16, 39, 231, 151, 168, 24, 135 };
                
        public MessageBase(bool encryptMessage, params MessageTag[] tags)
        {
            IsEncrypted = encryptMessage;
            Tags.AddRange(tags);
        }

        public abstract Guid MessageTypeGuid { get; }

        public string MessageText { get; set; }

        //TODO: How to restrict access to set but not break serialization?
        public string ClientName { get; internal set; }

        public MessageTagCollection Tags { get; private set; } = new MessageTagCollection();

        public bool IsEncrypted { get; private set; } = false;



        //public Dictionary<string, object> CustomProperties { get; private set; } = new Dictionary<string, object>();

        

        

        [field: NonSerializedAttribute()]
        public string Exchange { get; private set; }

        [field: NonSerializedAttribute()]
        public ulong DeliveryTag { get; private set; }

        [field: NonSerializedAttribute()]
        public PublicationAddress ReplyToAddres { get; private set; }

        [field: NonSerializedAttribute()]
        public IBasicProperties BasicProperties { get; private set; } = null;

        [field: NonSerializedAttribute()]
        public bool Redelivered { get; private set; }

        [field: NonSerializedAttribute()]
        public string RoutingKey { get; private set; }

        [field: NonSerializedAttribute()]
        public SignatureVerificationStatus SignatureVerificationStatus { get; private set; } = SignatureVerificationStatus.SignatureUnverified;
    }
}
