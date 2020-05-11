using System.Security.Cryptography;

namespace Pinknose.DistributedWorkers
{
    public interface IMessageClient
    {
        byte[] AddSignature(byte[] message);
        bool SignatureIsValid(byte[] message, ECDsaCng dsa);
    }
}