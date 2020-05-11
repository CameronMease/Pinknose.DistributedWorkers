using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Pinknose.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Pinknose.DistributedWorkers
{
    public class JsonShouldSerializeContractResolver : DefaultContractResolver
    {
        public static readonly JsonShouldSerializeContractResolver Instance = new JsonShouldSerializeContractResolver();

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            bool shouldSerialize;

            if (//property.DeclaringType != typeof(BatchDataStore) &&
                property.PropertyType.IsAssignableTo(typeof(IEnumerable)) &&
                property.PropertyType != typeof(string) &&
                property.PropertyType != typeof(byte[]))
            {
                shouldSerialize = false;
            }
            else
            {
                shouldSerialize = true;
            }


            property.ShouldSerialize = instance => shouldSerialize;

            return property;
        }
    }
}
