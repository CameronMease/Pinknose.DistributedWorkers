using Pinknose.DistributedWorkers.XBee.Messages;
using Pinknose.Utilities;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using XBeeLibrary.Core;

namespace Pinknose.DistributedWorkers.XBee.Serial
{
    public class XBeeRemoteSerialPort
    {
        RemoteXBeeDevice remoteDevice;

        public XBeeRemoteSerialPort(XBeeNetworkGatewayModule xBeeGateway, SerializableXBeeAddress remoteXBeeAddress)
        {
            RemoteXBeeAddress = remoteXBeeAddress;
            XBeeGateway = xBeeGateway;

            remoteDevice = XBeeGateway.GetRemoteDevice(RemoteXBeeAddress);

            var  test =  Encoding.UTF8.GetBytes("This is a test!");

            this.Write(test, 0, test.Length);

            //TODO: Set remote mode and destination address???
        }

        public bool IsOpen => XBeeGateway.XBeeIsOpen;

        public event SerialDataReceivedEventHandler DataReceived;


        public void Write(byte[] buffer, int offset, int count)
        {
            XBeeGateway.Write(remoteDevice, buffer.RangeByLength(offset, count));
        }

        public SerializableXBeeAddress RemoteXBeeAddress { get; private set; }

        public XBeeNetworkGatewayModule XBeeGateway { get; private set; }
    }
}
