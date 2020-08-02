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

using System;
using System.Security.Cryptography;
using System.Text;

namespace Pinknose.DistributedWorkers.Extensions
{
    public static class CngKeyExtensions
    {
        #region Methods

        public static CngKey GetPublicKey(this CngKey key)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            byte[] bytes = key.Export(CngKeyBlobFormat.EccFullPublicBlob);
            return CngKey.Import(bytes, CngKeyBlobFormat.EccFullPublicBlob);
        }

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

        #endregion Methods
    }
}