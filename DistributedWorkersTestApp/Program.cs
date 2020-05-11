using Pinknose.DistributedWorkers;
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
            using var serverKey = CngKey.Create(CngAlgorithm.ECDsaP256);
            using var client1Key = CngKey.Create(CngAlgorithm.ECDsaP256);
            using var client2Key = CngKey.Create(CngAlgorithm.ECDsaP256);
            using var client3Key = CngKey.Create(CngAlgorithm.ECDsaP256);

            var server= new MessageServer(
                "server",
                systemName,
                serverName,
                serverKey,
                "guest",
                "guest");

            server.SubscriptionQueue.MessageReceived += (sender, e) => e.Response = MessageResponse.Ack;

            System.Timers.Timer sendTimer = new System.Timers.Timer(1000)
            {
                AutoReset = true
            };

            sendTimer.Elapsed += (sender, e) =>
            {
                IntMessage message = new IntMessage(val);
                //server.WorkQueue.Write(message);

                string tag = val % 2 == 0 ? "even" : "odd";

                server.SubscriptionQueue.WriteToBoundExchange(message, new MessageTagValue("duhh", tag));
                val++;

                //server.BroacastToAllClients(message);
            };

            server.Start();
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
            client1.WorkQueue.MessageReceived += (sender, e) => Console.WriteLine($"Client 1: Message Payload: {((IntMessage)e.Message).Payload}.");
            //client1.DedicatedQueue.MessageReceived += (sender, e) => Console.WriteLine($"Client 1: Dedicated message received.");
            client1.SubscriptionQueue.MessageReceived += (sender, e) =>
            {
                e.Response = MessageResponse.Ack;
                Console.WriteLine($"Client 1: Message Payload: {((IntMessage)e.Message).Payload}.");
            };
            client1.WorkQueue.BeginFullConsume(true);

            MessageClient client2 = new MessageClient(
                "client2",
                systemName,
                serverName,
                client2Key,
                "guest",
                "guest",
                even);
            client2.WorkQueue.MessageReceived += (sender, e) => Console.WriteLine($"Client 2: Message Payload: {((IntMessage)e.Message).Payload}.");
            //client2.DedicatedQueue.MessageReceived += (sender, e) => Console.WriteLine($"Client 2: Dedicated message received.");
            client2.SubscriptionQueue.MessageReceived += (sender, e) =>
            {
                e.Response = MessageResponse.Ack;
                Console.WriteLine($"Client 2: Message Payload: {((IntMessage)e.Message).Payload}.");
            };
            client2.WorkQueue.BeginFullConsume(true);

            MessageClient client3 = new MessageClient(
                "client3",
                systemName,
                serverName,
                client3Key,
                "guest",
                "guest",
                odd | even);
            client3.WorkQueue.MessageReceived += (sender, e) => Console.WriteLine($"Client 3: Message Payload: {((IntMessage)e.Message).Payload}.");
            //client3.DedicatedQueue.MessageReceived += (sender, e) => Console.WriteLine($"Client 3: Dedicated message received.");
            client3.SubscriptionQueue.MessageReceived += (sender, e) =>
            {
                e.Response = MessageResponse.Ack;
                Console.WriteLine($"Client 3: Message Payload: {((IntMessage)e.Message).Payload}.");
            };

            client3.WorkQueue.BeginFullConsume(true);

            Console.ReadKey();
        }

        
    }
}
