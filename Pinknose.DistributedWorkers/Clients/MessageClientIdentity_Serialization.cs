using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pinknose.DistributedWorkers.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;

namespace Pinknose.DistributedWorkers.Clients
{
    public partial class MessageClientIdentity
    {
        /// <summary>
        /// Call by Binary Serializer.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            info.AddValue(nameof(SystemName), SystemName);
            info.AddValue(nameof(Name), Name);

            var parms = ECDiffieHellman.ExportParameters(false);

            if (!parms.Curve.IsNamed)
            {
                throw new NotImplementedException();
            }

            info.AddValue("CurveName", parms.Curve.Oid.FriendlyName);
            info.AddValue("Q.X", parms.Q.X);
            info.AddValue("Q.Y", parms.Q.Y);
        }

        /// <summary>
        /// Called by binary deserializer.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected MessageClientIdentity(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            SystemName = (string)info.GetValue(nameof(SystemName), typeof(string));
            Name = (string)info.GetValue(nameof(Name), typeof(string));

            ECParameters parms = new ECParameters();
            parms.Curve = ECCurve.CreateFromFriendlyName(info.GetString("CurveName"));
            parms.D = null;
            parms.Q.X = (byte[]) info.GetValue("Q.X", typeof(byte[]));
            parms.Q.Y = (byte[])info.GetValue("Q.Y", typeof(byte[]));

            ECDiffieHellman = CreateDH(parms);
            ECDsa = CreateDsa(parms);
        }

        public static MessageClientIdentity ImportFromFile(string keyFilePath, string password = "")
        {
            if (keyFilePath is null)
            {
                throw new ArgumentNullException(nameof(keyFilePath));
            }

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                keyFilePath = keyFilePath.Replace(@"\", "/");
            }

            var json = File.ReadAllText(keyFilePath);

            return Import(json, password);
        }

        public static MessageClientIdentity ImportFromFile(string keyFilePath, Func<string> getPasswordDelegate)
        {
            if (keyFilePath is null)
            {
                throw new ArgumentNullException(nameof(keyFilePath));
            }

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                keyFilePath = keyFilePath.Replace(@"\", "/");
            }

            var json = File.ReadAllText(keyFilePath);

            return Import(json, getPasswordDelegate);
        }

        public static MessageClientIdentity Import(string json, string password = "")
        {
            return JsonConvert.DeserializeObject<MessageClientIdentity>(
                json, 
                new MessageClientIdentityJsonConverter(password: password));
        }


        public static MessageClientIdentity Import(string json, Func<string> getPasswordDelegate)
        {
            if (getPasswordDelegate == null)
            {
                throw new ArgumentNullException(nameof(getPasswordDelegate));
            }

            var jsonConverter = new MessageClientIdentityJsonConverter();
            jsonConverter.PasswordRequired += (sender, e) => e.Password = getPasswordDelegate();

            return JsonConvert.DeserializeObject<MessageClientIdentity>(
                json,
                jsonConverter);
        }


        public string SerializePublicInfoToJson()
        {
            return SerializeToJson(false, Encryption.None, "");
        }

        public string SerializePrivateInfoToJson(Encryption encryption, string password = "")
        {
            return SerializeToJson(true, encryption, password);
        }

        private string SerializeToJson(bool includePrivateKey, Encryption encryption, string password)
        {
            return JsonConvert.SerializeObject(
                this,
                Formatting.Indented,
                new MessageClientIdentityJsonConverter(encryption, includePrivateKey, password));
        }
    }
}
