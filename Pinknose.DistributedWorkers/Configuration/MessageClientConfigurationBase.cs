using Pinknose.DistributedWorkers.Clients;
using System;


namespace Pinknose.DistributedWorkers.Configuration
{
    public abstract class MessageClientConfigurationBase
    {

    }

    public abstract class MessageClientConfigurationBase<TConfigType> : MessageClientConfigurationBase where TConfigType : MessageClientConfigurationBase
    {
        protected string _userName = "";
        protected string _password = "";

        protected string _rabbitMQServerName = "";

        public TConfigType Credentials(string userName, string password)
        {
            _userName = userName;
            _password = password;

            return (TConfigType)(object)this;
        }

        public TConfigType RabbitMQServer(string serverName)
        {
            _rabbitMQServerName = serverName;

            return (TConfigType)(object)this;
        }


    }

}