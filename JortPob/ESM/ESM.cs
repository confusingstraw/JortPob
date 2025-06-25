using HKLib.hk2018;
using JortPob.Common;
using JortPob.Worker;
using SharpAssimp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace JortPob
{
    public class ESM
    {
        /* Types of records in the ESM */
        public enum Type
        {
            Header, GameSetting, GlobalVariable, Class, Faction, Race, Sound, Skill, MagicEffect, Script, Region, Birthsign, LandscapeTexture, Spell, Static, Door,
            MiscItem, Weapon, Container, Creature, Bodypart, Light, Enchantment, Npc, Armor, Clothing, RepairTool, Activator, Apparatus, Lockpick, Probe, Ingredient,
            Book, Alchemy, LevelledItem, LevelledCreature, Cell, Landscape, PathGrid, SoundGen, Dialogue, Info
        }

        public Dictionary<Type, List<JsonNode>> records;
        public List<Cell> exterior, interior;
        private ConcurrentBag<Landscape> landscapes;

        public ESM(string path)
        {
            Lort.Log($"Loading '{path}' ...", Lort.Type.Main);

            string tempRawJson = File.ReadAllText(path);
            JsonArray json = JsonNode.Parse(tempRawJson).AsArray();

            records = new Dictionary<Type, List<JsonNode>>();
            foreach (string name in Enum.GetNames(typeof(Type)))
            {
                Enum.TryParse(name, out Type type);
                records.Add(type, new List<JsonNode>());
            }

            for (int i = 0; i < json.Count; i++)
            {
                JsonNode record = json[i];
                foreach (string name in Enum.GetNames(typeof(Type)))
                {
                    if (record["type"].ToString() == name)
                    {
                        Enum.TryParse(name, out Type type);
                        records[type].Add(record);
                    }
                }
            }

            /* Multi threading to speed this up... */
            List<List<Cell>> cells = CellWorker.Go(this);
            exterior = cells[0];
            interior = cells[1];
            landscapes = new();
        }

        /* List of types that we should search for references */
        // more const values we should move somewhere. @TODO
        public readonly Type[] VALID_CONTENT_TYPES = {
            Type.Static, Type.Container, Type.Light, Type.Sound, Type.Skill, Type.Region, Type.Door, Type.MiscItem, Type.Weapon,  Type.Creature, Type.Bodypart, Type.Npc,
            Type.Armor, Type.Clothing, Type.RepairTool, Type.Activator, Type.Apparatus, Type.Lockpick, Type.Probe, Type.Ingredient, Type.Book, Type.Alchemy, Type.LevelledItem,
            Type.LevelledCreature, Type.PathGrid, Type.SoundGen
        };

        /* References don't contain any explicit 'type' data so... we just gotta go find it lol */
        /* @TODO: well actually i think the 'flags' int value in some records is useed as a 32bit boolean array and that may specify record types possibly. Look into it? */
        public Record FindRecordById(string id)
        {
            foreach (ESM.Type type in VALID_CONTENT_TYPES)
            {
                List<JsonNode> list = records[type];

                foreach(JsonNode record in list)
                {
                    if (record["id"] != null && record["id"].ToString() == id)
                    {
                        return new Record(type, record);
                    }
                }
            }
            return null; // Not found!
        }

        public Cell GetCellByGrid(Int2 position)
        {
            foreach (Cell cell in exterior)
            {
                if (cell.coordinate == position) { return cell; }
            }
            return null;
        }

        public Cell GetCellByName(string name)
        {
            foreach (Cell cell in exterior)
            {
                if (cell.name == name) { return cell; }
            }
            foreach (Cell cell in interior)
            {
                if (cell.name == name) { return cell; }
            }
            return null;
        }

        public Landscape GetLandscape(Int2 coordinate)
        {
            if (GetCellByGrid(coordinate) == null) { return null; } // Performance hack.

            foreach(Landscape landscape in landscapes)
            {
                if (landscape.coordinate == coordinate) { return landscape; }
            }

            foreach (JsonNode json in records[Type.Landscape])
            {
                int x = int.Parse(json["grid"][0].ToString());
                int y = int.Parse(json["grid"][1].ToString());

                if (coordinate.x == x && coordinate.y == y)
                {
                    Landscape landscape = new Landscape(coordinate, json, records);
                    landscapes.Add(landscape);
                    return landscape;
                }
            }
            return null;

        }
    }

    public class Record
    {
        public readonly ESM.Type type;
        public readonly JsonNode json;
        public Record(ESM.Type type, JsonNode json)
        {
            this.type = type;
            this.json = json;
        }
    }
}
