using Pinknose.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Pinknose.DistributedWorkers.MessageTags
{
    /// <summary>
    /// Represents a tag that can be used to subscribe to certain messages.
    /// </summary>
    [Serializable]
    public class MessageTag : IEquatable<MessageTag>
    {
        public MessageTag(string tagName)
        {
            TagName = tagName.ToLower();
        }

        public string TagName { get; private set; }

        internal virtual string GetMangledTagAndValue()
        {
            return MangleTag(this.TagName, "");
        }

        internal string GetMangledTagName()
        {
            return MangleTag(this.TagName, "");
        }

        internal static string MangleTag(string tagName, object tagValue)
        {
            return $"{tagName}:{tagValue.ToString().ToLower()}";
        }

        public override bool Equals(object obj)
        {
            return obj.GetType().IsAssignableTo(typeof(MessageTag)) && this.Equals((MessageTag)obj);
        }

        public bool Equals([AllowNull] MessageTag other)
        {
            return !(other is null) && this.GetMangledTagAndValue() == other.GetMangledTagAndValue();
        }

        public static bool operator ==(MessageTag tag1, MessageTag tag2)
        {
            return tag1.Equals(tag2);
        }

        public static bool operator !=(MessageTag tag1, MessageTag tag2)
        {
            return !tag1.Equals(tag2);
        }

        public static MessageTagCollection operator | (MessageTag tag1, MessageTag tag2)
        {
            var tags = new MessageTagCollection();
            tags.Add(tag1);
            tags.Add(tag2);
            return tags;
        }

        public static MessageTagCollection operator | (MessageTagCollection tags, MessageTag tag1)
        {
            tags.Add(tag1);
            return tags;
        }

        public override int GetHashCode()
        {
            return GetMangledTagAndValue().GetHashCode(StringComparison.Ordinal);
        }

        public override string ToString()
        {
            return GetMangledTagAndValue();
        }
    }
}
