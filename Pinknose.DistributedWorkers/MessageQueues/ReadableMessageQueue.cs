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

using Pinknose.DistributedWorkers.Exceptions;
using Pinknose.DistributedWorkers.Messages;
using Pinknose.Utilities;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Pinknose.DistributedWorkers.MessageQueues
{
    /// <summary>
    /// Abstraction of a RabbitMQ queue.  This message queue can be written to and can be read from.
    /// </summary>
    public class ReadableMessageQueue : MessageQueue
    {
        #region Fields

        private bool autoAcknowledgeMessages = false;
        private CancellationTokenSource cancellationTokenSource;
        private bool disposedValue = false;
        private bool fullConsumeActive = false;
        private bool limitedConsumeActive = false;
        private CancellationToken limitedConsumeCancellationToken;
        private SimpleCountingSemaphore limitedConsumeSempahore = null;
        private Task limitedConsumeTask = null;

        #endregion Fields

        #region Events

        public event EventHandler<AsynchronousExceptionEventArgs> AsynchronousException;

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        #endregion Events

        #region Methods

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

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        public void StopConsume()
        {
            //TODO: Add code here!
            throw new NotImplementedException();
        }

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

        private void Consumer_Received(object sender, BasicDeliverEventArgs e)
        {
            try
            {
                var wrapper = MessageEnvelope.Deserialize(e.Body.ToArray(), e, this.ParentMessageClient);
                FireMessageReceivedEvent(wrapper);
            }
            catch (Exception ex)
            {
                AsynchronousException?.Invoke(this, new AsynchronousExceptionEventArgs(ex));
            }
        }

        private void FireMessageReceivedEvent(MessageEnvelope messageEnvelope)
        {
            var task = new Task(() =>
            {
                var eventArgs = new MessageReceivedEventArgs(messageEnvelope);

                MessageReceived?.Invoke(this, eventArgs);

                if (!autoAcknowledgeMessages)
                {
                    if (eventArgs.Response == MessageResponse.Ack)
                    {
                        Channel.BasicAck(messageEnvelope.DeliveryTag, false);
                    }
                    else if (eventArgs.Response == MessageResponse.Nack)
                    {
                        Channel.BasicAck(messageEnvelope.DeliveryTag, false);
                    }
                    else if (eventArgs.Response == MessageResponse.RejectPermanent)
                    {
                        Channel.BasicReject(messageEnvelope.DeliveryTag, false);
                    }
                    else
                    {
                        Channel.BasicReject(messageEnvelope.DeliveryTag, true);
                    }
                }

                limitedConsumeSempahore?.Give();
            });

            task.Start();
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

                var wrapper = MessageEnvelope.Deserialize(result.Body.ToArray(), result, this.ParentMessageClient);

                //MessageBase message = MessageBase.Deserialize(wrapper, result, this.ParentMessageClient);

                FireMessageReceivedEvent(wrapper);
            }
        }

        #endregion Methods

        // To detect redundant calls
        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~ReadableMessageQueue()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }
    }
}