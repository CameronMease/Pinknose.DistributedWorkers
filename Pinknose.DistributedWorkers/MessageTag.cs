using System;
using System.Collections.Generic;
using System.Text;

namespace Pinknose.DistributedWorkers
{
    /// <summary>
    /// Represents a tag that can be used to subscribe to certain messages.
    /// </summary>
    public class MessageTag
    {
        public MessageTag(string tagName)
        {
            TagName = tagName;
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
            return $"{tagName}:{tagValue.ToString()}";
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
    }
}
