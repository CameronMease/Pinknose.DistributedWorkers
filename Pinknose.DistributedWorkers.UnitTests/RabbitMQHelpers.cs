using Pinknose.DistributedWorkers.MessageTags;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
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

        public static MessageServer CreateServer(string serverName, CngKey key, [CallerMemberName] string systemName ="", params MessageTag[] subscriptionTags)
        {
            return new MessageServer(
                serverName,
                systemName,
                Properties.Resources.RabbitMQServerName,
                key,
                Properties.Resources.Username,
                Properties.Resources.Password,
                subscriptionTags);
        }

        public static MessageClient CreateClient(string clientName, CngKey key, [CallerMemberName] string systemName = "", params MessageTag[] subscriptionTags)
        {
            return new MessageClient(
                clientName,
                systemName,
                Properties.Resources.RabbitMQServerName,
                key,
                Properties.Resources.Username,
                Properties.Resources.Password,
                subscriptionTags);
        }
    }
}
