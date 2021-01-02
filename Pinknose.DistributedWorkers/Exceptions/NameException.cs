using System;
using System.Collections.Generic;
using System.Text;

namespace Pinknose.DistributedWorkers.Exceptions
{
    public class NameException : Exception
    {
        public NameException(string message) : base(message)
        {
        }

        public NameException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
