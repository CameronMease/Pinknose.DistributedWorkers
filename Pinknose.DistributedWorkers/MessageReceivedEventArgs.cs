using Pinknose.DistributedWorkers.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pinknose.DistributedWorkers
{
    public enum MessageResponse { Ack, Nack, RejectRequeue, RejectPermanent }

    public class MessageReceivedEventArgs : EventArgs
    {
        public MessageReceivedEventArgs(MessageBase message, byte[] rawData)
        {
            Message = message;
            RawData = rawData;
            Response = MessageResponse.RejectRequeue;
        }

        /// <summary>
        /// The serialized message.
        /// </summary>
        public MessageBase Message { get; private set; }

        /// <summary>
        /// The raw message data,
        /// </summary>
        public byte[] RawData { get; private set; }

        public MessageResponse Response { get; set; }
    }
}
