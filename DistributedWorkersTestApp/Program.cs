using Pinknose.DistributedWorkers;
using Pinknose.DistributedWorkers.Extensions;
using Pinknose.DistributedWorkers.Logging;
using Pinknose.DistributedWorkers.MessageTags;
using Serilog;
using System;
using System.Security.Cryptography;
using System.Threading;
using System.Timers;

namespace DistributedWorkersTestApp
{
    class Program
    {
        enum dumdum { a, v, c}

        static void Main(string[] args)
        {
            int val = 0;

            string systemName = "aSystem";
            string serverName = "localhost";




            using var serverKey = CngKey.Create(CngAlgorithm.ECDiffieHellmanP521);
            using var client1Key = CngKey.Create(CngAlgorithm.ECDiffieHellmanP521);
            using var client2Key = CngKey.Create(CngAlgorithm.ECDiffieHellmanP521);
            using var client3Key = CngKey.Create(CngAlgorithm.ECDiffieHellmanP521);

            

            var server= new MessageServer(
                "server",
                systemName,
                serverName,
                serverKey,
                "guest",
                "guest");

            server.MessageReceived += (sender, e) => e.Response = MessageResponse.Ack;

            

            System.Timers.Timer sendTimer = new System.Timers.Timer(1000)
            {
                AutoReset = true
            };

            sendTimer.Elapsed += (sender, e) =>
            {
                IntMessage message = new IntMessage(val);
                
                string tag = val % 2 == 0 ? "even" : "odd";
                message.Tags.Add(new MessageTagValue("duhh", tag));

                server.WriteToSubscriptionQueues(message);
                val++;

                //server.BroacastToAllClients(message);
            };

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.DistributedWorkersSink(server)
                .WriteTo.Console()
                .CreateLogger();

            Log.Verbose($"Server public string: {serverKey.PublicKeyToString()}");

            server.Connect(TimeSpan.FromSeconds(10));
            sendTimer.Start();

            //Thread.Sleep(30000);

            var odd = new MessageTagValue("duhh", "odd");
            var even = new MessageTagValue("duhh", "even");
            var never = new MessageTagValue("duhh", "never");

            MessageClient client1 = new MessageClient(
                "client1",
                systemName,
                serverName,
                client1Key,
                "guest",
                "guest",
                odd);

            client1.MessageReceived += (sender, e) =>
            {
                e.Response = MessageResponse.Ack;
                Console.WriteLine($"Client 1: Message Payload: {((IntMessage)e.Message).Payload}.");
            };
            client1.BeginFullWorkConsume(true);

            MessageClient client2 = new MessageClient(
                "client2",
                systemName,
                serverName,
                client2Key,
                "guest",
                "guest",
                even);
            client2.MessageReceived += (sender, e) =>
            {
                e.Response = MessageResponse.Ack;
                Console.WriteLine($"Client 2: Message Payload: {((IntMessage)e.Message).Payload}.");
            };
            client2.BeginFullWorkConsume(true);

            MessageClient client3 = new MessageClient(
                "client3",
                systemName,
                serverName,
                client3Key,
                "guest",
                "guest",
                odd | even | SystemTags.SerilogFatalEvent);
            client3.MessageReceived += (sender, e) =>
            {
                e.Response = MessageResponse.Ack;
                Console.WriteLine($"Client 3: Message Payload: {((IntMessage)e.Message).Payload}.");
            };

            client3.BeginFullWorkConsume(true);
            
            client1.Connect(TimeSpan.FromSeconds(10));
            //client2.Connect(TimeSpan.FromSeconds(10));
            //client3.Connect(TimeSpan.FromSeconds(10));


            Log.Information("Dumdum");

            Console.ReadKey();
        }        
    }
}
