using System;
using System.Collections.Generic;
using System.Text;

namespace DistributedWorkersTestApp
{
    public class UserSecrets
    {
        public string PushoverAppApiKey { get; set; }
        public string PushoverUserKey { get; set; }

        public string AzureIoTHubConnectionString { get; set; }
    }
}
