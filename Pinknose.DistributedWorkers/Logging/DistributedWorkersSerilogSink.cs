using Pinknose.DistributedWorkers.MessageTags;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pinknose.DistributedWorkers.Logging
{
    public class DistributedWorkersSerilogSink : ILogEventSink
    {
        private readonly IFormatProvider _formatProvider;
        private MessageClientBase messageClient;

        public DistributedWorkersSerilogSink(MessageClientBase client, IFormatProvider formatProvider)
        {
            messageClient = client;
            _formatProvider = formatProvider;
        }

        public void Emit(LogEvent logEvent)
        {
            var message = new SerilogEventMessage(
                logEvent,
                false,
                false,
                true,
                false);

            messageClient.WriteToSubscriptionQueues(message);
        }

        
    }
}
