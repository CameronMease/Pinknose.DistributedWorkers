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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;

namespace Pinknose.DistributedWorkers.Keystore
{
    [Serializable]
    public sealed class PublicKeystore : IEnumerable<MessageClientInfo>
    {
        #region Fields

        private Dictionary<string, MessageClientInfo> dictionary = new Dictionary<string, MessageClientInfo>();

        private Dictionary<(CngKey PrivateKey, CngKey PublicKey), byte[]> symmetricKeys = new Dictionary<(CngKey PrivateKey, CngKey PublicKey), byte[]>();

        #endregion Fields

        #region Constructors

        public PublicKeystore(MessageClientInfo parentClientInfo)
        {
            ParentClientInfo = parentClientInfo;
        }

        #endregion Constructors

        #region Properties

        public int CurrentSharedKeyId { get; set; } = 0;

        public MessageClientInfo ParentClientInfo { get; private set; }

        public SharedKeyCollection SystemSharedKeys { get; } = new SharedKeyCollection();

        public int Count => dictionary.Count;

        #endregion Properties

        #region Indexers

        public MessageClientInfo this[string key]
        {
            get => AddSymmetricKeyIfNotExist(dictionary[key]);
            //set => throw new NotImplementedException();
        }

        #endregion Indexers

        /*
        protected PublicKeystore(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
        */

        #region Methods

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

        public byte[] GetSymmetricKey(string clientName)
        {
            return symmetricKeys[(ParentClientInfo.ECKey, this[clientName].ECKey)];
        }

        public byte[] GetSymmetricKey(MessageClientInfo clientInfo)
        {
            return symmetricKeys[(ParentClientInfo.ECKey, clientInfo.ECKey)];
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

        private static byte[] GenerateSymmetricKey(CngKey privateKey, CngKey publicKey)
        {
            using ECDiffieHellmanCng ecdh = new ECDiffieHellmanCng(privateKey);
            ecdh.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
            ecdh.HashAlgorithm = CngAlgorithm.Sha256;
            return ecdh.DeriveKeyMaterial(publicKey);
        }

        private MessageClientInfo AddSymmetricKeyIfNotExist(MessageClientInfo clientInfo)
        {
            var key = (ParentClientInfo.ECKey, clientInfo.ECKey);

            if (!symmetricKeys.ContainsKey(key))
            {
                symmetricKeys.Add(key, GenerateSymmetricKey(ParentClientInfo.ECKey, clientInfo.ECKey));
            }

            return clientInfo;
        }

        #endregion Methods

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