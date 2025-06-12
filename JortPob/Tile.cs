using JortPob.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace JortPob
{
    /* A Tile is what we call a single square on the Elden Ring cell grid. It's basically the Elden Ring version of a "cell" */
    public class Tile : BaseTile
    {
        public HugeTile huge;
        public BigTile big;

        public Tile(int m, int x, int y, int b) : base(m, x, y, b)
        {

        }

        /* Checks ABSOLUTE POSITION! This is the position of an object from the ESM accounting for the layout offset! */
        public bool PositionInside(Vector3 position)
        {
            Vector3 pos = position + Const.LAYOUT_COORDINATE_OFFSET;

            float x1 = (coordinate.x * Const.TILE_SIZE) - (Const.TILE_SIZE * 0.5f);
            float y1 = (coordinate.y * Const.TILE_SIZE) - (Const.TILE_SIZE * 0.5f);
            float x2 = x1 + Const.TILE_SIZE;
            float y2 = y1 + Const.TILE_SIZE;

            if(pos.X >= x1 && pos.X < x2 && pos.Z >= y1 && pos.Z < y2)
            {
                return true;
            }

            return false;
        }

        public void AddTerrain(Vector3 position, TerrainInfo terrainInfo)
        {
            float x = (coordinate.x * Const.TILE_SIZE);
            float y = (coordinate.y * Const.TILE_SIZE);
            Vector3 relative = (position + Const.LAYOUT_COORDINATE_OFFSET) - new Vector3(x, 0, y);
            terrain.Add(new Tuple<Vector3, TerrainInfo>(relative, terrainInfo));
        }

        public new void AddContent(Cache cache, Content content)
        {
            float x = (coordinate.x * Const.TILE_SIZE);
            float y = (coordinate.y * Const.TILE_SIZE);
            content.relative = (content.position + Const.LAYOUT_COORDINATE_OFFSET) - new Vector3(x, 0, y);

            base.AddContent(cache, content);
        }
    }



    public abstract class BaseTile
    {
        public readonly int map;
        public readonly Int2 coordinate;
        public readonly int block;

        public readonly List<Tuple<Vector3, TerrainInfo>> terrain;
        public readonly List<AssetContent> assets;
        public readonly List<LightContent> lights;
        public readonly List<EmitterContent> emitters;
        public readonly List<CreatureContent> creatures;
        public readonly List<NpcContent> npcs;

        public BaseTile(int m, int x, int y, int b)
        {
            /* Tile Data */
            map = m;
            coordinate = new(x, y);
            block = b;

            /* Tile Content Data */
            terrain = new();
            assets = new();
            emitters = new();
            lights = new();
            creatures = new();
            npcs = new();
        }


        /* Incoming content is in aboslute worldspace from the ESM, when adding content to a tile we convert it's coordiantes to relative space */
        public void AddContent(Cache cache, Content content)
        {
            switch(content)
            {
                case AssetContent a:
                    assets.Add(a); break;
                case EmitterContent e:
                    emitters.Add(e); break;
                case LightContent l:
                    lights.Add(l); break;
                case NpcContent n:
                    npcs.Add(n); break;
                case CreatureContent c:
                    creatures.Add(c); break;
                default:
                    Console.WriteLine(" ## WARNING ## Unhandled Content class fell through AddContent()"); break;
            }
        }
    }
}
