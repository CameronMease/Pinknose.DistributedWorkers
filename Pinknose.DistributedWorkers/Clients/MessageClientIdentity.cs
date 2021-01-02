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

using Pinknose.DistributedWorkers.Exceptions;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Pinknose.DistributedWorkers.Clients
{
    public enum Encryption { None, CurrentUser, LocalMachine, Password }

    [Serializable]
    public partial class MessageClientIdentity : IDisposable, ISerializable, IComparable
    {
        #region Fields

        private string hash = null;

        private bool disposedValue = false;
        private string systemName;

        #endregion Fields

        #region Constructors

        internal MessageClientIdentity(string systemName, string clientName, ECDiffieHellman dh, bool isPrivateIdentity)
        {
            SystemName = systemName;
            Name = clientName;
            ECDiffieHellman = dh;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ECDsa = new ECDsaCng();
            }
            else
            {
                ECDsa = new ECDsaOpenSsl();
            }

            ECDsa.ImportParameters(dh.ExportExplicitParameters(isPrivateIdentity));
        }

        public MessageClientIdentity(string systemName, string clientName, ECDiffieHellmanCurve curve)
        {
            ECDiffieHellman dh;

            var dhCurve = curve switch
            {
                ECDiffieHellmanCurve.P256 => ECCurve.NamedCurves.nistP256,
                ECDiffieHellmanCurve.P384 => ECCurve.NamedCurves.nistP384,
                ECDiffieHellmanCurve.P521 => ECCurve.NamedCurves.nistP521,
                _ => throw new ArgumentOutOfRangeException(nameof(curve))
            };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                dh = new ECDiffieHellmanCng(dhCurve);
                ECDsa = new ECDsaCng();
            }
            else
            {
#if (!NET48)
                dh = new ECDiffieHellmanOpenSsl();
#else
                throw new NotSupportedException("ECDiffieHellmanOpenSsl is not supported in .NET 4.8");
#endif

                ECDsa = new ECDsaOpenSsl();
            }

            SystemName = systemName;
            Name = clientName;
            ECDiffieHellman = dh;

            ECDsa.ImportParameters(dh.ExportExplicitParameters(true));
        }

        #endregion Constructors

#if false
        internal MessageClientIdentity(string systemName, string clientName, byte[] pkcs8PrivateKey, ECDiffieHellmanCurve curve)
        {
            SystemName = systemName;
            Name = clientName;
            ECDiffieHellman = CreateDH(curve, false);
            int bytesRead;
            ECDiffieHellman.ImportPkcs8PrivateKey(pkcs8PrivateKey, out bytesRead);
        }
#endif

        #region Properties

        [DisplayName("Identity Hash")]
        public string IdentityHash
        {
            get
            {
                if (hash == null)
                {
                    byte[] nameBytes = Encoding.UTF8.GetBytes(SystemName + ":" + Name);

                    var parms = this.ECDiffieHellman.ExportParameters(false);
                    byte[] allBytes = new byte[nameBytes.Length + parms.Q.X.Length + parms.Q.Y.Length];
                    nameBytes.CopyTo(allBytes, 0);
                    parms.Q.X.CopyTo(allBytes, nameBytes.Length);
                    parms.Q.Y.CopyTo(allBytes, nameBytes.Length + parms.Q.X.Length);

                    using var sha2 = SHA256.Create();

                    var shaHash = sha2.ComputeHash(allBytes);

                    StringBuilder sb = new StringBuilder();
                    const int blockSize = 4;

                    for (int i = 0; i < shaHash.Length; i += 1)
                    {
                        sb.AppendFormat("{0:X}", shaHash[i]);

                        if ((i + 1) % blockSize == 0 && i != (shaHash.Length - 1))
                        {
                            sb.Append("-");
                        }
                    }

                    hash = sb.ToString();
                }

                return hash;
            }
        }

        //[Browsable(false)]
        //public ECDsa Dsa { get; private set; }

        //TODO: Make this private.  Expose the signing functions
        [Browsable(false)]
        public ECDsa ECDsa { get; private set; }

        [DisplayName("Client Name")]
        public string Name 
        { 
            get => name;
            private set
            {
                ValidateName(value);
                name = value;
            }
        }

        //[Browsable(false)]
        //public CngKey ECKey { get; private set; }
        [DisplayName("System Name")]
        public string SystemName
        {
            get => systemName;
            private set
            {
                ValidateName(value);
                systemName = value;
            }
        }

        private static Regex NameRegex = new Regex(@"[A-Za-z0-9_\-]+", RegexOptions.Compiled);
        private string name;

        private static void ValidateName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Name cannot be NULL or empty.", nameof(name));
            }

            if (!NameRegex.IsMatch(name))
            {
                throw new NameException("Name can only contain letters, numbers, underscores, and dashes.");
            }
        }

        [Browsable(false)]
        internal ECDiffieHellman ECDiffieHellman { get; set; }

        internal string DedicatedQueueName => NameHelper.GetDedicatedQueueName(SystemName, Name);

        #endregion Properties

#if false
        public static MessageClientIdentity CreateClientInfo(string systemName, string clientName, ECDiffieHellmanCurve privateKeyCurve, bool allowExport=false)
        {
            var dh = MessageClientIdentity.CreateDH(privateKeyCurve, allowExport);
            return new MessageClientIdentity(systemName, clientName, dh, true);
        }

        public static MessageClientIdentity CreateServerInfo(string systemName, ECDiffieHellmanCurve privateKeyCurve, bool allowExport = false)
        {
            var dh = MessageClientIdentity.CreateDH(privateKeyCurve, allowExport);
            return new MessageClientIdentity(systemName, NameHelper.GetServerName(), dh, true);
        }
#endif

        #region Methods

        public byte[] GenerateSymmetricKey(MessageClientIdentity otherIdentity)
        {
            if (otherIdentity is null)
            {
                throw new ArgumentNullException(nameof(otherIdentity));
            }

            return this.ECDiffieHellman.DeriveKeyFromHash(otherIdentity.ECDiffieHellman.PublicKey, HashAlgorithmName.SHA256);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        public int CompareTo(object obj)
        {
            if (obj is null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            if (obj.GetType() == this.GetType())
            {
                return this.IdentityHash.CompareTo(((MessageClientIdentity)obj).IdentityHash);
            }
            else
            {
                return -1;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    ECDiffieHellman?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        private static ECDsa CreateDsa(ECParameters ecParameters)
        {
            ECDsa ecDsa;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ecDsa = new ECDsaCng();
            }
            else
            {
                ecDsa = new ECDsaOpenSsl();
            }

            ecDsa.ImportParameters(ecParameters);

            return ecDsa;
        }

        private static ECDiffieHellman CreateDH(ECParameters ecParameters)
        {
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

            return dh;
        }

        #endregion Methods

#if false
        private static ECDiffieHellman CreateDH(int keySize, bool allowExport = false)
        {
            ECCurve namedCurve = keySize switch
            {
                256 => ECCurve.NamedCurves.nistP256,
                384 => ECCurve.NamedCurves.nistP384,
                521 => ECCurve.NamedCurves.nistP521,
                _ => throw new ArgumentOutOfRangeException(nameof(keySize))
            };

            return CreateDH(namedCurve, allowExport);
        }
#endif

#if false
        private static ECDiffieHellman CreateDH(ECDiffieHellmanCurve curve, bool allowExport = false)
        {
            ECCurve namedCurve = curve switch
            {
                ECDiffieHellmanCurve.P256 => ECCurve.NamedCurves.nistP256,
                ECDiffieHellmanCurve.P384 => ECCurve.NamedCurves.nistP384,
                ECDiffieHellmanCurve.P521 => ECCurve.NamedCurves.nistP521,
                _ => throw new ArgumentOutOfRangeException(nameof(curve))
            };

            return CreateDH(namedCurve, allowExport);
        }
#endif

#if false
        private static ECDiffieHellman CreateDH(ECCurve curve, bool allowExport = false)
        {
            CngKeyCreationParameters parameters = new CngKeyCreationParameters()
            {
                ExportPolicy = allowExport ? CngExportPolicies.AllowPlaintextExport : CngExportPolicies.None
            };

            //CngKey key = CngKey.Create(algorithm, null, parameters);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return ECDiffieHellmanCng.Create(curve);
            }
            else
            {
#if (!NET48)
                return ECDiffieHellmanOpenSsl.Create(curve);
#else
                throw new NotSupportedException("ECDiffieHellmanOpenSsl is not supported in .NET 4.8");
#endif
            }
        }
#endif

        // To detect redundant calls
        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~ClientInfo()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }
    }
}