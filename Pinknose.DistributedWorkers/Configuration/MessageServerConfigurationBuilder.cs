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
    public sealed class MessageServerConfigurationBuilder : MessageClientConfigurationBase<MessageServerConfigurationBuilder>
    {
        #region Fields

        private MessageClientIdentity _serverIdentity = null;

        #endregion Fields

        #region Methods

        public MessageServerConfigurationBuilder Identity(MessageClientIdentity identity)
        {
            _serverIdentity = identity;

            return this;
        }

        public MessageServer CreateMessageServer()
        {
            return new MessageServer(
                _serverIdentity,
                this._rabbitMQServerHostName,
                this._userName,
                this._password,
                _autoDeleteQueuesOnClose,
                _queuesAreDurable,
                _clientIdentities.ToArray());

        }

        #endregion Methods
    }
}