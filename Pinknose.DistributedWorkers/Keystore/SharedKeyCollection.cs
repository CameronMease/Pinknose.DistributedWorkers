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

using Pinknose.DistributedWorkers.Crypto;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Pinknose.DistributedWorkers.Keystore
{
    public class SharedKeyCollection : IEnumerable<TrustZoneSharedKey>
    {
        #region Fields

        private List<TrustZoneSharedKey> keyList = new List<TrustZoneSharedKey>();

        #endregion Fields

        #region Properties

        public int MaxKeyId { get; set; } = Int32.MaxValue;

        #endregion Properties

        #region Indexers

        public TrustZoneSharedKey this[DateTime validityTime]
        {
            get
            {
                var keys = keyList.Where(k => validityTime >= k.ValidFrom && validityTime < k.ValidTo);

                var keyCount = keys.Count();

                if (keyCount == 0)
                {
                    throw new KeyNotFoundException("No shared key was found within the specified validity range.");
                }
                else if (keyCount > 1)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    return keys.First();
                }
            }
        }

        #endregion Indexers

        #region Methods

        public IEnumerator<TrustZoneSharedKey> GetEnumerator()
        {
            return keyList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return keyList.GetEnumerator();
        }

        public void Add(TrustZoneSharedKey trustZoneSharedKey)
        {
            if (!keyList.Contains(trustZoneSharedKey))
            {
                keyList.Add(trustZoneSharedKey);

                if (keyList.Count > MaxKeys)
                {
                    var oldKeys = keyList
                        .OrderBy(k => k.ValidTo)
                        .Take(keyList.Count - MaxKeys)
                        .ToList();

                    foreach (var oldKey in oldKeys)
                    {
                        keyList.Remove(oldKey);
                    }
                }
            }
        }

        #endregion Methods

        public int MaxKeys { get; set; } = 5;
    }
}