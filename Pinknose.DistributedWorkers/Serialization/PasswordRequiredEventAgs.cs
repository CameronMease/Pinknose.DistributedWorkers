using System;
using System.Collections.Generic;
using System.Text;

namespace Pinknose.DistributedWorkers.Serialization
{
    public class PasswordRequiredEventAgs : EventArgs
    {
        public PasswordRequiredEventAgs()
        {
        }

        public string Password { get; set; } = "";
    }
}
