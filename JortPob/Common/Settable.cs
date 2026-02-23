using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace JortPob.Common
{
    public static class Settable
    {

        private static readonly JsonNode _json;
        private static readonly JsonSerializerOptions _options;
        
        static Settable()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
            if (!File.Exists(path)) throw new FileNotFoundException($"Settings file not found at {path}");
            
            _json = JsonNode.Parse(
                File.ReadAllText(path), 
                null, 
                // Allow comments in JSON for documentation
                new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Skip, 
                    AllowTrailingCommas = true
                }
            );
            
            _options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                IncludeFields = true,
                WriteIndented = true,
                Converters = 
                { 
                    new JsonStringEnumConverter(),
                    new Vector3ArrayConverter(), // <--- Adds support for [x,y,z]
                    new Vector2ArrayConverter()  // <--- Adds support for [x,y]
                }
            };
        }
        
        public static T Get<T>(string key)
        {
            var node = _json[key];
            return node.Deserialize<T>(_options);
        }

        public static T[] GetArray<T>(string key)
        {
            return Get<T[]>(key);
        }
        
        private class Vector3ArrayConverter : JsonConverter<Vector3>
        {
            public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException("Expected Array [x,y,z] for Vector3.");
                
                reader.Read();
                float x = (float)reader.GetDouble();
                
                reader.Read();
                float y = (float)reader.GetDouble();
                
                reader.Read();
                float z = (float)reader.GetDouble();

                reader.Read();
                if (reader.TokenType != JsonTokenType.EndArray) throw new JsonException("Vector3 Array has too many elements.");

                return new Vector3(x, y, z);
            }

            public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
            {
                writer.WriteStartArray();
                writer.WriteNumberValue(value.X);
                writer.WriteNumberValue(value.Y);
                writer.WriteNumberValue(value.Z);
                writer.WriteEndArray();
            }
        }

        private class Vector2ArrayConverter : JsonConverter<Vector2>
        {
            public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException("Expected Array [x,y] for Vector2.");

                reader.Read();
                float x = (float)reader.GetDouble();

                reader.Read();
                float y = (float)reader.GetDouble();

                reader.Read();
                if (reader.TokenType != JsonTokenType.EndArray) throw new JsonException("Vector2 Array has too many elements.");

                return new Vector2(x, y);
            }

            public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
            {
                writer.WriteStartArray();
                writer.WriteNumberValue(value.X);
                writer.WriteNumberValue(value.Y);
                writer.WriteEndArray();
            }
        }
    }
}
