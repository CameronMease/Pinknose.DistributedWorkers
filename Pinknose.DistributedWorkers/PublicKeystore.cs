using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;

namespace Pinknose.DistributedWorkers
{
    [Serializable]
    public sealed class PublicKeystore : IEnumerable<MessageClientInfo>
    {
        private Dictionary<string, MessageClientInfo> dictionary = new Dictionary<string, MessageClientInfo>();

        public PublicKeystore(MessageClientInfo parentClientInfo)
        {
            ParentClientInfo = parentClientInfo;
        }

        public MessageClientInfo this[string key] 
        {
            get => AddSymmetricKeyIfNotExist(dictionary[key]);
            //set => throw new NotImplementedException();
        }

        /*
        protected PublicKeystore(SerializationInfo info, StreamingContext context) : base(info, context)
        {

        }
        */

        private MessageClientInfo AddSymmetricKeyIfNotExist(MessageClientInfo clientInfo)
        {
            if (clientInfo.SymmetricKey == null)
            {
                clientInfo.GenerateSymmetricKey(this.ParentClientInfo.PublicKey);
            }

            return clientInfo;
        }

        public MessageClientInfo ParentClientInfo { get; private set; }

        public int Count => dictionary.Count;

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

        public void Add(string systemName, string clientName, byte[] publicKeyBlob, CngKeyBlobFormat format)
        {
            var key = CngKey.Import(publicKeyBlob, format);
            dictionary.Add(clientName, AddSymmetricKeyIfNotExist(new MessageClientInfo(systemName, clientName, publicKeyBlob, format)));
        }

        public void Add(string systemName, string clientName, CngKey key)
        {
            dictionary.Add(clientName, AddSymmetricKeyIfNotExist(new MessageClientInfo(systemName, clientName, key)));
        }

        public void Add(MessageClientInfo clientInfo)
        {
            if (clientInfo is null)
            {
                throw new ArgumentNullException(nameof(clientInfo));
            }

            dictionary.Add(clientInfo.Name, AddSymmetricKeyIfNotExist(clientInfo));
        }

        public void AddRange(IEnumerable<MessageClientInfo> clientInfos)
        {
            foreach (var clientInfo in clientInfos)
            {
                this.Add(clientInfo);
            }
        }

        public bool Contains(string clientName)
        {
            return dictionary.ContainsKey(clientName);
        }

        public void Merge(PublicKeystore keystore)
        {
            foreach (string key in dictionary.Keys)
            {
                if (!dictionary.Keys.Contains(key))
                {
                    dictionary.Add(key, AddSymmetricKeyIfNotExist(keystore[key]));
                }
            }
        }

        public bool Remove(string clientName)
        {
            return dictionary.Remove(clientName);
        }

        public bool Remove(KeyValuePair<string, MessageClientInfo> item)
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out MessageClientInfo value)
        {
            return dictionary.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return dictionary.Values.GetEnumerator();
        }

        IEnumerator<MessageClientInfo> IEnumerable<MessageClientInfo>.GetEnumerator()
        {
            return dictionary.Values.GetEnumerator();
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
