using JortPob.Common;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

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

        public override void AddCell(Cell cell)
        {
            cells.Add(cell);
            BigTile big = GetBigTile(cell.center);
            big.AddCell(cell);
        }

        public void AddTerrain(Vector3 position, TerrainInfo terrainInfo)
        {
            /*  // deprecated, all terrain has been moved to superoverworld in the OverworldManager class
            float x = (coordinate.x * 4f * Const.TILE_SIZE) + (Const.TILE_SIZE * 1.5f);
            float y = (coordinate.y * 4f * Const.TILE_SIZE) + (Const.TILE_SIZE * 1.5f);
            Vector3 relative = (position + Const.LAYOUT_COORDINATE_OFFSET) - new Vector3(x, 0, y);
            terrain.Add(new Tuple<Vector3, TerrainInfo>(relative, terrainInfo));*/

            Tile tile = GetTile(position);
            tile.AddTerrain(position, terrainInfo);
        }

        /* Incoming content is in aboslute worldspace from the ESM, when adding content to a tile we convert it's coordinates to relative space */
        public new void AddContent(Cache cache, Cell cell, Content content)
        {
            switch (content)
            {
                case AssetContent a:
                    ModelInfo modelInfo = cache.GetModel(a.mesh);
                    if (modelInfo.size * (content.scale * 0.01f) > Const.CONTENT_SIZE_HUGE)
                    {
                        float x = (coordinate.x * 4f * Const.TILE_SIZE) + (Const.TILE_SIZE * 1.5f);
                        float y = (coordinate.y * 4f * Const.TILE_SIZE) + (Const.TILE_SIZE * 1.5f);
                        content.relative = (content.position + Const.LAYOUT_COORDINATE_OFFSET) - new Vector3(x, 0, y);
                        Tile tile = GetTile(cell.center);
                        content.load = tile.coordinate;
                        base.AddContent(cache, cell, content);
                        break;
                    }
                    goto default;
                default:
                    BigTile big = GetBigTile(cell.center);
                    big.AddContent(cache, cell, content);
                    break;
            }
        }

        public BigTile GetBigTile(Vector3 position)
        {
            foreach (BigTile big in bigs)
            {
                if (big.PositionInside(position))
                {
                    return big;
                }
            }
            return null;
        }

        public Tile GetTile(Vector3 position)
        {
            foreach (Tile tile in tiles)
            {
                if (tile.PositionInside(position))
                {
                    return tile;
                }
            }
            return null;
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

        public string GetRegion()
        {
            Dictionary<string, int> regions = new();
            foreach (Cell cell in cells)
            {
                string r = cell.region.Trim().ToLower();
                if (regions.ContainsKey(r)) { regions[r]++; }
                else { regions.Add(r, 1); }
            }

            string most = regions.Keys.First();
            foreach (KeyValuePair<string, int> kvp in regions)
            {
                if (regions[most] < kvp.Value)
                {
                    most = kvp.Key;
                }
            }

            /* Red Mountain has priority for skybox */
            string redMountain = "Red Mountain Region".Trim().ToLower();
            if (regions.ContainsKey(redMountain))
            {
                if (regions[redMountain] >= 8) { most = redMountain; }
            }

            return most;
        }
    }
}
