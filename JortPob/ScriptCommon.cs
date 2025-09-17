using JortPob.Common;
using SoulsFormats;
using SoulsIds;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static JortPob.Script;

namespace JortPob
{
    /* Handles CommonEvent and CommonFunc EMEVD. These are different from map scripts so I decided to give them a seperate class */

    public class ScriptCommon
    {
        public Events AUTO;

        public readonly EMEVD emevd, func;

        public List<Flag> flags;
        private Dictionary<Flag.Category, uint> flagUsedCounts;

        public enum Event
        {
            LoadDoor, SpawnHandler, NpcHostilityHandler
        }
        public readonly Dictionary<Event, uint> events;

        public ScriptCommon()
        {
            AUTO = new(Utility.ResourcePath(@"script\\er-common.emedf.json"), true, true);

            emevd = EMEVD.Read(Utility.ResourcePath(@"script\common.emevd.dcx"));
            func = EMEVD.Read(Utility.ResourcePath(@"script\common_func.emevd.dcx"));

            flags = new();

            flagUsedCounts = new()
            {
                { Flag.Category.Event, 0 },
                { Flag.Category.Saved, 0 },
                { Flag.Category.Temporary, 0 }
            };

            events = new();

            /* Create an event for going through load doors */
            Flag doorEventFlag = CreateFlag(Flag.Category.Event, Flag.Type.Bit, Flag.Designation.Event, $"CommonFunc:DoorLoad");
            EMEVD.Event loadDoor = new(doorEventFlag.id);

            int pc = 0;
            string NextParameterName()
            {
                return $"X{pc++ * 4}_4";
            }

            string[] loadDoorEventRaw = new string[]
            {
                $"IfActionButtonInArea(MAIN, {NextParameterName()}, {NextParameterName()});",
                $"RotateCharacter(10000, {NextParameterName()}, 60000, false);",
                $"WaitFixedTimeSeconds(0.25);",
                $"PlaySE({NextParameterName()}, SoundType.Asset, 200);",
                $"WaitFixedTimeSeconds(0.75);",
                $"WarpPlayer({NextParameterName()}, {NextParameterName()}, {NextParameterName()}, {NextParameterName()}, {NextParameterName()}, -1);",
                $"EndUnconditionally(EventEndType.End);"
            };

            for (int i = 0; i < loadDoorEventRaw.Length; i++)
            {
                (EMEVD.Instruction instr, List<EMEVD.Parameter> newPs) = AUTO.ParseAddArg(loadDoorEventRaw[i], i);
                loadDoor.Parameters.AddRange(newPs);
                loadDoor.Instructions.Add(instr);
            }

            func.Events.Add(loadDoor);
            events.Add(Event.LoadDoor, doorEventFlag.id);

            /* Create an event for handling creature/npc spawn/respawn and disable/enable */
            Flag spawnEventFlag = CreateFlag(Flag.Category.Event, Flag.Type.Bit, Flag.Designation.Event, $"CommonFunc:SpawnHandler");
            EMEVD.Event spawnHandler = new(spawnEventFlag.id);

            pc = 0;

            string[] spawnHandlerEventRaw = new string[]
            {
                $"SkipIfEventFlag(2, OFF, TargetEventFlagType.EventFlag, {NextParameterName()});",   // check disabled flag
                $"ChangeCharacterEnableState({NextParameterName()}, Disabled);",
                $"EndUnconditionally(EventEndType.End);",
                $"SkipIfEventFlag(2, OFF, TargetEventFlagType.EventFlag, {NextParameterName()});",   // check dead flag
                $"ChangeCharacterEnableState({NextParameterName()}, Disabled);",
                $"EndUnconditionally(EventEndType.End);",
                $"IfCharacterHPValue(MAIN, {NextParameterName()}, 5, 0, 0, 1);", // check if hp is less or equal to 0. comparison values are in byte format so 5 is <= and 4 is >=
                $"SetEventFlag(TargetEventFlagType.EventFlag, {NextParameterName()}, ON);",  // set dead
                $"IncrementEventValue({NextParameterName()}, {NextParameterName()}, {NextParameterName()});", // count on kill record id flag
                $"EndUnconditionally(EventEndType.End);"
            };

            for (int i = 0; i < spawnHandlerEventRaw.Length; i++)
            {
                (EMEVD.Instruction instr, List<EMEVD.Parameter> newPs) = AUTO.ParseAddArg(spawnHandlerEventRaw[i], i);
                spawnHandler.Parameters.AddRange(newPs);
                spawnHandler.Instructions.Add(instr);
            }

            func.Events.Add(spawnHandler);
            events.Add(Event.SpawnHandler, spawnEventFlag.id);

            /* Create an event for handling friendly npc hostility */
            Flag hostileEventFlag = CreateFlag(Flag.Category.Event, Flag.Type.Bit, Flag.Designation.Event, $"CommonFunc:NpcHostilityHandler");
            EMEVD.Event hostileEvent = new(hostileEventFlag.id);

            pc = 0;

            string[] hostileEventRaw = new string[]
            {
                $"IfEventFlag(MAIN, ON, TargetEventFlagType.EventFlag, {NextParameterName()});", 
                $"SetCharacterTeamType({NextParameterName()}, 27);",   // hostile flag on, hostile   >:(     // 27: TeamType.HostileNPC
                $"IfEventFlag(MAIN, OFF, TargetEventFlagType.EventFlag, {NextParameterName()});",
                $"SetCharacterTeamType({NextParameterName()}, 26);",  // hostile flag off, friendly :D       //  26: TeamType.FriendlyNPC
                $"EndUnconditionally(EventEndType.Restart);",    // restart because it's possible for this to happen more than once
            };

            for (int i = 0; i < hostileEventRaw.Length; i++)
            {
                (EMEVD.Instruction instr, List<EMEVD.Parameter> newPs) = AUTO.ParseAddArg(hostileEventRaw[i], i);
                hostileEvent.Parameters.AddRange(newPs);
                hostileEvent.Instructions.Add(instr);
            }

            func.Events.Add(hostileEvent);
            events.Add(Event.NpcHostilityHandler, hostileEventFlag.id);
        }

        /* There are some bugs with this system. It defo wastes some flag space. We have lots tho. Maybe fix later */
        private static readonly uint[] COMMON_FLAG_BASES = new uint[]  // using flags from every msb slot along the bottom most edge of the world
        {
            1030290000, 1031290000, 1032290000, 1033290000, 1034290000, 1035290000, 1036290000, 1037290000, 1038290000, 1039290000 // if we run out of flag space it will throw an exception. adding more is easy tho
        };
        private static readonly Dictionary<Flag.Category, uint[]> FLAG_TYPE_OFFSETS = new()
        {
            { Flag.Category.Event, new uint[] { 1000, 3000, 6000 } },
            { Flag.Category.Saved, new uint[] { 0, 4000, 7000, 8000, 9000 } },
            { Flag.Category.Temporary, new uint[] { 2000, 5000 } }
        };
        public Flag CreateFlag(Flag.Category category, Flag.Type type, Flag.Designation designation, string name, uint value = 0)
        {
            /* Cap off a group of 1000 flags if it's near full. For example: This is to prevent us adding a multi bit flag like a byte when there is only 3 flags left */
            uint rawCount = flagUsedCounts[category];
            if ((rawCount % 1000) + ((uint)type) >= 1000)
            {
                flagUsedCounts[category] += 1000 - (rawCount % 1000);
                rawCount = flagUsedCounts[category];
            }

            /* Calculate next flag */
            uint perThou = (rawCount / 1000) % (uint)(FLAG_TYPE_OFFSETS[category].Length);
            uint perMsb = (rawCount / 1000) / (uint)(FLAG_TYPE_OFFSETS[category].Length);
            uint mod = rawCount % 1000;
            uint mapOffset = COMMON_FLAG_BASES[perMsb];
            uint id = mapOffset + FLAG_TYPE_OFFSETS[category][perThou] + mod;
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

        public void Write()
        {
            emevd.Write($"{Const.OUTPUT_PATH}\\event\\common.emevd.dcx");
            func.Write($"{Const.OUTPUT_PATH}\\event\\common_func.emevd.dcx");
        }
    }
}
