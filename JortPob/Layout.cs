using HKLib.hk2018.TypeRegistryTest;
using JortPob.Common;
using JortPob.Worker;
using SoulsFormats.Formats.Morpheme.MorphemeBundle;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata;
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

        public List<InteriorGroup> interiors;

        public Layout(Cache cache, ESM esm)
        {
            all = new();
            huges = new();
            bigs = new();
            tiles = new();

            interiors = new();

            /* Generate tiles based off base game msb info... */
            string msbdata = File.ReadAllText(Utility.ResourcePath(@"msb\msblist.txt"));
            string[] msblist = msbdata.Split(";");

            Lort.Log("Generating layout...", Lort.Type.Main);
            Lort.NewTask("Generating Layout", msblist.Length+esm.exterior.Count+esm.interior.Count);

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

                Lort.TaskIterate(); // Progress bar update
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

                Lort.TaskIterate(); // Progress bar update
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

                Lort.TaskIterate(); // Progress bar update
            }

            /* Generate Interior Groups */
            foreach(string msb in msblist)
            {
                string[] split = msb.Split(",");
                int m = int.Parse(split[0]);
                int a = int.Parse(split[1]);
                int u = int.Parse(split[2]);
                int b = int.Parse(split[3]);

                if ((m == 30 || m == 31 || m == 32) && u == 0 && b == 0)
                {
                    InteriorGroup group = new InteriorGroup(m, a, u, b);
                    interiors.Add(group);
                }

                Lort.TaskIterate(); // Progress bar update
            }

            Content EmitterConversionCheck(Content content)
            {
                if(content.GetType() != typeof(AssetContent)) { return content; }
                AssetContent assetContent = content as AssetContent;

                /* If an assetcontent has emitter nodes, we convert it to an emittercontent */
                /* We can't really do this earlier than this point sadly because we need both the ESM loaded an cache built to be able to catch this corner case */
                /* So we do it here */
                ModelInfo modelInfo = cache.GetModel(assetContent.mesh);
                if (!modelInfo.HasEmitter()) { return content; }

                EmitterContent emitterContent = assetContent.ConvertToEmitter();
                cache.AddConvertedEmitter(emitterContent);

                return emitterContent;
            }

            /* Subdivide all cell content into tiles */
            foreach (Cell cell in esm.exterior)
            {
                HugeTile huge = GetHugeTile(cell.center);
                TerrainInfo terrain = cache.GetTerrain(cell.coordinate);
                if (terrain != null)
                {
                    if (huge != null) { huge.AddTerrain(cell.center, terrain); }
                    else { Lort.Log($" ## WARNING ## Terrain fell outside of reality {cell.coordinate} -- {cell.region}", Lort.Type.Debug); }
                }

                huge.AddCell(cell);

                if (huge != null)
                {
                    foreach (Content content in cell.contents)
                    {
                        Content c = EmitterConversionCheck(content); // checks if we need to convert an assetcontent into an emittercontent due to it having emitter nodes but no light data

                        huge.AddContent(cache, cell, c);
                    }
                }
                else { Lort.Log($" ## WARNING ## Cell fell outside of reality {cell.coordinate} -- {cell.name}", Lort.Type.Debug); }
                Lort.TaskIterate(); // Progress bar update
            }


            /* Subdivide all interior cells into groups */
            int partition = (int)Math.Ceiling(esm.interior.Count / (float)interiors.Count);
            int start = 0, end = partition;
            foreach (InteriorGroup group in interiors)
            {
                for(int i=start; i<Math.Min(end, esm.interior.Count); i++)
                {
                    Cell cell = esm.interior[i];
                    group.AddCell(cell);

                    Lort.TaskIterate(); // Progress bar update
                }

                start += partition;
                end += partition;
            }

            /* Render an ASCII image of the tiles for verification! */
            Lort.Log("Drawing ASCII art of worldspace map...", Lort.Type.Debug);
            for (int y = 66; y >= 28; y--)
            {
                string line = "";
                for (int x = 30; x < 64; x++)
                {
                    Tile tile = GetTile(new Int2(x, y));
                    if(tile == null) { line += "-"; }
                    else
                    {
                        line += tile.assets.Count > 0 ? "X" : "~";
                    }
                }
                Lort.Log(line, Lort.Type.Debug);
            }
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

        public HugeTile GetHugeTile(Int2 coordinate)
        {
            foreach (HugeTile huge in huges)
            {
                if (huge.coordinate == coordinate)
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
