using EasyNetQ.Management.Client.Model;
using Pinknose.DistributedWorkers.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pinknose.DistributedWorkers.Exceptions
{
    public class IdentityException : Exception
    {
        public IdentityException(string message, MessageEnvelope messageEnvelope) : base(message)
        {
            MessageEnvelope = messageEnvelope;
        }

        public IdentityException(string message, Exception innerException, MessageEnvelope messageEnvelope) : base(message, innerException)
        {
            MessageEnvelope = messageEnvelope;
        }

        public MessageEnvelope MessageEnvelope { get; private set; }
    }
}
