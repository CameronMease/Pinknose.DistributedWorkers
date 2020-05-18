using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pinknose.DistributedWorkers.Messages;
using Pinknose.DistributedWorkers.MessageTags;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Pinknose.DistributedWorkers.UnitTests
{
    [TestClass]
    public class SubscriptionTests
    {
        public readonly MessageTag Tag1 = new MessageTag("Tag1");
        public readonly MessageTag Tag2 = new MessageTag("Tag2");
        public readonly MessageTagValue Tag2a = new MessageTagValue("Tag2", "a");
        public readonly MessageTagValue Tag2b = new MessageTagValue("Tag2", "b");

        [TestMethod]
        public void ReceiveAllMessagesTest()
        {
            bool gotTag1 = false;
            bool gotTag2a = false;
            bool gotTag2b = false;

            using var server = RabbitMQHelpers.CreateServer(
                "server",
                CngKey.Create(CngAlgorithm.ECDsaP256));

            //server.Start();

            using var sender = RabbitMQHelpers.CreateClient(
                "sender",
                CngKey.Create(CngAlgorithm.ECDsaP256));

            using var receiver = RabbitMQHelpers.CreateClient(
                nameof(ReceiveAllMessagesTest),
                CngKey.Create(CngAlgorithm.ECDsaP256));

            receiver.MessageReceived += (sender, e) =>
            {
                if (e.Message.Tags.Any(t => t == Tag1))
                {
                    gotTag1 = true;
                }
                else if (e.Message.Tags.Any(t => t == Tag2a))
                {
                    gotTag2a = true;
                }
                else if (e.Message.Tags.Any(t => t == Tag2b))
                {
                    gotTag2b = true;
                }

                e.Response = MessageResponse.Ack;
            };

            var message = new HeartbeatMessage(false, Tag1);
            //sender.WriteToBoundExchange(message);
            message = new HeartbeatMessage(false, Tag2a);
            //sender.SubscriptionQueue.WriteToBoundExchange(message);
            message = new HeartbeatMessage(false, Tag2b);
            //sender.SubscriptionQueue.WriteToBoundExchange(message);

            Thread.Sleep(10000);
            Assert.IsTrue(gotTag1);
            Assert.IsTrue(gotTag2a);
            Assert.IsTrue(gotTag2b);
        }
    }
}
