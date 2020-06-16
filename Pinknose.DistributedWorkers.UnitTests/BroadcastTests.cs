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
using Pinknose.DistributedWorkers.MessageTags;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Pinknose.DistributedWorkers.UnitTests
{
    [TestClass]
    public class BroadcastTests
    {
        #region Methods

        [TestMethod]
        public void BroadcastUnitTest()
        {
            MessageServer server;
            IEnumerable<MessageClient> clients;

            bool client1GotMessage = false;
            bool client2GotMessage = false;
            bool serverGotMessage = false;

            (server, clients) = TestHelpers.CreateClientsAndServer(nameof(BroadcastUnitTest), "client1", "client2");

            var client1 = clients.Single(c => c.ClientName == "client1");
            var client2 = clients.Single(c => c.ClientName == "client2");

            client1.MessageReceived += (sender, e) =>
            {
                client1GotMessage = e.MessageEnevelope.Message.GetType() == typeof(StringMessage);
            };

            client2.MessageReceived += (sender, e) =>
            {
                client2GotMessage = e.MessageEnevelope.Message.GetType() == typeof(StringMessage);
            };

            server.MessageReceived += (sender, e) =>
            {
                serverGotMessage = e.MessageEnevelope.Message.GetType() == typeof(StringMessage);
            };

            server.Connect(TimeSpan.FromSeconds(10));
            client1.Connect(TimeSpan.FromSeconds(10), SystemTags.SerilogAllEvents);
            client2.Connect(TimeSpan.FromSeconds(10));

            Assert.IsFalse(client1GotMessage);
            Assert.IsFalse(client2GotMessage);
            Assert.IsFalse(serverGotMessage);

            server.BroadcastToAllClients(new StringMessage("test"), EncryptionOption.None);

            Thread.Sleep(100);

            Assert.IsTrue(client1GotMessage);
            Assert.IsTrue(client2GotMessage);
            Assert.IsFalse(serverGotMessage);
        }

        #endregion Methods
    }
}