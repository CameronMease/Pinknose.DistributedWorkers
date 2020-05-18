using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;

namespace Pinknose.DistributedWorkers
{
    [Serializable]
    public class PublicKeystore : Dictionary<string, MessageClientInfo>
    {
        public PublicKeystore() : base()
        {

        }

        protected PublicKeystore(SerializationInfo info, StreamingContext context) : base(info, context)
        {

        }

        /*
        internal PublicKeystore(SerializationInfo info, StreamingContext context)
        {
            foreach (var entry in info)
            {
                var key = CngKey.Import((byte[])entry.Value, CngKeyBlobFormat.EccFullPublicBlob);
                this.Add(entry.Name, new MessageClientInfo(entry.Name, (byte[])entry.Value, CngKeyBlobFormat.EccFullPublicBlob));
            }

        }
        */

        public void Add(string clientName, byte[] publicKeyBlob, CngKeyBlobFormat format)
        {
            var key = CngKey.Import(publicKeyBlob, format);
            this.Add(clientName, new MessageClientInfo(clientName, publicKeyBlob, format));
        }

        public void Add(string clientName, CngKey key)
        {
            this.Add(clientName, new MessageClientInfo(clientName, key));
        }

        public void Add(MessageClientInfo clientInfo)
        {
            this.Add(clientInfo.Name, clientInfo);
        }

        public void Merge(PublicKeystore keystore)
        {
            foreach (string key in keystore.Keys)
            {
                if (!this.Keys.Contains(key))
                {
                    this.Add(key, keystore[key]);
                }
            }
        }

        /*
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            foreach (var clientName in Keys)
            {
                info.AddValue(clientName, this[clientName].Key.Export(CngKeyBlobFormat.EccFullPublicBlob));
            }
        }
        */
    }
}
