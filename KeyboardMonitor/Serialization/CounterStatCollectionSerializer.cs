using System;
using System.Linq;
using KeyboardMonitor.Stats;
using KeyboardMonitor.Stats.Support;
using Newtonsoft.Json;

namespace KeyboardMonitor.Serialization
{
    public class CounterStatCollectionSerializer : JsonConverter
    {
        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter"/> to write to.</param><param name="value">The value.</param><param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var cc = (ICounterStatCollection)value;
            writer.WriteStartObject();

            writer.WritePropertyName("Name");
            serializer.Serialize(writer, cc.Name);

            writer.WritePropertyName("Value");
            serializer.Serialize(writer, Math.Round(cc.Value, 2));

            writer.WritePropertyName("Values");
            writer.WriteStartArray();
            foreach (var v in cc.Select(t => t.Value))
            {
                serializer.Serialize(writer, Math.Round(v, 2));
            }
            writer.WriteEndArray();

            writer.WriteEndObject();
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader"/> to read from.</param><param name="objectType">Type of the object.</param><param name="existingValue">The existing value of object being read.</param><param name="serializer">The calling serializer.</param>
        /// <returns>
        /// The object value.
        /// </returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>
        /// <c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanConvert(Type objectType)
        {
            throw new NotImplementedException();
        }
    }
}