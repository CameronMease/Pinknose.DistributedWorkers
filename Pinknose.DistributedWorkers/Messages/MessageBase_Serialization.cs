using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Runtime;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Pinknose.DistributedWorkers.Messages
{
    public partial class MessageBase
    {
        [NonSerialized]
        private byte[] receivedRawMessage = null;

        [NonSerialized]
        private byte[] receivedSignature = null;

        internal void ReverifySignature(ECDsaCng senderDsa)
        {
            if (senderDsa.VerifyData(receivedRawMessage, receivedSignature))
            {
                this.SignatureVerificationStatus = SignatureVerificationStatus.SignatureValid;
            }
            else
            {
                this.SignatureVerificationStatus = SignatureVerificationStatus.SignatureNotValid;
            }
        }


        /// <summary>
        /// Serializes the message into a binary format.
        /// </summary>
        /// <returns></returns>
        internal byte[] Serialize(MessageClientBase messageClient)
        {
            var formatter = new BinaryFormatter();
            byte[] data;

            using (MemoryStream stream = new MemoryStream())
            {
                formatter.Serialize(stream, this);
                stream.Seek(0, SeekOrigin.Begin);
                data = new byte[stream.Length];
                if (stream.Read(data, 0, data.Length) != data.Length)
                {
                    throw new Exception();
                }
            }

            byte[] signature = messageClient.SignData(data);

            var header = new MessageSerializationHeader(IsEncrypted, signature);

            byte[] output = new byte[header.Length + data.Length];
            header.CopyTo(output, 0);            
            data.CopyTo(output, header.Length);

            return output;
        }

        private static MessageBase Deserialize(byte[] body, string exchange, ulong deliveryTag, bool redelivered, string routingKey, IBasicProperties basicProperties, MessageClientBase messageClient)
        {
            var header = MessageSerializationHeader.Deserialize(body);
                        

            byte[] messageContent = body[header.Length..];

            var formatter = new BinaryFormatter();

            using (MemoryStream memoryStream = new MemoryStream())
            {
                memoryStream.Write(messageContent, 0, messageContent.Length);
                memoryStream.Seek(0, SeekOrigin.Begin);

                var message = (MessageBase)formatter.Deserialize(memoryStream);
                message.Exchange = exchange;
                message.DeliveryTag = deliveryTag;
                message.Redelivered = redelivered;
                message.RoutingKey = routingKey;
                message.BasicProperties = basicProperties;

                message.SignatureVerificationStatus =  messageClient.ValidateSignature(messageContent, header.Signature, message.ClientName);
                message.receivedSignature = header.Signature;
                message.receivedRawMessage = messageContent;

                return message;
            }
        }

        internal static MessageBase Deserialize(BasicDeliverEventArgs e, MessageClientBase messageClient)
        {
            if (e == null)
            {
                throw new ArgumentNullException(nameof(e));
            }

            return Deserialize(
                e.Body.ToArray(),
                e.Exchange,
                e.DeliveryTag,
                e.Redelivered,
                e.RoutingKey,
                e.BasicProperties,
                messageClient);
        }
        internal static MessageBase Deserialize(BasicGetResult result, MessageClientBase messageClient)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            return Deserialize(
                result.Body.ToArray(),
                result.Exchange,
                result.DeliveryTag,
                result.Redelivered,
                result.RoutingKey,
                result.BasicProperties,
                messageClient);
        }

    }
}
