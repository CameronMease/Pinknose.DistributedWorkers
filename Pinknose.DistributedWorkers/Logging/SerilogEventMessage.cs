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
        public SerilogEventMessage(LogEvent payload, bool encryptMessage, bool compressPayload, bool serializePayloadToJson, bool dataIsAlreadyCompressed = false, params MessageTag[] tags) : base(payload, encryptMessage, compressPayload, serializePayloadToJson, dataIsAlreadyCompressed, tags)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            Tags.Add(SystemTags.SerilogEvent(payload.Level));
        }

        public override Guid MessageTypeGuid => new Guid("E29E6A36-7D57-4515-B3CA-0ACF288DF8A9");
    }
}
