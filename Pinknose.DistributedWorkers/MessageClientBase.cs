using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pinknose.DistributedWorkers
{
    public enum RpcCallResult { Success, Timeout }
    public abstract class MessageClientBase<TRpcQueue> : IDisposable where TRpcQueue : MessageQueue, new()
    {
        private string _userName;
        private string _password;

        protected string WorkQueueName => $"{SystemName}-queue-work".ToLowerInvariant();
        protected string BroadcastExchangeName => $"{SystemName}-exchange-broadcast".ToLowerInvariant();
        protected string DedicatedQueueName => $"{SystemName}-{ClientName}-queue-dedicated".ToLowerInvariant();
        protected string RpcQueueName => $"{SystemName}-queue-rpc".ToLowerInvariant();

        protected string LogQueueName => $"{SystemName}-queue-log".ToLowerInvariant();

        /// <summary>
        /// Heartbeat time between clients/server in milliseconds;
        /// </summary>
        protected const int HeartbeatTime = 10000;

        private System.Timers.Timer heartbeatTimer = new System.Timers.Timer()
        {
            Interval = HeartbeatTime,
            AutoReset = true
        };

        private IConnection connection;
        protected IModel Channel { get; private set; }

        protected MessageClientBase(string clientName, string systemName, string rabbitMqServerHostName, string userName, string password)
        {
            if (string.IsNullOrEmpty(clientName))
            {
                throw new ArgumentNullException(nameof(clientName));
            }

            if (string.IsNullOrEmpty(systemName))
            {
                throw new ArgumentNullException(nameof(systemName));
            }

            if (string.IsNullOrEmpty(rabbitMqServerHostName))
            {
                throw new ArgumentNullException(nameof(rabbitMqServerHostName));
            }

            if (string.IsNullOrEmpty(userName))
            {
                throw new ArgumentNullException(nameof(userName));
            }

            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException(nameof(password));
            }

            _userName = userName;
            _password = password;
            ClientName = clientName;

            InternalId = Guid.NewGuid().ToString();
            SystemName = systemName;

            ConnectionFactory factory = new ConnectionFactory()
            {
                HostName = rabbitMqServerHostName,
                UserName = userName,
                Password = password,
                //VirtualHost = systemName,
                RequestedHeartbeat = TimeSpan.FromSeconds(10) //todo: Need to set elsewhere, be configurable
            };

            connection = factory.CreateConnection();
            Channel = connection.CreateModel();

            WorkQueue = MessageQueue.CreateMessageQueue<ReadableMessageQueue>(Channel, ClientName, WorkQueueName);

            Channel.ExchangeDeclare(
                BroadcastExchangeName,
                ExchangeType.Fanout,
                false,
                false,
                new Dictionary<string, object>());

            RpcQueue = MessageQueue.CreateMessageQueue<TRpcQueue>(Channel, ClientName, RpcQueueName);
            LogQueue = MessageQueue.CreateMessageQueue<TRpcQueue>(Channel, ClientName, LogQueueName);

            heartbeatTimer.Elapsed += HeartbeatTimer_Elapsed;
            heartbeatTimer.Start();
        }

        private void HeartbeatTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            SendHeartbeat();
        }





        public ReadableMessageQueue WorkQueue { get; private set; }

        internal TRpcQueue LogQueue { get; private set; }

        /// <summary>
        /// Remote Procedure Call queue (in to master).
        /// </summary>
        public TRpcQueue RpcQueue { get; private set; }

        public string SystemName { get; private set; }

        public string InternalId { get; private set; }


        public string ClientName { get; private set; }


        protected abstract void SendHeartbeat();

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    heartbeatTimer?.Dispose();
                    connection?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~MessageClientBase()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }


        #endregion
    }
}
