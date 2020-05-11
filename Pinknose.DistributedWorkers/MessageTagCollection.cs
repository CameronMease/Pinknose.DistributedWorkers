using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Pinknose.DistributedWorkers
{
    public class MessageTagCollection : IEnumerable<MessageTag>
    {
        private List<MessageTag> _tags;

        public MessageTagCollection()
        {
            _tags = new List<MessageTag>();
        }

        public MessageTagCollection(IEnumerable<MessageTag>tags)
        {
            _tags = new List<MessageTag>(tags);
        }

        public IEnumerator<MessageTag> GetEnumerator()
        {
            return _tags.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _tags.GetEnumerator();
        }

        public void Add(MessageTag tag)
        {
            _tags.Add(tag);
        }

        public void AddRange(IEnumerable<MessageTag>tags)
        {
            _tags.AddRange(tags);
        }

        public static MessageTagCollection operator |(MessageTagCollection tags1, MessageTagCollection tags2)
        {
            tags1.AddRange(tags2);
            return tags1;
        }
    }
}
