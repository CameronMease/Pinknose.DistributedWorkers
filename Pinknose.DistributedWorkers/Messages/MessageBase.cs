using Newtonsoft.Json;
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
    public abstract class MessageBase
    {

        // Temporary to create AES encryption keys
        private static byte[] iv = new byte[] { 71, 120, 112, 163, 182, 229, 14, 24, 175, 168, 92, 79, 86, 30, 154, 197 };
        private static byte[] key = new byte[] { 180, 214, 175, 230, 229, 198, 219, 236, 136, 69, 104, 206, 171, 64, 247, 0, 247, 106, 127, 6, 72, 133, 211, 252, 188, 16, 39, 231, 151, 168, 24, 135 };

        
        public abstract Guid MessageTypeGuid { get; }

        public string MessageText { get; set; }

        //TODO: How to restrict access to set but not break serialization?
        public string ClientName { get; internal set; }


        public bool IsEncrypted { get; private set; } = false;

        public MessageBase(bool encryptMessage)
        {
            IsEncrypted = encryptMessage;
        }

        //public Dictionary<string, object> CustomProperties { get; private set; } = new Dictionary<string, object>();

        /// <summary>
        /// Serializes the message into a binary format.
        /// </summary>
        /// <returns></returns>
        internal ReadOnlyMemory<Byte> Serialize()
        {
            var formatter = new BinaryFormatter();
            byte[] bytes;

            using (MemoryStream stream = new MemoryStream())
            {
                formatter.Serialize(stream, this);
                stream.Seek(0, SeekOrigin.Begin);
                bytes = new byte[stream.Length];
                if (stream.Read(bytes, 0, bytes.Length) != bytes.Length)
                {
                    throw new Exception();
                }
            }

            if (IsEncrypted)
            {
                using (AesCng aes = new AesCng())
                {
                    aes.IV = iv;
                    aes.Key = key;

                    using (var transform = aes.CreateEncryptor())
                    {
                        byte[] encryptedBytes = transform.TransformFinalBlock(bytes, 0, bytes.Length);
                        return encryptedBytes;
                    }
                }
            }
            else
            {
                return bytes;
            }
        }

        internal static MessageBase Deserialize(BasicDeliverEventArgs e)
        {
            if (e == null)
            {
                throw new ArgumentNullException(nameof(e));
            }

            /*
            if (e.Body == null)
            {
                throw new ArgumentNullException(nameof(e.Body));
            }
            */

            var formatter = new BinaryFormatter();

            using (MemoryStream memoryStream = new MemoryStream())
            {
                memoryStream.Write(e.Body.ToArray(), 0, e.Body.Length);
                memoryStream.Seek(0, SeekOrigin.Begin);

                var message = (MessageBase)formatter.Deserialize(memoryStream);
                message.Exchange = e.Exchange;
                message.DeliveryTag = e.DeliveryTag;
                message.Redelivered = e.Redelivered;
                message.RoutingKey = e.RoutingKey;
                message.BasicProperties = e.BasicProperties;

                return message;
            }
        }

        internal static MessageBase Deserialize(BasicGetResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            var formatter = new BinaryFormatter();

            using (MemoryStream memoryStream = new MemoryStream())
            {
                memoryStream.Write(result.Body.ToArray(), 0, result.Body.Length);
                memoryStream.Seek(0, SeekOrigin.Begin);

                var message = (MessageBase)formatter.Deserialize(memoryStream);
                message.Exchange = result.Exchange;
                message.DeliveryTag = result.DeliveryTag;
                message.Redelivered = result.Redelivered;
                message.RoutingKey = result.RoutingKey;
                message.BasicProperties = result.BasicProperties;

                return message;
            }
        }

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
    }
}
