using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ScenarioForge.Json
{
    /// <summary>
    /// Explicit converters for Unity structs. Without them Newtonsoft follows properties such as
    /// <c>Vector3.normalized</c> and loops forever.
    /// </summary>
    static class UnityStructConverters
    {
        internal static JsonConverter[] Create() => new JsonConverter[]
        {
            new Vector2Converter(),
            new Vector3Converter(),
            new QuaternionConverter(),
            new ColorConverter()
        };

        static float Read(JObject obj, string name, float fallback)
        {
            JToken token = obj[name];
            return token == null || token.Type == JTokenType.Null ? fallback : token.Value<float>();
        }

        static JObject Load(JsonReader reader, Type type)
        {
            JToken token = JToken.Load(reader);
            if (token is JObject obj)
                return obj;
            throw new JsonSerializationException($"Expected an object for {type.Name} at '{token.Path}', but found {token.Type}.");
        }

        sealed class Vector2Converter : JsonConverter<Vector2>
        {
            public override void WriteJson(JsonWriter writer, Vector2 value, JsonSerializer serializer)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("x"); writer.WriteValue(value.x);
                writer.WritePropertyName("y"); writer.WriteValue(value.y);
                writer.WriteEndObject();
            }

            public override Vector2 ReadJson(JsonReader reader, Type objectType, Vector2 existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                JObject obj = Load(reader, objectType);
                return new Vector2(Read(obj, "x", 0f), Read(obj, "y", 0f));
            }
        }

        sealed class Vector3Converter : JsonConverter<Vector3>
        {
            public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("x"); writer.WriteValue(value.x);
                writer.WritePropertyName("y"); writer.WriteValue(value.y);
                writer.WritePropertyName("z"); writer.WriteValue(value.z);
                writer.WriteEndObject();
            }

            public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                JObject obj = Load(reader, objectType);
                return new Vector3(Read(obj, "x", 0f), Read(obj, "y", 0f), Read(obj, "z", 0f));
            }
        }

        sealed class QuaternionConverter : JsonConverter<Quaternion>
        {
            public override void WriteJson(JsonWriter writer, Quaternion value, JsonSerializer serializer)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("x"); writer.WriteValue(value.x);
                writer.WritePropertyName("y"); writer.WriteValue(value.y);
                writer.WritePropertyName("z"); writer.WriteValue(value.z);
                writer.WritePropertyName("w"); writer.WriteValue(value.w);
                writer.WriteEndObject();
            }

            public override Quaternion ReadJson(JsonReader reader, Type objectType, Quaternion existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                JObject obj = Load(reader, objectType);
                return new Quaternion(Read(obj, "x", 0f), Read(obj, "y", 0f), Read(obj, "z", 0f), Read(obj, "w", 1f));
            }
        }

        sealed class ColorConverter : JsonConverter<Color>
        {
            public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("r"); writer.WriteValue(value.r);
                writer.WritePropertyName("g"); writer.WriteValue(value.g);
                writer.WritePropertyName("b"); writer.WriteValue(value.b);
                writer.WritePropertyName("a"); writer.WriteValue(value.a);
                writer.WriteEndObject();
            }

            public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                JObject obj = Load(reader, objectType);
                return new Color(Read(obj, "r", 0f), Read(obj, "g", 0f), Read(obj, "b", 0f), Read(obj, "a", 1f));
            }
        }
    }
}
