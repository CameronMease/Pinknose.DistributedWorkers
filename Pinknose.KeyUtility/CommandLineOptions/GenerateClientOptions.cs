using CommandLine;
using System;
using System.Collections.Generic;
using System.Security;
using System.Text;

namespace Pinknose.DistributedWorkers.KeyUtility.CommandLineOptions
{
    [Verb("generate-client", HelpText = "Generate a new client key")]
    internal class GenerateClientOptions : GenerateOptionsBase
    {
      
        [Option(shortName: 'n', longName: "client-name", Required = true, HelpText = "The name of the client.")]
        public string ClientName { get; set; }

    }
}
