using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pinknose.DistributedWorkers.Clients;
using Pinknose.DistributedWorkers.Configuration;
using Pinknose.DistributedWorkers.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pinknose.DistributedWorkers.UnitTests
{
    [TestClass]
    public class IdentityTests
    {
        [TestMethod]
        public void IdentityInvalidUnitTest()
        {
            var clientIdent = new MessageClientIdentity("system", "client", ECDiffieHellmanCurve.P256);
            var imposterClientIdent = new MessageClientIdentity("system", "client", ECDiffieHellmanCurve.P256);
            //TODO: WHere to get server name from?
            var serverIdent = new MessageClientIdentity("system", "Server", ECDiffieHellmanCurve.P256);

            var server = new MessageServerConfigurationBuilder()
                .AddClientIdentity(clientIdent)
                .AutoDeleteQueuesOnClose(true)
                .QueuesAreDurable(false)
                .Identity(serverIdent)
                .CreateMessageServer();

            server.Connect(1000);

            var client = new MessageClientConfigurationBuilder()
                .Identity(imposterClientIdent)
                .ServerIdentity(serverIdent)
                .QueuesAreDurable(false)
                .AutoDeleteQueuesOnClose(true)
                .CreateMessageClient();

            bool identityExceptionCaught = false;

            server.AsynchronousException += (sender, e) =>
            {
                if (e.Exception.GetType() == typeof(IdentityException))
                {
                    identityExceptionCaught = true;
                }
            };

            Pinknose.Utilities.CatchExceptionHelper.VerifyExceptionCaught<ConnectionException>(() => client.Connect(5000));
            Assert.IsTrue(identityExceptionCaught);
            
            // TODO:  Need the server to respond back to client saying the identity is invalid.
            Assert.IsTrue(false);
        }
    }
}
