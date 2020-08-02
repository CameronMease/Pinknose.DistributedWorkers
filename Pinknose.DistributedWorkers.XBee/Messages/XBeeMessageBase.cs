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
    public abstract class XBeeMessageBase : MessageBase
    {
        public XBeeMessageBase()
        {
        }       

        public abstract string RawData { get; }        
    }
}
