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

using Pinknose.DistributedWorkers.MessageTags;
using RabbitMQ.Client;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Pinknose.DistributedWorkers.UnitTests
{
    public static class RabbitMQHelpers
    {
        #region Methods

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

        public static MessageServer CreateServer(string serverName, CngKey key, [CallerMemberName] string systemName = "", params MessageTag[] subscriptionTags)
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