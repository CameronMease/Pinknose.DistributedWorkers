using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pinknose.DistributedWorkers.MessageTags
{
    public static class SystemTags
    {
        private const string SerilogTagName = "SerilogEvent";
        public static MessageTag SerilogAllEvents => new MessageTag(SerilogTagName);
        public static MessageTagValue SerilogEvent(LogEventLevel eventLevel) => new MessageTagValue(SerilogTagName, eventLevel);
        public static MessageTagValue SerilogDebugEvent => SerilogEvent(LogEventLevel.Debug);
        public static MessageTagValue SerilogErrorEvent => SerilogEvent(LogEventLevel.Error);
        public static MessageTagValue SerilogFatalEvent => SerilogEvent(LogEventLevel.Fatal);
        public static MessageTagValue SerilogVerboseEvent => SerilogEvent(LogEventLevel.Verbose);
        public static MessageTagValue SerilogWarningEvent => SerilogEvent(LogEventLevel.Warning);
        public static MessageTagValue SerilogInformationEvent => SerilogEvent(LogEventLevel.Information);
        public static MessageTag BroadcastTag => new MessageTag("broadcast");
    }
}
