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

using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Pinknose.DistributedWorkers.Clients;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Pinknose.DistributedWorkers.Messages
{
    public partial class MessageBase
    {
        [JsonIgnore]
        static readonly JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings()
        {
            Formatting = Formatting.None,
            TypeNameHandling = TypeNameHandling.Objects
        };


        [JsonIgnore]
        private byte[] receivedRawMessage = null;

        [JsonIgnore]
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
        internal byte[] Serialize()
        {
            string json = JsonConvert.SerializeObject(this, jsonSerializerSettings);
            return Encoding.UTF8.GetBytes(json);
#if false
            using (FileStream inputFile = new FileStream(@"C:\VeryLargeFile.bin", FileMode.Open, FileAccess.Read))
            using (CryptoStream base64Stream = new CryptoStream(inputFile, new ToBase64Transform(), CryptoStreamMode.Read))
            using (FileStream outputFile = new FileStream(@"C:\VeryLargeBase64File.txt", FileMode.CreateNew, FileAccess.Write))
            {
                await base64Stream.CopyToAsync(outputFile).ConfigureAwait(false);
            }

            using (MemoryStream ms = new MemoryStream())
            using (BsonDataWriter datawriter = new BsonDataWriter(ms))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(datawriter, this);
                return Convert.ToBase64String(ms.ToArray());
            }


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

            return data;
#endif
        }

#if false

        /// <summary>
        /// Serializes the message into a binary format.
        /// </summary>
        /// <returns></returns>
        internal byte[] Serialize(MessageClientBase messageClient, string clientName, EncryptionOption encryptionOption)
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

            byte[] iv = null;
            MessageSerializationWrapper header;

            if (encryptionOption == EncryptionOption.EncryptWithPrivateKey)
            {
                (data, iv) = messageClient.EncryptDataWithClientKey(data, clientName);
            }
            else if (encryptionOption == EncryptionOption.EncryptWithSystemSharedKey)
            {
                (data, iv) = messageClient.EncryptDataWithSystemSharedKey(data);
            }

            byte[] signature = messageClient.SignData(data);

            if (encryptionOption != EncryptionOption.None)
            {
                header = new MessageSerializationWrapper(signature, iv, encryptionOption);
            }
            else
            {
                header = new MessageSerializationWrapper(signature);
            }

            byte[] output = new byte[header.Length + data.Length];
            header.CopyTo(output, 0);
            data.CopyTo(output, header.Length);

            return output;
        }

#endif

        internal static MessageBase Deserialize(byte[] messageBytes)
        {
            string json = Encoding.UTF8.GetString(messageBytes);
            var message = (MessageBase)JsonConvert.DeserializeObject(json, jsonSerializerSettings);
            return message;

#if false
            //var header = MessageSerializationWrapper.Deserialize(body);

            //byte[] messageContent = body[header.Length..];

            var formatter = new BinaryFormatter();

            using (MemoryStream memoryStream = new MemoryStream())
            {
                memoryStream.Write(messageBytes, 0, messageBytes.Length);
                memoryStream.Seek(0, SeekOrigin.Begin);

                var message = (MessageBase)formatter.Deserialize(memoryStream);

                return message;
            }
#endif
        }

#if false
        internal static MessageBase Deserialize(MessageEnvelope wrapper, BasicDeliverEventArgs e, MessageClientBase messageClient)
        {
            if (e == null)
            {
                throw new ArgumentNullException(nameof(e));
            }

            return Deserialize(
                wrapper,
                e.Exchange,
                e.DeliveryTag,
                e.Redelivered,
                e.RoutingKey,
                e.BasicProperties,
                messageClient);
        }
        internal static MessageBase Deserialize(MessageEnvelope wrapper, BasicGetResult result, MessageClientBase messageClient)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            return Deserialize(
                wrapper,
                result.Exchange,
                result.DeliveryTag,
                result.Redelivered,
                result.RoutingKey,
                result.BasicProperties,
                messageClient);
        }
#endif
    }
}