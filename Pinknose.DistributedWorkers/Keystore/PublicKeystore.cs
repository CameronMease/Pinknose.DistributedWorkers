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
    /// <summary>
    /// Stores the public keys of other clients.  Creates shared keys between the 
    /// parent of this keystore and any of the other known clients.
    /// </summary>
    //[Serializable]
    public sealed class PublicKeystore : IEnumerable<MessageClientIdentity>
    {
        #region Fields

        private Dictionary<string, MessageClientIdentity> dictionary = new Dictionary<string, MessageClientIdentity>();

        private SortedDictionary<string, byte[]> symmetricKeys = new SortedDictionary<string, byte[]>();

        #endregion Fields

        #region Constructors

        public PublicKeystore(MessageClientIdentity parentClientInfo)
        {
            ParentClientInfo = parentClientInfo;
        }

        #endregion Constructors

        #region Properties

        public int CurrentSharedKeyId { get; set; } = 0;

        public MessageClientIdentity ParentClientInfo { get; private set; }

        public SharedKeyCollection SystemSharedKeys { get; } = new SharedKeyCollection();

        public int Count => dictionary.Count;

        #endregion Properties

        #region Indexers

        public MessageClientIdentity this[string key]
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

#if false
        public void Add(string systemName, string clientName, byte[] publicKeyBlob, CngKeyBlobFormat format)
        {
            var key = CngKey.Import(publicKeyBlob, format);
            dictionary.Add(clientName, AddSymmetricKeyIfNotExist(new MessageClientIdentity(systemName, clientName, publicKeyBlob, format)));
        }

        public void Add(string systemName, string clientName, CngKey key)
        {
            dictionary.Add(clientName, AddSymmetricKeyIfNotExist(new MessageClientIdentity(systemName, clientName, key)));
        }
#endif

        public void Add(MessageClientIdentity clientInfo)
        {
            if (clientInfo is null)
            {
                throw new ArgumentNullException(nameof(clientInfo));
            }

            dictionary.Add(clientInfo.IdentityHash, AddSymmetricKeyIfNotExist(clientInfo));
        }

        public void AddRange(IEnumerable<MessageClientIdentity> clientInfos)
        {
            foreach (var clientInfo in clientInfos)
            {
                this.Add(clientInfo);
            }
        }

        public bool Contains(string clientIdentityHash)
        {
            return dictionary.ContainsKey(clientIdentityHash);
        }

        public byte[] GetSymmetricKey(string clientIdentityHash)
        {
            return symmetricKeys[clientIdentityHash];
        }

        public byte[] GetSymmetricKey(MessageClientIdentity clientInfo)
        {
            return symmetricKeys[clientInfo.IdentityHash];
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

        public bool Remove(string clientIdentityHash)
        {
            return dictionary.Remove(clientIdentityHash);
        }

        public bool Remove(KeyValuePair<string, MessageClientIdentity> item)
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(string key, out MessageClientIdentity value)
        {
            return dictionary.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return dictionary.Values.GetEnumerator();
        }

        IEnumerator<MessageClientIdentity> IEnumerable<MessageClientIdentity>.GetEnumerator()
        {
            return dictionary.Values.GetEnumerator();
        }

        private MessageClientIdentity AddSymmetricKeyIfNotExist(MessageClientIdentity clientInfo)
        {
            string hash = clientInfo.IdentityHash;

            if (!symmetricKeys.ContainsKey(clientInfo.IdentityHash))
            {
                symmetricKeys.Add(clientInfo.IdentityHash, this.ParentClientInfo.GenerateSymmetricKey(clientInfo));
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