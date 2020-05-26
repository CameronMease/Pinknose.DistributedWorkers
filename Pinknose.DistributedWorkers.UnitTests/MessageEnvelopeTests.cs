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
using Pinknose.DistributedWorkers.Messages;
using System.Security.Cryptography;

namespace Pinknose.DistributedWorkers.UnitTests
{
    [TestClass]
    internal class MessageEnvelopeTests
    {
        #region Methods

        [TestMethod]
        public void DumdumTest()
        {
            const string serverName = "server";
            CngKey serverKey = CngKey.Create(CngAlgorithm.ECDiffieHellmanP256);
            var server = RabbitMQHelpers.CreateServer(serverName, serverKey, "system");

            var message = new HeartbeatMessage();
            var envelope = MessageEnvelope.WrapMessage(message, server, EncryptionOption.None);

            Assert.AreEqual(envelope.SenderName, serverName);
            //Assert.AreEqual(envelope.SenderNameLength, serverName.Length);
            Assert.AreEqual(1, 2);
        }

        #endregion Methods
    }
}