using JortPob.Common;
using SharpAssimp;
using SoulsFormats;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace JortPob
{
    public class Cell
    {
        public readonly string name;
        public readonly string region;
        public readonly Int2 coordinate;  // Position on the cell grid
        public readonly Vector3 center;
        public readonly Vector3 boundsMin;
        public readonly Vector3 boundsMax;

        public readonly List<Content> contents;            // All of this
        public readonly List<CreatureContent> creatures;
        public readonly List<NpcContent> npcs;
        public readonly List<AssetContent> assets;
        public readonly List<LightContent> lights;
        public readonly List<EmitterContent> emitters;

        public Cell(ESM esm, JsonNode json)
        {
            /* Cell Data */
            name = json["name"].ToString();
            region = json["region"] != null ? json["region"].ToString() : "null";

            int x = int.Parse(json["data"]["grid"][0].ToString());
            int y = int.Parse(json["data"]["grid"][1].ToString());
            coordinate = new Int2(x, y);

            center = new Vector3((Const.CELL_SIZE * coordinate.x) + (Const.CELL_SIZE * 0.5f), 0.0f, (Const.CELL_SIZE * coordinate.y) + (Const.CELL_SIZE * 0.5f));

            /* Cell Content Data */
            contents = new();
            creatures = new();
            npcs = new();
            assets = new();
            emitters = new();
            lights = new();

            foreach (JsonNode reference in json["references"].AsArray())
            {
                string id = reference["id"].ToString();
                Record record = esm.FindRecordById(id);

                if(record == null) { continue; }

                string mesh = record.json["mesh"] != null ? record.json["mesh"].ToString() : null;
                if (mesh != null && mesh.Trim() == "") { mesh = null; }                             // For some reason a null mesh can just be "" sometimes?

                switch(record.type)
                {
                    case ESM.Type.Static:
                    case ESM.Type.Container:
                        if (mesh != null) { assets.Add(new AssetContent(reference, record)); }
                        break;
                    case ESM.Type.Light:
                        if (mesh == null) { lights.Add(new LightContent(reference, record)); }
                        else { emitters.Add(new EmitterContent(reference, record)); }
                        break;
                    case ESM.Type.Npc:
                        npcs.Add(new NpcContent(reference, record));
                        break;
                    case ESM.Type.Creature:
                    case ESM.Type.LevelledCreature:
                        creatures.Add(new CreatureContent(reference, record));
                        break;
                }
            }

            contents.AddRange(creatures);
            contents.AddRange(npcs);
            contents.AddRange(assets);
            contents.AddRange(emitters);
            contents.AddRange(lights);


            /* Calculate bounding box */
            float x1 = float.MaxValue, y1 = float.MaxValue, z1 = float.MaxValue, x2 = float.MinValue, y2 = float.MinValue, z2 = float.MinValue;
            foreach (Content content in contents)
            {
                x1 = Math.Min(x1, content.position.X);
                y1 = Math.Min(y1, content.position.Y);
                z1 = Math.Min(z1, content.position.Z);
                x2 = Math.Max(x2, content.position.X);
                y2 = Math.Max(y2, content.position.Y);
                z2 = Math.Max(z2, content.position.Z);
            }
            boundsMin = new Vector3(x1, y1, z1);
            boundsMax = new Vector3(x2, y2, z2);
        }
    }
}
