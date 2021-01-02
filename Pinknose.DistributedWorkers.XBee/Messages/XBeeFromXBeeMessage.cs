using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using XBeeLibrary.Core.Models;

namespace Pinknose.DistributedWorkers.XBee.Messages
{
    public class XBeeFromXBeeMessage : XBeeMessageBase
    {
        private string _rawData;

        public XBeeFromXBeeMessage(XBeeMessage xBeeMessage) : this(new SerializableXBeeAddress(xBeeMessage.Device.XBee64BitAddr), xBeeMessage.DataString)
        {

        }

        [JsonConstructor]
        public XBeeFromXBeeMessage(SerializableXBeeAddress xBeeSourceAddress, string rawData) : base()
        {
            XBeeSourceAddress = xBeeSourceAddress;
            _rawData = rawData;
        }

        public SerializableXBeeAddress XBeeSourceAddress { get; private set; }

        public static XBeeFromXBeeMessage DeserializeFromXbee()
        {
            throw new NotImplementedException();
        }

        public override string RawData => _rawData;

        public JObject GetJObject()
        {
            return (JObject)JsonConvert.DeserializeObject(RawData);
        }
    }
}
