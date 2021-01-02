///////////////////////////////////////////////////////////////////////////////////
// MIT License
//
// Copyright(c) 2020 Cameron Mease
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
///////////////////////////////////////////////////////////////////////////////////

using Pinknose.DistributedWorkers.Clients;
using Pinknose.DistributedWorkers.Exceptions;
using Pinknose.DistributedWorkers.MessageTags;
using Pinknose.Utilities;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace Pinknose.DistributedWorkers.Messages
{
    /// <summary>
    /// Represents a Message Envelope (message + metadata needed for options and addressing).
    /// </summary>
    public class MessageEnvelope
    {
        #region Fields

        //private static readonly int isClientAnnounceMask = BitVector32.CreateMask();
        //private static readonly int isClientAnnounceResponseMask = BitVector32.CreateMask(isClientAnnounceMask);

        private static readonly BitVector32.Section signatureLengthSection = BitVector32.CreateSection(256);
        private static readonly BitVector32.Section ivLengthSection = BitVector32.CreateSection(16, signatureLengthSection);
        private static readonly BitVector32.Section encryptionOptionSection = BitVector32.CreateSection(7, ivLengthSection);
        private static readonly BitVector32.Section senderNameLengthSection = BitVector32.CreateSection(255, encryptionOptionSection);
        private static readonly BitVector32.Section sharedKeyIdSection = BitVector32.CreateSection(255, senderNameLengthSection);

        private EncryptionOption _encryptionOption;
        private MessageClientBase _messageClient = null;

        #endregion Fields

        #region Constructors

        private MessageEnvelope()
        {
        }

        #endregion Constructors

        #region Properties

        public IBasicProperties BasicProperties { get; private set; } = null;
        public ulong DeliveryTag { get; private set; }
        public string Exchange { get; private set; }
        public MessageBase Message { get; private set; } = null;

        public string RecipientIdentityHash { get; private set; }
        public bool Redelivered { get; private set; }
        public PublicationAddress ReplyToAddres { get; private set; }
        public string RoutingKey { get; private set; }

        public string SenderIdentityHash { get; private set; }
        public SignatureVerificationStatus SignatureVerificationStatus { get; private set; }

        public DateTime Timestamp { get; private set; }

        public MessageTag[] Tags { get; private set; }

        #endregion Properties

        #region Methods

        public static MessageEnvelope WrapMessage(MessageBase message, string recipientIdentityHash, MessageClientBase messageClient, EncryptionOption encryptionOption)
        {
            if (string.IsNullOrEmpty(recipientIdentityHash) && encryptionOption == EncryptionOption.EncryptWithPrivateKey)
            {
                throw new ArgumentOutOfRangeException(nameof(recipientIdentityHash), $"{nameof(recipientIdentityHash)} cannot be empty if encryption is set to private key.");
            }

            if (messageClient == null)
            {
                throw new ArgumentNullException(nameof(messageClient));
            }

            var wrapper = new MessageEnvelope();
            wrapper.Message = message;
            wrapper.SenderIdentityHash = messageClient.Identity.IdentityHash;
            wrapper._messageClient = messageClient;
            wrapper._encryptionOption = encryptionOption;
            wrapper.RecipientIdentityHash = recipientIdentityHash;
            wrapper.Timestamp = DateTime.Now;

            return wrapper;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// Message Wrapper Format:
        ///
        /// [ Info Bits ][ Info Numbers ][ Timestamp ][ Sender Name ][ IV - Optional][ Serialized Message (encryption optional)][ Signature ]
        /// [                                               Signature based on this data                                       ]
        ///
        /// </remarks>

        public byte[] Serialize()
        {
            int index = 0;
            byte[] senderBytes = Encoding.UTF8.GetBytes(SenderIdentityHash);
            byte[] iv = Array.Empty<byte>();
            int sharedKeyId = 0;

            var messageBytes = Message.Serialize();

            // Add encryption, if required;
            if (this._encryptionOption == EncryptionOption.EncryptWithPrivateKey)
            {
                (messageBytes, iv) = this._messageClient.EncryptDataWithClientKey(messageBytes, this.RecipientIdentityHash);
            }
            else if (this._encryptionOption == EncryptionOption.EncryptWithSystemSharedKey)
            {
                (messageBytes, iv, sharedKeyId) = this._messageClient.EncryptDataWithSystemSharedKey(messageBytes, this.Timestamp);
            }

            int totalBufferSize = 8 + sizeof(long) + senderBytes.Length + messageBytes.Length + iv.Length + _messageClient.SignatureLength;

            byte[] bytes = new byte[totalBufferSize];

            // Load bits;
            var bits = new BitVector32(0);
            //bits[isClientAnnounceMask] = this.Message.GetType() == typeof(ClientAnnounceMessage);
            //bits[isClientAnnounceResponseMask] = false; //this.Message.GetType() == typeof(ClientAnnounceResponseMessage);
            BitConverter.GetBytes(bits.Data).CopyTo(bytes, index);
            index += 4;

            // Load numbers
            var numbers = new BitVector32(0);
            numbers[signatureLengthSection] = _messageClient.SignatureLength;
            numbers[encryptionOptionSection] = (int)_encryptionOption;
            numbers[senderNameLengthSection] = SenderIdentityHash.Length;
            numbers[ivLengthSection] = iv.Length;
            numbers[sharedKeyIdSection] = sharedKeyId;
            BitConverter.GetBytes(numbers.Data).CopyTo(bytes, index);
            index += 4;

            // Copy fields
            BitConverter.GetBytes(this.Timestamp.Ticks).CopyTo(bytes, index);
            index += sizeof(long);
            senderBytes.CopyTo(bytes, index);
            index += senderBytes.Length;
            iv.CopyTo(bytes, index);
            index += iv.Length;
            messageBytes.CopyTo(bytes, index);
            index += messageBytes.Length;

            byte[] signature = _messageClient.SignData(bytes, 0, totalBufferSize - _messageClient.SignatureLength);

            if (signature.Length != _messageClient.SignatureLength)
            {
                // Made bad assumption of signature length!
                throw new NotImplementedException();
            }

            signature.CopyTo(bytes, totalBufferSize - signature.Length);

            return bytes;
        }

        internal static MessageEnvelope Deserialize(byte[] bytes, BasicDeliverEventArgs e, MessageClientBase messageClient)
        {
            if (e == null)
            {
                throw new ArgumentNullException(nameof(e));
            }

            var envelope = Deserialize(
                bytes,
                e.Exchange,
                e.DeliveryTag,
                e.Redelivered,
                e.RoutingKey,
                e.BasicProperties,
                messageClient);

            return envelope;
        }

        internal static MessageEnvelope Deserialize(byte[] bytes, BasicGetResult result, MessageClientBase messageClient)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            var envelope = Deserialize(
                bytes,
                result.Exchange,
                result.DeliveryTag,
                result.Redelivered,
                result.RoutingKey,
                result.BasicProperties,
                messageClient);

            return envelope;
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
            var envelope = new MessageEnvelope();

            try
            {
                var bits = new BitVector32(BitConverter.ToInt32(bytes, 0));
                //var isClientAnnounce = bits[isClientAnnounceMask];
                //var isClientAnnounceResponse = bits[isClientAnnounceResponseMask];

                var numbers = new BitVector32(BitConverter.ToInt32(bytes, 4));
                var signatureLength = numbers[signatureLengthSection];
                envelope._encryptionOption = (EncryptionOption)numbers[encryptionOptionSection];
                var senderIdentityLength = numbers[senderNameLengthSection];
                var ivLength = numbers[ivLengthSection];
                var sharedKeyId = numbers[sharedKeyIdSection];

                int index = 8;

                envelope.Timestamp = new DateTime(BitConverter.ToInt64(bytes, index));
                index += sizeof(long);

                envelope.SenderIdentityHash = Encoding.UTF8.GetString(bytes, index, senderIdentityLength);
                index += senderIdentityLength;

                //var iv = bytes[(index)..(index + ivLength)];
                var iv = bytes.RangeByLength(index, ivLength);
                index += ivLength;

                var signature = bytes.RangeFromEnd(signatureLength);

                if (signatureLength != messageClient.SignatureLength)
                {
                    throw new NotImplementedException("Signautre length is invalid.");
                }

                envelope.SignatureVerificationStatus = messageClient.ValidateSignature(bytes.RangeExcludeLast(signatureLength), signature, envelope.SenderIdentityHash);

                if (envelope.SignatureVerificationStatus != SignatureVerificationStatus.SignatureValid)
                {
                    //TODO: Add back: throw new IdentityException(envelope.SignatureVerificationStatus.ToString());
                }

                // Get the bytes representing the message
                //byte[] messageBytes = bytes[index..^signatureLength];
                byte[] messageBytes = bytes.RangeExcludeLast(index, signatureLength);

                // Decrypt the bytes if necessary
                if (envelope._encryptionOption == EncryptionOption.EncryptWithPrivateKey)
                {
                    messageBytes = messageClient.DecryptDataWithClientKey(messageBytes, envelope.SenderIdentityHash, iv);
                }
                else if (envelope._encryptionOption == EncryptionOption.EncryptWithSystemSharedKey)
                {
                    messageBytes = messageClient.DecryptDataWithSystemSharedKey(messageBytes, iv, envelope.Timestamp);
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

            if (basicProperties != null && basicProperties.Headers != null)
            {
                List<MessageTag> tags = new List<MessageTag>();

                foreach (var key in basicProperties.Headers.Keys)
                {
                    tags.Add(MessageTag.DemangleTag(key));
                }

                envelope.Tags = tags.ToArray();
            }

            return envelope;
        }

        #endregion Methods
    }
}