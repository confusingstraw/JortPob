using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace JortPob.Common
{
    public class Settable
    {
        private static JsonNode json;
        public static string Get(string key)
        {
            if(json == null)
            {
                string tempRawJson = File.ReadAllText($"{AppDomain.CurrentDomain.BaseDirectory}settings.json");
                json = JsonNode.Parse(tempRawJson);
            }

            return json[key].ToString();
        }
    }
}
