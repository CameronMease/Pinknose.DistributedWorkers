using Newtonsoft.Json;
using Pinknose.DistributedWorkers.Clients;
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
        public MessageBase()
        {
        }

        public abstract Guid MessageTypeGuid { get; }

        public string MessageText { get; set; }

        //TODO: How to restrict access to set but not break serialization?
        //public string ClientName { get; internal set; }

        //public MessageTagCollection Tags { get; private set; } = new MessageTagCollection();

        //public bool IsEncrypted { get; private set; } = false;



        //public Dictionary<string, object> CustomProperties { get; private set; } = new Dictionary<string, object>();

        

        

        

        [field: NonSerializedAttribute()]
        public SignatureVerificationStatus SignatureVerificationStatus { get; private set; } = SignatureVerificationStatus.SignatureUnverified;
    }
}
