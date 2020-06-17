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
                    if (Directory.Exists(generateClientOptions.Directory))
                    {
                        Console.WriteLine($"{generateClientOptions.Directory} is not a valid directory.");
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
                else if (generateOptionsBase.KeySize512)
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


                File.WriteAllText(path + ".priv", clientInfo.SerializePrivateInfoToJson(password));
                File.WriteAllText(path + ".pub", clientInfo.SerializePublicInfoToJson());               

            }
        }
    }
}
