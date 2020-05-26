using Pinknose.DistributedWorkers.Clients;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pinknose.DistributedWorkers.Configuration
{

    public sealed class MessageServerConfiguration : MessageClientConfigurationBase<MessageServerConfiguration>
    {
        private HashSet<MessageClientInfo> _clientInfos = new HashSet<MessageClientInfo>();
        private MessageClientInfo _serverInfo = null;

        public MessageServerConfiguration AddClientInfo(MessageClientInfo clientInfo)
        {
            _clientInfos.Add(clientInfo);

            return this;
        }

        public MessageServerConfiguration ServerInfo(MessageClientInfo serverInfo)
        {
            _serverInfo = serverInfo;

            return this;
        }

        public MessageServer CreateMessageServer()
        {
            return new MessageServer(
                _serverInfo,
                this._rabbitMQServerName,
                this._userName,
                this._password,
                _clientInfos.ToArray());
        }
    }

}