using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Pinknose.DistributedWorkers.Extensions;

namespace Pinknose.DistributedWorkers.Crypto
{
    public static class RandomNumberOracle
    {
        private static RandomNumberGenerator RandomNumberGenerator { get; } = RNGCryptoServiceProvider.Create();

        public static byte[] GetRandomBytes(int length)
        {
            lock (RandomNumberGenerator)
            {
                byte[] bytes = new byte[length];
                RandomNumberGenerator.GetBytes(bytes);
                return bytes;
            }
        }
    }
}
