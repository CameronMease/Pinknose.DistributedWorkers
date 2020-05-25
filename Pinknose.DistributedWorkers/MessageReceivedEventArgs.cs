using Pinknose.DistributedWorkers.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pinknose.DistributedWorkers
{
    public enum MessageResponse { Ack, Nack, RejectRequeue, RejectPermanent }

    public class MessageReceivedEventArgs : EventArgs
    {
        public MessageReceivedEventArgs(MessageEnvelope envelope)
        {
            MessageEnevelope = envelope;
            Response = MessageResponse.RejectRequeue;
        }

        /// <summary>
        /// The serialized message.
        /// </summary>
        public MessageEnvelope MessageEnevelope { get; private set; }

        public MessageResponse Response { get; set; }
    }
}
