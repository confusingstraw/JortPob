using JortPob.Common;
using SoulsFormats;
using SoulsIds;
using System.Collections.Generic;

/* THIS CLASS FUCKING SUCKS */
/* this was rushed to get a video out. this class desperately needs rewriting and rethinking. it just doesn't cut it for what it needs to do in the future */

namespace JortPob
{
    public class Script
    {
        private static int nextEvtID = 1000;
        public static int NewEvtID() { return nextEvtID++; }

        private static int nextFlag = 13000000;   // Testing
        public static int NewFlag() { return nextFlag++; }

        public static readonly int EVT_LOAD_DOOR = 100;
        public static Dictionary<int, int> COMMON_EVENT_SLOTS = new() {
            { EVT_LOAD_DOOR, 0 }
        };

        public Events AUTO;

        public readonly int map, x, y, block;
        public readonly int DOOR_ID;
        public int DOOR_SLOT;

        public EMEVD emevd;
        public EMEVD.Event init;
        public Script(int map, int x, int y, int block)
        {
            this.map = map;
            this.x = x;
            this.y = y;
            this.block = block;

            AUTO = new(Utility.ResourcePath(@"script\\er-common.emedf.json"), true, true);

            emevd = new EMEVD();
            emevd.Compression = SoulsFormats.DCX.Type.DCX_KRAK;
            emevd.Format = SoulsFormats.EMEVD.Game.Sekiro;

            DOOR_ID = int.Parse($"{map.ToString("D2")}{x.ToString("D2")}0000");
            DOOR_SLOT = 0;

            init = new EMEVD.Event(0);
            emevd.Events.Add(init);
            emevd.Events.Add(CreateLoadDoorEvent());

            //init.Instructions.Add(AUTO.ParseAdd($"IfPlayerInoutMap(MAIN, true, {map}, {x}, {y}, {block});"));  // just a test
            //init.Instructions.Add(AUTO.ParseAdd($"DisplayBanner(TextBannerType.Stalemate);"));
        }

        public EMEVD.Event CreateLoadDoorEvent()
        {
            EMEVD.Event loadDoor = new(DOOR_ID);

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

            return loadDoor;
        }

        public void RegisterLoadDoor(DoorContent door)
        {
            int actionParam = door.warp.map == 60 ? 1501 : 1500;  // enter or exit
            init.Instructions.Add(AUTO.ParseAdd($"InitializeEvent({DOOR_SLOT++}, {DOOR_ID}, {actionParam}, {door.entity}, {door.entity}, {1000}, {door.warp.map}, {door.warp.x}, {door.warp.y}, {door.warp.block}, {door.warp.entity});"));
        }

        /*
        public void RegisterLoadDoor(DoorContent door)
        {
            int actionParam = 9340;
            int area, block;
            if (door.marker.exit.layout != null) { area = 54; block = door.marker.exit.layout.id; } // Hacky and bad
            else { area = 30; block = door.marker.exit.layint.id; }

            int SLOT = COMMON_EVENT_SLOTS[EVT_LOAD_DOOR]++;
            init.Instructions.Add(AUTO.ParseAdd($"InitializeEvent({SLOT}, {EVT_LOAD_DOOR}, {area}, {block}, {actionParam}, {door.entityID}, {door.marker.entityID});"));
        }
        */

        public void Write(string emevdPath)
        {
            emevd.Write(emevdPath);
        }

        /* Handles static calls to get ids and shit */
        public class Global
        {
            public static Dictionary<uint, uint> nextEntityByMSB = new(), nextFlagByMSB = new(), nextEventByMSB = new();  //ids

            public static uint NextEntityId(int map, int x, int y, int block, int type)
            {
                uint msbid;
                if(map == 60) { msbid = uint.Parse($"10{x.ToString("D2")}{y.ToString("D2")}{type.ToString("D1")}"); }
                else { msbid = uint.Parse($"{map.ToString("D2")}{x.ToString("D2")}"); }

                uint newId;
                if (!nextEntityByMSB.ContainsKey(msbid)) { nextEntityByMSB.Add(msbid, 1); newId = 0; }
                else { newId = nextEntityByMSB[msbid]++; }

                return uint.Parse($"{msbid}{newId.ToString("D3")}");
            }

        }
    }
}
