using Pinknose.DistributedWorkers.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pinknose.DistributedWorkers
{
    public enum MessageResponse { Ack, Nack, RejectRequeue, RejectPermanent }

    public class MessageReceivedEventArgs : EventArgs
    {
        public MessageReceivedEventArgs(MessageBase message)
        {
            Message = message;
            Response = MessageResponse.RejectRequeue;
        }

        public MessageBase Message { get; private set; }

        public MessageResponse Response { get; set; }
    }
}
