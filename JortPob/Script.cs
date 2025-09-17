﻿using HKLib.hk2018.hk;
using HKX2;
using JortPob.Common;
using SoulsFormats;
using SoulsIds;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using static SoulsFormats.MSBAC4.Event;

/* Individual script for an msb. */
/* managed by ScriptManager 
/* When using the word "entity" in this code i am refering to entity id. i just like shorter names */

/* Using this research as a base for conventions here https://docs.google.com/spreadsheets/d/17sE1a1h87BhpiUwKUyJ9ZjKTeehXA4OuLwmQvTfwo_M/edit?gid=1770617590#gid=1770617590 */

namespace JortPob
{
    public class Script
    {
        public Events AUTO;

        public readonly int map, x, y, block;

        public readonly ScriptCommon common; // commonevent and commonfunc emevds
        public readonly EMEVD emevd;
        public readonly EMEVD.Event init;

        public enum EntityType
        {
            Enemy = 0, Asset = 1000, Region = 2000, Event = 3000, Collision = 4000, Group = 5000
        }

        public List<Flag> flags;
        private Dictionary<Flag.Category, uint> flagUsedCounts;
        private Dictionary<EntityType, uint> entityUsedCounts;

        public Script(ScriptCommon common, int map, int x, int y, int block)
        {
            this.common = common;
            this.map = map;
            this.x = x;
            this.y = y;
            this.block = block;

            AUTO = new(Utility.ResourcePath(@"script\\er-common.emedf.json"), true, true);

            EMEVD DEBUGTESTDELETE = EMEVD.Read($"{Const.ELDEN_PATH}\\game\\event\\m60_42_36_00.emevd.dcx");

            emevd = new EMEVD();
            emevd.Compression = SoulsFormats.DCX.Type.DCX_KRAK;
            emevd.Format = SoulsFormats.EMEVD.Game.Sekiro;

            // Bytes here are raw string data that points to the filenames of common and common_func
            emevd.StringData = new byte[] { 78, 0, 58, 0, 92, 0, 71, 0, 82, 0, 92, 0, 100, 0, 97, 0, 116, 0, 97, 0, 92, 0, 80, 0, 97, 0, 114, 0, 97, 0, 109, 0, 92, 0, 101, 0, 118, 0, 101, 0, 110, 0, 116, 0, 92, 0, 99, 0, 111, 0, 109, 0, 109, 0, 111, 0, 110, 0, 95, 0, 102, 0, 117, 0, 110, 0, 99, 0, 46, 0, 101, 0, 109, 0, 101, 0, 118, 0, 100, 0, 0, 0, 78, 0, 58, 0, 92, 0, 71, 0, 82, 0, 92, 0, 100, 0, 97, 0, 116, 0, 97, 0, 92, 0, 80, 0, 97, 0, 114, 0, 97, 0, 109, 0, 92, 0, 101, 0, 118, 0, 101, 0, 110, 0, 116, 0, 92, 0, 99, 0, 111, 0, 109, 0, 109, 0, 111, 0, 110, 0, 95, 0, 109, 0, 97, 0, 99, 0, 114, 0, 111, 0, 46, 0, 101, 0, 109, 0, 101, 0, 118, 0, 100, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            emevd.LinkedFileOffsets = new() { 0, 82 };

            init = new EMEVD.Event(0);
            emevd.Events.Add(init);

            flags = new();

            flagUsedCounts = new()
            {
                { Flag.Category.Event, 0 },
                { Flag.Category.Saved, 0 },
                { Flag.Category.Temporary, 0 }
            };

            entityUsedCounts = new()
            {
                { EntityType.Enemy, 0 },
                { EntityType.Asset, 0 },
                { EntityType.Region, 0 },
                { EntityType.Event, 0 },
                { EntityType.Collision, 0 },
                { EntityType.Group, 0 }
            };
        }

        public void RegisterLoadDoor(DoorContent door)
        {
            int actionParam = door.warp.map == 60 ? 1501 : 1500;  // enter or exit
            init.Instructions.Add(AUTO.ParseAdd($"InitializeCommonEvent(0, {common.events[ScriptCommon.Event.LoadDoor]}, {actionParam}, {door.entity}, {door.entity}, {1000}, {door.warp.map}, {door.warp.x}, {door.warp.y}, {door.warp.block}, {door.warp.entity});"));
        }

        public void RegisterNpcHostility(NpcContent npc)
        {
            CreateFlag(Flag.Category.Temporary, Flag.Type.Nibble, Flag.Designation.FriendHitCounter, npc.entity.ToString()); // setup friendly hit counter
            Flag hostileFlag = CreateFlag(Flag.Category.Saved, Flag.Type.Bit, Flag.Designation.Hostile, npc.entity.ToString());
            init.Instructions.Add(AUTO.ParseAdd($"InitializeCommonEvent(0, {common.events[ScriptCommon.Event.NpcHostilityHandler]}, {hostileFlag.id}, {npc.entity}, {hostileFlag.id}, {npc.entity});"));
        }

        public void RegisterNpc(NpcContent npc, Flag count)
        {
            /* Dead/disable spawn event */
            Flag deadFlag = CreateFlag(Script.Flag.Category.Saved, Script.Flag.Type.Bit, Script.Flag.Designation.Dead, npc.entity.ToString());
            Flag disableFlag = CreateFlag(Script.Flag.Category.Saved, Script.Flag.Type.Bit, Script.Flag.Designation.Disabled, npc.entity.ToString());
            init.Instructions.Add(AUTO.ParseAdd($"InitializeCommonEvent(0, {common.events[ScriptCommon.Event.SpawnHandler]}, {disableFlag.id}, {npc.entity}, {deadFlag.id}, {npc.entity}, {npc.entity}, {deadFlag.id}, {count.id}, {count.Bits()}, {count.MaxValue()});"));
        }

        public void RegisterCreature(CreatureContent creature, Flag count)
        {
            /* Dead/disable spawn event */
            Flag deadFlag = CreateFlag(Script.Flag.Category.Saved, Script.Flag.Type.Bit, Script.Flag.Designation.Dead, creature.entity.ToString());
            Flag disableFlag = CreateFlag(Script.Flag.Category.Saved, Script.Flag.Type.Bit, Script.Flag.Designation.Disabled, creature.entity.ToString());
            init.Instructions.Add(AUTO.ParseAdd($"InitializeCommonEvent(0, {common.events[ScriptCommon.Event.SpawnHandler]}, {disableFlag.id}, {creature.entity}, {deadFlag.id}, {creature.entity}, {creature.entity}, {deadFlag.id}, {count.id}, {count.Bits()}, {count.MaxValue()});"));
        }

        /* Create an EMEVD flag for this MSB */
        private static readonly Dictionary<Flag.Category, uint[]> FLAG_TYPE_OFFSETS = new()
        {
            { Flag.Category.Event, new uint[] { 1000, 3000, 6000 } },
            { Flag.Category.Saved, new uint[] { 0, 4000, 7000, 8000, 9000 } },
            { Flag.Category.Temporary, new uint[] { 2000, 5000 } }
        };
        public Flag CreateFlag(Flag.Category category, Flag.Type type, Flag.Designation designation, string name, uint value = 0)
        {
            uint rawCount = flagUsedCounts[category];
            uint perThou = rawCount / 1000;
            uint mod = rawCount % 1000;
            uint mapOffset;
            if(map == 60) { mapOffset = uint.Parse($"10{x:D2}{y:D2}0000"); }
            else { mapOffset = uint.Parse($"{map:D2}{x:D2}0000"); }

            uint id = mapOffset + FLAG_TYPE_OFFSETS[category][perThou] + mod;  // if we run out of flags this will throw an out of bounds exception. that situation would be bad but should't happen.
            flagUsedCounts[category] += ((uint)type);

            // Check for a collision with a common event flag, if we find a collision we recursviely try making another flag
            if (ScriptManager.DO_NOT_USE_FLAGS.Contains(id))
            {
                Lort.Log($" ## WARNING ## Flag collision with commonevent found: {id}", Lort.Type.Debug);
                return CreateFlag(category, type, designation, name, value);
            }

            Flag flag = new(category, type, designation, name, id, value);
            flags.Add(flag);
            return flag;
        }

        /* Create a unique entity id for this MSB */
        public uint CreateEntity(EntityType type)
        {
            uint rawCount = entityUsedCounts[type]++;
            uint mapOffset;
            if (map == 60)
            {
                mapOffset = uint.Parse($"10{x:D2}{y:D2}0000");
            }
            else
            {
                mapOffset = uint.Parse($"{map:D2}{x:D2}0000");
            }
            

            if (rawCount >= 1000) { Lort.Log($" ## CRITICAL ## ENTITY ID OVERFLOWED IN m{map:D2}_{x:D2}_{y:D2}", Lort.Type.Debug); }

            return mapOffset + ((uint)type) + rawCount;
        }

        public void Write()
        {
            emevd.Write($"{Const.OUTPUT_PATH}\\event\\m{map:D2}_{x:D2}_{y:D2}_{block:D2}.emevd.dcx");
        }

        public class Flag
        {
            public enum Category
            {
                Event, Saved, Temporary
            }

            public enum Type
            {
                Bit = 1, Nibble = 4, Byte = 8, Short = 16, Int = 32
            }

            public enum Designation
            {
                Event,                                          // Flag is an event ID
                Global, Local, Reputation, Journal,
                Dead, DeadCount, Disabled, Hostile, FriendHitCounter,        // hostile flag exists for friendly npcs, if you piss em off they stab you
                TopicEnabled, TalkedToPc, Disposition, PlayerRace,
                FactionJoined, FactionReputation, FactionRank, FactionExpelled,    // faction stuff
                ReturnValueRankReq                              // these are temp values used by ESD to store variables
            }

            public readonly Category category;
            public readonly Type type;
            public readonly Designation designation;
            public readonly string name;  // general purpose string to identify this flag. for example, if this is a papyrus global variable, it would be that variables name
            public readonly uint id, value;   // id is flag, value is the default initial value. usually 0

            public Flag(Category category, Type type, Designation designation, string name, uint id, uint value)
            {
                this.category = category;
                this.type = type;
                this.designation = designation;
                this.name = name;
                this.id = id;
                this.value = value;
            }

            public uint Bits()
            {
                return (uint)type;
            }

            public uint MaxValue()
            {
                return (uint)Utility.Pow(2, (uint)type) - 1;
            }
        }
    }
}
