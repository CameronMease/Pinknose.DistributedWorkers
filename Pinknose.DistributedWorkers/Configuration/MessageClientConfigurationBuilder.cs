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

using EasyNetQ.Management.Client.Model;
using Pinknose.DistributedWorkers.Clients;
using System.Linq;

namespace Pinknose.DistributedWorkers.Configuration
{
    public abstract class MessageClientConfigurationBuilder<TConfigType, TClient> : MessageClientConfigurationBase<MessageClientConfigurationBuilder<TConfigType, TClient>> where TConfigType : MessageClientConfigurationBuilderBase
    {
        #region Fields

        protected MessageClientIdentity _thisIdentity = null;
        protected MessageClientIdentity _serverIdentity = null;
        protected int _heartbeatInterval = 1000;

        #endregion Fields

        #region Methods

        public TConfigType Identity(MessageClientIdentity identity)
        {
            _thisIdentity = identity;

            return (TConfigType)(object)this;
        }

        public TConfigType ServerIdentity(MessageClientIdentity serverIdentity)
        {
            _serverIdentity = serverIdentity;

            return (TConfigType)(object)this;
        }

        public TConfigType HeartbeatInterval(int interval)
        {
            _heartbeatInterval = interval;
            return (TConfigType)(object)this;
        }

        public abstract TClient CreateMessageClient();
        

        #endregion Methods
    }

    public class MessageClientConfigurationBuilder : MessageClientConfigurationBuilder<MessageClientConfigurationBuilder, MessageClient>
    {
        public override MessageClient CreateMessageClient()
        {
            return new MessageClient(
               _thisIdentity,
               _serverIdentity,
               this._rabbitMQServerHostName,
               this._userName,
               this._password,
               _autoDeleteQueuesOnClose,
               _queuesAreDurable,
               _heartbeatInterval,
               this._clientIdentities.ToArray());
        }
    }
}