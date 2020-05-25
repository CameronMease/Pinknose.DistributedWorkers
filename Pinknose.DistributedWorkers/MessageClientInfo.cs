﻿using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;

namespace Pinknose.DistributedWorkers
{
    [Serializable]
    public class MessageClientInfo : IDisposable, ISerializable
    {
        internal MessageClientInfo(string systemName, string clientName, CngKey publicKey)
        {
            SystemName = systemName;
            Name = clientName;
            PublicKey = publicKey;
            Dsa = new ECDsaCng(publicKey);
        }

        internal MessageClientInfo(string systemName, string clientName, byte[] publicKeyBytes, CngKeyBlobFormat format)
        {
            SystemName = systemName;
            Name = clientName;
            PublicKey = CngKey.Import(publicKeyBytes, format);
            Dsa = new ECDsaCng(PublicKey);
        }

        protected MessageClientInfo(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            SystemName = (string)info.GetValue(nameof(SystemName), typeof(string));
            Name = (string)info.GetValue(nameof(Name), typeof(string));
            PublicKey = CngKey.Import((byte[])info.GetValue(nameof(PublicKey), typeof(byte[])), CngKeyBlobFormat.EccFullPublicBlob);
            Dsa = new ECDsaCng(PublicKey);
        }

        public string SystemName { get; private set; }
        public string Name { get; private set; }
        public CngKey PublicKey { get; private set; }

        /// <summary>
        /// Symmetric key between the client and the holder of the MessageClientInfo.  Note: This field is not serialized.
        /// </summary>
        public byte[] SymmetricKey { get; private set; }

        public void GenerateSymmetricKey(CngKey localKey)
        {
            using ECDiffieHellmanCng ecdh = new ECDiffieHellmanCng(localKey);
            ecdh.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
            ecdh.HashAlgorithm = CngAlgorithm.Sha256;
            SymmetricKey = ecdh.DeriveKeyMaterial(PublicKey);
        }

        /// <summary>
        /// Initialization vector for crypto between the client and the holder of the MessageClientInfo.  Note: This field is not serialized.
        /// </summary>
        public byte[] Iv { get; set; } = null;

        public ECDsaCng Dsa { get; private set; }

        internal string DedicatedQueueName => NameHelper.GetDedicatedQueueName(SystemName, Name);

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            info.AddValue(nameof(SystemName), SystemName);
            info.AddValue(nameof(Name), Name);
            info.AddValue(nameof(PublicKey), PublicKey.Export(CngKeyBlobFormat.EccFullPublicBlob));
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    PublicKey?.Dispose();
                    Dsa?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~ClientInfo()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
