using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pinknose.DistributedWorkers.Clients;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Pinknose.DistributedWorkers.Serialization
{
    public class MessageClientIdentityJsonConverter : JsonConverter
    {
        private const int SaltSize = 8;
        private const int KeyDerivationIterations = 1000000;

        private bool _savePrivateKey;
        private Encryption _encryptionType;
        private string _password;

        public event EventHandler<PasswordRequiredEventAgs> PasswordRequired;


        public MessageClientIdentityJsonConverter(Encryption encryptionType=Encryption.None, bool savePrivateKey = false, string password=null)
        {
            _savePrivateKey = savePrivateKey;
            _encryptionType = encryptionType;
            _password = password;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(MessageClientIdentity);
        }

        public override bool CanRead => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            string systemName ="";
            string clientName ="";
            byte[] iv = null;
            byte[] salt = null;
            Encryption encryption = Encryption.None;
            string curveOid = "";
            string d = null;
            string qx = "";
            string qy = "";
            string hash = "";

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    string propertyName = (string)reader.Value;

                    reader.Read();

                    switch (propertyName)
                    {
                        case nameof(MessageClientIdentity.SystemName):
                            systemName = (string)reader.Value;
                            break;

                        case nameof(MessageClientIdentity.Name):
                            clientName = (string)reader.Value;
                            break;

                        case "IV":
                            iv = Convert.FromBase64String((string)reader.Value);
                            break;

                        case "Salt":
                            salt = Convert.FromBase64String((string)reader.Value);
                            break;

                        case "Encryption":
                            encryption = (Encryption)Enum.Parse(typeof(Encryption), (string)reader.Value);
                            break;

                        case "CurveOid":
                            curveOid = (string)reader.Value;
                            break;

                        case "D":
                            d = (string)reader.Value;
                            break;

                        case "Q.Y":
                            qy = (string)reader.Value;
                            break;

                        case "Q.X":
                            qx = (string)reader.Value;
                            break;

                        case "Hash":
                            hash = (string)reader.Value;
                            break;
                    }
                }
            }

            byte[] privateKey = null;

            if (d != null)
            {
                privateKey = Decrypt(Convert.FromBase64String(d), encryption, iv, salt, _password);
            }

            ECParameters ecParameters = new ECParameters()
            {
                Curve = ECCurve.CreateFromOid(new Oid(curveOid)),
                D = d == null ? null : privateKey,
                Q = new ECPoint()
                {
                    X = Convert.FromBase64String(qx),
                    Y = Convert.FromBase64String(qy)
                }
            };

            ECDiffieHellman dh;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                dh = new ECDiffieHellmanCng();
            }
            else
            {
#if (!NET48)
                dh = new ECDiffieHellmanOpenSsl();
#else
                throw new NotSupportedException("ECDiffieHellmanOpenSsl is not supported in .NET 4.8");
#endif
            }

            dh.ImportParameters(ecParameters);

            var ident = new MessageClientIdentity(systemName, clientName, dh, !string.IsNullOrEmpty(d));

            if (ident.IdentityHash != hash)
            {
                throw new NotImplementedException();
            }

            return ident;
        }

        private byte[] Decrypt(byte[] buffer, Encryption encryption, byte[] iv=null, byte[] salt=null, string password=null)
        {
            if (buffer is null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (encryption == Encryption.Password)
            {
                if (iv is null)
                {
                    throw new ArgumentNullException(nameof(iv));
                }

                if (salt is null)
                {
                    throw new ArgumentNullException(nameof(salt));
                }

                if (string.IsNullOrEmpty(password))
                {
                    PasswordRequiredEventAgs e = new PasswordRequiredEventAgs();
                    PasswordRequired?.Invoke(this, e);

                    if (string.IsNullOrEmpty(e.Password))
                    {
                        throw new ArgumentException("Password cannot be empty or null.", nameof(password));
                    }
                    else
                    {
                        password = e.Password;
                    }
                }
            }

            byte[] plainText = new byte[buffer.Length];
            buffer.CopyTo(plainText, 0);

            if (encryption == Encryption.Password)
            {
                using var random = RNGCryptoServiceProvider.Create();

                using Rfc2898DeriveBytes deriveBytes = new Rfc2898DeriveBytes(password, salt, KeyDerivationIterations, HashAlgorithmName.SHA512);

                Aes aes;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    aes = new AesCng();
                }
                else
                {
                    aes = new AesManaged();
                }
                aes.Key = deriveBytes.GetBytes(aes.KeySize / 8);
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor();
                plainText = decryptor.TransformFinalBlock(plainText, 0, plainText.Length);

                aes.Dispose();
            }
            else if (encryption == Encryption.LocalMachine)
            {
                plainText = ProtectedData.Unprotect(plainText, null, DataProtectionScope.LocalMachine);
            }
            else if (encryption == Encryption.CurrentUser)
            {
                plainText = ProtectedData.Unprotect(plainText, null, DataProtectionScope.CurrentUser);
            }

            return plainText;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            MessageClientIdentity clientIdent = (MessageClientIdentity)value;

            JObject jObject = new JObject();
            jObject.Add(nameof(MessageClientIdentity.SystemName), clientIdent.SystemName);
            jObject.Add(nameof(MessageClientIdentity.Name), clientIdent.Name);            

            ECParameters parms = clientIdent.ECDiffieHellman.ExportParameters(_savePrivateKey);

            if (!parms.Curve.IsNamed)
            {
                throw new NotImplementedException();
            }

            jObject.Add("CurveOid", parms.Curve.Oid.Value);

            if (parms.D != null && _savePrivateKey)
            {
                byte[] privateKey = parms.D;

                if (_encryptionType == Encryption.Password)
                {
                    using var random = RNGCryptoServiceProvider.Create();
                    byte[] salt = new byte[SaltSize];
                    random.GetBytes(salt);

                    using Rfc2898DeriveBytes deriveBytes = new Rfc2898DeriveBytes(_password, salt, KeyDerivationIterations, HashAlgorithmName.SHA512);
                    Aes aes;
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        aes = new AesCng();
                    }
                    else
                    {
                        aes = new AesManaged();
                    }
                    aes.Key = deriveBytes.GetBytes(aes.KeySize / 8);

                    jObject.Add("Salt", salt);
                    jObject.Add("IV", aes.IV);

                    using var encryptor = aes.CreateEncryptor();
                    privateKey = encryptor.TransformFinalBlock(privateKey, 0, privateKey.Length);

                    aes.Dispose(); 
                }
                else if (_encryptionType == Encryption.LocalMachine)
                {
                    privateKey = ProtectedData.Protect(privateKey, null, DataProtectionScope.LocalMachine);
                }
                else if (_encryptionType == Encryption.CurrentUser)
                {
                    privateKey = ProtectedData.Protect(privateKey, null, DataProtectionScope.CurrentUser);
                }

                jObject.Add("Encryption", _encryptionType.ToString());

                jObject.Add("D", privateKey);
            }

            jObject.Add("Q.X", parms.Q.X);
            jObject.Add("Q.Y", parms.Q.Y);

            jObject.Add("Hash", clientIdent.IdentityHash);
            jObject.WriteTo(writer);
        }
    }
}
