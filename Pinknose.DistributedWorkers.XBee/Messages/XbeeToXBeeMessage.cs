using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Pinknose.DistributedWorkers.XBee.Messages
{
    [Serializable]
    public class XbeeToXBeeMessage : XBeeMessageBase
    {
        public XbeeToXBeeMessage(XBeeMessageType xBeeMessageType) : base(xBeeMessageType)
        {
        }

        public SerializableXBeeAddress XBeeDestinationAddress { get; set; }

        public string SerializeForXBee()
        {
            var props = this.GetType().GetProperties().Where(p => p.CustomAttributes.Any(a => a.AttributeType == typeof(XBeeSerializeableAttribute)));

            JObject jObject = new JObject();

            foreach (var prop in props)
            {
                var attrib = prop.GetCustomAttribute<XBeeSerializeableAttribute>();

                //jObject.Add(attrib.IdentifierName, ));
            }

            return "";
        }
    }
}
