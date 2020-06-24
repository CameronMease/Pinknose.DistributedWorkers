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
using System.Collections.Generic;

namespace Pinknose.DistributedWorkers.Configuration
{
    public abstract class MessageClientConfigurationBuilderBase
    {
    }

    public abstract class MessageClientConfigurationBase<TConfigType> : MessageClientConfigurationBuilderBase where TConfigType : MessageClientConfigurationBuilderBase
    {
        #region Fields

        protected string _userName = "guest";
        protected string _password = "guest";

        protected string _rabbitMQServerHostName = "localhost";

        protected bool _queuesAreDurable = true;
        protected bool _autoDeleteQueuesOnClose = false;

        protected HashSet<MessageClientIdentity> _clientIdentities = new HashSet<MessageClientIdentity>();

        #endregion Fields

        #region Methods

        public TConfigType RabbitMQCredentials(string userName, string password)
        {
            _userName = userName;
            _password = password;

            return (TConfigType)(object)this;
        }

        /// <summary>
        /// The hostname or IP address of the computer running the RabbitMQ Server
        /// </summary>
        /// <param name="hostName"></param>
        /// <returns></returns>
        public TConfigType RabbitMQServerHostName(string hostName)
        {
            _rabbitMQServerHostName = hostName;

            return (TConfigType)(object)this;
        }

        public TConfigType AutoDeleteQueuesOnClose(bool value)
        {
            _autoDeleteQueuesOnClose = value;
            return (TConfigType)(object)this;
        }

        public TConfigType QueuesAreDurable(bool value)
        {
            _queuesAreDurable = value;
            return (TConfigType)(object)this;
        }

        public TConfigType AddClientIdentity(MessageClientIdentity clientIdentity)
        {
            _clientIdentities.Add(clientIdentity);

            return (TConfigType)(object)this;
        }

        public TConfigType AddClientInfoRange(IEnumerable<MessageClientIdentity> clientIdentity)
        {
            foreach (var info in clientIdentity)
            {
                _clientIdentities.Add(info);
            }

            return (TConfigType)(object)this;
        }

        #endregion Methods
    }
}