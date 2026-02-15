using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JortPob.Common
{
    public static class Settable
    {

        private static readonly JsonNode _json;
        
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
        }
        
        public static T Get<T>(string key)
        {
            var node = _json[key] ?? throw new KeyNotFoundException($"Setting '{key}' missing from settings.json");
            return node.Deserialize<T>();
        }

        public static T[] GetArray<T>(string key)
        {
            return Get<T[]>(key);
        }
        
        public static List<T[]> GetJaggedArray<T>(string key)
        {
            var node = _json[key] ?? throw new KeyNotFoundException($"Setting '{key}' missing from settings.json");

            if (node is not JsonArray outerArray)
            {
                throw new InvalidOperationException($"Setting '{key}' is not a JSON Array.");
            }

            List<T[]> result = new();

            foreach (var innerNode in outerArray)
            {
                if (innerNode is JsonArray innerArray)
                {
                    result.Add(innerArray.Select(x => x.GetValue<T>()).ToArray());
                }
                else
                {
                    throw new InvalidDataException($"Item inside '{key}' was expected to be an array (e.g. [1, 2, 3]), but found a single value.");
                }
            }

            return result;
        }

        public static Vector3 GetVector3(string key)
        {
            var arr = GetArray<float>(key);
            if (arr.Length != 3) throw new InvalidDataException($"Setting '{key}' has more than 3 elements. Element count: {arr.Length}");
            return new Vector3(arr[0], arr[1], arr[2]);
        }
        
        public static Vector2 GetVector2(string key)
        {
            var arr = GetArray<float>(key);
            if (arr.Length != 2) throw new InvalidDataException($"Setting '{key}' has more than 2 elements. Element count: {arr.Length}");
            return new Vector2(arr[0], arr[1]);
        }
    }
}
