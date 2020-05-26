using EasyNetQ.Management.Client.Model;
using Pinknose.DistributedWorkers.Clients;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Text.Unicode;

namespace Pinknose.DistributedWorkers.Messages
{
    public class MessageEnvelope
    {
        //TODO:  How to calculate this?
        private const int SignatureLengthConst = 132;

        private static readonly int isClientAnnounceMask = BitVector32.CreateMask();
        private static readonly int isClientAnnounceResponseMask = BitVector32.CreateMask(isClientAnnounceMask);

        private static readonly BitVector32.Section signatureLengthSection = BitVector32.CreateSection(256);
        private static readonly BitVector32.Section ivLengthSection = BitVector32.CreateSection(16, signatureLengthSection);
        private static readonly BitVector32.Section encryptionOptionSection = BitVector32.CreateSection(7, ivLengthSection);
        private static readonly BitVector32.Section senderNameLengthSection = BitVector32.CreateSection(255, encryptionOptionSection);
        private static readonly BitVector32.Section sharedKeyIdSection = BitVector32.CreateSection(255, senderNameLengthSection);

        public MessageBase Message { get; private set; } = null;

        public string SenderName { get; private set; }

        public string RecipientName { get; private set; }
        

        private MessageClientBase _messageClient = null;
        private EncryptionOption _encryptionOption;

        public static MessageEnvelope WrapMessage(MessageBase message, string recipientName, MessageClientBase messageClient, EncryptionOption encryptionOption)
        {
            if (string.IsNullOrEmpty(recipientName) && encryptionOption == EncryptionOption.EncryptWithPrivateKey)
            {
                throw new ArgumentOutOfRangeException(nameof(recipientName), $"{nameof(recipientName)} cannot be empty if encryption is set to private key.");
            }
            
            var wrapper = new MessageEnvelope();
            wrapper.Message = message;
            wrapper.SenderName = messageClient.ClientName;
            wrapper._messageClient = messageClient;
            wrapper._encryptionOption = encryptionOption;
            wrapper.RecipientName = recipientName;

            return wrapper;
        }


        private MessageEnvelope()
        {
        }



   
    

        public string Exchange { get; private set; }

        public ulong DeliveryTag { get; private set; }

        public PublicationAddress ReplyToAddres { get; private set; }

        public IBasicProperties BasicProperties { get; private set; } = null;

        public bool Redelivered { get; private set; }

        public string RoutingKey { get; private set; }

        public SignatureVerificationStatus SignatureVerificationStatus { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// Message Wrapper Format:
        /// 
        /// [ Info Bits ][ Info Numbers ][ Sender Name ][ IV - Optional][ Serialized Message (encryption optional)][ Signature ]
        /// [                                          Signature based on this data                               ]
        /// 
        /// </remarks>

        public byte[] Serialize()
        {
            byte[] senderBytes = Encoding.UTF8.GetBytes(SenderName);
            byte[] iv = Array.Empty<byte>();
            int sharedKeyId = 0;

            var messageBytes = Message.Serialize();

            // Add encryption, if required;
            if (this._encryptionOption == EncryptionOption.EncryptWithPrivateKey)
            {
                (messageBytes, iv) = this._messageClient.EncryptDataWithClientKey(messageBytes, this.RecipientName);
            }
            else if (this._encryptionOption == EncryptionOption.EncryptWithSystemSharedKey)
            {
                (messageBytes, iv, sharedKeyId) = this._messageClient.EncryptDataWithSystemSharedKey(messageBytes);
            }

            int totalBufferSize = 8 + senderBytes.Length + messageBytes.Length + iv.Length + SignatureLengthConst;

            byte[] bytes = new byte[totalBufferSize];

            // Load bits
            var bits = new BitVector32(0);
            bits[isClientAnnounceMask] = this.Message.GetType() == typeof(ClientAnnounceMessage);
            bits[isClientAnnounceResponseMask] = this.Message.GetType() == typeof(ClientAnnounceResponseMessage);
            BitConverter.GetBytes(bits.Data).CopyTo(bytes, 0);

            // Load numbers
            var numbers = new BitVector32(0);
            numbers[signatureLengthSection] = SignatureLengthConst;
            numbers[encryptionOptionSection] = (int)_encryptionOption;
            numbers[senderNameLengthSection] = SenderName.Length;
            numbers[ivLengthSection] = iv.Length;
            numbers[sharedKeyIdSection] = sharedKeyId;
            BitConverter.GetBytes(numbers.Data).CopyTo(bytes, 4);

            // Copy fields
            senderBytes.CopyTo(bytes, 8);
            iv.CopyTo(bytes, 8 + senderBytes.Length );
            messageBytes.CopyTo(bytes, 8 + senderBytes.Length + iv.Length);

            byte[] signature = _messageClient.SignData(bytes, 0, totalBufferSize - SignatureLengthConst);

            if (signature.Length != SignatureLengthConst)
            {
                // Made bad assumption of signature length!
                throw new NotImplementedException();
            }

            signature.CopyTo(bytes, totalBufferSize - signature.Length);

            return bytes;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        /// <remarks>
        /// Message Wrapper Format:
        /// 
        /// [ Info Bits ][ Info Numbers ][ Sender Name ][ IV - Optional][ Serialized Message (encryption optional)][ Signature ]
        /// 
        /// </remarks>
        private static MessageEnvelope Deserialize(byte[] bytes, MessageClientBase messageClient)
        {
            try
            {
                var envelope = new MessageEnvelope();

                var bits = new BitVector32(BitConverter.ToInt32(bytes, 0));
                var isClientAnnounce = bits[isClientAnnounceMask];
                var isClientAnnounceResponse = bits[isClientAnnounceResponseMask];

                var numbers = new BitVector32(BitConverter.ToInt32(bytes, 4));
                var signatureLength = numbers[signatureLengthSection];
                envelope._encryptionOption = (EncryptionOption)numbers[encryptionOptionSection];
                var senderNameLength = numbers[senderNameLengthSection];
                var ivLength = numbers[ivLengthSection];
                var sharedKeyId = numbers[sharedKeyIdSection];

                int index = 8;

                envelope.SenderName = Encoding.UTF8.GetString(bytes, index, senderNameLength);
                index += senderNameLength;

                var iv = bytes[(index)..(index + ivLength)];
                index += ivLength;


                var signature = bytes[^signatureLength..];

                if (signatureLength != SignatureLengthConst)
                {
                    throw new Exception();
                }

                envelope.SignatureVerificationStatus = messageClient.ValidateSignature(bytes[..^signatureLength], signature, envelope.SenderName);

                if (envelope.SignatureVerificationStatus != SignatureVerificationStatus.SignatureValid)
                {
                    throw new NotImplementedException();
                }
                

                // Get the bytes representing the message
                byte[] messageBytes = bytes[index..^signatureLength];

                // Decrypt the bytes if necessary
                if (envelope._encryptionOption == EncryptionOption.EncryptWithPrivateKey)
                {
                    messageBytes = messageClient.DecryptDataWithClientKey(messageBytes, envelope.SenderName, iv);
                }
                else if (envelope._encryptionOption == EncryptionOption.EncryptWithSystemSharedKey)
                {
                    messageBytes = messageClient.DecryptDataWithSystemSharedKey(messageBytes, iv);
                }

                // Deserialize the message
                envelope.Message = MessageBase.Deserialize(messageBytes);

                return envelope;
            }
            catch (Exception e)
            {
                 throw;
            }
        }

        private static MessageEnvelope Deserialize(byte[] bytes, string exchange, ulong deliveryTag, bool redelivered, string routingKey, IBasicProperties basicProperties, MessageClientBase messageClient)
        {
            var envelope = MessageEnvelope.Deserialize(bytes, messageClient);
            envelope.Exchange = exchange;
            envelope.DeliveryTag = deliveryTag;
            envelope.Redelivered = redelivered;
            envelope.RoutingKey = routingKey;
            envelope.BasicProperties = basicProperties;

            return envelope;
        }

        internal static MessageEnvelope Deserialize(byte[] bytes, BasicDeliverEventArgs e, MessageClientBase messageClient)
        {
            if (e == null)
            {
                throw new ArgumentNullException(nameof(e));
            }

            return Deserialize(
                bytes,
                e.Exchange,
                e.DeliveryTag,
                e.Redelivered,
                e.RoutingKey,
                e.BasicProperties,
                messageClient);
        }
        internal static MessageEnvelope Deserialize(byte[] bytes, BasicGetResult result, MessageClientBase messageClient)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            return Deserialize(
                bytes,
                result.Exchange,
                result.DeliveryTag,
                result.Redelivered,
                result.RoutingKey,
                result.BasicProperties,
                messageClient);
        }

    }
}
