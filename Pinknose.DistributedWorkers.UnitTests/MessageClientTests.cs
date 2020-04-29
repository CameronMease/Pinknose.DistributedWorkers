using Microsoft.VisualStudio.TestTools.UnitTesting;
using RabbitMQ.Client;

namespace Pinknose.DistributedWorkers.UnitTests
{
    [TestClass]
    public class MessageClientTests
    {
        [TestMethod]
        public void Duhh()
        {
            MessageServer server = new MessageServer(
                "Server",
                Properties.Resources.SystemName,
                Properties.Resources.RabbitMQServerName,
                Properties.Resources.Username,
                Properties.Resources.Password);

            server.Start();

            MessageClient client = new MessageClient(
                "Client1",
                Properties.Resources.SystemName,
                Properties.Resources.RabbitMQServerName,
                Properties.Resources.Username,
                Properties.Resources.Password);

            IConnection connection = RabbitMQHelpers.GetConnection();
        }
    }
}
