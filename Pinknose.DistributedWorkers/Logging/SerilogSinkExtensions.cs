using Pinknose.DistributedWorkers.Clients;
using Serilog;
using Serilog.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pinknose.DistributedWorkers.Logging
{
    public static class SerilogSinkExtensions
    {
        public static LoggerConfiguration DistributedWorkersSink(
              this LoggerSinkConfiguration loggerConfiguration,
              MessageClientBase client,
              IFormatProvider formatProvider = null)
        {
            if (loggerConfiguration == null)
            {
                throw new ArgumentNullException(nameof(loggerConfiguration));
            }
            
            return loggerConfiguration.Sink(new DistributedWorkersSerilogSink(client, formatProvider));
        }
    }
}
