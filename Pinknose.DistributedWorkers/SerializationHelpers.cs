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

using Newtonsoft.Json;
using Pinknose.DistributedWorkers.Extensions;
using System;
using System.IO;
using System.IO.Compression;

namespace Pinknose.DistributedWorkers
{
    public static class SerializationHelpers
    {
        #region Methods

        public static T DeserializeFromGZippedJson<T>(byte[] data)
        {
            return DeserializeFromJson<T>(data.GunzipToString());
        }

        public static object DeserializeFromGZippedJson(byte[] data, Type dataType)
        {
            return DeserializeFromJson(data.GunzipToString(), dataType);
        }

        public static T DeserializeFromJson<T>(string data)
        {
            return JsonConvert.DeserializeObject<T>(data);
        }

        public static object DeserializeFromJson(string data, Type dataType)
        {
            object obj = JsonConvert.DeserializeObject(data, dataType);
            return obj;
        }

        /// <summary>
        /// Decompresses byte array using GZip.
        /// </summary>
        /// <param name="data">Compressed data.</param>
        /// <returns>Decompressed data.</returns>
        public static byte[] GUnzip(byte[] data)
        {
            using (MemoryStream outputMemoryStream = new MemoryStream())
            {
                GUnzipToStream(data, outputMemoryStream);
                byte[] unzippedBytes = new byte[outputMemoryStream.Length];
                outputMemoryStream.Seek(0, SeekOrigin.Begin);
                outputMemoryStream.Read(unzippedBytes, 0, unzippedBytes.Length);
                return unzippedBytes;
            }
        }

        public static void GUnzipToStream(byte[] data, Stream outStream)
        {
            using (MemoryStream memoryStream = new MemoryStream(data))
            {
                using (GZipStream gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                {
                    gzipStream.CopyTo(outStream);
                }
            }
        }

        public static byte[] GZip(byte[] data)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (GZipStream gzipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
                {
                    gzipStream.Write(data, 0, data.Length);
                }

                memoryStream.Seek(0, SeekOrigin.Begin);
                byte[] bytes = new byte[memoryStream.Length];
                memoryStream.Read(bytes, 0, bytes.Length);

                return bytes;
            }
        }

        public static string SerializeToJson(object data)
        {
            JsonSerializerSettings serializerSettings = new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Error,
                ContractResolver = JsonShouldSerializeContractResolver.Instance,
                TypeNameHandling = TypeNameHandling.Arrays,
            };

            return JsonConvert.SerializeObject(data, serializerSettings);
        }

        public static byte[] SerializeToJsonAndGZip(object data)
        {
            string json = SerializeToJson(data);
            return json.GZipToBytes();
        }

        #endregion Methods
    }
}