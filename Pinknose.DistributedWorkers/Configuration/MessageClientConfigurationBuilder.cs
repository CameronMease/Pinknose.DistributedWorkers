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
using System.Linq;

namespace Pinknose.DistributedWorkers.Configuration
{
    public class MessageClientConfigurationBuilder : MessageClientConfigurationBase<MessageClientConfigurationBuilder>
    {
        #region Fields

        private MessageClientIdentity _thisIdentity = null;
        private MessageClientIdentity _serverIdentity = null;
        private int _heartbeatInterval = 1000;

        #endregion Fields

        #region Methods

        public MessageClientConfigurationBuilder Identity(MessageClientIdentity identity)
        {
            _thisIdentity = identity;

            return this;
        }

        public MessageClientConfigurationBuilder ServerIdentity(MessageClientIdentity serverIdentity)
        {
            _serverIdentity = serverIdentity;

            return this;
        }

        public MessageClientConfigurationBuilder HeartbeatInterval(int interval)
        {
            _heartbeatInterval = interval;
            return this;
        }

        public MessageClient CreateMessageClient()
        {
            return new MessageClient(
                _thisIdentity,
                _serverIdentity,
                this._rabbitMQServerHostName,
                this._userName,
                this._password,
                this._clientIdentities.ToArray())
            {
                QueuesAreDurable = _queuesAreDurable,
                AutoDeleteQueuesOnClose = _autoDeleteQueuesOnClose,
                HeartbeatInterval = _heartbeatInterval
            };
        }

        #endregion Methods
    }
}