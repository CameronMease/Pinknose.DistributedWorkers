using Pinknose.DistributedWorkers.MessageTags;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pinknose.DistributedWorkers.Modules
{
    public sealed class CoordinatorModule : ClientModule
    {
        public CoordinatorModule(MessageTagCollection tags) : this(tags.ToArray())
        {
        }

        public CoordinatorModule(params MessageTag[] tags) : base(tags)
        {


        }
    }
}
