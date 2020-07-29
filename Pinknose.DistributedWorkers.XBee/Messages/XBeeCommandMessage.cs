using System;
using System.Collections.Generic;
using System.Text;

namespace Pinknose.DistributedWorkers.XBee.Messages
{
    public enum XBeeDeviceCommand { SoftReset = 1 }

    [Serializable]
    public class XBeeCommandMessage : XBeeMessageBase
    {
        public XBeeCommandMessage() : base(XBeeMessageType.Command)
        {
        }

        
    }
}
