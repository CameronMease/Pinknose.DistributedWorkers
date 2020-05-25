using System;
using System.Security.Cryptography;

namespace Pinknose.DistributedWorkers
{
    public enum ECDiffieHellmanCurve { P256, P384, P521}

    public static class MessageClientCrypto
    {
        public static CngKey CreateClientKey(ECDiffieHellmanCurve curve)
        {
            CngAlgorithm algorithm = curve switch
            {
                ECDiffieHellmanCurve.P256 => CngAlgorithm.ECDiffieHellmanP521,
                ECDiffieHellmanCurve.P384 => CngAlgorithm.ECDiffieHellmanP384,
                ECDiffieHellmanCurve.P521 => CngAlgorithm.ECDiffieHellmanP521
            };

            return CngKey.Create(algorithm);
        }

        public static MessageClientInfo CreateClientInfo(string systemName, string clientName, ECDiffieHellmanCurve privateKeyCurve)
        {
            var key = CreateClientKey(privateKeyCurve);
            return new MessageClientInfo(systemName, clientName, key);
        }

        public static MessageClientInfo CreateServerInfo(string systemName, ECDiffieHellmanCurve privateKeyCurve)
        {
            var key = CreateClientKey(privateKeyCurve);
            return new MessageClientInfo(systemName, NameHelper.GetServerName(), key);
        }


    }

}