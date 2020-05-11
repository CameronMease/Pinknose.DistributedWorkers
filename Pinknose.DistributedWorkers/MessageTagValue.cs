using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks.Dataflow;

namespace Pinknose.DistributedWorkers
{
    public class MessageTagValue : MessageTag
    {
        public MessageTagValue(string tagName, object value) : base(tagName)
        {
            Value = value;
        }

        public object Value { get; private set; }

        internal override string GetMangledTagAndValue()
        {
            return MangleTag(this.TagName, this.Value);
        }
    }
}
