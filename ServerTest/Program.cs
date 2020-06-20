using Newtonsoft.Json.Linq;
using Pinknose.DistributedWorkers.Clients;
using Pinknose.DistributedWorkers.Configuration;
using System;
using System.Collections.Generic;
using System.IO;

namespace ServerTest
{
    class Program
    {
        static void Main(string[] args)
        {
            // Assuming first argument is the JSON Configuration file name
            JObject config = JObject.Parse(File.ReadAllText(args[0]));

            var serverIdentity = MessageClientInfo.Import(config["ServerIdentity"].ToString(), "monkey123");

            var clientsJArray = (JArray)config["ClientIdentities"];
            List<MessageClientInfo> clients = new List<MessageClientInfo>();

            foreach (var item in clientsJArray)
            {
                clients.Add(MessageClientInfo.Import(item.ToString()));
            }

            var server = new MessageServerConfigurationBuilder()
                .RabbitMQCredentials(config["RabbitMQServer"]["UserName"].Value<string>(), config["RabbitMQServer"]["Password"].Value<string>())
                .RabbitMQServerHostName(config["RabbitMQServer"]["HostName"].Value<string>())
                .ServerInfo(serverIdentity)
                .AddClientInfoRange(clients)
                .AutoDeleteQueuesOnClose(true)
                .QueuesAreDurable(false)
                .CreateMessageServer();

        }
    }
}
