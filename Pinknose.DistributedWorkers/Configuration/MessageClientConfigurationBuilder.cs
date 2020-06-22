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

namespace Pinknose.DistributedWorkers.Configuration
{
    public class MessageClientConfigurationBuilder : MessageClientConfigurationBase<MessageClientConfigurationBuilder>
    {
        #region Fields

        private MessageClientInfo _clientInfo = null;
        private MessageClientInfo _serverInfo = null;

        #endregion Fields

        #region Methods

        public MessageClientConfigurationBuilder ClientInfo(MessageClientInfo clientInfo)
        {
            _clientInfo = clientInfo;

            return this;
        }

        public MessageClientConfigurationBuilder ServerInfo(MessageClientInfo serverInfo)
        {
            _serverInfo = serverInfo;

            return this;
        }

        public MessageClient CreateMessageClient()
        {
            return new MessageClient(
                _clientInfo,
                _serverInfo,
                this._rabbitMQServerHostName,
                this._userName,
                this._password)
            {
                QueuesAreDurable = _queuesAreDurable,
                AutoDeleteQueuesOnClose = _autoDeleteQueuesOnClose
            };
        }

        #endregion Methods
    }
}