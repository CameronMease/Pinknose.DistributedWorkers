using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Pinknose.DistributedWorkers.XBee.Messages
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class XBeeSerializeableAttribute : Attribute
    { 
        public XBeeSerializeableAttribute([CallerMemberName] string identifierName = null) : base()
        {
            IdentifierName = identifierName;
        }

        public string IdentifierName { get; private set; }
    }
}
