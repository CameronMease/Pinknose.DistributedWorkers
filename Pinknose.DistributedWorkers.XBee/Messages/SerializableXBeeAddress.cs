﻿using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using XBeeLibrary.Core.Models;

namespace Pinknose.DistributedWorkers.XBee.Messages
{
    [Serializable]
    public class SerializableXBeeAddress : ISerializable
    {
        private const string ValueName = "Value";

        protected SerializableXBeeAddress(SerializationInfo info, StreamingContext context)
        {
            Is64BitAddress = info.GetBoolean(nameof(Is64BitAddress));

            if (Is64BitAddress)
            {
                XBee64BitAddress = new XBee64BitAddress((byte[])info.GetValue(ValueName, typeof(byte[])));
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public SerializableXBeeAddress(XBee64BitAddress address)
        {
            XBee64BitAddress = address;
            Is64BitAddress = true;
        }


        [field: NonSerializedAttribute()]
        private XBee64BitAddress XBee64BitAddress { get; set; }

        public bool Is64BitAddress { get; private set; } = true;

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Is64BitAddress), Is64BitAddress);

            if (Is64BitAddress)
            {
                info.AddValue(ValueName, XBee64BitAddress.Value);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public override string ToString()
        {
            if (Is64BitAddress)
            {
                return XBee64BitAddress.ToString();
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
