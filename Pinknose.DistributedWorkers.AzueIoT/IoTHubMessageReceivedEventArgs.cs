using System;
using System.Collections.Generic;
using System.Text;

namespace Pinknose.DistributedWorkers.AzueIoT
{
    public enum ResponseAction { 
        /// <summary>
        /// Deletes the message from the message queue.
        /// </summary>
        Complete,
        /// <summary>
        /// Puts a message back on the message queue.
        /// </summary>
        Abandon,
        /// <summary>
        /// Deletes the message from the message queue and tells the server the message could not be processed.
        /// </summary>
        Reject
    }

    public class IoTHubMessageReceivedEventArgs : EventArgs
    {
        

        public IoTHubMessageReceivedEventArgs(Microsoft.Azure.Devices.Client.Message message) : base()
        {

        }

        public Microsoft.Azure.Devices.Client.Message Message { get; private set; }

        public ResponseAction ResponseAction { get; set; } = ResponseAction.Complete;
    }
}
