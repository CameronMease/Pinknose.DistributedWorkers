using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Pinknose.DistributedWorkers.Messages
{
    [Serializable]
    public abstract class PayloadMessage<TPayload> : MessageBase where TPayload:class
    {

        public PayloadMessage(TPayload payload, bool encryptMessage, bool compressPayload, bool serializePayloadToJson, bool dataIsAlreadyCompressed = false) : base(encryptMessage)
        {
            //MessageType = messageType;
            //PayloadSerializationMode = payloadSerializationMode;

            if (payload != null)
            {
                if ((serializePayloadToJson && compressPayload) & !dataIsAlreadyCompressed)
                {
                    PayloadInternal = SerializationHelpers.SerializeToJsonAndGZip(payload);
                }
                else if (serializePayloadToJson)
                {
                    PayloadInternal = SerializationHelpers.SerializeToJson(payload);
                }
                else if (compressPayload & !dataIsAlreadyCompressed)
                {
                    PayloadInternal = SerializationHelpers.GZip((byte[])(object)payload);
                }
                else
                {
                    PayloadInternal = payload;
                }

                PayloadIsCompressed = compressPayload | dataIsAlreadyCompressed;
                PayloadIsJsonSerialized = serializePayloadToJson;
                KeepPayloadCompressed = dataIsAlreadyCompressed;

                /*

                switch (payloadSerializationMode)
                {
                    

                    case SerializationMode.Binary:
                        PayloadInternal = payload;
                        break;

                    case SerializationMode.EncryptedString:
                        using (AesCng aes = new AesCng())
                        {
                            aes.IV = iv;
                            aes.Key = key;

                            using (var transform = aes.CreateEncryptor())
                            {
                                byte[] bytes = UTF8Encoding.UTF8.GetBytes((string)(object)payload);
                                PayloadInternal = transform.TransformFinalBlock(bytes, 0, bytes.Length);
                            }
                        }
                        break;

                    case SerializationMode.GZippedBinary:
                        if (!dataIsAlreadyCompressed)
                        {
                            PayloadInternal = SerializationHelpers.GZip((byte[])(object)payload);
                        }
                        else
                        {
                            PayloadInternal = (byte[])(object)payload;
                        }
                        break;

                    default:
                        throw new NotImplementedException();
                }
                */
            }

            if (payload != null)
            {
                PayloadTypeName = payload.GetType().FullName;
            }
        }

        //public SerializationMode PayloadSerializationMode { get; private set; }

        

        public string PayloadTypeName { get; private set; }

        private object PayloadInternal { get; set; } = null;

        private bool PayloadIsCompressed { get; set; }

        private bool PayloadIsJsonSerialized { get; set; }

        private bool KeepPayloadCompressed { get; set; } = false;

        public TPayload Payload
        {
            get
            {
                if ((KeepPayloadCompressed || !PayloadIsCompressed) && !PayloadIsJsonSerialized)
                {
                    return (TPayload)PayloadInternal;
                }
                else if (!PayloadIsCompressed && PayloadIsJsonSerialized)
                {
                    return (TPayload)SerializationHelpers.DeserializeFromJson((string)PayloadInternal, typeof(TPayload));
                }
                else if (PayloadIsCompressed && !PayloadIsJsonSerialized)
                {
                    return (TPayload)(object)SerializationHelpers.GUnzip((byte[])PayloadInternal);
                }
                else if (PayloadIsCompressed && PayloadIsJsonSerialized)
                {
                    return (TPayload)SerializationHelpers.DeserializeFromGZippedJson((byte[])PayloadInternal, typeof(TPayload));
                }
                else
                {
                    throw new NotImplementedException();
                }
                    /*
                    using (AesCng aes = new AesCng())
                    {
                        aes.IV = iv;
                        aes.Key = key;

                        using (var transform = aes.CreateDecryptor())
                        {
                            byte[] bytes = transform.TransformFinalBlock((byte[])PayloadInternal, 0, ((byte[])PayloadInternal).Length);
                            string payload = UTF8Encoding.UTF8.GetString(bytes);
                            return payload;
                        }
                    }
                    */
            }
        }
    }
}
