using Pinknose.DistributedWorkers.MessageTags;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Pinknose.DistributedWorkers.Messages
{
    /// <summary>
    /// A message that has a strongly-typed payload.  There are a variety of options for compression or serialization
    /// of the data.  This is good for payload types that cannot be binary serialized.
    /// </summary>
    /// <typeparam name="TPayload"></typeparam>
    [Serializable]
    public abstract class PayloadMessage<TPayload> : MessageBase 
    {
        public PayloadMessage(TPayload payload, bool encryptMessage, bool compressPayload, bool serializePayloadToJson, bool dataIsAlreadyCompressed = false, params MessageTag[] tags) : base(encryptMessage, tags)
        {
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

                PayloadTypeName = payload.GetType().FullName;
            }
        }
        
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
            }
        }
    }
}
