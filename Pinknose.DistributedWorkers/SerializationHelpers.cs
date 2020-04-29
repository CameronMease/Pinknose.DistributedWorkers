using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Pinknose.DistributedWorkers
{
    public static class SerializationHelpers
    {
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

        public static T DeserializeFromJson<T>(string data)
        {
            return JsonConvert.DeserializeObject<T>(data);
        }

        public static object DeserializeFromJson(string data, Type dataType)
        {
            object obj = JsonConvert.DeserializeObject(data, dataType);
            return obj;
        }

        public static T DeserializeFromGZippedJson<T>(byte[] data)
        {
            return DeserializeFromJson<T>(data.GunzipToString());
        }

        public static object DeserializeFromGZippedJson(byte[] data, Type dataType)
        {
            return DeserializeFromJson(data.GunzipToString(), dataType);
        }

        public static byte[] GZip(byte[] data)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (GZipStream gzipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
                {
                    gzipStream.Write(data);
                }

                memoryStream.Seek(0, SeekOrigin.Begin);

                byte[] bytes = new byte[memoryStream.Length];

                memoryStream.Read(bytes);

                return bytes;
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
                outputMemoryStream.Read(unzippedBytes);
                return unzippedBytes;
            }
        }

    }
}
