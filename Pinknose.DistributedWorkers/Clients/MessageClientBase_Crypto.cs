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
using System.Security.Cryptography;

namespace Pinknose.DistributedWorkers.Clients
{
    public enum ECDiffieHellmanCurve { P256, P384, P521 }

    public partial class MessageClientBase
    {
        #region Properties

        internal int SignatureLength
        {
            get
            {
                var length = this.ClientInfo.Dsa.KeySize switch
                {
                    256 => 64,
                    384 => 96,
                    521 => 132,
                    _ => throw new NotImplementedException()
                };

                return length;
            }
        }

        private static RandomNumberGenerator RandomNumberGenerator { get; } = RNGCryptoServiceProvider.Create();

        #endregion Properties

        #region Methods

        public static CngKey CreateClientKey(ECDiffieHellmanCurve curve, bool allowExport = false)
        {
            CngAlgorithm algorithm = curve switch
            {
                ECDiffieHellmanCurve.P256 => CngAlgorithm.ECDiffieHellmanP256,
                ECDiffieHellmanCurve.P384 => CngAlgorithm.ECDiffieHellmanP384,
                ECDiffieHellmanCurve.P521 => CngAlgorithm.ECDiffieHellmanP521
            };

            CngKeyCreationParameters parameters = new CngKeyCreationParameters()
            {
                ExportPolicy = allowExport ? CngExportPolicies.AllowPlaintextExport : CngExportPolicies.None
            };

            return CngKey.Create(algorithm, null, parameters);
        }

        public static byte[] GetRandomBytes(int length)
        {
            lock (RandomNumberGenerator)
            {
                byte[] bytes = new byte[length];
                RandomNumberGenerator.GetBytes(bytes);
                return bytes;
            }
        }

        internal static SignatureVerificationStatus ValidateSignature(byte[] rawMessage, byte[] signature, byte[] publicKey)
        {
            using CngKey key = CngKey.Import(publicKey, CngKeyBlobFormat.EccFullPublicBlob);
            using ECDsaCng tempDsa = new ECDsaCng(key);

            if (tempDsa.VerifyData(rawMessage, signature))
            {
                return SignatureVerificationStatus.SignatureValid;
            }
            else
            {
                return SignatureVerificationStatus.SignatureNotValid;
            }
        }

        internal byte[] DecryptDataWithClientKey(byte[] cypherText, string clientName, byte[] iv)
        {
            //TODO: Need to make sure the symmetric key was created first.
            return DecryptData(cypherText, this.PublicKeystore.GetSymmetricKey(clientName), iv);
        }

        internal byte[] DecryptDataWithSystemSharedKey(byte[] cypherText, byte[] iv)
        {
            return DecryptData(cypherText, PublicKeystore.SystemSharedKeys[PublicKeystore.CurrentSharedKeyId], iv);
        }

        internal (byte[] CipherText, byte[] IV) EncryptDataWithClientKey(byte[] data, string clientName)
        {
            return EncryptDataWithClientKey(data, PublicKeystore[clientName]);
        }

        internal (byte[] CipherText, byte[] IV) EncryptDataWithClientKey(byte[] data, MessageClientInfo clientInfo)
        {
            return EncryptData(data, this.PublicKeystore.GetSymmetricKey(clientInfo));
        }

        internal (byte[] CipherText, byte[] IV, int keyId) EncryptDataWithSystemSharedKey(byte[] data)
        {
            var response = EncryptData(data, PublicKeystore.SystemSharedKeys[PublicKeystore.CurrentSharedKeyId]);
            return (response.CipherText, response.IV, 0);
        }

        internal byte[] SignData(byte[] data)
        {
            return this.ClientInfo.Dsa.SignData(data);
        }

        internal byte[] SignData(byte[] data, int offset, int count)
        {
            return this.ClientInfo.Dsa.SignData(data, offset, count);
        }

        internal SignatureVerificationStatus ValidateSignature(byte[] message, byte[] signature, string clientName)
        {
            if (!PublicKeystore.Contains(clientName) && clientName != this.ClientName)
            {
                return SignatureVerificationStatus.NoValidClientInfo;
            }
            else if ((clientName == this.ClientName && PublicKeystore.ParentClientInfo.Dsa.VerifyData(message, signature)) ||
                PublicKeystore[clientName].Dsa.VerifyData(message, signature))
            {
                return SignatureVerificationStatus.SignatureValid;
            }
            else
            {
                return SignatureVerificationStatus.SignatureNotValid;
            }
        }

        private static byte[] DecryptData(byte[] cipherText, byte[] key, byte[] iv)
        {
            using AesCng aes = new AesCng();
            aes.Key = key;
            aes.IV = iv;

            //Log.Verbose($"Ciphertext: {cipherText.ToHashedHexString()}");
            //Log.Verbose($"Decrypting message with key: {key.ToHashedHexString()}");
            //Log.Verbose($"Decrypting message with IV: {aes.IV.ToHashedHexString()}");

            using var decryptor = aes.CreateDecryptor();

            byte[] plainText = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);

            //Log.Verbose($"Plaintext: {plainText.ToHashedHexString()}");

            return plainText;
        }

        private static (byte[] CipherText, byte[] IV) EncryptData(byte[] data, byte[] key)
        {
            using AesCng aes = new AesCng();
            aes.Key = key;
            aes.IV = GetRandomBytes(16);

            using var encryptor = aes.CreateEncryptor();

            byte[] cipherText = encryptor.TransformFinalBlock(data, 0, data.Length);

            //Log.Verbose($"Encrypting message with key: {key.ToHashedHexString()}");
            //Log.Verbose($"Encrypting message with IV: {aes.IV.ToHashedHexString()}");
            //Log.Verbose($"Cipher text: {cipherText.ToHashedHexString()}");
            //Log.Verbose($"Plaintext: {data.ToHashedHexString()}");

            return (cipherText, aes.IV);
        }

        #endregion Methods
    }
}