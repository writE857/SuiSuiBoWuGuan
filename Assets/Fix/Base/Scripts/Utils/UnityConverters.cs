using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Fix.Editor
{
    public class UnityConverters
    {
        public static IEnumerable<JsonConverter> Converters => typeof(UnityConverters)
            .GetNestedTypes()
            .Select(Activator.CreateInstance)
            .OfType<JsonConverter>();

        public class Vector2Converter : JsonConverter<Vector2>
        {
            public override void WriteJson(JsonWriter writer, Vector2 value, JsonSerializer serializer)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("x");
                writer.WriteValue(value.x);
                writer.WritePropertyName("y");
                writer.WriteValue(value.y);
                writer.WriteEndObject();
            }

            public override Vector2 ReadJson(JsonReader reader, Type objectType, Vector2 existingValue,
                bool hasExistingValue, JsonSerializer serializer)
            {
                JObject jo = JObject.Load(reader);
                return new Vector2(
                    jo["x"].Value<float>(),
                    jo["y"].Value<float>()
                );
            }
        }

        public class Vector2IntConverter : JsonConverter<Vector2Int>
        {
            public override void WriteJson(JsonWriter writer, Vector2Int value, JsonSerializer serializer)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("x");
                writer.WriteValue(value.x);
                writer.WritePropertyName("y");
                writer.WriteValue(value.y);
                writer.WriteEndObject();
            }

            public override Vector2Int ReadJson(JsonReader reader, Type objectType, Vector2Int existingValue,
                bool hasExistingValue, JsonSerializer serializer)
            {
                JObject jo = JObject.Load(reader);
                return new Vector2Int(
                    jo["x"].Value<int>(),
                    jo["y"].Value<int>()
                );
            }
        }

        public class Vector3Converter : JsonConverter<Vector3>
        {
            public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("x");
                writer.WriteValue(value.x);
                writer.WritePropertyName("y");
                writer.WriteValue(value.y);
                writer.WritePropertyName("z");
                writer.WriteValue(value.z);
                writer.WriteEndObject();
            }

            public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue,
                bool hasExistingValue, JsonSerializer serializer)
            {
                JObject jo = JObject.Load(reader);
                return new Vector3(
                    jo["x"].Value<float>(),
                    jo["y"].Value<float>(),
                    jo["z"].Value<float>()
                );
            }
        }

        public class Vector3IntConverter : JsonConverter<Vector3Int>
        {
            public override void WriteJson(JsonWriter writer, Vector3Int value, JsonSerializer serializer)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("x");
                writer.WriteValue(value.x);
                writer.WritePropertyName("y");
                writer.WriteValue(value.y);
                writer.WritePropertyName("z");
                writer.WriteValue(value.z);
                writer.WriteEndObject();
            }

            public override Vector3Int ReadJson(JsonReader reader, Type objectType, Vector3Int existingValue,
                bool hasExistingValue, JsonSerializer serializer)
            {
                JObject jo = JObject.Load(reader);
                return new Vector3Int(
                    jo["x"].Value<int>(),
                    jo["y"].Value<int>(),
                    jo["z"].Value<int>()
                );
            }
        }

        public class Vector4Converter : JsonConverter<Vector4>
        {
            public override void WriteJson(JsonWriter writer, Vector4 value, JsonSerializer serializer)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("x");
                writer.WriteValue(value.x);
                writer.WritePropertyName("y");
                writer.WriteValue(value.y);
                writer.WritePropertyName("z");
                writer.WriteValue(value.z);
                writer.WritePropertyName("w");
                writer.WriteValue(value.w);
                writer.WriteEndObject();
            }

            public override Vector4 ReadJson(JsonReader reader, Type objectType, Vector4 existingValue,
                bool hasExistingValue, JsonSerializer serializer)
            {
                JObject jo = JObject.Load(reader);
                return new Vector4(
                    jo["x"].Value<float>(),
                    jo["y"].Value<float>(),
                    jo["z"].Value<float>(),
                    jo["w"].Value<float>()
                );
            }
        }

        public class ColorConverter : JsonConverter<Color>
        {
            public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("r");
                writer.WriteValue(value.r);
                writer.WritePropertyName("g");
                writer.WriteValue(value.g);
                writer.WritePropertyName("b");
                writer.WriteValue(value.b);
                writer.WritePropertyName("a");
                writer.WriteValue(value.a);
                writer.WriteEndObject();
            }

            public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue,
                bool hasExistingValue, JsonSerializer serializer)
            {
                JObject jo = JObject.Load(reader);
                return new Color(
                    jo["r"].Value<float>(),
                    jo["g"].Value<float>(),
                    jo["b"].Value<float>(),
                    jo["a"].Value<float>()
                );
            }
        }

        public class Color32Converter : JsonConverter<Color32>
        {
            public override void WriteJson(JsonWriter writer, Color32 value, JsonSerializer serializer)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("r");
                writer.WriteValue(value.r);
                writer.WritePropertyName("g");
                writer.WriteValue(value.g);
                writer.WritePropertyName("b");
                writer.WriteValue(value.b);
                writer.WritePropertyName("a");
                writer.WriteValue(value.a);
                writer.WriteEndObject();
            }

            public override Color32 ReadJson(JsonReader reader, Type objectType, Color32 existingValue,
                bool hasExistingValue, JsonSerializer serializer)
            {
                JObject jo = JObject.Load(reader);
                return new Color32(
                    jo["r"].Value<byte>(),
                    jo["g"].Value<byte>(),
                    jo["b"].Value<byte>(),
                    jo["a"].Value<byte>()
                );
            }
        }

        public class QuaternionConverter : JsonConverter<Quaternion>
        {
            public override void WriteJson(JsonWriter writer, Quaternion value, JsonSerializer serializer)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("x");
                writer.WriteValue(value.x);
                writer.WritePropertyName("y");
                writer.WriteValue(value.y);
                writer.WritePropertyName("z");
                writer.WriteValue(value.z);
                writer.WritePropertyName("w");
                writer.WriteValue(value.w);
                writer.WriteEndObject();
            }

            public override Quaternion ReadJson(JsonReader reader, Type objectType, Quaternion existingValue,
                bool hasExistingValue, JsonSerializer serializer)
            {
                JObject jo = JObject.Load(reader);
                return new Quaternion(
                    jo["x"].Value<float>(),
                    jo["y"].Value<float>(),
                    jo["z"].Value<float>(),
                    jo["w"].Value<float>()
                );
            }
        }

        public class BoundsConverter : JsonConverter<Bounds>
        {
            public override void WriteJson(JsonWriter writer, Bounds value, JsonSerializer serializer)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("center");
                writer.WriteValue(value.center);
                writer.WritePropertyName("extents");
                writer.WriteValue(value.extents);
                writer.WriteEndObject();
            }

            public override Bounds ReadJson(JsonReader reader, Type objectType, Bounds existingValue,
                bool hasExistingValue, JsonSerializer serializer)
            {
                JObject jo = JObject.Load(reader);
                return new Bounds(
                    jo["center"].Value<Vector3>(),
                    jo["extents"].Value<Vector3>()
                );
            }
        }

        public class BoundsIntConverter : JsonConverter<BoundsInt>
        {
            public override void WriteJson(JsonWriter writer, BoundsInt value, JsonSerializer serializer)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("position");
                writer.WriteValue(value.position);
                writer.WritePropertyName("size");
                writer.WriteValue(value.size);
                writer.WriteEndObject();
            }

            public override BoundsInt ReadJson(JsonReader reader, Type objectType, BoundsInt existingValue,
                bool hasExistingValue, JsonSerializer serializer)
            {
                JObject jo = JObject.Load(reader);
                return new BoundsInt(
                    jo["position"].Value<Vector3Int>(),
                    jo["size"].Value<Vector3Int>()
                );
            }
        }
    }
}