using System;
using System.Collections.Generic;
using System.Text;

namespace Pinknose.DistributedWorkers.Serialization
{
    public class PasswordRequiredEventArgs : EventArgs
    {
        public PasswordRequiredEventArgs()
        {
        }

        public string Password { get; set; } = "";
    }
}
