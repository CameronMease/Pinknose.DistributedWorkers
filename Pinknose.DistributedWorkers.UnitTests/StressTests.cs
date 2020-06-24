using EasyNetQ.Management.Client.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pinknose.DistributedWorkers.Clients;
using Pinknose.DistributedWorkers.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pinknose.DistributedWorkers.UnitTests
{
    [TestClass]
    public class StressTests
    {
        [TestMethod]
        public void SingleReceiverStressTest()
        {
            const int messageCountTarget = 50000;
            const int msBetweenSends = 10;


            var waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

            MessageServer server;
            IEnumerable<MessageClient> clients;

            (server, clients) = TestHelpers.CreateClientsAndServer(nameof(SingleReceiverStressTest), "receiver");

            var receiver = clients.Single(c => c.ClientName == "receiver");

            var lockObject = new object();
            int sendCount = 0;
            int receiveCount = 0;

            receiver.MessageReceived += (sender, e) =>
            {
                lock (lockObject)
                {
                    receiveCount++;

                    if (receiveCount == messageCountTarget)
                    {
                        waitHandle.Set();
                    }
                }

                e.Response = MessageQueues.MessageResponse.Ack;               
            };

            server.Connect(1000);
            receiver.Connect(1000);

            var message = new StringMessage("test");


            var receiveStopwatch = new Stopwatch();
            var sendStopwatch = new Stopwatch();
            receiveStopwatch.Start();
            sendStopwatch.Start();

            for (int i = 0; i < messageCountTarget; i++)
            {
                server.WriteToClientNoWait(receiver.Identity,  message, true);
                sendCount++;
                //Thread.Sleep(msBetweenSends);
            }

            sendStopwatch.Stop();
            waitHandle.WaitOne();
            receiveStopwatch.Stop();

            Debug.WriteLine($"Send time: {sendStopwatch.Elapsed}");
            Debug.WriteLine($"Receive time: {receiveStopwatch.Elapsed}");
            Debug.WriteLine($"Messages per second: {receiveCount / receiveStopwatch.Elapsed.TotalSeconds}");
            Assert.AreEqual(sendCount, receiveCount);
        }
    }
}
