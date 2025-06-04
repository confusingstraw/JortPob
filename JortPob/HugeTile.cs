using JortPob.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace JortPob
{
    /* HugeTile is a 4x4 grid of Tiles. Sort of like an LOD type thing. (????) */
    public class HugeTile : BaseTile
    {
        public List<BigTile> bigs;
        public List<Tile> tiles;

        public HugeTile(int m, int x, int y, int b) : base(m, x, y, b)
        {
            bigs = new();
            tiles = new();
        }

        /* Checks ABSOLUTE POSITION! This is the position of an object from the ESM accounting for the layout offset! */
        public bool PositionInside(Vector3 position)
        {
            Vector3 pos = position + Const.LAYOUT_COORDINATE_OFFSET;

            float x1 = (coordinate.x * 4f * Const.TILE_SIZE) - (Const.TILE_SIZE * 0.5f);
            float y1 = (coordinate.y * 4f * Const.TILE_SIZE) - (Const.TILE_SIZE * 0.5f);
            float x2 = x1 + (Const.TILE_SIZE * 4f);
            float y2 = y1 + (Const.TILE_SIZE * 4f);

            if (pos.X >= x1 && pos.X < x2 && pos.Z >= y1 && pos.Z < y2)
            {
                return true;
            }

            return false;
        }

        public void AddTerrain(Vector3 position, TerrainInfo terrainInfo)
        {
            float x = (coordinate.x * 4f * Const.TILE_SIZE) + (Const.TILE_SIZE * 1.5f);
            float y = (coordinate.y * 4f * Const.TILE_SIZE) + (Const.TILE_SIZE * 1.5f);
            Vector3 relative = (position + Const.LAYOUT_COORDINATE_OFFSET) - new Vector3(x, 0, y);
            terrain.Add(new Tuple<Vector3, TerrainInfo>(relative, terrainInfo));
        }

        /* Incoming content is in aboslute worldspace from the ESM, when adding content to a tile we convert it's coordiantes to relative space */
        public void AddContent(AssetContent content)
        {
            float x = (coordinate.x * 4f * Const.TILE_SIZE) + (Const.TILE_SIZE * 1.5f);
            float y = (coordinate.y * 4f * Const.TILE_SIZE) + (Const.TILE_SIZE * 1.5f);
            content.relative = (content.position + Const.LAYOUT_COORDINATE_OFFSET) - new Vector3(x, 0, y);
            assets.Add(content);
        }

        public void AddBig(BigTile big)
        {
            bigs.Add(big);
            big.huge = this;
        }

        public void AddTile(Tile tile)
        {
            tiles.Add(tile);
            tile.huge = this;
        }
    }
}
