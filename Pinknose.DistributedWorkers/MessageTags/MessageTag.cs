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

using Pinknose.Utilities;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Pinknose.DistributedWorkers.MessageTags
{
    /// <summary>
    /// Represents a tag that can be used to subscribe to certain messages.
    /// </summary>
    [Serializable]
    public class MessageTag : IEquatable<MessageTag>
    {
        #region Constructors

        public MessageTag(string tagName)
        {
            TagName = tagName.ToLower();
        }

        #endregion Constructors

        #region Properties

        public string TagName { get; private set; }

        public virtual bool HasValue => false;

        #endregion Properties

        #region Methods

        public static bool operator !=(MessageTag tag1, MessageTag tag2)
        {
            return !tag1.Equals(tag2);
        }

        public static bool operator ==(MessageTag tag1, MessageTag tag2)
        {
            return tag1.Equals(tag2);
        }

        public override bool Equals(object obj)
        {
            return obj.GetType().IsAssignableTo(typeof(MessageTag)) && this.Equals((MessageTag)obj);
        }

        public bool Equals(MessageTag other)
        {
            return !(other is null) && this.GetMangledTagAndValue() == other.GetMangledTagAndValue();
        }

        public override int GetHashCode()
        {
            return GetMangledTagAndValue().GetHashCode();
        }

        public override string ToString()
        {
            return GetMangledTagAndValue();
        }

        public static MessageTagCollection operator |(MessageTag tag1, MessageTag tag2)
        {
            var tags = new MessageTagCollection();
            tags.Add(tag1);
            tags.Add(tag2);
            return tags;
        }

        public static MessageTagCollection operator |(MessageTagCollection tags, MessageTag tag1)
        {
            tags.Add(tag1);
            return tags;
        }

        internal static string MangleTag(string tagName, object tagValue)
        {
            return $"{tagName}:{tagValue.ToString().ToLower()}";
        }

        internal static MessageTag DemangleTag(string mangledTag)
        {
            string[] values = mangledTag.Split(':');

            if (values.Length != 2)
            {
                throw new NotImplementedException();
            }

            if (string.IsNullOrEmpty(values[1]))
            {
                return new MessageTag(values[0]);
            }
            else
            {
                return new MessageTagValue(values[0], values[1]);
            }
        }

        internal virtual string GetMangledTagAndValue()
        {
            return MangleTag(this.TagName, "");
        }

        internal string GetMangledTagName()
        {
            return MangleTag(this.TagName, "");
        }

        #endregion Methods
    }
}