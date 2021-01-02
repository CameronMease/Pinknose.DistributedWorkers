using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using XBeeLibrary.Core.Models;

namespace Pinknose.DistributedWorkers.XBee.Messages
{
    public class SerializableXBeeAddressJsonConverter : JsonConverter<SerializableXBeeAddress>
    {
        public override SerializableXBeeAddress ReadJson(JsonReader reader, Type objectType, SerializableXBeeAddress existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var jObject = JObject.ReadFrom(reader);


            bool is64BitsAddresss = jObject.Value<bool>(nameof(SerializableXBeeAddress.Is64BitAddress));

            if (!is64BitsAddresss)
            {
                throw new NotImplementedException();
            }

            string addressText = jObject.Value<string>("Address");

            var address =  new SerializableXBeeAddress(new XBee64BitAddress(addressText));
            return address;
        }

        public override void WriteJson(JsonWriter writer, SerializableXBeeAddress address, JsonSerializer serializer)
        {
            JObject jObject = new JObject();
            jObject.Add(nameof(address.Is64BitAddress), address.Is64BitAddress);

            if (address.Is64BitAddress)
            {
                jObject.Add("Address", address.ToString());
            }
            else
            {
                throw new NotImplementedException();
            }

            jObject.WriteTo(writer);
        }
    }
}
