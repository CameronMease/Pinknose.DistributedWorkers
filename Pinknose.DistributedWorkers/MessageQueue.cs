using EasyNetQ.Management.Client.Model;
using Pinknose.DistributedWorkers.Messages;
using Pinknose.DistributedWorkers.MessageTags;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Pinknose.DistributedWorkers
{
    /// <summary>
    /// Abstraction of a RabbitMQ queue.  This message queue can be written to, but cannot be read from.
    /// </summary>
    public class MessageQueue : IDisposable
    {
        private QueueDeclareOk queueInfo;
        private string _clientName;
        internal MessageClientBase ParentMessageClient { get; private set; }

        private string boundExchangeName = String.Empty;

        internal static TQueueType CreateMessageQueue<TQueueType>(MessageClientBase parentMessageClient, IModel channel, string clientName, string queueName) where TQueueType : MessageQueue, new()
        {
            var queue = new TQueueType()
            {
                Channel = channel,
                queueInfo = channel.QueueDeclare(
                    queue: queueName,
                    durable: false,
                    exclusive: false,
                    autoDelete: false),
                _clientName = clientName,
                ParentMessageClient = parentMessageClient
            };

            return queue;
        }

        internal static TQueueType CreateExchangeBoundMessageQueue<TQueueType>(MessageClientBase parentMessageClient, IModel channel, string clientName, string exchangeName, string queueName, params MessageTag[] subscriptionTags) where TQueueType : MessageQueue, new()
        {
            return CreateExchangeBoundMessageQueue<TQueueType>(
                parentMessageClient,
                channel,
                clientName,
                exchangeName,
                queueName,
                new MessageTagCollection(subscriptionTags));
        }

        /// <summary>
        /// Declares a queue and binds it to an exchange.  Exchanges allow messages to be sent to more than one queue.  Exchange-bound queues are useful when a
        /// receiver needs to consume broadcast messages.
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="queueName"></param>
        internal static TQueueType CreateExchangeBoundMessageQueue<TQueueType>(MessageClientBase parentMessageClient, IModel channel, string clientName, string exchangeName, string queueName, MessageTagCollection subscriptionTags) where TQueueType : MessageQueue, new()
        {
            var queue = new TQueueType()
            {
                Channel = channel,
                queueInfo = channel.QueueDeclare(
                    queue: queueName,
                    durable: false,
                    exclusive: true,
                    autoDelete: false),
                _clientName = clientName,
                ParentMessageClient = parentMessageClient,
                boundExchangeName = exchangeName
            };

            if (!subscriptionTags.Any())
            {
                channel.QueueBind(queueName, exchangeName, "");
            }
            else
            {
                var map = new Dictionary<string, object>();

                map.Add("x-match", "any");

                foreach (var item in subscriptionTags)
                {
                    map.Add(item.GetMangledTagAndValue(), "");
                }

                channel.QueueBind(queueName, exchangeName, "", map);
            }

            return queue;
        }

        /// <summary>
        /// Returns the name of the queue.
        /// </summary>
        public string Name => queueInfo.QueueName;

        protected IModel Channel { get; set; }

        public void WriteToBoundExchange(MessageBase message, EncryptionOption encryptionOption, MessageTagCollection tags)
        {
            if (encryptionOption == EncryptionOption.EncryptWithPrivateKey)
            {
                //TODO: Add message text
                throw new ArgumentOutOfRangeException(nameof(encryptionOption));
            }

            Write(message, boundExchangeName, encryptionOption, tags);
        }

        public void Write(MessageBase message, EncryptionOption encryptionOptions, MessageTagCollection tags)
        {
            Write(message, "", encryptionOptions, tags);
        }

        public void Write(MessageBase message, IBasicProperties basicProperties, EncryptionOption encryptionOptions, MessageTagCollection tags)
        {
            if (basicProperties is null)
            {
                throw new ArgumentNullException(nameof(basicProperties));
            }

            Write(message, "", basicProperties, encryptionOptions, tags);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="exchangeName"></param>
        private void Write(MessageBase message, string exchangeName, EncryptionOption encryptionOptions, MessageTagCollection tags)
        {
            Write(message, exchangeName, Channel.CreateBasicProperties(), encryptionOptions, tags);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="exchangeName"></param>
        /// <param name="basicProperties"></param>
        private void Write(MessageBase message, string exchangeName, IBasicProperties basicProperties, EncryptionOption encryptionOption, MessageTagCollection tags)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            //message.ClientName = _clientName;

            var envelope = MessageEnvelope.WrapMessage(message, "", this.ParentMessageClient, encryptionOption);

            byte[] hashedMessage = envelope.Serialize();

            //TODO: Move this into the message serialization?
            basicProperties.Headers = new Dictionary<string, object>();
            basicProperties.Headers.Add(new MessageSenderTag(this._clientName).GetMangledTagAndValue(), "");

            foreach (var tag in tags)
            {
                basicProperties.Headers.Add(tag.GetMangledTagName(), "");

                // If this is a tag with a value (not just a tag)
                if (tag.GetType() != typeof(MessageTag))
                {
                    basicProperties.Headers.Add(tag.GetMangledTagAndValue(), "");
                }
            }

            Channel.BasicPublish(
                exchange: exchangeName,
                routingKey: queueInfo.QueueName,
                basicProperties: basicProperties,
                hashedMessage);
        }



        /// <summary>
        /// Regular expression used to parse the exchange name and routing keys from the message's basic properties.
        /// </summary>
        private static Regex replyToRegex = new Regex("exchangeName:(?<exchangeName>.*),routingKey:(?<routingKey>.*)", RegexOptions.Compiled);

        public void RespondToMessage(MessageEnvelope originalMessageEnvelope, MessageBase responseMessage, EncryptionOption encryptionOption)
        {
            if (originalMessageEnvelope == null)
            {
                throw new ArgumentNullException(nameof(originalMessageEnvelope));
            }

            if (responseMessage == null)
            {
                throw new ArgumentNullException(nameof(responseMessage));
            }

            //responseMessage.ClientName = _clientName;

            IBasicProperties basicProperties = this.Channel.CreateBasicProperties();
            basicProperties.CorrelationId = originalMessageEnvelope.BasicProperties.CorrelationId;

            Match match = replyToRegex.Match(originalMessageEnvelope.BasicProperties.ReplyTo);
            string exchangeName = match.Groups["exchangeName"].Value;
            string routingKey = match.Groups["routingKey"].Value;

            var envelope = MessageEnvelope.WrapMessage(responseMessage, originalMessageEnvelope.SenderName, this.ParentMessageClient, encryptionOption);
            byte[] hashedMessage = envelope.Serialize();

            Channel.BasicPublish(
                exchange: exchangeName,
                routingKey: routingKey,
                basicProperties: basicProperties,
                hashedMessage);
        }

        public int QueueCount => (int)Channel.MessageCount(Name);
        public int ConsumerCount => (int)Channel.ConsumerCount(Name);

        public void Purge() => Channel.QueuePurge(Name);

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                Channel.QueueDelete(Name, true, true);

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


    }
}
