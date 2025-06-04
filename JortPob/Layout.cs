using JortPob.Common;
using SoulsFormats.Formats.Morpheme.MorphemeBundle;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace JortPob
{

    /* Takes the Morrowind ESM cell grid and re-subdivides it into the Elden Ring tile grid */
    public class Layout
    {
        public List<BaseTile> all;
        public List<HugeTile> huges;
        public List<BigTile> bigs;
        public List<Tile> tiles;

        public Layout(Cache cache, ESM esm)
        {
            all = new();
            huges = new();
            bigs = new();
            tiles = new();

            /* Generate tiles based off base game msb info... */
            string msbdata = File.ReadAllText(Utility.ResourcePath(@"msb\msblist.txt"));
            string[] msblist = msbdata.Split(";");
            foreach (string msb in msblist)
            {
                string[] split = msb.Split(",");
                int m = int.Parse(split[0]);
                int x = int.Parse(split[1]);
                int y = int.Parse(split[2]);
                int b = int.Parse(split[3]);

                if(m == 60 && b == 0)
                {
                    Tile tile = new Tile(m, x, y, b);
                    tiles.Add(tile);
                    all.Add(tile);
                }
            }

            /* Generate BigTiles... */
            foreach (string msb in msblist)
            {
                string[] split = msb.Split(",");
                int m = int.Parse(split[0]);
                int x = int.Parse(split[1]);
                int y = int.Parse(split[2]);
                int b = int.Parse(split[3]);

                if (m == 60 && b == 1)
                {
                    BigTile big = new BigTile(m, x, y, b);

                    foreach (Tile tile in tiles)
                    {
                        int x1 = x * 2;
                        int y1 = y * 2;
                        int x2 = x1 + 2;
                        int y2 = y1 + 2;
                        if (tile.coordinate.x >= x1 && tile.coordinate.x < x2 && tile.coordinate.y >= y1 && tile.coordinate.y < y2)
                        {
                            big.AddTile(tile);
                        }
                    }

                    bigs.Add(big);
                    all.Add(big);
                }
            }

            /* Generate HugeTiles... */
            foreach (string msb in msblist)
            {
                string[] split = msb.Split(",");
                int m = int.Parse(split[0]);
                int x = int.Parse(split[1]);
                int y = int.Parse(split[2]);
                int b = int.Parse(split[3]);

                if (m == 60 && b == 2)
                {
                    HugeTile huge = new HugeTile(m, x, y, b);

                    foreach(BigTile big in bigs)
                    {
                        int x1 = x * 2;
                        int y1 = y * 2;
                        int x2 = x1 + 2;
                        int y2 = y1 + 2;
                        if (big.coordinate.x >= x1 && big.coordinate.x < x2 && big.coordinate.y >= y1 && big.coordinate.y < y2)
                        {
                            huge.AddBig(big);
                        }
                    }

                    foreach(Tile tile in tiles)
                    {
                        int x1 = x * 4;
                        int y1 = y * 4;
                        int x2 = x1 + 4;
                        int y2 = y1 + 4;
                        if(tile.coordinate.x >= x1 && tile.coordinate.x < x2 && tile.coordinate.y >= y1 && tile.coordinate.y < y2)
                        {
                            huge.AddTile(tile);
                        }
                    }

                    huges.Add(huge);
                    all.Add(huge);
                }
            }

            /* Subdivide all cell content into tiles */
            foreach (Cell cell in esm.exterior)
            {
                TerrainInfo terrain = cache.GetTerrain(cell.coordinate);
                if(terrain != null)
                {
                    HugeTile huge = GetHugeTile(cell.center);
                    if(huge != null)
                    {
                        huge.AddTerrain(cell.center, terrain);
                    }
                }
                foreach(AssetContent content in cell.assets)
                {
                    ModelInfo modelInfo = cache.GetModel(content.mesh);

                    if (modelInfo.size * content.scale > Const.CONTENT_SIZE_HUGE)
                    {
                        HugeTile huge = GetHugeTile(content.position);
                        if (huge != null) { huge.AddContent(content); }
                    }
                    else if (modelInfo.size * content.scale > Const.CONTENT_SIZE_BIG)
                    {
                        BigTile big = GetBigTile(content.position);
                        if (big != null) { big.AddContent(content); }
                    }
                    else
                    {
                        Tile tile = GetTile(content.position);
                        if (tile != null) { tile.AddContent(content); }
                    }
                }
                foreach (EmitterContent content in cell.emitters)
                {
                    Tile tile = GetTile(content.position);
                    if (tile != null)
                    {
                        tile.AddContent(content);
                    }
                }
                foreach (LightContent content in cell.lights)
                {
                    Tile tile = GetTile(content.position);
                    if (tile != null)
                    {
                        tile.AddContent(content);
                    }
                }
            }

            /* Render an ASCII image of the tiles for verification! */
            Console.WriteLine("Drawing ASCII map of worldspace map...\n");
            for (int y = 66; y >= 28; y--)
            {
                for (int x = 30; x < 64; x++)
                {
                    Tile tile = GetTile(new Int2(x, y));
                    if(tile == null) { Console.Write("-"); }
                    else
                    {
                        Console.Write(tile.assets.Count > 0 ? "X" : "~");
                    }
                }
                Console.Write("\n");
            }

            Console.WriteLine("DEBUG");
        }

        public HugeTile GetHugeTile(Vector3 position)
        {
            foreach (HugeTile huge in huges)
            {
                if (huge.PositionInside(position))
                {
                    return huge;
                }
            }
            return null;
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
            foreach(Tile tile in tiles)
            {
                if (tile.PositionInside(position))
                {
                    return tile;
                }
            }
            return null;
        }

        public Tile GetTile(Int2 coordinate)
        {
            foreach(Tile tile in tiles)
            {
                if(tile.coordinate == coordinate)
                {
                    return tile;
                }
            }
            return null;
        }
    }
}
