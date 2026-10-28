using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Global.Serialization
{
    /// <summary>
    /// Serializes <see cref="Vector3"/> as its three axes only. Without this converter Json.NET walks
    /// derived properties such as <c>normalized</c> and <c>magnitude</c>.
    /// </summary>
    public sealed class Vector3JsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(Vector3) || objectType == typeof(Vector3?);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return objectType == typeof(Vector3?) ? null : (object)Vector3.zero;
            }

            JObject json = JObject.Load(reader);

            return new Vector3(ReadAxis(json, "x"), ReadAxis(json, "y"), ReadAxis(json, "z"));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null) { writer.WriteNull(); return; }
            var vector = (Vector3)value;
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(vector.x);
            writer.WritePropertyName("y");
            writer.WriteValue(vector.y);
            writer.WritePropertyName("z");
            writer.WriteValue(vector.z);
            writer.WriteEndObject();
        }

        private static float ReadAxis(JObject json, string axis)
        {
            if (!json.TryGetValue(axis, StringComparison.OrdinalIgnoreCase, out JToken token))
            {
                return 0f;
            }

            return token.Type == JTokenType.Null ? 0f : token.Value<float>();
        }
    }
}
