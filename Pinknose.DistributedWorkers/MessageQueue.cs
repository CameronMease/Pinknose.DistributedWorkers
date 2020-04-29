using Pinknose.DistributedWorkers.Messages;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Pinknose.DistributedWorkers
{
    public class MessageQueue
    {
        private QueueDeclareOk queueInfo;
        private string _clientName;

        internal static TQueueType CreateMessageQueue<TQueueType>(IModel channel, string clientName, string queueName) where TQueueType : MessageQueue, new()
        {
            var queue = new TQueueType()
            {
                Channel = channel,
                queueInfo = channel.QueueDeclare(
                    queue: queueName,
                    durable: false,
                    exclusive: false,
                    autoDelete: true),
                _clientName = clientName
            };

            return queue;
        }

        /// <summary>
        /// Declares a queue and binds it to an exchange
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="queueName"></param>
        internal static TQueueType CreateExchangeBoundMessageQueue<TQueueType>(IModel channel, string clientName, string exchangeName, string queueName) where TQueueType : MessageQueue, new()
        {
            var queue = new TQueueType()
            {
                Channel = channel,
                queueInfo = channel.QueueDeclare(
                    queue: queueName,
                    durable: false,
                    exclusive: true,
                    autoDelete: true),
                _clientName = clientName
            };

            channel.QueueBind(queueName, exchangeName, "");

            return queue;
        }

        public string Name => queueInfo.QueueName;

        protected IModel Channel { get; set; }

        public void Write(MessageBase message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            message.ClientName = _clientName;

            Channel.BasicPublish(
                exchange: "",
                routingKey: queueInfo.QueueName,
                basicProperties: null,
                message.Serialize());
        }



        private static Regex replyToRegex = new Regex("exchangeName:(?<exchangeName>.*),routingKey:(?<routingKey>.*)", RegexOptions.Compiled);

        public void RespondToMessage(MessageBase originalMessage, MessageBase responseMessage)
        {
            if (originalMessage == null)
            {
                throw new ArgumentNullException(nameof(originalMessage));
            }

            if (responseMessage == null)
            {
                throw new ArgumentNullException(nameof(responseMessage));
            }

            responseMessage.ClientName = _clientName;

            IBasicProperties basicProperties = this.Channel.CreateBasicProperties();
            basicProperties.CorrelationId = originalMessage.BasicProperties.CorrelationId;

            Match match = replyToRegex.Match(originalMessage.BasicProperties.ReplyTo);
            string exchangeName = match.Groups["exchangeName"].Value;
            string routingKey = match.Groups["routingKey"].Value;

            Channel.BasicPublish(
                exchange: exchangeName,
                routingKey: routingKey,
                basicProperties: basicProperties,
                responseMessage.Serialize());
        }

        /*

        public void StopLimitedConsume()
        {
            cancellationTokenSource?.Cancel();
        }
        */

        public int QueueCount => (int)Channel.MessageCount(Name);
        public int ConsumerCount => (int)Channel.ConsumerCount(Name);

        public void Purge() => Channel.QueuePurge(Name);

        /*
                #region IDisposable Support
                private bool disposedValue = false; // To detect redundant calls

                protected virtual void Dispose(bool disposing)
                {
                    if (!disposedValue)
                    {
                        if (disposing)
                        {
                            // TODO: dispose managed state (managed objects).
                            cancellationTokenSource?.Dispose();

                        }

                        // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                        // TODO: set large fields to null.

                        disposedValue = true;
                    }
                }

                // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
                // ~MessageQueue()
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
                */
    }
}
