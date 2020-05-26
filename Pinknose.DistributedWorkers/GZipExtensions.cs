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

using System.IO;
using System.IO.Compression;
using System.Text;

namespace Pinknose.DistributedWorkers
{
    public static class GZipExtensions
    {
        #region Methods

        public static string GunzipToString(this byte[] bytes)
        {
            using (MemoryStream memoryStream = new MemoryStream(bytes))
            {
                using (GZipStream gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                {
                    using (MemoryStream outputMemoryStream = new MemoryStream())
                    {
                        gzipStream.CopyTo(outputMemoryStream);
                        byte[] unzippedBytes = new byte[outputMemoryStream.Length];
                        outputMemoryStream.Seek(0, SeekOrigin.Begin);
                        outputMemoryStream.Read(unzippedBytes);
                        return UTF8Encoding.UTF8.GetString(unzippedBytes);
                    }
                }
            }
        }

        public static byte[] GZipToBytes(this string stringValue)
        {
            var stringBytes = UTF8Encoding.UTF8.GetBytes(stringValue);

            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (GZipStream gzipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
                {
                    gzipStream.Write(stringBytes);
                }

                memoryStream.Seek(0, SeekOrigin.Begin);
                byte[] bytes = new byte[memoryStream.Length];
                memoryStream.Read(bytes);
                return bytes;
            }
        }

        #endregion Methods
    }
}