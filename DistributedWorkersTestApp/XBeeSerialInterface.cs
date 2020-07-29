using System;
using System.Collections.Generic;
using System.Text;
using XBeeLibrary.Core.Connection;
using System.IO.Ports;
using System.Linq;

namespace DistributedWorkersTestApp
{
    public class XBeeSerialInterface : IConnectionInterface
    {
        private SerialPort serialPort;
        private DataStream stream;

        public XBeeSerialInterface(string portName, int baudRate, System.IO.Ports.Parity parity, int dataBits, System.IO.Ports.StopBits stopBits)
        {
            serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
            stream = new DataStream()
        }

        public bool IsOpen => serialPort.IsOpen;

        public DataStream Stream => throw new NotImplementedException();

        public void Close()
        {
            serialPort.Close();
        }

        public ConnectionType GetConnectionType() => ConnectionType.SERIAL;

        public void Open()
        {
            serialPort.Open();
        }

        public int ReadData(byte[] data)
        {
            return ReadData(data, 0, data.Length);
        }

        public int ReadData(byte[] data, int offset, int length)
        {
            return serialPort.Read(data, offset, length);
        }

        public void SetEncryptionKeys(byte[] key, byte[] txNonce, byte[] rxNonce)
        {
            throw new NotImplementedException();
        }

        public void WriteData(byte[] data)
        {
            WriteData(data, 0, data.Length);
        }

        public void WriteData(byte[] data, int offset, int length)
        {
            serialPort.Write(data, offset, length);
        }
    }
}
