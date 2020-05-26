using EasyNetQ.Management.Client.Model;
using Pinknose.DistributedWorkers.Clients;
using System;

namespace Pinknose.DistributedWorkers.Configuration
{

    public class MessageClientConfiguration : MessageClientConfigurationBase<MessageClientConfiguration>
    {
        private MessageClientInfo _clientInfo = null;
        private MessageClientInfo _serverInfo = null;

        public MessageClientConfiguration ClientInfo(MessageClientInfo clientInfo)
        {
            _clientInfo = clientInfo;

            return this;
        }

        public MessageClientConfiguration ServerInfo(MessageClientInfo serverInfo)
        {
            _serverInfo = serverInfo;

            return this;
        }

        public MessageClient CreateMessageClient()
        {
            return new MessageClient(
                _clientInfo,
                _serverInfo,
                this._rabbitMQServerName,
                this._userName,
                this._password);
        }
    }

}