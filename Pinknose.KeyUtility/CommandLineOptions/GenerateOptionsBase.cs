using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pinknose.KeyUtility.CommandLineOptions
{
    public abstract class GenerateOptionsBase
    {
        [Option(shortName: 's', longName: "system-name", Required = true, HelpText = "The name of the system.")]
        public string SystemName { get; set; }

        [Option(longName: "256", Group = "Key Size", HelpText = "Use key size of 256 bits.")]
        public bool KeySize256 { get; set; } = false;

        [Option(longName: "384", Group = "Key Size", HelpText = "Use key size of 384 bits.")]
        public bool KeySize384 { get; set; } = false;

        [Option(longName: "512", Group = "Key Size", HelpText = "Use key size of 512 bits.")]
        public bool KeySize512 { get; set; } = false;

        [Option(shortName: 'd', longName:"dir", HelpText = "Directory to store the key files.")]
        public string Directory { get; set; } = null;
    }
}
