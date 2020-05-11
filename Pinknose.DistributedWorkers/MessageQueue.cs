using Pinknose.DistributedWorkers.Messages;
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
    public class MessageQueue
    {
        private QueueDeclareOk queueInfo;
        private string _clientName;
        protected IMessageClient ParentMessageClient { get; private set; }

        private string boundExchangeName = String.Empty;

        internal static TQueueType CreateMessageQueue<TQueueType>(IMessageClient parentMessageClient, IModel channel, string clientName, string queueName) where TQueueType : MessageQueue, new()
        {
            var queue = new TQueueType()
            {
                Channel = channel,
                queueInfo = channel.QueueDeclare(
                    queue: queueName,
                    durable: false,
                    exclusive: false,
                    autoDelete: true),
                _clientName = clientName,
                ParentMessageClient = parentMessageClient
            };

            return queue;
        }

        internal static TQueueType CreateExchangeBoundMessageQueue<TQueueType>(IMessageClient parentMessageClient, IModel channel, string clientName, string exchangeName, string queueName, params MessageTag[] subscriptionTags) where TQueueType : MessageQueue, new()
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
        internal static TQueueType CreateExchangeBoundMessageQueue<TQueueType>(IMessageClient parentMessageClient, IModel channel, string clientName, string exchangeName, string queueName, MessageTagCollection subscriptionTags) where TQueueType : MessageQueue, new()
        {
            var queue = new TQueueType()
            {
                Channel = channel,
                queueInfo = channel.QueueDeclare(
                    queue: queueName,
                    durable: false,
                    exclusive: true,
                    autoDelete: true),
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

        public void WriteToBoundExchange(MessageBase message, params MessageTag[] tags) => Write(message, boundExchangeName, tags);

        public void WriteToBoundExchange(MessageBase message, MessageTagCollection tags) => Write(message, boundExchangeName, tags);

         public void Write(MessageBase message, params MessageTag[] tags) => Write(message, "", tags);

          /// <summary>
        /// Writes a message to the queue.
        /// </summary>
        /// <param name="message">The message to be written to the queue.</param>
        private void Write(MessageBase message, string exchangeName, params MessageTag[] tags)
        {
            Write(message, exchangeName, new MessageTagCollection(tags));
        }

        private void Write(MessageBase message, string exchangeName, MessageTagCollection tags)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            message.ClientName = _clientName;

            byte[] hashedMessage = ParentMessageClient.AddSignature(message.Serialize());

            IBasicProperties basicProperties = null;

            if (tags != null && tags.Any())
            {
                basicProperties = Channel.CreateBasicProperties();
                basicProperties.Headers = new Dictionary<string, object>();

                foreach (var tag in tags)
                {
                    basicProperties.Headers.Add(tag.GetMangledTagName(), "");

                    // If this is a tag with a value (not just a tag)
                    if (tag.GetType() != typeof(MessageTag))
                    {
                        basicProperties.Headers.Add(tag.GetMangledTagAndValue(), "");
                    }
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

            byte[] hashedMessage = ParentMessageClient.AddSignature(responseMessage.Serialize());

            Channel.BasicPublish(
                exchange: exchangeName,
                routingKey: routingKey,
                basicProperties: basicProperties,
                hashedMessage);
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

      
    }
}
