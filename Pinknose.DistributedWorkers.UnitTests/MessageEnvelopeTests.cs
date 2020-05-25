using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pinknose.DistributedWorkers.Messages;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Pinknose.DistributedWorkers.UnitTests
{
    [TestClass]
    class MessageEnvelopeTests
    {
        [TestMethod]
        public void DumdumTest()
        {
            const string serverName = "server";
            CngKey serverKey = CngKey.Create(CngAlgorithm.ECDiffieHellmanP256);
            var server = RabbitMQHelpers.CreateServer(serverName, serverKey, "system");

            var message = new HeartbeatMessage();
            var envelope = MessageEnvelope.WrapMessage(message, server, EncryptionOption.None);

            Assert.AreEqual(envelope.SenderName, serverName);
            Assert.AreEqual(envelope.SenderNameLength, serverName.Length);
            Assert.AreEqual(1, 2);
        }
    }
}
