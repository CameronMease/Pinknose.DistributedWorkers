///////////////////////////////////////////////////////////////////////////////////
// MIT License
//
// Copyright(c) 2020 Cameron Mease
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
///////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Pinknose.DistributedWorkers.MessageTags
{
    [Serializable]
    public class MessageTagCollection : IEnumerable<MessageTag>, ISerializable
    {
        #region Fields

        private List<MessageTag> _tags;

        #endregion Fields

        #region Constructors

        public MessageTagCollection()
        {
            _tags = new List<MessageTag>();
        }

        public MessageTagCollection(IEnumerable<MessageTag> tags)
        {
            if (tags == null)
            {
                throw new ArgumentNullException(nameof(tags));
            }

            _tags = new List<MessageTag>();
            this.AddRange(tags);
        }

        protected MessageTagCollection(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            _tags = new List<MessageTag>((IEnumerable<MessageTag>)info.GetValue("tags", typeof(MessageTag[])));
        }

        #endregion Constructors

        #region Methods

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

        public void AddRange(IEnumerable<MessageTag> tags)
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

        public bool ContainsTag(MessageTag tag)
        {
            return _tags.Any(t => t == tag);
        }

        public IEnumerator<MessageTag> GetEnumerator()
        {
            return _tags.GetEnumerator();
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
            if (tags1 is null)
            {
                throw new ArgumentNullException(nameof(tags1));
            }

            if (tags2 is null)
            {
                throw new ArgumentNullException(nameof(tags2));
            }

            tags1.AddRange(tags2);
            return tags1;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _tags.GetEnumerator();
        }

        public static MessageTagCollection BitwiseOr(MessageTagCollection left, MessageTagCollection right) => left | right;
        

        #endregion Methods
    }
}