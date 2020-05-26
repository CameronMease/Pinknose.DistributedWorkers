using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Pinknose.DistributedWorkers.Keystore
{
    public class SharedKeyCollection : IEnumerable<byte[]>
    {
        private SortedDictionary<int, (byte[] Key, DateTime CreationTime)> _dictionary = new SortedDictionary<int, (byte[] Key, DateTime CreationTime)>();

        public byte[] this[int keyId]
        {
            get => _dictionary[keyId].Key;
            set
            {
                if (keyId > MaxKeyId)
                {
                    throw new ArgumentOutOfRangeException(nameof(keyId));
                }
                _dictionary[keyId] = (value, DateTime.Now);
            }
        }

        public int MaxKeyId { get; set; } = Int32.MaxValue;

        public IEnumerator<byte[]> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
