using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Pinknose.DistributedWorkers.Extensions
{
    public static class RandomExtensions
    {
        public static UInt32 NextUInt32(this Random random)
        {
            var bytes = new byte[4];

            random.NextBytes(bytes);

            return BitConverter.ToUInt32(bytes);
        }
    }
}
