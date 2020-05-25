using System;
using System.Security.Cryptography;
using System.Text;

namespace Pinknose.DistributedWorkers.Extensions
{

    public static class ArrayExtensions 
    {
        public static string ToHashedHexString(this byte[] byteArray)
        {
            using SHA256Managed hasher = new SHA256Managed();

            StringBuilder sb = new StringBuilder();

            foreach (byte singleByte in hasher.ComputeHash(byteArray))
            {
                sb.Append(singleByte.ToString("x2"));
            }

            return sb.ToString();
        }

    }

}