using CommandLine;
using Pinknose.DistributedWorkers.Clients;
using Pinknose.KeyUtility.CommandLineOptions;
using System;
using System.IO;

namespace Pinknose.KeyUtility
{
    class Program
    {
        static void Main(string[] args)
        {
            GenerateClientOptions generateClientOptions = null;
            GenerateServerOptions generateServerOptions = null;

            CommandLine.Parser.Default.ParseArguments<GenerateClientOptions, GenerateServerOptions>(args)
                .WithParsed<GenerateClientOptions>(opts => generateClientOptions = opts)
                .WithParsed<GenerateServerOptions>(opts => generateServerOptions = opts)
                .WithNotParsed(errors => Environment.Exit(-1));


            if (generateClientOptions != null || generateServerOptions != null)
            {
                MessageClientInfo clientInfo = null;
                GenerateOptionsBase generateOptionsBase = generateClientOptions != null ? (GenerateOptionsBase)generateClientOptions: (GenerateOptionsBase)generateServerOptions;

                if (string.IsNullOrEmpty(generateOptionsBase.Directory))
                {
                    generateOptionsBase.Directory = Directory.GetCurrentDirectory();
                }
                else
                {
                    if (Directory.Exists(generateOptionsBase.Directory))
                    {
                        Console.WriteLine($"{generateOptionsBase.Directory} is not a valid directory.");
                    }
                }

                ECDiffieHellmanCurve curve = ECDiffieHellmanCurve.P256;
                string json;

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
                    clientInfo = MessageClientInfo.CreateClientInfo(generateClientOptions.SystemName, generateClientOptions.ClientName, curve, true);
                }
                else if (generateServerOptions != null)
                {
                    clientInfo = MessageClientInfo.CreateServerInfo(generateServerOptions.SystemName, curve, true);
                }

                string path = Path.Combine(generateOptionsBase.Directory, clientInfo.SystemName + "-" + clientInfo.Name);

                bool encrypted = false;

                Console.Write("Do you want to encrypt the private key (highly recommended!)? (Y/N): ");

                ConsoleKeyInfo keyInfo;

                do
                {
                    keyInfo = Console.ReadKey();
                } while (keyInfo.Key != ConsoleKey.Y && keyInfo.Key != ConsoleKey.N);

                Console.WriteLine();

                string password = "";
                bool match = false;

                if (keyInfo.Key == ConsoleKey.Y)
                {
                    encrypted = true;

                    do      
                    {
                        do
                        {
                            Console.Write("Enter password: ");
                            password = ConsolePasswordReader.ReadPassword();
                        } while (string.IsNullOrWhiteSpace(password));

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
                }

                string privateFile = path + ".priv";
                string publicFile = path + ".pub"; 

                Console.WriteLine("\nCreating Client Key");
                Console.WriteLine($"   Diffie-Hellman Elliptic Curve: {curve}");
                Console.WriteLine($"   System Name:                   {clientInfo.SystemName}");
                Console.WriteLine($"   Client Name:                   {clientInfo.Name}");
                Console.WriteLine($"   Encrypted Private Key:         {encrypted}");
                Console.WriteLine($"   Public Key File:               {publicFile}");
                Console.WriteLine($"   Private Key File:              {privateFile}");

                File.WriteAllText(privateFile, clientInfo.SerializePrivateInfoToJson(password));
                File.WriteAllText(publicFile, clientInfo.SerializePublicInfoToJson());

                var duhh = MessageClientInfo.Import(privateFile, password);
                var duhh1 = MessageClientInfo.Import(publicFile);
            }
        }
    }
}
