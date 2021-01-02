using Newtonsoft.Json;
using Pinknose.DistributedWorkers.Clients;
using Pinknose.DistributedWorkers.Crypto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pinknose.DistributedWorkers.Keystore
{
    public class TrustZoneSharedKey
    {
        protected internal static readonly int SharedKeyByteSize = 32;

        internal TrustZoneSharedKey(string trustZoneName, DateTime validFrom, DateTime validTo)
        {
            TrustZoneName = trustZoneName;
            AesKey = RandomNumberOracle.GetRandomBytes(SharedKeyByteSize);
            ValidFrom = validFrom;
            ValidTo = validTo;
        }

        [JsonConstructor]
        internal TrustZoneSharedKey(string trustZoneName, byte[] aesKey, DateTime validFrom, DateTime validTo)
        {
            TrustZoneName = trustZoneName;
            AesKey = aesKey;
            ValidFrom = validFrom;
            ValidTo = validTo;
        }

        public string TrustZoneName { get; private set; }
        public byte[] AesKey { get; private set; }
        public DateTime ValidFrom { get; private set; }
        public DateTime ValidTo { get; private set; }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != typeof(TrustZoneSharedKey))
            {
                return false;
            }

            var other = (TrustZoneSharedKey)obj;

            return
                this.TrustZoneName == other.TrustZoneName &&
                this.ValidFrom == other.ValidFrom &&
                this.ValidTo == other.ValidTo &&
                Enumerable.SequenceEqual<byte>(this.AesKey, other.AesKey);
        }
    }
}
