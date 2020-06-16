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
using System.Text;

namespace DistributedWorkersTestApp
{
    public static class SimpleCBC
    {
        #region Methods

        public static (byte[] CipherText, UInt32 Signature) Encode(string plaintext, UInt32 key, UInt32 iv)
        {
            int blockLength = plaintext.Length + plaintext.Length % 4;

            byte[] bytes = new byte[blockLength];
            Encoding.UTF8.GetBytes(plaintext).CopyTo(bytes, 0);

            UInt32 currentBlock;
            UInt32 previousBlock = 0;

            for (int i = 0; i < bytes.Length; i += 4)
            {
                currentBlock = BitConverter.ToUInt32(bytes, i);

                if (i == 0)
                {
                    currentBlock ^= iv;
                }
                else
                {
                    currentBlock ^= previousBlock;
                }

                currentBlock ^= key;

                BitConverter.GetBytes(currentBlock).CopyTo(bytes, i);
                previousBlock = currentBlock;
            }

            return (bytes, BitConverter.ToUInt32(bytes[^4..]));
        }

        public static string Decode(byte[] bytes, UInt32 key, UInt32 iv)
        {
            UInt32 currentBlock;
            UInt32 previousBlock = 0;

            for (int i = 0; i < bytes.Length; i += 4)
            {
                currentBlock = BitConverter.ToUInt32(bytes, i);
                var previousBlockTemp = currentBlock;

                currentBlock ^= key;

                if (i == 0)
                {
                    currentBlock ^= iv;
                }
                else
                {
                    currentBlock ^= previousBlock;
                }

                BitConverter.GetBytes(currentBlock).CopyTo(bytes, i);
                previousBlock = previousBlockTemp;
            }

            return Encoding.UTF8.GetString(bytes).Trim('\0');
        }

        #endregion Methods
    }
}