using EasyNetQ.Management.Client.Model;
using Pinknose.DistributedWorkers.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pinknose.DistributedWorkers.Exceptions
{
#pragma warning disable CA1032 // Implement standard exception constructors
    public class IdentityException : Exception
#pragma warning restore CA1032 // Implement standard exception constructors
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
