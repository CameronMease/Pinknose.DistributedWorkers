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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pinknose.DistributedWorkers.Clients;
using Pinknose.DistributedWorkers.Configuration;
using Pinknose.DistributedWorkers.Messages;
using Pinknose.DistributedWorkers.MessageTags;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
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
        public readonly MessageTag Tag3 = new MessageTag("Tag3");


        /// <summary>
        /// Tests that all subscribed messages are received.  Unsubscribed messages are not received;
        /// </summary>
        [TestMethod]
        public void ReceiveAllSubscribedMessagesTest()
        {
            bool gotTag1 = false;
            bool gotTag2a = false;
            bool gotTag2b = false;
            bool gotTag3 = false;

            MessageServer server;
            IEnumerable<MessageClient> clients;

            (server, clients) = TestHelpers.CreateClientsAndServer(nameof(ReceiveAllSubscribedMessagesTest), "receiver");

            var receiver = clients.Single(c => c.ClientName == "receiver");

            receiver.MessageReceived += (sender, e) =>
            {
                if (e.MessageEnevelope.Tags.Any(t => t == Tag1))
                {
                    gotTag1 = true;
                }
                else if (e.MessageEnevelope.Tags.Any(t => t == Tag2a))
                {
                    gotTag2a = true;
                }
                else if (e.MessageEnevelope.Tags.Any(t => t == Tag2b))
                {
                    gotTag2b = true;
                }
                else if (e.MessageEnevelope.Tags.Any(t => t == Tag3))
                {
                    gotTag3 = true;
                }

                //e.Response = MessageResponse.Ack;
            };

            server.Connect(1000);
            receiver.Connect(1000, Tag1, Tag2a, Tag2b, Tag3);

            Assert.IsFalse(gotTag1);
            Assert.IsFalse(gotTag2a);
            Assert.IsFalse(gotTag2b);
            Assert.IsFalse(gotTag3);


            var message = new StringMessage("Test");
            server.WriteToSubscriptionQueues(message, false, Tag1);
            server.WriteToSubscriptionQueues(message, false, Tag2a);
            server.WriteToSubscriptionQueues(message, false, Tag2b);
            server.WriteToSubscriptionQueues(message, false, Tag3);

            Thread.Sleep(1000);
            Assert.IsTrue(gotTag1);
            Assert.IsTrue(gotTag2a);
            Assert.IsTrue(gotTag2b);
            Assert.IsTrue(gotTag3);
        }

        [TestMethod]
        public void ReceiveAllMessagesTest()
        {
            bool gotTag1 = false;
            bool gotTag2a = false;
            bool gotTag2b = false;
            bool gotTag3 = false;

            MessageServer server;
            IEnumerable<MessageClient> clients;

            (server, clients) = TestHelpers.CreateClientsAndServer(nameof(ReceiveAllMessagesTest), "receiver");

            var receiver = clients.Single(c => c.ClientName == "receiver");

            receiver.MessageReceived += (sender, e) =>
            {
                if (e.MessageEnevelope.Tags.Any(t => t == Tag1))
                {
                    gotTag1 = true;
                }
                else if (e.MessageEnevelope.Tags.Any(t => t == Tag2a))
                {
                    gotTag2a = true;
                }
                else if (e.MessageEnevelope.Tags.Any(t => t == Tag2b))
                {
                    gotTag2b = true;
                }
                else if (e.MessageEnevelope.Tags.Any(t => t == Tag3))
                {
                    gotTag3 = true;
                }

                //e.Response = MessageResponse.Ack;
            };

            server.Connect(1000);
            receiver.Connect(1000);

            Assert.IsFalse(gotTag1);
            Assert.IsFalse(gotTag2a);
            Assert.IsFalse(gotTag2b);
            Assert.IsFalse(gotTag3);


            var message = new StringMessage("Test");
            server.WriteToSubscriptionQueues(message, false, Tag1);
            server.WriteToSubscriptionQueues(message, false, Tag2a);
            server.WriteToSubscriptionQueues(message, false, Tag2b);
            server.WriteToSubscriptionQueues(message, false, Tag3);

            Thread.Sleep(1000);
            Assert.IsTrue(gotTag1);
            Assert.IsTrue(gotTag2a);
            Assert.IsTrue(gotTag2b);
            Assert.IsTrue(gotTag3);
        }

        [TestMethod]
        public void ReceiveSubscribedMessagesTest()
        {
            bool gotTag1 = false;
            bool gotTag2a = false;
            bool gotTag2b = false;
            bool gotTag3 = false;

            MessageServer server;
            IEnumerable<MessageClient> clients;

            (server, clients) = TestHelpers.CreateClientsAndServer(nameof(ReceiveSubscribedMessagesTest), "receiver");

            var receiver = clients.Single(c => c.ClientName == "receiver");

            receiver.MessageReceived += (sender, e) =>
            {
                if (e.MessageEnevelope.Tags.Any(t => t == Tag1))
                {
                    gotTag1 = true;
                }
                else if (e.MessageEnevelope.Tags.Any(t => t == Tag2a))
                {
                    gotTag2a = true;
                }
                else if (e.MessageEnevelope.Tags.Any(t => t == Tag2b))
                {
                    gotTag2b = true;
                }
                else if (e.MessageEnevelope.Tags.Any(t => t == Tag3))
                {
                    gotTag3 = true;
                }

                //e.Response = MessageResponse.Ack;
            };

            server.Connect(1000);
            receiver.Connect(1000, Tag1, Tag2a);

            Assert.IsFalse(gotTag1);
            Assert.IsFalse(gotTag2a);
            Assert.IsFalse(gotTag2b);
            Assert.IsFalse(gotTag3);


            var message = new StringMessage("Test");
            server.WriteToSubscriptionQueues(message, false, Tag1);
            server.WriteToSubscriptionQueues(message, false, Tag2a);
            server.WriteToSubscriptionQueues(message, false, Tag2b);
            server.WriteToSubscriptionQueues(message, false, Tag3);

            Thread.Sleep(1000);
            Assert.IsTrue(gotTag1);
            Assert.IsTrue(gotTag2a);
            Assert.IsFalse(gotTag2b);
            Assert.IsFalse(gotTag3);
        }
    }
}