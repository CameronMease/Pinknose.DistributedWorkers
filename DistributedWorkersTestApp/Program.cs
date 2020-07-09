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

using EasyNetQ.Management.Client.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Pinknose.DistributedWorkers.Clients;
using Pinknose.DistributedWorkers.Configuration;
using Pinknose.DistributedWorkers.Crypto;
using Pinknose.DistributedWorkers.Extensions;
using Pinknose.DistributedWorkers.Logging;
using Pinknose.DistributedWorkers.MessageQueues;
using Pinknose.DistributedWorkers.MessageTags;
using Pinknose.DistributedWorkers.Pushover;
using Serilog;
using System;
using System.Configuration;
using System.IO;
using System.Security.Cryptography;

namespace DistributedWorkersTestApp
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // Get secrets
            var devEnvironmentVariable = Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT");
            var isDevelopment = string.IsNullOrEmpty(devEnvironmentVariable) || devEnvironmentVariable.ToLower() == "development";

            string secretsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                @"Microsoft\UserSecrets\9a735c2c-ec7a-4c5a-936e-7210fc978f5d",
                "secrets.json");

            var secrets = JsonConvert.DeserializeObject<UserSecrets>(File.ReadAllText(secretsPath));

            // Do everything else

            int val = 0;

            string systemName = "aSystem";
            string rabbitMQServerName = "garage";
            string userName = "test";
            string password = "test";

            var duhh1 = AesCng.Create();


            CngProvider provider = new CngProvider("dumdum");


            var random = new Random();

            UInt32 key = random.NextUInt32();
            UInt32 iv = random.NextUInt32();
            (var cipherText, var signature) = SimpleCBC.Encode("Hi", key, iv, 2);

            var sig = BitConverter.GetBytes(signature);

            var duhh = System.Text.Encoding.UTF8.GetString(cipherText);
            var message = SimpleCBC.Decode(cipherText, key, iv, 2);


            var errorTag = new MessageTagValue("Severity", "Error");
            var infoTag = new MessageTagValue("Severity", "Info");
            var debugTag = new MessageTagValue("Severity", "Debug");

            using var serverPublicInfo = MessageClientIdentity.ImportFromFile(@"keys\system-server.pub");
            using var serverPrivateInfo = MessageClientIdentity.ImportFromFile(@"keys\system-server.priv", "monkey123");

            using var client1PublicInfo = MessageClientIdentity.ImportFromFile(@"keys\system-client1.pub");
            using var client1PrivateInfo = MessageClientIdentity.ImportFromFile(@"keys\system-client1.priv");

            using var client2PublicInfo = MessageClientIdentity.ImportFromFile(@"keys\system-client2.pub");
            using var client2PrivateInfo = MessageClientIdentity.ImportFromFile(@"keys\system-client2.priv");

            using var client3PublicInfo = MessageClientIdentity.ImportFromFile(@"keys\system-client3.pub");
            using var client3PrivateInfo = MessageClientIdentity.ImportFromFile(@"keys\system-client3.priv");

            using var pushoverPublicInfo = MessageClientIdentity.ImportFromFile(@"keys\system-pushoverClient.pub");
            using var pushoverPrivateInfo = MessageClientIdentity.ImportFromFile(@"keys\system-pushoverClient.priv");


            Console.WriteLine(serverPublicInfo.IdentityHash);
            Console.WriteLine(client1PublicInfo.IdentityHash);
            Console.WriteLine(client2PublicInfo.IdentityHash);
            Console.WriteLine(client3PublicInfo.IdentityHash);

            var server = new MessageServerConfigurationBuilder()
                .RabbitMQCredentials(userName, password)
                .RabbitMQServerHostName(rabbitMQServerName)
                .Identity(serverPrivateInfo)
                .AddClientIdentity(client1PublicInfo)
                .AddClientIdentity(client2PublicInfo)
                .AddClientIdentity(client3PublicInfo)
                .AddClientIdentity(pushoverPublicInfo)
                .AutoDeleteQueuesOnClose(true)
                .QueuesAreDurable(false)
                .CreateMessageServer();


            server.MessageReceived += (sender, e) => e.Response = MessageResponse.Ack;
            server.AsynchronousException += Client_AsynchronousException;

            System.Timers.Timer sendTimer = new System.Timers.Timer(1000)
            {
                AutoReset = true
            };

            sendTimer.Elapsed += (sender, e) =>
            {
                IntMessage message = new IntMessage(val);

                string tag = val % 2 == 0 ? "even" : "odd";

                server.WriteToSubscriptionQueues(message, true, new MessageTagValue("duhh", tag), errorTag, infoTag);
                val++;

                //server.BroacastToAllClients(message);
            };

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.DistributedWorkersSink(server)
                .WriteTo.Console()
                .CreateLogger();

            //Log.Verbose($"Server public string: {serverInfo.PublicKey.PublicKeyToString()}");

            server.Connect(TimeSpan.FromSeconds(10));
            sendTimer.Start();

            //Thread.Sleep(30000);

            var odd = new MessageTagValue("duhh", "odd");
            var even = new MessageTagValue("duhh", "even");
            var never = new MessageTagValue("duhh", "never");

            // TODO: Not all configuration options return the right configuration builder type

            var pushoverClient = new PushoverMessageClientConfigurationBuilder()
                .PushoverAppApiKey(secrets.PushoverAppApiKey)
                .PushoverUserKey(secrets.PushoverUserKey)
                .RabbitMQCredentials(userName, password)
                .RabbitMQServerHostName(rabbitMQServerName)
                .ServerIdentity(serverPublicInfo)
                .Identity(pushoverPrivateInfo)
                .AutoDeleteQueuesOnClose(true)
                .QueuesAreDurable(false)
                .CreateMessageClient();


            pushoverClient.AddTransform<IntMessage>(t =>
            {
                if (t.Payload % 10 == 0)
                {
                    return null; // (t.Payload.ToString());
                }

                return null;
            });

            pushoverClient.AsynchronousException += Client_AsynchronousException;
            pushoverClient.Connect(10000);

            MessageClient client1 = new MessageClientConfigurationBuilder()
                .RabbitMQCredentials(userName, password)
                .RabbitMQServerHostName(rabbitMQServerName)
                .ServerIdentity(serverPublicInfo)
                .Identity(client1PrivateInfo)
                .AutoDeleteQueuesOnClose(true)
                .QueuesAreDurable(false)
                .CreateMessageClient();

            client1.AsynchronousException += Client_AsynchronousException;

            client1.MessageReceived += (sender, e) =>
            {
                e.Response = MessageResponse.Ack;

                if (e.MessageEnevelope.Message.GetType() == typeof(IntMessage))
                {
                    Console.WriteLine($"Client 1: Message Payload: {((IntMessage)e.MessageEnevelope.Message).Payload}.");
                }
            };
            //client1.Connect(TimeSpan.FromSeconds(10), odd);
            //client1.BeginFullWorkConsume(true);

            MessageClient client2 = new MessageClientConfigurationBuilder()
                .RabbitMQCredentials(userName, password)
                .RabbitMQServerHostName(rabbitMQServerName)
                .ServerIdentity(serverPublicInfo)
                .Identity(client2PrivateInfo)
                .AutoDeleteQueuesOnClose(true)
                .QueuesAreDurable(false)
                .CreateMessageClient();

            client2.AsynchronousException += Client_AsynchronousException;

            client2.MessageReceived += (sender, e) =>
            {
                e.Response = MessageResponse.Ack;
                if (e.MessageEnevelope.Message.GetType() == typeof(IntMessage))
                {
                    Console.WriteLine($"Client 2: Message Payload: {((IntMessage)e.MessageEnevelope.Message).Payload}.");
                }
            };
            //client2.Connect(TimeSpan.FromSeconds(10), even);
            //client2.BeginFullWorkConsume(true);

#if false

            MessageClient client3 = new MessageClientConfiguration()
                .RabbitMQCredentials(userName, password)
                .RabbitMQServer(rabbitMQServerName)
                .ServerInfo(serverInfo)
                .ClientInfo(client3Info)
                .AutoDeleteQueuesOnClose(true)
                .QueuesAreDurable(false)
                .CreateMessageClient();
            client3.MessageReceived += (sender, e) =>
            {
                e.Response = MessageResponse.Ack;
                Console.WriteLine($"Client 3: Message Payload: {((IntMessage)e.MessageEnevelope.Message).Payload}.");
            };

            client3.BeginFullWorkConsume(true);

#endif

            //
            //client3.Connect(TimeSpan.FromSeconds(10));

            Log.Information("Dumdum");

            Console.ReadKey();
        }

        private static void Client_AsynchronousException(object sender, Pinknose.DistributedWorkers.Exceptions.AsynchronousExceptionEventArgs e)
        {
            Log.Warning(e.Exception, $"Asynchronous Exception from {((MessageClientBase)sender).ClientName}");
        }
    }
}