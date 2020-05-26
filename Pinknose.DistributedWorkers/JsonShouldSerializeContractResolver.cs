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

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Pinknose.Utilities;
using System.Collections;
using System.Reflection;

namespace Pinknose.DistributedWorkers
{
    public class JsonShouldSerializeContractResolver : DefaultContractResolver
    {
        #region Fields

        public static readonly JsonShouldSerializeContractResolver Instance = new JsonShouldSerializeContractResolver();

        #endregion Fields

        #region Methods

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

        #endregion Methods
    }
}