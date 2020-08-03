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
using Pinknose.DistributedWorkers.Modules;
using System.Collections.Generic;
using System.Linq;

namespace Pinknose.DistributedWorkers.Configuration
{
    public abstract class MessageClientConfigurationBuilder<TConfigType, TClient> : MessageClientConfigurationBase<MessageClientConfigurationBuilder<TConfigType, TClient>> where TConfigType : MessageClientConfigurationBuilderBase
    {
        #region Fields

#pragma warning disable CA1051 // Do not declare visible instance fields
        protected MessageClientIdentity _thisIdentity = null;
        protected MessageClientIdentity _trustCoordinatorIdentity = null;
        protected int _heartbeatInterval = 1000;
        protected List<ClientModule> _modules = new List<ClientModule>();
#pragma warning restore CA1051 // Do not declare visible instance fields

        #endregion Fields

        #region Methods

        public TConfigType Identity(MessageClientIdentity identity)
        {
            _thisIdentity = identity;

            return (TConfigType)(object)this;
        }

        public TConfigType TrustCoordinatorIdentity(MessageClientIdentity trustCoordinatorIdentity)
        {
            _trustCoordinatorIdentity = trustCoordinatorIdentity;

            return (TConfigType)(object)this;
        }

        public TConfigType HeartbeatInterval(int interval)
        {
            _heartbeatInterval = interval;
            return (TConfigType)(object)this;
        }

        public abstract TClient CreateMessageClient();
        
        public TConfigType AddModule(ClientModule module)
        {
            _modules.Add(module);
            return (TConfigType)(object)this;
        }

        #endregion Methods
    }

    public class MessageClientConfigurationBuilder : MessageClientConfigurationBuilder<MessageClientConfigurationBuilder, MessageClient>
    {
        public override MessageClient CreateMessageClient()
        {
            MessageClientIdentity coordinatorIdent = _trustCoordinatorIdentity;

            if (_modules.Any(m => m.GetType() == typeof(TrustCoordinatorModule)))
            {
                //This client is the trust coordinator
                coordinatorIdent = _thisIdentity;
            }

            var client = new MessageClient(
               _thisIdentity,
               coordinatorIdent,
               this._rabbitMQServerHostName,
               this._userName,
               this._password,
               _autoDeleteQueuesOnClose,
               _queuesAreDurable,
               _heartbeatInterval,
               this._clientIdentities.ToArray());

            client.AddModuleRange(_modules);

            return client;
        }
    }
}