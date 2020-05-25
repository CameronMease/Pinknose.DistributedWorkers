using Pinknose.DistributedWorkers.Messages;
using Pinknose.DistributedWorkers.MessageTags;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pinknose.DistributedWorkers.Logging
{
    [Serializable]
    public class SerilogEventMessage : PayloadMessage<LogEvent>
    {
        public SerilogEventMessage(LogEvent payload, params MessageTag[] tags) : base(payload, false, true, false, tags)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            //TODO: How do we do this now?
            //Tags.Add(SystemTags.SerilogEvent(payload.Level));
        }

        public override Guid MessageTypeGuid => new Guid("E29E6A36-7D57-4515-B3CA-0ACF288DF8A9");
    }
}
