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

            var serverIdentity = MessageClientIdentity.Import(config["ServerIdentity"].ToString(), "monkey123");

            var clientsJArray = (JArray)config["ClientIdentities"];
            List<MessageClientIdentity> clients = new List<MessageClientIdentity>();

            foreach (var item in clientsJArray)
            {
                clients.Add(MessageClientIdentity.Import(item.ToString()));
            }

            var server = new MessageServerConfigurationBuilder()
                .RabbitMQCredentials(config["RabbitMQServer"]["UserName"].Value<string>(), config["RabbitMQServer"]["Password"].Value<string>())
                .RabbitMQServerHostName(config["RabbitMQServer"]["HostName"].Value<string>())
                .Identity(serverIdentity)
                .AddClientInfoRange(clients)
                .AutoDeleteQueuesOnClose(true)
                .QueuesAreDurable(false)
                .CreateMessageServer();

        }
    }
}
