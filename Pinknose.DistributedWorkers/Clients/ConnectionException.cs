using System;
using System.Collections.Generic;
using System.Text;

namespace Pinknose.DistributedWorkers.Clients
{
    public class ConnectionException : Exception
    {
        public ConnectionException(string message) : base(message)
        {
        }

        public ConnectionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public ConnectionException()
        {
        }
    }
}
