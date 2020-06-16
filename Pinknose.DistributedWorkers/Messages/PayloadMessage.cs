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

using System;

namespace Pinknose.DistributedWorkers.Messages
{
    /// <summary>
    /// A message that has a strongly-typed payload.  There are a variety of options for compression or serialization
    /// of the payload.  This is good for payload types that cannot be binary serialized.
    /// </summary>
    /// <typeparam name="TPayload">The .NET type of the payload.</typeparam>
    [Serializable]
    public class PayloadMessage<TPayload> : MessageBase
    {
        #region Constructors

        public PayloadMessage(TPayload payload, bool compressPayload, bool serializePayloadToJson, bool dataIsAlreadyCompressed = false) : base()
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

        #endregion Constructors

        #region Properties

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

        public string PayloadTypeName { get; private set; }

        private bool KeepPayloadCompressed { get; set; } = false;
        private object PayloadInternal { get; set; } = null;

        private bool PayloadIsCompressed { get; set; }

        private bool PayloadIsJsonSerialized { get; set; }

        #endregion Properties
    }
}