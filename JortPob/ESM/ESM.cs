using JortPob.Common;
using JortPob.Worker;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;

namespace JortPob
{
    public class ESM
    {
        /* Types of records in the ESM */
        public enum Type
        {
            Header, GameSetting, GlobalVariable, Class, Faction, Race, Sound, Skill, MagicEffect, Script, Region, Birthsign, LandscapeTexture, Spell, Static, Door,
            MiscItem, Weapon, Container, Creature, Bodypart, Light, Enchantment, Npc, Armor, Clothing, RepairTool, Activator, Apparatus, Lockpick, Probe, Ingredient,
            Book, Alchemy, LevelledItem, LevelledCreature, Cell, Landscape, PathGrid, SoundGen, Dialogue, DialogueInfo
        }

        public Dictionary<Type, List<JsonNode>> records;
        public List<DialogRecord> dialog;
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
                if (type == Type.Dialogue || type == Type.DialogueInfo) { continue; } // special records, need to be handled specially
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
                        if (type == Type.Dialogue || type == Type.DialogueInfo) { continue; } // special records, need to be handled specially
                        records[type].Add(record);
                    }
                }
            }

            /* Handle dialog stuff now */
            dialog = new();
            DialogRecord current = null;
            for (int i = 0; i < json.Count; i++)
            {
                JsonNode record = json[i];
                Enum.TryParse(record["type"].ToString(), out Type type);

                if (type == Type.Dialogue)
                {
                    string idstr = record["id"].ToString();
                    string typestr = idstr.Replace(" ", "");
                    string diatype = record["dialogue_type"].ToString();
                    typestr = new String(typestr.Where(c => c != '-' && (c < '0' || c > '9')).ToArray());
                    if(!Enum.TryParse(typestr, out DialogRecord.Type dtype)) { dtype = DialogRecord.Type.Topic; }
                    if (diatype.ToLower() == "journal") { dtype = DialogRecord.Type.Journal; }

                    if(current != null && current.type == DialogRecord.Type.Greeting && dtype == DialogRecord.Type.Greeting) { continue; } // skip so we can merge all 9 greeting levels into a single thingy

                    current = new(dtype, idstr);
                    dialog.Add(current);
                }
                else if(type == Type.DialogueInfo)
                {
                    DialogInfoRecord dialogInfoRecord = new(current.type, json[i]);
                    current.infos.Add(dialogInfoRecord);
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
                    Landscape landscape = new Landscape(this, coordinate, json);
                    landscapes.Add(landscape);
                    return landscape;
                }
            }
            return null;

        }

        /* Same as above but only retursn a landscape if its already fully loaded. Returns null if its not loaded */
        public Landscape GetLoadedLandscape(Int2 coordinate)
        {
            foreach (Landscape landscape in landscapes)
            {
                if (landscape.coordinate == coordinate) { return landscape; }
            }
            return null;
        }

        /* Load all landscapes, single threaded */
        public void LoadLandscapes()
        {
            Lort.Log($"Processing {exterior.Count} landscapes...", Lort.Type.Main);
            Lort.NewTask("Processing Landscape", exterior.Count);
            foreach (Cell cell in exterior)
            {
                GetLandscape(cell.coordinate);
                Lort.TaskIterate();
            }
        }

        /* Get dialog and character data for building esd */
        public List<Tuple<DialogRecord, DialogInfoRecord>> GetDialog(NpcContent npc)
        {
            List<Tuple<DialogRecord, DialogInfoRecord>> d = new();
            foreach(DialogRecord dialogRecord in dialog)
            {
                if (dialogRecord.type == DialogRecord.Type.Journal) { continue; } // obviously skip these lmao

                // Check if the npc meets requirements for this topic or w/e
                foreach(DialogInfoRecord info in dialogRecord.infos)
                {
                    // Check if the npc meets all requirements for this dialog line
                    if (info.speaker != null && info.speaker != npc.id) { continue; }
                    if (info.race != NpcContent.Race.Any && info.race != npc.race) { continue; }
                    if (info.job != null && info.job != npc.job) { continue; }
                    if (info.faction != null && info.faction != npc.faction) { continue; }
                    if (info.disposition > npc.disposition) { continue; }
                    if (info.rank > npc.rank) { continue; }
                    if (info.cell != null && info.cell != npc.cell.name) { continue; }
                    if (info.sex != NpcContent.Sex.Any && info.sex != npc.sex) { continue; }
                    if (info.filters && dialogRecord.type == DialogRecord.Type.Greeting) { continue; }                     // @TODO: STUB, just discarding on filters or player data checks
                    if (info.playerFaction != null) { continue; }

                    d.Add(new(dialogRecord, info));
                    break;
                }
            }

            return d;
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

    /* Just a serializiation of the dialog and dialoginfo thingies. We will be iterating through them a lot so may as well do it. */
    public class DialogRecord
    {
        public enum Type
        {
            Greeting, Topic, Journal,
            Alarm, Attack, Flee, Hello, Hit, Idle, Intruder, Thief, 
            AdmireFail, AdmireSuccess, BribeFail, BribeSuccess, InfoRefusal, InfoFail, IntimidateFail, IntimidateSuccess, ServiceRefusal, TauntFail, TauntSuccess
        }

        public readonly Type type;
        public readonly string id;

        public readonly List<DialogInfoRecord> infos;

        public DialogRecord(Type type, string id)
        {
            this.type = type;
            this.id = id;

            infos = new();
        }
    }

    public class DialogInfoRecord
    {
        private static uint NEXT_ID = 0;

        public readonly uint id; // generated id used when lookin up wems, not used by elden ring or morrowind
        public readonly DialogRecord.Type type;

        // static requirements for a dialog to be added
        public readonly string speaker, job, faction, cell;
        public readonly int rank;
        public readonly NpcContent.Race race;
        public readonly NpcContent.Sex sex;

        // non-static requirements
        public readonly string playerFaction;
        public readonly int disposition, playerRank;

        public readonly bool filters; // STUB!!! @TODO: actually implement later once we have script data managed

        public readonly string text; // actual dialog text

        public DialogInfoRecord(DialogRecord.Type type, JsonNode json)
        {
            id = DialogInfoRecord.NEXT_ID++;
            this.type = type;

            string NullEmpty(string s) { return s.Trim() == "" ? null : s; }

            speaker = NullEmpty(json["speaker_id"].ToString());
            string raceStr = NullEmpty(json["speaker_race"].ToString());
            race = raceStr != null ? (NpcContent.Race)System.Enum.Parse(typeof(NpcContent.Race), raceStr.Replace(" ", "")) : NpcContent.Race.Any;
            job = NullEmpty(json["speaker_class"].ToString());
            faction = NullEmpty(json["speaker_faction"].ToString());
            cell = NullEmpty(json["speaker_cell"].ToString());
            rank = int.Parse(json["data"]["speaker_rank"].ToString());
            Enum.TryParse(json["data"]["speaker_sex"].ToString(), out sex);

            playerFaction = NullEmpty(json["player_faction"].ToString());
            disposition = int.Parse(json["data"]["disposition"].ToString());
            playerRank = int.Parse(json["data"]["player_rank"].ToString());

            filters = json["filters"].AsArray().Count() > 0;

            text = json["text"].ToString();
        }
    }
}
