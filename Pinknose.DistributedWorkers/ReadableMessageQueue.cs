using Pinknose.DistributedWorkers.Messages;
using Pinknose.Utilities;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pinknose.DistributedWorkers
{
    /// <summary>
    /// Abstraction of a RabbitMQ queue.  This message queue can be written to and can be read from.
    /// </summary>
    public class ReadableMessageQueue : MessageQueue, IDisposable
    {
        private bool fullConsumeActive = false;
        private bool limitedConsumeActive = false;
        private bool autoAcknowledgeMessages = false;

        private SimpleCountingSemaphore limitedConsumeSempahore = null;
        private Task limitedConsumeTask = null;
        private CancellationToken limitedConsumeCancellationToken;

        private CancellationTokenSource cancellationTokenSource;

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;
        
        /// <summary>
        /// Begin consuming messages as they come in (not rate limited).
        /// </summary>
        public void BeginFullConsume(bool autoAcknowledge)
        {
            if (fullConsumeActive || limitedConsumeActive)
            {
                throw new InvalidOperationException();
            }

            fullConsumeActive = true;
            autoAcknowledgeMessages = autoAcknowledge;

            var consumer = new EventingBasicConsumer(Channel);
            consumer.Received += Consumer_Received;
            Channel.BasicConsume(this.Name, autoAcknowledge, consumer);

        }

        /// <summary>
        /// Begins consuming messages from the queue.  The number of messages consumed is limited by the maxActiveMessages value.  Once the
        /// client has consumed the max number of messages, no new messages will be received until a message has been acknolwedged.
        /// </summary>
        /// <param name="maxActiveMessages">The maximum number of messages that can be processed at one time.</param>
        public void BeginLimitedConsume(int maxActiveMessages, bool autoAcknowledge)
        {
            if (maxActiveMessages <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxActiveMessages), "Value must be more than 0.");
            }

            if (limitedConsumeSempahore != null)
            {
                throw new Exception();
            }

            if (fullConsumeActive || limitedConsumeActive)
            {
                throw new InvalidOperationException();
            }

            limitedConsumeActive = true;
            autoAcknowledgeMessages = autoAcknowledge;

            limitedConsumeSempahore = new SimpleCountingSemaphore(maxActiveMessages);
            cancellationTokenSource = new CancellationTokenSource();
            limitedConsumeCancellationToken = cancellationTokenSource.Token;

            limitedConsumeTask = new Task(RunLimitedConsumeTask, limitedConsumeCancellationToken);


            limitedConsumeTask.Start();
        }

        public void StopConsume()
        {
            //TODO: Add code here!
        }

        /// <summary>
        /// This task runs continuously, waiting for new messages.  The task will pend on a semaphore when
        /// the maximum number of active messages has been reached.
        /// </summary>
        private void RunLimitedConsumeTask()
        {
            while (true)
            {
                limitedConsumeSempahore.Take();

                BasicGetResult result;

                do
                {
                    result = Channel.BasicGet(queue: Name, autoAck: autoAcknowledgeMessages);

                    if (result == null)
                    {
                        // Wait before checking again.
                        //TODO: Not the smartest way to do this.  Is there a better way?
                        Thread.Sleep(1000);
                    }
                } while (result == null);

                MessageBase message = MessageBase.Deserialize(result);

                FireMessageReceivedEvent(message, result.Body.ToArray());
            }
        }

        private void FireMessageReceivedEvent(MessageBase message, byte[] rawData)
        {
            var task = new Task(() =>
            {
                var eventArgs = new MessageReceivedEventArgs(message, rawData);

                MessageReceived?.Invoke(this, eventArgs);

                if (!autoAcknowledgeMessages)
                {
                    if (eventArgs.Response == MessageResponse.Ack)
                    {
                        Channel.BasicAck(message.DeliveryTag, false);
                    }
                    else if (eventArgs.Response == MessageResponse.Nack)
                    {
                        Channel.BasicAck(message.DeliveryTag, false);
                    }
                    else if (eventArgs.Response == MessageResponse.RejectPermanent)
                    {
                        Channel.BasicReject(message.DeliveryTag, false);
                    }
                    else
                    {
                        Channel.BasicReject(message.DeliveryTag, true);
                    }
                }

                limitedConsumeSempahore?.Give();
            });

            task.Start();
        }

        private void Consumer_Received(object sender, BasicDeliverEventArgs e)
        {
            try
            {
                FireMessageReceivedEvent(MessageBase.Deserialize(e), e.Body.ToArray());
            }
            catch (SerializationException ex)
            {
                throw new NotImplementedException();
            }
        }

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
                    limitedConsumeSempahore?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~ReadableMessageQueue()
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
