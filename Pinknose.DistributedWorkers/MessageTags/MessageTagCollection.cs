using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Pinknose.DistributedWorkers.MessageTags
{
    [Serializable]
    public class MessageTagCollection : IEnumerable<MessageTag>, ISerializable
    {
        private List<MessageTag> _tags;

        public MessageTagCollection()
        {
            _tags = new List<MessageTag>();
        }

        public MessageTagCollection(IEnumerable<MessageTag>tags)
        {
            if (tags == null)
            {
                throw new ArgumentNullException(nameof(tags));
            }

            _tags = new List<MessageTag>();
            this.AddRange(tags);
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
            if (tag == null)
            {
                throw new ArgumentNullException(nameof(tag));
            }

            if (!tag.GetType().CustomAttributes.Any(a => a.AttributeType == typeof(SerializableAttribute)))
            {
                //TODO: Create new exception
                throw new Exception("Tag must be have the attribute 'Serializable'.");
            }
            _tags.Add(tag);
        }

        public void AddRange(IEnumerable<MessageTag>tags)
        {
            if (tags == null)
            {
                throw new ArgumentNullException(nameof(tags));
            }

            foreach (var tag in tags)
            {
                this.Add(tag);
            }
        }

        protected MessageTagCollection(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            _tags = new List<MessageTag>((IEnumerable<MessageTag>)info.GetValue("tags", typeof(MessageTag[])));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            info.AddValue("tags", _tags.ToArray());
        }

        public static MessageTagCollection operator |(MessageTagCollection tags1, MessageTagCollection tags2)
        {
            tags1.AddRange(tags2);
            return tags1;
        }

        public bool ContainsTag(MessageTag tag)
        {
            return _tags.Any(t => t == tag);
        }
    }
}
