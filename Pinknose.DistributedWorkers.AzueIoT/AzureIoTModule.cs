using Microsoft.Azure.Devices.Client;
using Pinknose.DistributedWorkers.Messages;
using Pinknose.DistributedWorkers.MessageTags;
using Pinknose.DistributedWorkers.Modules;
using Pinknose.DistributedWorkers.XBee.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pinknose.DistributedWorkers.AzueIoT
{
    public class AzureIoTModule : ClientModule
    {
        public event EventHandler<IoTHubMessageReceivedEventArgs> IoTHubMessageReceived;

        DeviceClient azureDeviceClient;
        Task messageReceiveTask;

        public AzureIoTModule(string azureIoTHubConnectionString, params MessageTag[] tags) : base(tags)
        {
            azureDeviceClient = DeviceClient.CreateFromConnectionString(azureIoTHubConnectionString);
            this.MessageClientRegistered += AzureIoTModule_MessageClientRegistered;

            messageReceiveTask = Task.Run(async () => 
            {
                while (true)
                {
                    var message = await azureDeviceClient.ReceiveAsync();

                    if (message != null)
                    {
                        var eventArgs = new IoTHubMessageReceivedEventArgs(message);

                        IoTHubMessageReceived?.Invoke(this, eventArgs);

                        switch (eventArgs.ResponseAction)
                        {
                            case ResponseAction.Complete:
                                await azureDeviceClient.CompleteAsync(message);
                                break;

                            case ResponseAction.Reject:
                                await azureDeviceClient.RejectAsync(message);
                                break;

                            case ResponseAction.Abandon:
                                await azureDeviceClient.AbandonAsync(message);
                                break;

                            default:
                                throw new NotImplementedException();
                        }                        
                    }
                };
            });
        }

        private void AzureIoTModule_MessageClientRegistered(object sender, MessageClientRegisteredEventArgs e)
        {
            e.MessageClient.MessageReceived += MessageClient_MessageReceived;
        }

        private void MessageClient_MessageReceived(object sender, MessageQueues.MessageReceivedEventArgs e)
        {
            if (ForwardMessageToIoTHubTransform != null)
            {
                var sendMessage = ForwardMessageToIoTHubTransform(e.MessageEnevelope);
                azureDeviceClient.SendEventAsync(sendMessage);
            }
        }

        public AzureIoTModule(string azureIoTHubConnectionString, MessageTagCollection tags) : this(azureIoTHubConnectionString, tags.ToArray())
        {
            // Do not implement
        }

        /// <summary>
        /// Implement this delegate to control transformation of a DistributedWorkers message to an Azure IoT message.  Return null if you
        /// do not want to foward the message.  If this delegate is not implemented, no messages will be fowarded to Azure IoT Hub.
        /// </summary>
        public Func<MessageEnvelope, Microsoft.Azure.Devices.Client.Message> ForwardMessageToIoTHubTransform { get; set; } = null;
    }
}
