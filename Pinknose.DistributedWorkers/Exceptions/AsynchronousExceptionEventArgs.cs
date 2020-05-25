using System;

namespace Pinknose.DistributedWorkers.Exceptions
{

    public class AsynchronousExceptionEventArgs : EventArgs
    {
        public AsynchronousExceptionEventArgs(Exception exception) : base()
        {
            Exception = exception;
        }

        public Exception Exception { get; private set; }
    }

}