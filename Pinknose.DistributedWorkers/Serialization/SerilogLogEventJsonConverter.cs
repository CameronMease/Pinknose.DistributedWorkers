using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pinknose.DistributedWorkers.Logging;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;


namespace Pinknose.DistributedWorkers.Serialization
{
    public class SerilogLogEventJsonConverter : JsonConverter<LogEvent>
    {
        public override LogEvent ReadJson(JsonReader reader, Type objectType, LogEvent existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var jObject = JObject.ReadFrom(reader);

                

            return null;
        }

        public override void WriteJson(JsonWriter writer, LogEvent logEvent, JsonSerializer serializer)
        {
            if (logEvent is null)
            {
                throw new ArgumentNullException(nameof(logEvent));
            }

            JObject jObject = new JObject();
            jObject.Add(nameof(logEvent.Timestamp), logEvent.Timestamp);
            jObject.Add(nameof(logEvent.Level), logEvent.Level.ToString());
            //jObject.Add(nameof(logEvent.Exception), logEvent.Exception);
            //jObject.Add(nameof(logEvent.MessageTemplate), logEvent.MessageTemplate);

            jObject.WriteTo(writer);
        }
    }
}
