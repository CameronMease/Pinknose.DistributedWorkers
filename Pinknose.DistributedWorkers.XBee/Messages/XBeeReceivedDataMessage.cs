using Pinknose.DistributedWorkers.Messages;
using System;
using System.Collections.Generic;
using System.Text;
using XBeeLibrary.Core.Models;

namespace Pinknose.DistributedWorkers.XBee.Messages
{
    [Serializable]
    public class XBeeReceivedDataMessage : XBeeMessageBase 
    {
        public XBeeReceivedDataMessage(XBeeMessage xBeeMessage) : base(XBeeMessageType.Status)
        {
            XBeeSender = xBeeMessage.Device.XBee64BitAddr.ToString();
            Data = xBeeMessage.Data;
            DataString = xBeeMessage.DataString;
        }

        public string XBeeSender {get; private set; }
        public string DataString {get; private set; }
        public byte[] Data { get; private set; }
    }
}
