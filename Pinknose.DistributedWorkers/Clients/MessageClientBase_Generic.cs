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
using Pinknose.DistributedWorkers.MessageQueues;
using Pinknose.DistributedWorkers.MessageTags;
using System;

public abstract class MessageClientBase<TServerQueue> : MessageClientBase where TServerQueue : MessageQueue, new()
{
    //internal TServerQueue LogQueue { get; private set; }

    #region Constructors

    protected MessageClientBase(MessageClientInfo clientInfo, string rabbitMqServerHostName, string userName, string password) :
                base(clientInfo, rabbitMqServerHostName, userName, password)
    {
    }

    #endregion Constructors

    #region Properties

    /// <summary>
    /// Remote Procedure Call queue (in to master).
    /// </summary>
    protected TServerQueue ServerQueue { get; private set; }

    #endregion Properties

    #region Methods

    protected override void SetupConnections(TimeSpan timeout, MessageTagCollection subscriptionTags)
    {
        base.SetupConnections(timeout, subscriptionTags);

        ServerQueue = MessageQueue.CreateMessageQueue<TServerQueue>(this, Channel, ClientName, ServerQueueName, this.QueuesAreDurable, this.AutoDeleteQueuesOnClose);
    }

    #endregion Methods
}