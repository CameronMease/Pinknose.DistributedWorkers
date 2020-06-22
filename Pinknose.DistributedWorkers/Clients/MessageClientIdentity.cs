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
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text.Json;

namespace Pinknose.DistributedWorkers.Clients
{
    [Serializable]
    public class MessageClientIdentity : IDisposable, ISerializable
    {
        private const int SaltSize = 8;
        private const int KeyDerivationIterations = 1000000;

        #region Fields

        private bool disposedValue = false;

        #endregion Fields

        #region Constructors

        internal MessageClientIdentity(string systemName, string clientName, CngKey publicKey)
        {
            SystemName = systemName;
            Name = clientName;
            ECKey = publicKey;
            Dsa = new ECDsaCng(publicKey);
        }

        internal MessageClientIdentity(string systemName, string clientName, byte[] publicKeyBytes, CngKeyBlobFormat format)
        {
            SystemName = systemName;
            Name = clientName;
            ECKey = CngKey.Import(publicKeyBytes, format);
            Dsa = new ECDsaCng(ECKey);
        }

        protected MessageClientIdentity(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            SystemName = (string)info.GetValue(nameof(SystemName), typeof(string));
            Name = (string)info.GetValue(nameof(Name), typeof(string));
            ECKey = CngKey.Import((byte[])info.GetValue(nameof(ECKey), typeof(byte[])), CngKeyBlobFormat.EccFullPublicBlob);
            Dsa = new ECDsaCng(ECKey);
        }

        #endregion Constructors

        #region Properties

        public ECDsaCng Dsa { get; private set; }

        public CngKey ECKey { get; private set; }

        public string Name { get; private set; }

        public string SystemName { get; private set; }

        /// <summary>
        /// Symmetric key between the client and the holder of the MessageClientInfo.  Note: This field is not serialized.
        /// </summary>
        //public byte[] SymmetricKey { get; private set; }
        internal string DedicatedQueueName => NameHelper.GetDedicatedQueueName(SystemName, Name);

        #endregion Properties

        #region Methods

        public static MessageClientIdentity CreateClientInfo(string systemName, string clientName, ECDiffieHellmanCurve privateKeyCurve, bool allowExport=false)
        {
            var key = MessageClientBase.CreateClientKey(privateKeyCurve, allowExport);
            return new MessageClientIdentity(systemName, clientName, key);
        }

        public static MessageClientIdentity CreateServerInfo(string systemName, ECDiffieHellmanCurve privateKeyCurve, bool allowExport = false)
        {
            var key = MessageClientBase.CreateClientKey(privateKeyCurve, allowExport);
            return new MessageClientIdentity(systemName, NameHelper.GetServerName(), key);
        }

        public static MessageClientIdentity ImportFromFile(string keyFilePath, string password = "")
        {
            var json = File.ReadAllText(keyFilePath);

            return Import(json, password);
        }

        public static MessageClientIdentity Import(string json, string password="")
        {
            JObject jObject = JObject.Parse(json);

            byte[] ecKey;

            var isPrivateKey = jObject.ContainsKey(nameof(ECKey) + "-Private");

            if (isPrivateKey)
            {
                 ecKey = Convert.FromBase64String(jObject.Value<string>(nameof(ECKey) + "-Private"));
            }
            else
            {
                 ecKey = Convert.FromBase64String(jObject.Value<string>(nameof(ECKey) + "-Public"));
            }

            bool encrypted = jObject.Value<bool>("Encrypted");

            if (encrypted)
            {
                using var random = RNGCryptoServiceProvider.Create();
                byte[] salt = Convert.FromBase64String(jObject.Value<string>("Salt"));
                byte[] iv = Convert.FromBase64String(jObject.Value<string>("IV"));

                using Rfc2898DeriveBytes deriveBytes = new Rfc2898DeriveBytes(password, salt, KeyDerivationIterations, HashAlgorithmName.SHA512);
                using AesCng aes = new AesCng();
                aes.Key = deriveBytes.GetBytes(aes.KeySize / 8);
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor();
                ecKey = decryptor.TransformFinalBlock(ecKey, 0, ecKey.Length);
            }

            CngKey cngKey = CngKey.Import(ecKey, isPrivateKey ? CngKeyBlobFormat.EccFullPrivateBlob : CngKeyBlobFormat.EccFullPublicBlob);

            return new MessageClientIdentity(jObject.Value<string>(nameof(SystemName)), jObject.Value<string>(nameof(Name)), cngKey);
        }

        public string SerializePublicInfoToJson()
        {
            return SerializeToJson(false);
        }

        public string SerializePrivateInfoToJson(string password=null)
        {
            return SerializeToJson(true, password);
        }

        private string SerializeToJson(bool includePrivateKey=false, string password=null)
        {
            var jObject = new JObject();
            jObject.Add(nameof(SystemName), this.SystemName);
            jObject.Add(nameof(Name), this.Name);
            jObject.Add("KeySize", this.ECKey.KeySize);
            
            if (includePrivateKey)
            {
                var eccKey = this.ECKey.Export(CngKeyBlobFormat.EccFullPrivateBlob);


                if (!string.IsNullOrEmpty(password))
                {
                    using var random = RNGCryptoServiceProvider.Create();
                    byte[] salt = new byte[SaltSize];
                    random.GetBytes(salt);

                    using Rfc2898DeriveBytes deriveBytes = new Rfc2898DeriveBytes(password, salt, KeyDerivationIterations, HashAlgorithmName.SHA512);
                    using AesCng aes = new AesCng();
                    aes.Key = deriveBytes.GetBytes(aes.KeySize / 8);

                    jObject.Add("Encrypted", true);
                    jObject.Add("Salt", salt);
                    jObject.Add("IV", aes.IV);

                    using var encryptor = aes.CreateEncryptor();
                    eccKey = encryptor.TransformFinalBlock(eccKey, 0, eccKey.Length);
                }
                else
                {
                    jObject.Add("Encrypted", false);
                }

                jObject.Add(nameof(ECKey) + "-Private",eccKey);
            }
            else
            {
                jObject.Add(nameof(ECKey) + "-Public", this.ECKey.Export(CngKeyBlobFormat.EccFullPublicBlob));
            }

            return jObject.ToString();
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            info.AddValue(nameof(SystemName), SystemName);
            info.AddValue(nameof(Name), Name);
            info.AddValue(nameof(ECKey), ECKey.Export(CngKeyBlobFormat.EccFullPublicBlob));
        }

        // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    ECKey?.Dispose();
                    Dsa?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        #endregion Methods

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~ClientInfo()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }
    }
}