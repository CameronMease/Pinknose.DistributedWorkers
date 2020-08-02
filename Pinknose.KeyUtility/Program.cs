using CommandLine;
using EasyNetQ.Management.Client.Model;
using Newtonsoft.Json.Serialization;
using Pinknose.DistributedWorkers.Clients;
using Pinknose.DistributedWorkers.KeyUtility.CommandLineOptions;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Pinknose.DistributedWorkers.KeyUtility
{
    internal class Program
    {
        #region Methods

        private static void Main(string[] args)
        {
            GenerateClientOptions generateClientOptions = null;
            GenerateServerOptions generateServerOptions = null;
            ConvertPrivateKeyOptions convertPrivateKeyOptions = null;

            CommandLine.Parser.Default.ParseArguments<GenerateClientOptions, GenerateServerOptions, ConvertPrivateKeyOptions>(args)
                .WithParsed<GenerateClientOptions>(opts => generateClientOptions = opts)
                .WithParsed<GenerateServerOptions>(opts => generateServerOptions = opts)
                .WithParsed<ConvertPrivateKeyOptions>(opts => convertPrivateKeyOptions = opts)
                .WithNotParsed(errors => Environment.Exit(-1));

            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
            var title = assembly.GetCustomAttribute<AssemblyTitleAttribute>();


            Console.WriteLine($"{title.Title}, {version.Version}");

            if (generateClientOptions != null || generateServerOptions != null)
            {
                GenerateKey(generateClientOptions, generateServerOptions);
            }

            if (convertPrivateKeyOptions != null)
            {
                ConvertPrivateKey(convertPrivateKeyOptions);
            }

            Environment.ExitCode = 0;
        }

        private static void ConvertPrivateKey(ConvertPrivateKeyOptions convertPrivateKeyOptions)
        {
            throw new NotImplementedException();
        }

        private static void GenerateKey(GenerateClientOptions generateClientOptions, GenerateServerOptions generateServerOptions)
        {
            StringBuilder sb = new StringBuilder();

            MessageClientIdentity clientInfo = null;
            GenerateOptionsBase generateOptionsBase = generateClientOptions != null ? (GenerateOptionsBase)generateClientOptions : (GenerateOptionsBase)generateServerOptions;

            if (generateOptionsBase.Directory == null)
            {
                generateOptionsBase.Directory = Directory.GetCurrentDirectory();
            }
            else if (!Directory.Exists(generateOptionsBase.Directory))
            {
                Console.WriteLine($"{generateOptionsBase.Directory} is not a valid directory.");
                Environment.Exit(-1);
            }

            ECDiffieHellmanCurve curve = ECDiffieHellmanCurve.P256;

            if (generateOptionsBase.KeySize256)
            {
                curve = ECDiffieHellmanCurve.P256;
            }
            else if (generateOptionsBase.KeySize384)
            {
                curve = ECDiffieHellmanCurve.P384;
            }
            else if (generateOptionsBase.KeySize521)
            {
                curve = ECDiffieHellmanCurve.P521;
            }

            if (generateClientOptions != null)
            {
                clientInfo = new MessageClientIdentity(generateClientOptions.SystemName, generateClientOptions.ClientName, curve);
            }
            else if (generateServerOptions != null)
            {
                //TODO: Need to get server name from somehwere else.
                clientInfo = new MessageClientIdentity(generateServerOptions.SystemName, "Server", curve);
            }

            string path = Path.Combine(generateOptionsBase.Directory, clientInfo.SystemName + "-" + clientInfo.Name);

            string publicFile = path + ".pub";
            File.WriteAllText(publicFile, clientInfo.SerializePublicInfoToJson());

            sb.AppendLine("\nCreating Client Key");
            sb.AppendLine($"   Diffie-Hellman Elliptic Curve             {curve}");
            sb.AppendLine($"   System Name:                              {clientInfo.SystemName}");
            sb.AppendLine($"   Client Name:                              {clientInfo.Name}");
            sb.AppendLine($"   Identity Hash:                            {clientInfo.IdentityHash}");
            sb.AppendLine($"   Public Key File:                          {publicFile}");

            //Console.WriteLine($"   Private Key File:              {privateFile}");

            // Private Key saving

            if (generateOptionsBase.JsonPrivateKey)
            {
                string password = "";
                bool match = false;
                string privateFile = path + ".priv";

                do
                {
                    do
                    {
                        Console.Write("Enter password: ");
                        password = ConsolePasswordReader.ReadPassword();

                        Console.Write("Re-enter password: ");
                        if (ConsolePasswordReader.ReadPassword() != password)
                        {
                            Console.WriteLine("Passwords do not match.");
                            match = false;
                        }
                        else
                        {
                            match = true;
                        }
                    } while (!match);

                    if (password == "")
                    {
                        Console.Write("You have entered a blank password.  Are you sure you want to save the private key without a password (not recommended!)? Type YES to continue without password: ");

                        string line = Console.ReadLine();

                        if (line == "YES")
                        {
                            Console.WriteLine();
                            File.WriteAllText(privateFile, clientInfo.SerializePrivateInfoToJson(Encryption.None));
                            sb.AppendLine($"   Unencrypted Private Key File:             {privateFile}");
                            break;
                        }
                    }
                    else
                    {
                        File.WriteAllText(privateFile, clientInfo.SerializePrivateInfoToJson(Encryption.Password, password));
                        sb.AppendLine($"   Password-Protected Private Key File:      {privateFile}");
                        break;
                    }
                } while (true);

                Console.WriteLine();
            }

            if (generateOptionsBase.LocalMachineEncryptedPrivateKey)
            {
                string privateFile = path + ".privl";

                File.WriteAllText(privateFile, clientInfo.SerializePrivateInfoToJson(Encryption.LocalMachine));
                sb.AppendLine($"   Current User-Encrypted Private Key File:  {privateFile}");
            }

            if (generateOptionsBase.CurrentUserEncryptedPrivateKey)
            {
                string privateFile = path + ".privu";

                File.WriteAllText(privateFile, clientInfo.SerializePrivateInfoToJson(Encryption.CurrentUser));
                sb.AppendLine($"   Local Machine-Encrypted Private Key File: {privateFile}");
            }

            Console.WriteLine(sb.ToString());

            // TEMP Test Code
            //var idu = MessageClientIdentity.ImportFromFile(path + ".privu");
            //var idl = MessageClientIdentity.ImportFromFile(path + ".privl");
            //var idj = MessageClientIdentity.ImportFromFile(path + ".priv", "duhh");
        }

        #endregion Methods
    }
}