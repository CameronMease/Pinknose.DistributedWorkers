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

using Pinknose.DistributedWorkers.Clients;
using Pinknose.DistributedWorkers.Configuration;
using Pinknose.DistributedWorkers.MessageTags;
using RabbitMQ.Client;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Pinknose.DistributedWorkers.UnitTests
{
    public static class TestHelpers
    {
        #region Methods

        public static MessageClient CreateClient(string clientName, MessageClientIdentity serverInfo, [CallerMemberName] string systemName = "")
        {
            return new MessageClientConfigurationBuilder()
                .Identity(MessageClientIdentity.CreateClientInfo(systemName, clientName, ECDiffieHellmanCurve.P256))
                .ServerIdentity(serverInfo)
                .RabbitMQServerHostName(Properties.Resources.RabbitMQServerName)
                .RabbitMQCredentials(Properties.Resources.Username, Properties.Resources.Password)
                .QueuesAreDurable(false)
                .AutoDeleteQueuesOnClose(true)
                .CreateMessageClient();
         }

        public static (MessageServer messageServer, MessageClientIdentity serverInfo) CreateServer(string serverName, CngKey key, [CallerMemberName] string systemName = "", params MessageClientIdentity[] clientInfo)
        {
            var serverInfo = MessageClientIdentity.CreateServerInfo(systemName, ECDiffieHellmanCurve.P256);

            return (new MessageServerConfigurationBuilder()
                .Identity(serverInfo)
                .AddClientInfoRange(clientInfo)
                .RabbitMQServerHostName(Properties.Resources.RabbitMQServerName)
                .RabbitMQCredentials(Properties.Resources.Username, Properties.Resources.Password)
                .QueuesAreDurable(false)
                .AutoDeleteQueuesOnClose(true)
                .CreateMessageServer(), serverInfo);
        }

        public static (MessageServer messageServer, IEnumerable<MessageClient> clients) CreateClientsAndServer([CallerMemberName] string systemName = "", params string[] clientNames)
        {
            MessageClientIdentity serverInfo = MessageClientIdentity.CreateServerInfo(systemName, ECDiffieHellmanCurve.P256);
            MessageClientIdentity[] clientsInfo = new MessageClientIdentity[clientNames.Count()];
            MessageClient[] messageClients = new MessageClient[clientNames.Count()];

            for (int i = 0; i < clientNames.Count(); i++)
            {
                clientsInfo[i] = MessageClientIdentity.CreateClientInfo(systemName, clientNames[i], ECDiffieHellmanCurve.P256);

                messageClients[i] = new MessageClientConfigurationBuilder()
                                        .Identity(clientsInfo[i])
                                        .ServerIdentity(serverInfo)
                                        .RabbitMQServerHostName(Properties.Resources.RabbitMQServerName)
                                        .RabbitMQCredentials(Properties.Resources.Username, Properties.Resources.Password)
                                        .QueuesAreDurable(false)
                                        .AutoDeleteQueuesOnClose(true)
                                        .CreateMessageClient();
            }

            var messageServer = new MessageServerConfigurationBuilder()
                                    .Identity(serverInfo)
                                    .AddClientInfoRange(clientsInfo)
                                    .RabbitMQServerHostName(Properties.Resources.RabbitMQServerName)
                                    .RabbitMQCredentials(Properties.Resources.Username, Properties.Resources.Password)
                                    .QueuesAreDurable(false)
                                    .AutoDeleteQueuesOnClose(true)
                                    .CreateMessageServer();

            return (messageServer, messageClients);
        }

        public static IConnection GetConnection()
        {
            ConnectionFactory factory = new ConnectionFactory();

            factory.UserName = Properties.Resources.Username;
            factory.Password = Properties.Resources.Password;
            factory.HostName = Properties.Resources.RabbitMQServerName;

            IConnection conn = factory.CreateConnection();

            return conn;
        }

        #endregion Methods
    }
}