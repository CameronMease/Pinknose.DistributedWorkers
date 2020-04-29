using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Pinknose.DistributedWorkers
{
    public static class GZipExtensions
    {
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
    }
}
