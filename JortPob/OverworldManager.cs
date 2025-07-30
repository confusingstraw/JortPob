using HKLib.hk2018.hkaiWorldCommands;
using JortPob.Common;
using PortJob;
using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static JortPob.OverworldManager;
using static JortPob.Paramanager;
using static SoulsAssetPipeline.Audio.Wwise.WwiseBlock;
using static SoulsFormats.MSB3.Region;

namespace JortPob
{
    public class OverworldManager
    {
        /* Makes a modified version  of m60_00_00_99 */
        /* This msb is the "super overworld" an lod msb that is always visible at any distance in the overworld */
        /* It also contains things like the sky and water so yee */
        public static ResourcePool Generate(Cache cache, ESM esm, Layout layout, Paramanager param)
        {
            Lort.Log($"Building Overworld...", Lort.Type.Main);
            Lort.NewTask("Overworld Generation", 2);

            MSBE msb = MSBE.Read(Utility.ResourcePath(@"msb\m60_00_00_99.msb.dcx"));
            LightManager lightManager = new(60, 00, 00, 99);
            ResourcePool pool = new(msb, lightManager);

            /* Delete all vanilla map parts */
            msb.Parts.MapPieces.Clear();

            /* Delete all vanilla envboxes and stuffs */
            msb.Regions.EnvironmentMapEffectBoxes.Clear();
            msb.Regions.EnvironmentMapPoints.Clear();
            msb.Regions.SoundRegions.Clear();
            msb.Regions.Sounds.Clear();

            /* Delete all vanilla assets except the skyboxs */
            List<MSBE.Part.Asset> keep = new();
            foreach (MSBE.Part.Asset asset in msb.Parts.Assets)
            {
                int id1 = int.Parse(asset.Name.Substring(3, 3));
                int id2 = int.Parse(asset.Name.Substring(7, 3));

                // Sky stuff
                if (id1 == 96) { keep.Add(asset); continue; }
            }

            msb.Parts.Assets = keep;

            /* Calculate actual 0,0 of morrowind against the superoverworld root offset (DUMB!) */
            Cell cell = esm.GetCellByGrid(new Int2(0, 0));
            Vector3 center;
            if (cell != null)
            {
                float x = (10 * 4f * Const.TILE_SIZE) + (Const.TILE_SIZE * 1.5f);
                float y = (8 * 4f * Const.TILE_SIZE) + (Const.TILE_SIZE * 1.5f);
                center = (cell.center + Const.LAYOUT_COORDINATE_OFFSET) - new Vector3(x, 0, y);
            }
            else
            {
                // if we have the const debug flag set for building a specific cell or group of cells the esm 0,0 cell may not be loaded
                // in that case here is the correct value under normal circumstances, its better to calc it for safety but its debug so w/e
                center = new Vector3(15.360352f, 0f, 2831.3604f);
            }

            /* Add water */
            MSBE.Part.Asset water = MakePart.Asset(cache.GetWater());
            water.Position = center + Const.TEST_OFFSET1 + Const.TEST_OFFSET2;
            msb.Parts.Assets.Add(water);

            /* Add swamp */
            MSBE.Part.Asset swamp = MakePart.Asset(cache.GetSwamp());
            swamp.Position = center + Const.TEST_OFFSET1 + Const.TEST_OFFSET2;
            msb.Parts.Assets.Add(swamp);

            /* Add lava */
            MSBE.Part.Asset lava = MakePart.Asset(cache.GetLava());
            lava.Position = center + Const.TEST_OFFSET1 + Const.TEST_OFFSET2 + new Vector3(0f, Const.LAVA_VISUAL_OFFSET, 0f);
            msb.Parts.Assets.Add(lava);

            /* Add terrain */
            foreach (TerrainInfo terrainInfo in cache.terrains)
            {
                Vector3 position = new Vector3(terrainInfo.coordinate.x * Const.CELL_SIZE, 0, terrainInfo.coordinate.y * Const.CELL_SIZE) + center;

                MSBE.Part.MapPiece map = MakePart.MapPiece();
                map.Name = $"m{terrainInfo.id.ToString("D8")}_0000";
                map.ModelName = $"m{terrainInfo.id.ToString("D8")}";
                map.Position = position + Const.TEST_OFFSET1 + Const.TEST_OFFSET2;
                map.PartsDrawParamID = param.terrainDrawParamID;

                msb.Parts.MapPieces.Add(map);
                pool.mapIndices.Add(new Tuple<int, string>(terrainInfo.id, terrainInfo.path));
            }

            Lort.TaskIterate();

            //MSBE SOURCE = MSBE.Read(Utility.ResourcePath(@"msb\m60_00_00_99.msb.dcx"));

            Int2 min = new(-1, 1);
            Int2 max = new(3, 6);
            int[] start = new int[] { 60, 10, 8, 2 }; // first msb at the min coord
            float size = 1024f; float crossfade = 32f;
            int id = 960;

            for (int y = min.y; y < max.y; y++)
            {
                for (int x = min.x; x < max.x; x++)
                {

                    /* Grab tile */
                    int[] msbid = new int[] { start[0], start[1] + x, start[2] + y, start[3] };
                    HugeTile tile = layout.GetHugeTile(new Int2(msbid[1], msbid[2]));
                    if(tile.IsEmpty()) { continue; } // skip empty

                    /* Create envmap texture file */
                    CreateEnvMaps(tile, id);

                    /* Create an envbox */
                    MSBE.Region.EnvironmentMapEffectBox envBox = new();
                    envBox.Name = $"Env_Box{id.ToString("D3")}";
                    envBox.Shape = new MSB.Shape.Box(size+crossfade, size + crossfade, size + crossfade);
                    envBox.Position = new Vector3(x * size, -256f, y * size);
                    envBox.Rotation = Vector3.Zero;
                    envBox.ActivationPartName = null;
                    envBox.EntityID = 0;
                    envBox.IsModifyLight = true;
                    envBox.MapID = -1;
                    envBox.MapStudioLayer = 4294967295;
                    envBox.PointLightMult = 1f;
                    envBox.RegionID = 0;
                    envBox.SpecularLightMult = 1f;
                    envBox.TransitionDist = crossfade / 2f;
                    envBox.Unk40 = 0;
                    envBox.UnkE08 = 255;
                    envBox.UnkS04 = 0;
                    envBox.UnkS0C = -1;
                    envBox.UnkT08 = 0;
                    envBox.UnkT09 = 10;
                    envBox.UnkT0A = -1;
                    envBox.UnkT2C = 0;
                    envBox.UnkT2F = false;
                    envBox.UnkT30 = -1;
                    envBox.UnkT32 = false;
                    envBox.UnkT33 = true;
                    envBox.UnkT34 = 1;
                    envBox.UnkT36 = 1;
                    msb.Regions.EnvironmentMapEffectBoxes.Add(envBox);

                    MSBE.Region.EnvironmentMapPoint envPoint = new();
                    envPoint.Name = $"Env_Point{id.ToString("D3")}";
                    envPoint.Shape = new MSB.Shape.Point();
                    envPoint.Position = new Vector3(x*size, 128f, y*size);
                    envPoint.Rotation = Vector3.Zero;
                    envPoint.ActivationPartName = null;
                    envPoint.EntityID = 0;
                    envPoint.MapID = -1;
                    envPoint.MapStudioLayer = 4294967295;
                    envPoint.RegionID = 0;
                    envPoint.Unk40 = 0;
                    envPoint.UnkE08 = 255;
                    envPoint.UnkMapID = new byte[] { (byte)msbid[0], (byte)msbid[1], (byte)msbid[2], (byte)msbid[3] };
                    envPoint.UnkS04 = 0;
                    envPoint.UnkS0C = -1;
                    envPoint.UnkT00 = 200;
                    envPoint.UnkT04 = 2;
                    envPoint.UnkT0D = true;
                    envPoint.UnkT0E = true;
                    envPoint.UnkT0F = true;
                    envPoint.UnkT10 = 1;
                    envPoint.UnkT14 = 1;
                    envPoint.UnkT20 = 512;
                    envPoint.UnkT24 = 64;
                    envPoint.UnkT28 = 5;
                    envPoint.UnkT2C = 0;
                    envPoint.UnkT2D = 1;
                    msb.Regions.EnvironmentMapPoints.Add(envPoint);

                    id++;
                }
            }

            Lort.TaskIterate();

            /* Regenerate resources */
            msb.Models.Assets.Clear();
            msb.Models.MapPieces.Clear();
            AutoResource.Generate(60, 0, 0, 99, msb);

            return pool;
        }

        public class EnvMap
        {
            public readonly List<string> tpfs;
            public readonly List<string> infos;
            public EnvMap(int map, Int2 coordinate, int block, int count)
            {
                tpfs = new();
                infos = new();

                string[] levs = new string[] { "high", "middle", "low" };
                string mid = $"{map.ToString("D2")}_{coordinate.x.ToString("D2")}_{coordinate.y.ToString("D2")}_{block.ToString("D2")}"; // msb full name

                for (int i=0;i<count;i++)
                {
                    foreach(string lev in levs)
                    {
                        tpfs.Add($"env\\m{mid}_envmap_{i.ToString("D2")}_{lev}_00.tpfbnd.dcx");
                    }
                }

                foreach(string lev in levs)
                {
                    infos.Add($"env\\m{mid}_{lev}.ivinfobnd.dcx");
                }

            }
        }

        public static void CreateEnvMaps(HugeTile tile, int id)
        {
            
            string region = tile.GetRegion();
            WeatherData weatherData = null;
            foreach (WeatherData w in WEATHER_DATA_LIST)
            {
                if (w.match.Contains(region))
                {
                    weatherData = w; break;
                }
            }
            Lort.Log($" ## INFO ### MSB {tile.coordinate.x}_{tile.coordinate.y} region truncated to -> {region}", Lort.Type.Debug);

            EnvMap template = weatherData.env;

            string mid = $"{tile.map.ToString("D2")}_{tile.coordinate.x.ToString("D2")}_{tile.coordinate.y.ToString("D2")}_{tile.block.ToString("D2")}"; // msb full name
            foreach (string tpfPath in template.tpfs)
            {
                BND4 bnd = BND4.Read(Utility.ResourcePath(tpfPath));
                string lev = tpfPath.Split("_")[6];
                string inst = tpfPath.Split("_")[5];

                foreach (BinderFile file in bnd.Files)
                {
                    string ext = Utility.PathToFileName(file.Name).Split("_").Last();
                    string g = Utility.PathToFileName(file.Name).Contains("GILM") ? "GILM" : "GIIV";

                    TPF tpf = TPF.Read(file.Bytes);
                    TPF.Texture tex = tpf.Textures[0];
                    tex.Name = $"m{mid}_{g}{id.ToString("D4")}_{inst}_{ext}";
                    file.Bytes = tpf.Write();

                    file.Name = $"N:\\GR\\data\\INTERROOT_win64\\map\\m{mid}\\tex\\Envmap\\{lev}\\{inst}\\m{mid}_{g}{id.ToString("D4")}_{inst}_{ext}.tpf";
                }

                bnd.Write($"{Const.OUTPUT_PATH}map\\m{tile.map.ToString("D2")}\\m{mid}\\m{mid}_envmap_{inst}_{lev}_00.tpfbnd.dcx");
            }

            foreach(string infoPath in template.infos)
            {
                BND4 bnd = BND4.Read(Utility.ResourcePath(infoPath));
                string lev = Utility.PathToFileName(infoPath).Split("_")[4].Split(".")[0];

                foreach (BinderFile file in bnd.Files)
                {
                    string inst = file.Name.Split("_").Last().Split(".")[0];

                    file.Name = $"N:\\GR\\data\\INTERROOT_win64\\map\\m{mid}\\tex\\Envmap\\{lev}\\IvInfo\\m{mid}_GIIV{id.ToString("D4")}_{inst}.ivInfo";
                }

                bnd.Write($"{Const.OUTPUT_PATH}map\\m{tile.map.ToString("D2")}\\m{mid}\\m{mid}_{lev}.ivinfobnd.dcx");
            }
        }
    }
}
