using Newtonsoft.Json.Linq;
using Pinknose.DistributedWorkers.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Pinknose.DistributedWorkers.XBee.Messages
{
    public enum XBeeMessageType { Status = 0, Command =1 }

    [Serializable]
    public class XBeeMessageBase : MessageBase
    {
        public XBeeMessageBase(XBeeMessageType xBeeMessageType)
        {
            XBeeMessageType = xBeeMessageType;
        }

        [XBeeSerializeable("#MT")]
        public XBeeMessageType XBeeMessageType { get; private set; }

        
        
    }
}
