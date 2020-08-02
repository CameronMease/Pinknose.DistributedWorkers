using Pinknose.DistributedWorkers.MessageTags;
using Pinknose.DistributedWorkers.Modules;
using Pinknose.DistributedWorkers.XBee.Messages;
using Pinknose.Utilities;
using Serilog;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using XBeeLibrary.Core.Events;
using XBeeLibrary.Core.Models;

namespace Pinknose.DistributedWorkers.XBee
{
    public class XBeeNetworkGatewayModule : ClientModule
    {
        #region Fields

        private XBeeLibrary.Windows.ZigBeeDevice xBee;

        private bool xBeeWasOpen = false;

        private bool tryToReconnect = false;

        private ReusableThreadSafeTimer checkXBeeTimer = new ReusableThreadSafeTimer()
        {
            Interval = 1000,
            AutoReset = true
        };


        #endregion Fields

        #region Constructors

        public XBeeNetworkGatewayModule(string xbeeComPortName, XBeeLibrary.Windows.Connection.Serial.SerialPortParameters xbeeComPortParameters, MessageTagCollection tags) :
            this(xbeeComPortName, xbeeComPortParameters, tags.ToArray())
        {
            // Implement in constructor below:
        }

        public XBeeNetworkGatewayModule(string xBeeComPortName, XBeeLibrary.Windows.Connection.Serial.SerialPortParameters xBeeComPortParameters, params MessageTag[] tags) : base(tags)
        {
            XBeeComPortName = xBeeComPortName;
            XBeeComPortParameters = xBeeComPortParameters;
            xBee = new XBeeLibrary.Windows.ZigBeeDevice(XBeeComPortName, XBeeComPortParameters);

            checkXBeeTimer.Elapsed += CheckXBeeTimer_Elapsed;
        }

        #endregion Constructors

        #region Events

        public event EventHandler<ModemStatusReceivedEventArgs> ModemStatusReceived;

        public event EventHandler<XBeeLibrary.Core.Events.DataReceivedEventArgs> XBeeDataReceived;

        #endregion Events

        #region Properties

        public string XBeeComPortName { get; private set; }
        public XBeeLibrary.Windows.Connection.Serial.SerialPortParameters XBeeComPortParameters { get; private set; }
        public bool XBeeIsOpen => xBee.IsOpen;

        /// <summary>
        /// Forward data received from the XBee network to subscription queues.
        /// </summary>
        public bool ForwardReceivedData { get; set; } = true;

        #endregion Properties

        #region Methods

        public void OpenXBee()
        {
            xBee.Open();

            var network = xBee.GetNetwork();

            xBeeWasOpen = true;
            checkXBeeTimer.Start();

            //xBeeNetwork = xBee.GetNetwork();

            xBee.DataReceived += XBee_DataReceived;
            xBee.ModemStatusReceived += XBee_ModemStatusReceived;
            xBee.IOSampleReceived += XBee_IOSampleReceived;
            xBee.MicroPythonDataReceived += XBee_MicroPythonDataReceived;
            xBee.PacketReceived += XBee_PacketReceived;
        }

        public void CloseXBee()
        {
            checkXBeeTimer.Stop();
            xBeeWasOpen = false;
            xBee.Close();
        }

        private void CheckXBeeTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (xBeeWasOpen && !xBee.IsOpen && !tryToReconnect)
            {
                //TODO: add event
                tryToReconnect = true;
                //xBee.Close();
            }
            else if (tryToReconnect)
            {
                try
                {
                    xBee.Close();
                    this.OpenXBee();
                    tryToReconnect = false;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message);
                }
            }
        }

        private void XBee_PacketReceived(object sender, XBeeLibrary.Core.Events.PacketReceivedEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void XBee_MicroPythonDataReceived(object sender, XBeeLibrary.Core.Events.Relay.MicroPythonDataReceivedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void XBee_IOSampleReceived(object sender, XBeeLibrary.Core.Events.IOSampleReceivedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void XBee_ModemStatusReceived(object sender, XBeeLibrary.Core.Events.ModemStatusReceivedEventArgs e)
        {
            Log.Verbose(e.ModemStatusEvent.ToDisplayString());
            this.ModemStatusReceived?.Invoke(this, e);
        }

        private void XBee_DataReceived(object sender, XBeeLibrary.Core.Events.DataReceivedEventArgs e)
        {
            XBeeDataReceived?.Invoke(this, e);

            if (ForwardReceivedData)
            {
                var message = new XBeeFromXBeeMessage(e.DataReceived);

                //TODO: When to encrypt?
                this.MessageClient.WriteToSubscriptionQueues(message, false, new XBeeReceivedDataTag());
            }
        }

        #endregion Methods
    }
}