using Pinknose.DistributedWorkers.MessageTags;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pinknose.DistributedWorkers.Modules
{
    public sealed class TrustCoordinatorModule : ClientModule
    {
        public TrustCoordinatorModule(MessageTagCollection tags) : this(tags.ToArray())
        {
        }

        public TrustCoordinatorModule(params MessageTag[] tags) : base(tags)
        {


        }
    }
}
