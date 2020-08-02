using System;
using System.Collections.Generic;
using System.Text;
using XBeeLibrary.Core.Models;

namespace Pinknose.DistributedWorkers.XBee.Messages
{
    [Serializable]
    public class XBeeFromXBeeMessage : XBeeMessageBase
    {
        private string _rawData;

        public XBeeFromXBeeMessage(XBeeMessage xBeeMessage) : this(new SerializableXBeeAddress(xBeeMessage.Device.XBee64BitAddr), xBeeMessage.DataString)
        {

        }

        public XBeeFromXBeeMessage(SerializableXBeeAddress sourceAddress, string rawData) : base()
        {
            XBeeSourceAddress = sourceAddress;
            _rawData = rawData;
        }

        public SerializableXBeeAddress XBeeSourceAddress { get; private set; }

        public static XBeeFromXBeeMessage DeserializeFromXbee()
        {
            throw new NotImplementedException();
        }

        public override string RawData => _rawData;
    }
}
