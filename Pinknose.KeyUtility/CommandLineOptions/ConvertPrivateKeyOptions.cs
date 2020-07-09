using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pinknose.DistributedWorkers.KeyUtility.CommandLineOptions
{
    [Verb("convert-private-key", HelpText = "Convert a JSON-formatted private key to a user- or machine-encrypted file.")]
    internal class ConvertPrivateKeyOptions
    {
        [Option(shortName: 'd', longName: "dir", HelpText = "Directory to store the key file(s).")]
        public string Directory { get; set; } = null;

        [Option(shortName: 'k', longName: "key-file", Required = true, HelpText = "JSON private key file name/path.")]
        public string JsonPrivateKey { get; set; }

        [Option(shortName: 'u', longName: "user-encrypted", Group = "Private Key File Format", HelpText = "Store private key in encrypted format that can be decrypted by any process run by the current user.")]
        public bool CurrentUserEncryptedPrivateKey { get; set; } = false;

        [Option(shortName: 'l', longName: "local-machine-encrypted", Group = "Private Key File Format", HelpText = "Store private key in encrypted format that can be decrypted by any process running on this machine.")]
        public bool LocalMachineEncryptedPrivateKey { get; set; } = false;

    }
}
