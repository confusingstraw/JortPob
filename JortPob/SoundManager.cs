using System;
using JortPob.Common;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JortPob
{
    public class SoundManager
    {
        private int nextBankId;
        private readonly Dictionary<(NpcContent.Race, NpcContent.Sex), SoundBankInfo> banksByDemographic;
        private readonly SoundBankGlobals globals;

        public SoundManager()
        {
            nextBankId = 100;
            banksByDemographic = new();
            globals = new();
        }

        /* Either returns an existing bank meeting the requirements, or makes a new one */
        public SoundBankInfo GetBank(NpcContent npc)
        {
            ValueTuple<NpcContent.Race, NpcContent.Sex> key = (npc.race, npc.sex);
            SoundBankInfo bnk;

            if (banksByDemographic.TryGetValue((npc.race, npc.sex), out bnk))
            {
                return bnk;
            }

            bnk = new SoundBankInfo(nextBankId++, npc.race, npc.sex, new SoundBank(globals));
            banksByDemographic.Add(key, bnk);

            return bnk;
        }

        public SoundBank.Sound FindSound(NpcContent npc, uint dialogInfo)
        {
            if (banksByDemographic.TryGetValue((npc.race, npc.sex), out SoundBankInfo bnk))
            {
                return bnk.bank.sounds.FirstOrDefault(snd => snd.dialogInfo == dialogInfo);
            }

            return null; // no match found
        }

        /* Writes all soundbanks to given dir */
        public void Write(string dir)
        {
            Lort.Log($"Writing {banksByDemographic.Count()} BNKs...", Lort.Type.Main);
            Lort.NewTask("Writing BNKs", banksByDemographic.Count);

            foreach (SoundBankInfo bankInfo in banksByDemographic.Values)
            {
                bankInfo.bank.Write(dir, bankInfo.id);
                Lort.TaskIterate();
            }
        }

        public class SoundBankGlobals
        {
            private readonly uint[] usedHeaderIds, usedBnkIds, usedSourceIds;  // list of every single used bnk id (of the multiple id types) in stock elden ring. bnk ids are global so we want to avoid collisions
            private readonly List<uint> bnkCallIds; // list of every generating "play" or "stop" bnk id, these are not sequential like other ids so we track them here
            private uint nextBnkId, nextHeaderId, nextSourceId;  // do not use directly, call NextID()
            private uint nextRowId;  // increments by 10

            public SoundBankGlobals()
            {
                uint[] LoadIdList(string path)
                {
                    string[] lines = System.IO.File.ReadAllLines(path);
                    uint[] ids = new uint[lines.Length];
                    for (int i = 0; i < lines.Length; i++)
                    {
                        ids[i] = uint.Parse(lines[i]);
                    }

                    return ids;
                }

                usedBnkIds = LoadIdList(Utility.ResourcePath(@"sound\all_used_bnk_ids.txt"));
                usedHeaderIds = LoadIdList(Utility.ResourcePath(@"sound\all_used_bnk_header_ids.txt"));
                usedSourceIds = LoadIdList(Utility.ResourcePath(@"sound\all_used_source_ids.txt"));

                bnkCallIds = new();

                nextHeaderId = 100;
                nextSourceId = 100000000;
                nextBnkId = 1000;
                nextRowId = 20000000;
            }

            public uint[] GetEventBnkId()
            {
                uint[] TryGetNextCallIds(uint rowId)
                {
                    byte[] playCallBytes = Encoding.ASCII.GetBytes($"Play_v{rowId.ToString("D8")}0".ToLower());
                    byte[] stopCallBytes = Encoding.ASCII.GetBytes($"Stop_v{rowId.ToString("D8")}0".ToLower());

                    uint playCallId = Utility.FNV1_32(playCallBytes);
                    uint stopCallId = Utility.FNV1_32(stopCallBytes);

                    return new uint[] { rowId, playCallId, stopCallId };
                }

                uint[] ids = TryGetNextCallIds(NextRowId());
                while (usedBnkIds.Contains(ids[1]) || usedBnkIds.Contains(ids[2]))
                {
                    ids = TryGetNextCallIds(NextRowId());
                }

                bnkCallIds.Add(ids[1]);
                bnkCallIds.Add(ids[2]);

                return ids;
            }

            public uint NextBnkId()
            {
                while (bnkCallIds.Contains(nextBnkId) || usedBnkIds.Contains(nextBnkId))
                {
                    nextBnkId++;
                }

                return nextBnkId++;
            }

            public uint NextHeaderId()
            {
                while (usedHeaderIds.Contains(nextHeaderId))
                {
                    nextHeaderId++;
                }

                return nextHeaderId++;
            }

            public uint NextSourceId()
            {
                while (usedSourceIds.Contains(nextSourceId))
                {
                    nextSourceId++;
                }

                return nextSourceId++;
            }

            public uint NextRowId()
            {
                return nextRowId += 10;
            }
        }

        public class SoundBankInfo
        {
            public readonly int id;         // vc###.bnk id
            public readonly NpcContent.Race race;    // race of npcs that use this bank
            public readonly NpcContent.Sex sex;
            public int uses;                // how many esds use this same bank

            public readonly SoundBank bank;

            public SoundBankInfo(int id, NpcContent.Race race, NpcContent.Sex sex, SoundBank bank)
            {
                this.id = id;
                this.race = race;
                this.sex = sex;
                this.bank = bank;

                uses = 0;
            }
        }
    }
}
