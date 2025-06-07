using JortPob.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace JortPob.Worker
{
    public class InteriorGroup
    {
        public readonly int map;
        public readonly int area;
        public readonly int unk;
        public readonly int block;

        public readonly List<Chunk> chunks;

        public InteriorGroup(int m, int a, int u, int b)
        {
            /* Interior Data */
            map = m;
            area = a;
            unk = u;
            block = b;

            chunks = new();
        }

        public void AddCell(Cell cell)
        {
            Chunk chunk = new(cell);
            chunks.Add(chunk);
        }

        public class Chunk
        {
            public readonly List<AssetContent> assets;
            public readonly List<LightContent> lights;
            public readonly List<EmitterContent> emitters;
            public readonly List<CreatureContent> creatures;
            public readonly List<NpcContent> npcs;

            public Chunk(Cell cell)
            {
                assets = new();
                emitters = new();
                lights = new();
                creatures = new();
                npcs = new();

                /* Process cell data... */
                // stubbbbbbbbbb
            }
        }
    }
}
