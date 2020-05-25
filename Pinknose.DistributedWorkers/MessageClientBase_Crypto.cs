using Pinknose.DistributedWorkers.Extensions;
using Pinknose.DistributedWorkers.Messages;
using Serilog;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Pinknose.DistributedWorkers
{
    public partial class MessageClientBase
    {
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

        public static RandomNumberGenerator Rand { get; } = RNGCryptoServiceProvider.Create();

        internal (byte[] CipherText, byte[] IV, int keyId) EncryptDataWithSystemSharedKey(byte[] data)
        {
            var response = EncryptData(data, CurrentSystemSharedKey.ToArray());
            return (response.CipherText, response.IV, 0);
        }

        internal (byte[] CipherText, byte[] IV) EncryptDataWithClientKey(byte[] data, string clientName)
        {
            return EncryptDataWithClientKey(data, PublicKeystore[clientName]);
        }

        internal (byte[] CipherText, byte[] IV) EncryptDataWithClientKey(byte[] data, MessageClientInfo clientInfo)
        {
            return EncryptData(data, clientInfo.SymmetricKey);
        }

        private static (byte[] CipherText, byte[] IV) EncryptData(byte[] data, byte[] key)
        {
            using AesCng aes = new AesCng();
            aes.Key = key;
            byte[] iv = new byte[16];

            lock (MessageClientBase.Rand)
            {
                MessageClientBase.Rand.GetBytes(iv);
            }

            aes.IV = iv;

            using var encryptor = aes.CreateEncryptor();

            byte[] cipherText = encryptor.TransformFinalBlock(data, 0, data.Length);

            //Log.Verbose($"Encrypting message with key: {key.ToHashedHexString()}");
            //Log.Verbose($"Encrypting message with IV: {iv.ToHashedHexString()}");
            //Log.Verbose($"Cipher text: {cipherText.ToHashedHexString()}");
            //Log.Verbose($"Plaintext: {data.ToHashedHexString()}");

            return (cipherText, aes.IV);

        }

        internal byte[] DecryptDataWithSystemSharedKey(byte[] cypherText, byte[] iv)
        {
            return DecryptData(cypherText, CurrentSystemSharedKey.ToArray(), iv);
        }

        internal byte[] DecryptDataWithClientKey(byte[] cypherText, string clientName, byte[] iv)
        {
            //TODO: Need to make sure the symmetric key was created first.
            return DecryptData(cypherText, PublicKeystore[clientName].SymmetricKey, iv);
        }

        private static byte[] DecryptData(byte[] cipherText, byte[] key, byte[] iv)
        {
            using AesCng aes = new AesCng();
            aes.Key = key;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();

            byte[] plainText = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);

            //Log.Verbose($"Ciphertext: {cipherText.ToHashedHexString()}");
            //Log.Verbose($"Plaintext: {plainText.ToHashedHexString()}");
            //Log.Verbose($"Decrypting message with key: {key.ToHashedHexString()}");
            //Log.Verbose($"Decrypting message with IV: {iv.ToHashedHexString()}");

            return plainText;
        }
    }
}
