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
using Pinknose.DistributedWorkers.Clients;
using Pinknose.DistributedWorkers.Configuration;
using Pinknose.DistributedWorkers.Crypto;
using Pinknose.DistributedWorkers.Extensions;
using Pinknose.DistributedWorkers.Logging;
using Pinknose.DistributedWorkers.MessageQueues;
using Pinknose.DistributedWorkers.MessageTags;
using Pinknose.DistributedWorkers.Modules;
using Pinknose.DistributedWorkers.Pushover;
using Pinknose.DistributedWorkers.XBee;
using Pinknose.DistributedWorkers.XBee.Messages;
using Serilog;
using System;
using System.Configuration;
using System.IO;
using System.IO.Ports;
using System.Security.Cryptography;
using XBeeLibrary.Core;
using XBeeLibrary.Core.Connection;
using XBeeLibrary.Windows;
using XBeeLibrary.Windows.Connection.Serial;

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

            using var serverPublicInfo = MessageClientIdentity.ImportFromFile(@"Keys\system-server.pub");
            using var serverPrivateInfo = MessageClientIdentity.ImportFromFile(@"Keys\system-server.priv", "abc");

            using var coordinatorPublicInfo = MessageClientIdentity.ImportFromFile(@"Keys\system-coordinator.pub");
            using var coordinatorPrivateInfo = MessageClientIdentity.ImportFromFile(@"Keys\system-coordinator.priv", "monkey123");

            using var client1PublicInfo = MessageClientIdentity.ImportFromFile(@"Keys\system-client1.pub");
            using var client1PrivateInfo = MessageClientIdentity.ImportFromFile(@"Keys\system-client1.priv", "abc");

            using var client2PublicInfo = MessageClientIdentity.ImportFromFile(@"Keys\system-client2.pub");
            using var client2PrivateInfo = MessageClientIdentity.ImportFromFile(@"Keys\system-client2.priv", "abc");

            using var client3PublicInfo = MessageClientIdentity.ImportFromFile(@"Keys\system-client3.pub");
            using var client3PrivateInfo = MessageClientIdentity.ImportFromFile(@"Keys\system-client3.priv", "abc");

            //using var pushoverPublicInfo = MessageClientIdentity.ImportFromFile(@"Keys\system-pushoverClient.pub");
            //using var pushoverPrivateInfo = MessageClientIdentity.ImportFromFile(@"Keys\system-pushoverClient.priv", "abc");


            Console.WriteLine(serverPublicInfo.IdentityHash);
            Console.WriteLine(client1PublicInfo.IdentityHash);
            Console.WriteLine(client2PublicInfo.IdentityHash);
            Console.WriteLine(client3PublicInfo.IdentityHash);



            var xbeeModule = new XBeeNetworkGatewayModule("COM12", new SerialPortParameters(115200, 8, StopBits.One, Parity.None, Handshake.None));
            var coordinatorModule = new TrustCoordinatorModule(TimeSpan.FromMinutes(1));

            var server = new MessageClientConfigurationBuilder()
                .RabbitMQCredentials(userName, password)
                .RabbitMQServerHostName(rabbitMQServerName)
                .Identity(coordinatorPrivateInfo)
                .AddClientIdentity(client1PublicInfo)
                .AddClientIdentity(client2PublicInfo)
                .AddClientIdentity(client3PublicInfo)
                //.AddClientIdentity(pushoverPublicInfo)
                .AutoDeleteQueuesOnClose(true)
                .QueuesAreDurable(false)
                .AddModule(coordinatorModule)
                .AddModule(xbeeModule)
                .CreateMessageClient();


#if false
            var server = new MessageServerConfigurationBuilder()
                .RabbitMQCredentials(userName, password)
                .RabbitMQServerHostName(rabbitMQServerName)
                .Identity(serverPrivateInfo)
                .AddClientIdentity(client1PublicInfo)
                .AddClientIdentity(client2PublicInfo)
                .AddClientIdentity(client3PublicInfo)
                //.AddClientIdentity(pushoverPublicInfo)
                .AutoDeleteQueuesOnClose(true)
                .QueuesAreDurable(false)
                .CreateMessageServer();
#endif

            server.MessageReceived += (sender, e) =>
            {
                e.Response = MessageResponse.Ack;

                if (e.MessageEnevelope.Message.GetType() == typeof(XBeeFromXBeeMessage))
                {
                    var tempMessage = (XBeeFromXBeeMessage)e.MessageEnevelope.Message;

                    Log.Verbose($"{tempMessage.XBeeSourceAddress}: {tempMessage.RawData}");
                }
            };

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

            server.Connect(TimeSpan.FromSeconds(20));
            sendTimer.Start();

            //Thread.Sleep(30000);

            var odd = new MessageTagValue("duhh", "odd");
            var even = new MessageTagValue("duhh", "even");
            var never = new MessageTagValue("duhh", "never");

            var pushoverModule = new PushoverModule(secrets.PushoverAppApiKey, secrets.PushoverUserKey);
            pushoverModule.AddTransform<IntMessage>(t =>
            {
                if (t.Payload % 10 == 0)
                {
                    return null; // (t.Payload.ToString());
                }

                return null;
            });




            // TODO: Not all configuration options return the right configuration builder type
#if false
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
            pushoverClient.Connect(20000);
#endif

            MessageClient client1 = new MessageClientConfigurationBuilder()
                .RabbitMQCredentials(userName, password)
                .RabbitMQServerHostName(rabbitMQServerName)
                .TrustCoordinatorIdentity(coordinatorPublicInfo)
                .Identity(client1PrivateInfo)
                .AutoDeleteQueuesOnClose(true)
                .QueuesAreDurable(false)
                .AddModule(pushoverModule)
                .CreateMessageClient();

            xbeeModule.OpenXBee();

            client1.AsynchronousException += Client_AsynchronousException;

            client1.MessageReceived += (sender, e) =>
            {
                e.Response = MessageResponse.Ack;

                if (e.MessageEnevelope.Message.GetType() == typeof(IntMessage))
                {
                    Console.WriteLine($"Client 1: Message Payload: {((IntMessage)e.MessageEnevelope.Message).Payload}.");
                }
            };
            client1.Connect(TimeSpan.FromSeconds(10), odd, new XBeeReceivedDataTag());
            client1.BeginFullWorkConsume(true);

            MessageClient client2 = new MessageClientConfigurationBuilder()
                .RabbitMQCredentials(userName, password)
                .RabbitMQServerHostName(rabbitMQServerName)
                .TrustCoordinatorIdentity(coordinatorPublicInfo)
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
                else if (e.MessageEnevelope.Message.GetType() == typeof(XBeeFromXBeeMessage))
                {
                    var tempMessage = (XBeeFromXBeeMessage)e.MessageEnevelope.Message;

                    Console.WriteLine($"Client 2 XBee ({tempMessage.XBeeSourceAddress}): {tempMessage.RawData}");
                }
            };
            client2.Connect(TimeSpan.FromSeconds(10), even, new XBeeReceivedDataTag());
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