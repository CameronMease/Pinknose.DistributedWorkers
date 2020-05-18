using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Pinknose.DistributedWorkers.Extensions
{
    public static class CngKeyExtensions
    {
        public static string GetPublicKeyHash(this CngKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            byte[] keyBytes = key.Export(CngKeyBlobFormat.EccPublicBlob);
            using SHA256Managed hasher = new SHA256Managed();

            StringBuilder sb = new StringBuilder();

            foreach (byte singleByte in hasher.ComputeHash(keyBytes))
            {
                sb.Append(singleByte.ToString("x2"));
            }

            return sb.ToString();
        }

        public static string PublicKeyToString(this CngKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            byte[] keyBytes = key.Export(CngKeyBlobFormat.EccFullPublicBlob);

            return Convert.ToBase64String(keyBytes);
        }
    }
}
