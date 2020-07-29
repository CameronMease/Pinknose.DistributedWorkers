using System;
using System.Collections.Generic;
using System.Text;

namespace Pinknose.DistributedWorkers.XBee.Messages
{
    [Serializable]
    public class XBeeFromXBeeMessage : XBeeMessageBase
    {
        public XBeeFromXBeeMessage(XBeeMessageType xBeeMessageType) : base(xBeeMessageType)
        {
        }

        public SerializableXBeeAddress XBeeSourceAddress { get; set; }

        public static XBeeFromXBeeMessage DeserializeFromXbee()
        {
            throw new NotImplementedException();
        }
    }
}
