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
using Pinknose.DistributedWorkers.Messages;
using Pinknose.DistributedWorkers.MessageTags;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Pinknose.DistributedWorkers.MessageQueues
{
    /// <summary>
    /// Abstraction of a RabbitMQ queue.  This message queue can be written to, but cannot be read from.
    /// </summary>
    public class MessageQueue : IDisposable
    {
        #region Fields

        /// <summary>
        /// Regular expression used to parse the exchange name and routing keys from the message's basic properties.
        /// </summary>
        private static Regex replyToRegex = new Regex("exchangeName:(?<exchangeName>.*),routingKey:(?<routingKey>.*)", RegexOptions.Compiled);

        private string _clientName;
        private string boundExchangeName = String.Empty;
        private bool disposedValue = false;
        private QueueDeclareOk queueInfo;

        #endregion Fields

        #region Properties

        /// <summary>
        /// Returns the name of the queue.
        /// </summary>
        public string Name => queueInfo.QueueName;

        public int QueueCount => (int)Channel.MessageCount(Name);
        public int ConsumerCount => (int)Channel.ConsumerCount(Name);
        internal MessageClientBase ParentMessageClient { get; private set; }
        protected IModel Channel { get; set; }

        #endregion Properties

        #region Methods

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        public void Purge() => Channel.QueuePurge(Name);

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

        /*
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
        */

        public void WriteToBoundExchange(MessageBase message, bool encryptMessage, MessageTagCollection tags)
        {
            var encryptionOption = encryptMessage switch
            {
                true => EncryptionOption.EncryptWithSystemSharedKey,
                false => EncryptionOption.None
            };

            Write(message, boundExchangeName, encryptionOption, tags);
        }

        internal static TQueueType CreateExchangeBoundMessageQueue<TQueueType>(MessageClientBase parentMessageClient, IModel channel, string clientName, string exchangeName, string queueName, bool durable, bool autoDelete, params MessageTag[] subscriptionTags) where TQueueType : MessageQueue, new()
        {
            return CreateExchangeBoundMessageQueue<TQueueType>(
                parentMessageClient,
                channel,
                clientName,
                exchangeName,
                queueName,
                durable,
                autoDelete,
                new MessageTagCollection(subscriptionTags));
        }

        /// <summary>
        /// Declares a queue and binds it to an exchange.  Exchanges allow messages to be sent to more than one queue.  Exchange-bound queues are useful when a
        /// receiver needs to consume broadcast messages.
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="queueName"></param>
        internal static TQueueType CreateExchangeBoundMessageQueue<TQueueType>(MessageClientBase parentMessageClient, IModel channel, string clientName, string exchangeName, string queueName, bool durable, bool autoDelete, MessageTagCollection subscriptionTags) where TQueueType : MessageQueue, new()
        {
            var queue = new TQueueType()
            {
                Channel = channel,
                queueInfo = channel.QueueDeclare(
                    queue: queueName,
                    durable: durable,
                    exclusive: true,
                    autoDelete: autoDelete),
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

        internal static TQueueType CreateMessageQueue<TQueueType>(MessageClientBase parentMessageClient, IModel channel, string clientName, string queueName, bool durable, bool autoDelete) where TQueueType : MessageQueue, new()
        {
            var queue = new TQueueType()
            {
                Channel = channel,
                queueInfo = channel.QueueDeclare(
                    queue: queueName,
                    durable: durable,
                    exclusive: false,
                    autoDelete: autoDelete),
                _clientName = clientName,
                ParentMessageClient = parentMessageClient
            };

            return queue;
        }

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
                //Channel.QueueDelete(Name, true, true);

                disposedValue = true;
            }
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

        #endregion Methods

        // To detect redundant calls
        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~MessageQueue()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }
    }
}