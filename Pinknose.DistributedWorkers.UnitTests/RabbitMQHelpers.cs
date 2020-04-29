using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pinknose.DistributedWorkers.UnitTests
{
    public static class RabbitMQHelpers
    {
        public static IConnection GetConnection()
        {
            ConnectionFactory factory = new ConnectionFactory();

            factory.UserName = Properties.Resources.Username;
            factory.Password = Properties.Resources.Password;
            factory.HostName = Properties.Resources.RabbitMQServerName;

            IConnection conn = factory.CreateConnection();

            return conn;
        }
    }
}
