using Pinknose.DistributedWorkers.Clients;
using Pinknose.DistributedWorkers.MessageQueues;
using Pinknose.DistributedWorkers.MessageTags;
using System;

public abstract class MessageClientBase<TServerQueue> : MessageClientBase where TServerQueue : MessageQueue, new()
{

    //internal TServerQueue LogQueue { get; private set; }

    protected MessageClientBase(MessageClientInfo clientInfo, string rabbitMqServerHostName, string userName, string password) :
                base(clientInfo, rabbitMqServerHostName, userName, password)
    {

    }

    /// <summary>
    /// Remote Procedure Call queue (in to master).
    /// </summary>
    protected TServerQueue ServerQueue { get; private set; }
    protected override void SetupConnections(TimeSpan timeout, MessageTagCollection subscriptionTags)
    {
        base.SetupConnections(timeout, subscriptionTags);

        ServerQueue = MessageQueue.CreateMessageQueue<TServerQueue>(this, Channel, ClientName, ServerQueueName);
    }
}