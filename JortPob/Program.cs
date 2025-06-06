using JortPob.Common;
using JortPob.Model;
using PortJob;
using SharpAssimp;
using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net.Mime;
using System.Numerics;
using System.Reflection.Metadata;
using System.Text.Json;

namespace JortPob
{
    class Program
    {
        static void Main(string[] args)
        {
            string morrowindPath = Const.MORROWIND_PATH + @"Data Files\";
            string modPath = Const.OUTPUT_PATH;
            string cachePath = modPath + @"cache\";

            ESM esm = new ESM(morrowindPath + @"Morrowind.json");
            Cache cache = Cache.Load(esm, cachePath, morrowindPath);
            Layout layout = new(cache, esm);


            /* Generate msbs from layout */
            Vector3 TEST_OFFSET1 = new(0, 200, 0); // just shifting vertical position a bit so the morrowind map isn't super far down
            Vector3 TEST_OFFSET2 = new(0, -15, 0);
            short TEST_PART_DRAW = 1001;
            int INSTANCETEST = 0;
            List<Tuple<BaseTile, MSBE>> msbs = new();
            foreach (BaseTile tile in layout.all)
            {
                if(tile.assets.Count <= 0 && tile.terrain.Count <= 0) { continue; }   // Skip empty tiles.
                Console.WriteLine($"Generating MSB m{tile.map} [{tile.coordinate.x},{tile.coordinate.y}] :: b{tile.block}");

                /* Generate msb from tile */
                MSBE msb = new();
                msb.Compression = SoulsFormats.DCX.Type.DCX_KRAK;

                /* TEST big flat piece of collision */
                if (tile.GetType() == typeof(Tile))
                {
                    MSBE.Part.Collision collision = new();
                    collision.Name = $"h{tile.coordinate.x.ToString("D2")}{tile.coordinate.y.ToString("D2")}00_test";
                    collision.ModelName = $"h{tile.coordinate.x.ToString("D2")}{tile.coordinate.y.ToString("D2")}00";
                    collision.MapStudioLayer = 4294967295;
                    collision.Position = new Vector3(0, -65.574f, 0) + TEST_OFFSET1;
                    msb.Parts.Collisions.Add(collision);
                }

                /* Add terrain */
                foreach (Tuple<Vector3, TerrainInfo> tuple in tile.terrain)
                {
                    Vector3 position = tuple.Item1;
                    TerrainInfo terrainInfo = tuple.Item2;

                    MSBE.Part.MapPiece map = new();
                    map.Name = $"m{terrainInfo.id.ToString("D8")}_test";
                    map.ModelName = $"m{terrainInfo.id.ToString("D8")}";
                    map.MapStudioLayer = 4294967295;

                    map.isUsePartsDrawParamID = 1;
                    map.PartsDrawParamID = TEST_PART_DRAW;

                    map.Position = position + TEST_OFFSET1 + TEST_OFFSET2;
                    msb.Parts.MapPieces.Add(map);
                }

                /* Add assets */
                foreach (AssetContent content in tile.assets)
                {
                    // Grab da thing
                    ModelInfo modelInfo = cache.GetModel(content.mesh);

                    // Make da thing
                    MSBE.Part.Asset asset = new();
                    asset.Name = $"{modelInfo.AssetName().ToUpper()}_test";
                    asset.ModelName = modelInfo.AssetName().ToUpper();
                    asset.MapStudioLayer = 4294967295;

                    asset.isUsePartsDrawParamID = 1;
                    asset.PartsDrawParamID = TEST_PART_DRAW;

                    asset.Unk1.DisplayGroups[0] = 16;

                    asset.InstanceID = INSTANCETEST++;

                    asset.Position = content.relative + TEST_OFFSET1 + TEST_OFFSET2;
                    asset.Rotation = content.rotation;
                    asset.Scale = new Vector3(content.scale);
                    msb.Parts.Assets.Add(asset);
                }

                /* Test players */
                if (tile.GetType() == typeof(Tile))
                {
                    MSBE.Part.Player player_0 = new();
                    player_0.Name = "c0000_9001";
                    player_0.ModelName = "c0000";
                    player_0.InstanceID = 9001;
                    player_0.MapStudioLayer = 4294967295;
                    player_0.Unk1.DisplayGroups[0] = 16;
                    player_0.Position = TEST_OFFSET1;
                    msb.Parts.Add(player_0);
                }

                /* Auto resource */
                AutoResource.Generate(tile.map, tile.coordinate.x, tile.coordinate.y, tile.block, msb);

                /* Done */
                msbs.Add(new Tuple<BaseTile, MSBE>(tile, msb));
            }

            /* Bind and write all materials and textures */
            Console.WriteLine($"Writing materials and textures...");
            Bind.BindMaterials(cachePath, $"{modPath}material\\allmaterial.matbinbnd.dcx");
            Bind.BindTPF(cache, cachePath, $"{modPath}map\\m60\\common\\m60_0000");

            /* Bind all assets */
            Console.WriteLine($"Writing assets...");
            foreach (ModelInfo mod in cache.assets)
            {
                Bind.BindAsset(mod, cachePath, $"{modPath}asset\\aeg\\{mod.AssetPath()}.geombnd.dcx");
            }

            /* Write msbs */
            foreach (Tuple<BaseTile, MSBE> tuple in msbs)
            {
                BaseTile tile = tuple.Item1;
                MSBE msb = tuple.Item2;
                string name = $"{tile.map.ToString("D2")}_{tile.coordinate.x.ToString("D2")}_{tile.coordinate.y.ToString("D2")}_{tile.block.ToString("D2")}";

                Console.WriteLine($"Writing files for -> m{tile.map} [{tile.coordinate.x},{tile.coordinate.y}] :: b{tile.block}");

                msb.Write($"{modPath}map\\mapstudio\\m{name}.msb.dcx");

                /* Write terrain */
                foreach (Tuple<Vector3, TerrainInfo> tup in tile.terrain)
                {
                    TerrainInfo terrainInfo = tup.Item2;
                    FLVER2 flver = FLVER2.Read($"{cachePath}{terrainInfo.path}");

                    BND4 bnd = new();
                    bnd.Compression = SoulsFormats.DCX.Type.DCX_KRAK;
                    bnd.Version = "07D7R6";

                    BinderFile file = new();
                    file.CompressionType = SoulsFormats.DCX.Type.Zlib;
                    file.Flags = SoulsFormats.Binder.FileFlags.Flag1;
                    file.ID = 200;
                    file.Name = $"N:\\GR\\data\\INTERROOT_win64\\map\\m{name}\\m{name}_{terrainInfo.id.ToString("D8")}\\Model\\m{name}_{terrainInfo.id.ToString("D8")}.flver";
                    file.Bytes = flver.Write();
                    bnd.Files.Add(file);

                    bnd.Write($"{modPath}map\\m60\\m{name}\\m{name}_{terrainInfo.id.ToString("D8")}.mapbnd.dcx");
                }

                /* Write TEST hkx binds */
                 BXF4 TEST = BXF4.Read(@"I:\SteamLibrary\steamapps\common\ELDEN RING\Game\map\m60\m60_43_36_00\h60_43_36_00.hkxbhd", @"I:\SteamLibrary\steamapps\common\ELDEN RING\Game\map\m60\m60_43_36_00\h60_43_36_00.hkxbdt");
                foreach (BinderFile file in TEST.Files)
                {
                    file.Name = file.Name.Replace("43_36", $"{tile.coordinate.x.ToString("D2")}_{tile.coordinate.y.ToString("D2")}");
                    file.Name = file.Name.Replace("4336", $"{tile.coordinate.x.ToString("D2")}{tile.coordinate.y.ToString("D2")}");
                }
                TEST.Write($"{modPath}map\\m60\\m{name}\\h{name}.hkxbhd", $"{modPath}map\\m60\\m{name}\\h{name}.hkxbdt");

                BXF4 TEST2 = BXF4.Read(@"I:\SteamLibrary\steamapps\common\ELDEN RING\Game\map\m60\m60_43_36_00\l60_43_36_00.hkxbhd", @"I:\SteamLibrary\steamapps\common\ELDEN RING\Game\map\m60\m60_43_36_00\l60_43_36_00.hkxbdt");
                foreach (BinderFile file in TEST2.Files)
                {
                    file.Name = file.Name.Replace("43_36", $"{tile.coordinate.x.ToString("D2")}_{tile.coordinate.y.ToString("D2")}");
                    file.Name = file.Name.Replace("4336", $"{tile.coordinate.x.ToString("D2")}{tile.coordinate.y.ToString("D2")}");
                }
                TEST2.Write($"{modPath}map\\m60\\m{name}\\l{name}.hkxbhd", $"{modPath}map\\m60\\m{name}\\l{name}.hkxbdt");
            }

            Console.WriteLine("## Nice! ##");

        }
    }
}
