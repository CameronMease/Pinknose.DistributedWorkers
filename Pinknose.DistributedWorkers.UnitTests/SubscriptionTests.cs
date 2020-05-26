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
using Pinknose.DistributedWorkers.MessageTags;

namespace Pinknose.DistributedWorkers.UnitTests
{
    [TestClass]
    public class SubscriptionTests
    {
        public readonly MessageTag Tag1 = new MessageTag("Tag1");
        public readonly MessageTag Tag2 = new MessageTag("Tag2");
        public readonly MessageTagValue Tag2a = new MessageTagValue("Tag2", "a");
        public readonly MessageTagValue Tag2b = new MessageTagValue("Tag2", "b");

#if false
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
#endif
    }
}