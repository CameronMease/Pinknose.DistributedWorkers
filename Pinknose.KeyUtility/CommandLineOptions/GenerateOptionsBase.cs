using CommandLine;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;

namespace Pinknose.DistributedWorkers.KeyUtility.CommandLineOptions
{
    public abstract class GenerateOptionsBase
    {
        [Option(shortName: 's', longName: "system-name", Required = true, HelpText = "The name of the system.")]
        public string SystemName { get; set; }

        [Option(longName: "256", Group = "Key Size", HelpText = "Use key size of 256 bits.")]
        public bool KeySize256 { get; set; } = false;

        [Option(longName: "384", Group = "Key Size", HelpText = "Use key size of 384 bits.")]
        public bool KeySize384 { get; set; } = false;

        [Option(longName: "521", Group = "Key Size", HelpText = "Use key size of 512 bits.")]
        public bool KeySize521 { get; set; } = false;

        [Option(shortName: 'd', longName:"dir", HelpText = "Directory to store the key files.")]
        public string Directory { get; set; } = null;

        [Option(shortName: 'j', longName: "json", Group = "Private Key File Format", HelpText = "Store private key in JSON file.")]
        public bool JsonPrivateKey { get; set; } = false;

        [Option(shortName: 'u', longName: "user-encrypted", Group = "Private Key File Format", HelpText = "Store private key in encrypted format that can be decrypted by any process run by the current user.")]
        public bool CurrentUserEncryptedPrivateKey { get; set; } = false;

        [Option(shortName: 'l', longName: "local-macine-encrypted", Group = "Private Key File Format", HelpText = "Store private key in encrypted format that can be decrypted by any process running on this machine.")]
        public bool LocalMachineEncryptedPrivateKey { get; set; } = false;

    }
}
