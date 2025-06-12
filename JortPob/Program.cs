using JortPob.Common;
using JortPob.Model;
using JortPob.Worker;
using PortJob;
using SharpAssimp;
using SoulsFormats;
using SoulsFormats.KF4;
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
            // Bunch of debugging and research garbage code. Ignore */
            MSBE TESTO = MSBE.Read(@"I:\SteamLibrary\steamapps\common\ELDEN RING\Game\map\mapstudio\m60_42_36_00.msb.dcx");
            /*
            FLVER2 GUHTEST = FLVER2.Read(@"I:\SteamLibrary\steamapps\common\ELDEN RING\Game\asset\aeg\aeg001\aeg001_178-geombnd-dcx\GR\data\INTERROOT_win64\asset\aeg\AEG001\AEG001_178\sib\AEG001_178.flver");
            MATBIN BUHTEST = MATBIN.Read(@"I:\SteamLibrary\steamapps\common\ELDEN RING\Game\material\allmaterial-matbinbnd-dcx\GR\data\INTERROOT_win64\material\matbin\Map_Preset\matxml\Field_01_Grass_base.matbin");
            MATBIN GRUH = MATBIN.Read(@"I:\SteamLibrary\steamapps\common\ELDEN RING\Game\material\allmaterial-matbinbnd-dcx\GR\data\INTERROOT_win64\material\matbin\Map_m10_00\matxml\m10_00_027.matbin");

            foreach(FLVER.Vertex vert in GUHTEST.Meshes[0].Vertices)
            {
                if (vert.Colors[0].R != 1f || vert.Colors[0].A != 1f || vert.Colors[0].G != 0f || vert.Colors[0].B != 0f)
                {
                    Console.WriteLine("HI");
                }
                if (vert.UVs[2].X != 0f || vert.UVs[2].Y != 0f || vert.UVs[2].Z != 0f) {
                    Console.WriteLine("REEEEEEEEEEEEE");
                }
            }
            */

            string morrowindPath = Const.MORROWIND_PATH + @"Data Files\";
            string modPath = Const.OUTPUT_PATH;
            string cachePath = modPath + @"cache\";

            ESM esm = new ESM(morrowindPath + @"Morrowind.json");
            Cache cache = Cache.Load(esm, cachePath, morrowindPath);
            Layout layout = new(cache, esm);

            /* Generate exterior msbs from layout */
            Vector3 TEST_OFFSET1 = new(0, 200, 0); // just shifting vertical position a bit so the morrowind map isn't super far down
            Vector3 TEST_OFFSET2 = new(0, -15, 0);
            short TEST_PART_DRAW = 1001;
            int INSTANCETEST = 0;
            List<ResourcePool> msbs = new();

            foreach (BaseTile tile in layout.all)
            {
                if(tile.assets.Count <= 0 && tile.terrain.Count <= 0) { continue; }   // Skip empty tiles.
                Console.WriteLine($"Generating MSB m{tile.map} [{tile.coordinate.x},{tile.coordinate.y}] :: b{tile.block}");

                /* Generate msb from tile */
                MSBE msb = new();
                msb.Compression = SoulsFormats.DCX.Type.DCX_KRAK;

                /* TEST big flat piece of collision */  // Just need something to stand on so I can walk around and test stuff
                /*if (tile.GetType() == typeof(Tile))
                {
                    MSBE.Part.Collision collision = new();
                    collision.Name = $"h{tile.coordinate.x.ToString("D2")}{tile.coordinate.y.ToString("D2")}00_test";
                    collision.ModelName = $"h{tile.coordinate.x.ToString("D2")}{tile.coordinate.y.ToString("D2")}02";
                    collision.MapStudioLayer = 4294967295;
                    collision.Position = new Vector3(0, -5, 0) + TEST_OFFSET1;
                    collision.PlayRegionID = -1;
                    collision.LocationTextID = -1;
                    collision.InstanceID = -1;
                    collision.TileLoad.CullingHeightBehavior = -1;
                    collision.TileLoad.MapID = new byte[] { 255, 255, 255, 255 };
                    collision.TileLoad.Unk0C = -1;
                    collision.Unk1.UnkC4 = -1;
                    collision.Unk2.Condition = -1;
                    collision.Unk2.Unk26 = -1;
                    collision.UnkE0F = 1;
                    collision.UnkE3C = -1;
                    collision.UnkT01 = 255;
                    collision.UnkT02 = 255;
                    collision.UnkT04 = 64.8087158f;
                    collision.UnkT14 = -1;
                    collision.UnkT1C = -1;
                    collision.UnkT24 = -1;
                    collision.UnkT30 = -1;
                    collision.UnkT35 = 255;
                    collision.UnkT3C = -1;
                    collision.UnkT3E = -1;
                    collision.UnkT4E = -1;

                    msb.Parts.Collisions.Add(collision);
                }*/

                /* Collision index */
                int nextcol = 0;
                List<Tuple<string, CollisionInfo>> collisionIndices = new();

                /* Add terrain */
                foreach (Tuple<Vector3, TerrainInfo> tuple in tile.terrain)
                {
                    Vector3 position = tuple.Item1;
                    TerrainInfo terrainInfo = tuple.Item2;

                    if (tile.GetType() == typeof(HugeTile))
                    {
                        MSBE.Part.MapPiece map = new();
                        map.Name = $"m{terrainInfo.id.ToString("D8")}_test";
                        map.ModelName = $"m{terrainInfo.id.ToString("D8")}";
                        map.MapStudioLayer = 4294967295;

                        map.isUsePartsDrawParamID = 1;
                        map.PartsDrawParamID = TEST_PART_DRAW;

                        map.Position = position + TEST_OFFSET1 + TEST_OFFSET2;
                        msb.Parts.MapPieces.Add(map);
                    }
                    else if (tile.GetType() == typeof(Tile))
                    {
                        string collisionIndex = $"{tile.coordinate.x.ToString("D2")}{tile.coordinate.y.ToString("D2")}{nextcol++.ToString("D2")}";

                        MSBE.Part.Collision collision = new();
                        collision.Name = $"h{collisionIndex}_test";
                        collision.ModelName = $"h{collisionIndex}";
                        collision.MapStudioLayer = 4294967295;
                        collision.Position = position + TEST_OFFSET1 + TEST_OFFSET2;
                        collision.PlayRegionID = -1;
                        collision.LocationTextID = -1;
                        collision.InstanceID = -1;
                        collision.TileLoad.CullingHeightBehavior = -1;
                        collision.TileLoad.MapID = new byte[] { 255, 255, 255, 255 };
                        collision.TileLoad.Unk0C = -1;
                        collision.Unk1.UnkC4 = -1;
                        collision.Unk2.Condition = -1;
                        collision.Unk2.Unk26 = -1;
                        collision.UnkE0F = 1;
                        collision.UnkE3C = -1;
                        collision.UnkT01 = 255;
                        collision.UnkT02 = 255;
                        collision.UnkT04 = 64.8087158f;
                        collision.UnkT14 = -1;
                        collision.UnkT1C = -1;
                        collision.UnkT24 = -1;
                        collision.UnkT30 = -1;
                        collision.UnkT35 = 255;
                        collision.UnkT3C = -1;
                        collision.UnkT3E = -1;
                        collision.UnkT4E = -1;

                        msb.Parts.Collisions.Add(collision);
                        collisionIndices.Add(new Tuple<string, CollisionInfo>(collisionIndex, terrainInfo.collision));
                    }
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

                /* TEST NPCs */  // make some c0000 npcs where humanoid npcs would spawn as a test
                foreach(NpcContent npc in tile.npcs)
                {
                    MSBE.Part.Enemy enemy = (MSBE.Part.Enemy)TESTO.Parts.Enemies[0].DeepCopy();
                    enemy.Name = $"c0000_{npc.id.Replace(" ", "")}";
                    enemy.Position = new Vector3(npc.relative.X, 3, npc.relative.Z) + TEST_OFFSET1;
                    enemy.Rotation = npc.rotation;
                    enemy.InstanceID = -1;
                    enemy.EntityID = 0;

                    msb.Parts.Enemies.Add(enemy);
                }

                /* TEST Creatures */  // make some goats where enemies would spawn just as a test
                foreach (CreatureContent creature in tile.creatures)
                {
                    MSBE.Part.Enemy enemy = (MSBE.Part.Enemy)TESTO.Parts.Enemies[35].DeepCopy();
                    enemy.Name = $"c6060_{creature.id.Replace(" ", "")}";
                    enemy.Position = new Vector3(creature.relative.X, 3, creature.relative.Z) + TEST_OFFSET1;
                    enemy.Rotation = creature.rotation;
                    enemy.InstanceID = -1;
                    enemy.ModelName = "c6060";
                    enemy.WalkRouteName = "";

                    msb.Parts.Enemies.Add(enemy);
                }

                /* TEST players */  // Generic player spawn point at the center of the cell for testing purposes
                if (tile.GetType() == typeof(Tile))
                {
                    MSBE.Part.Player player_0 = new();
                    player_0.Name = "c0000_9001";
                    player_0.ModelName = "c0000";
                    player_0.InstanceID = 9001;
                    player_0.MapStudioLayer = 4294967295;
                    player_0.Unk1.DisplayGroups[0] = 16;
                    player_0.Position = TEST_OFFSET1;
                    msb.Parts.Players.Add(player_0);
                }

                /* Auto resource */
                AutoResource.Generate(tile.map, tile.coordinate.x, tile.coordinate.y, tile.block, msb);

                /* Done */
                msbs.Add(new ResourcePool(tile, msb, collisionIndices));
            }

            /* Generate interior msbs from interiorgroups */
            foreach(InteriorGroup group in layout.interiors)
            {
                if (Const.DEBUG_SKIP_INTERIOR) { break; }

                if (group.chunks.Count <= 0 && group.chunks.Count <= 0) { continue; }   // Skip empty groups.
                Console.WriteLine($"Generating MSB m{group.map} - {group.area} :: b{group.block}");

                /* Generate msb from group */
                MSBE msb = new();
                msb.Compression = SoulsFormats.DCX.Type.DCX_KRAK;

                /* TEST big flat piece of collision */  // Just need something to stand on so I can walk around and test stuff
                MSBE.Part.Collision collision = new();
                collision.Name = $"h{group.area.ToString("D2")}{group.unk.ToString("D2")}00_test";
                collision.ModelName = $"h{group.area.ToString("D2")}{group.unk.ToString("D2")}00";
                collision.MapStudioLayer = 4294967295;
                collision.Position = new Vector3(0, -65.574f, 0) + TEST_OFFSET1;
                collision.PlayRegionID = -1;
                collision.LocationTextID = -1;
                collision.InstanceID = -1;
                collision.TileLoad.CullingHeightBehavior = -1;
                collision.TileLoad.MapID = new byte[] { 255, 255, 255, 255 };
                collision.TileLoad.Unk0C = -1;
                collision.Unk1.UnkC4 = -1;
                collision.Unk2.Condition = -1;
                collision.Unk2.Unk26 = -1;
                collision.UnkE0F = 1;
                collision.UnkE3C = -1;
                collision.UnkT01 = 255;
                collision.UnkT02 = 255;
                collision.UnkT04 = 64.8087158f;
                collision.UnkT14 = -1;
                collision.UnkT1C = -1;
                collision.UnkT24 = -1;
                collision.UnkT30 = -1;
                collision.UnkT35 = 255;
                collision.UnkT3C = -1;
                collision.UnkT3E = -1;
                collision.UnkT4E = -1;

                msb.Parts.Collisions.Add(collision);

                /* Handle chunks */
                foreach (InteriorGroup.Chunk chunk in group.chunks)
                {
                    /* Add assets */
                    foreach (AssetContent content in chunk.assets)
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

                    /* TEST NPCs */  // make some c0000 npcs where humanoid npcs would spawn as a test
                    foreach (NpcContent npc in chunk.npcs)
                    {
                        MSBE.Part.Enemy enemy = (MSBE.Part.Enemy)TESTO.Parts.Enemies[0].DeepCopy();
                        enemy.Name = $"c0000_{npc.id.Replace(" ", "")}";
                        enemy.Position = new Vector3(npc.relative.X, 3, npc.relative.Z) + TEST_OFFSET1;
                        enemy.Rotation = npc.rotation;
                        enemy.InstanceID = -1;
                        enemy.EntityID = 0;

                        msb.Parts.Enemies.Add(enemy);
                    }

                    /* TEST Creatures */  // make some goats where enemies would spawn just as a test
                    foreach (CreatureContent creature in chunk.creatures)
                    {
                        MSBE.Part.Enemy enemy = (MSBE.Part.Enemy)TESTO.Parts.Enemies[35].DeepCopy();
                        enemy.Name = $"c6060_{creature.id.Replace(" ", "")}";
                        enemy.Position = new Vector3(creature.relative.X, 3, creature.relative.Z) + TEST_OFFSET1;
                        enemy.Rotation = creature.rotation;
                        enemy.InstanceID = -1;
                        enemy.ModelName = "c6060";
                        enemy.WalkRouteName = "";

                        msb.Parts.Enemies.Add(enemy);
                    }
                }

                /* TEST players */  // Generic player spawn point at the center of the cell for testing purposes
                MSBE.Part.Player player_0 = new();
                player_0.Name = "c0000_9001";
                player_0.ModelName = "c0000";
                player_0.InstanceID = 9001;
                player_0.MapStudioLayer = 4294967295;
                player_0.Unk1.DisplayGroups[0] = 16;
                player_0.Position = TEST_OFFSET1;
                msb.Parts.Players.Add(player_0);

                /* Auto resource */
                AutoResource.Generate(group.map, group.area, group.unk, group.block, msb);

                /* Done */
                msbs.Add(new ResourcePool(group, msb));
            }

            /* Bind and write all materials and textures */
            Console.WriteLine($"Binding materials...");
            Bind.BindMaterials(cachePath, $"{modPath}material\\allmaterial.matbinbnd.dcx");
            Console.WriteLine($"Binding texture...");
            Bind.BindTPF(cache, cachePath, $"{modPath}map\\m60\\common\\m60_0000");

            /* Bind all assets */    // Multithreaded because slow
            Console.WriteLine($"Binding {cache.assets.Count} assets...  t[{Const.THREAD_COUNT}]");
            Bind.BindAssets(cache, cachePath, modPath);

            /* Write msbs */    // Multithreaded because insanely slow
            Console.WriteLine($"Writing {msbs.Count} msbs...  t[{Const.THREAD_COUNT}]");
            int partition = (int)Math.Ceiling(msbs.Count / (float)Const.THREAD_COUNT);
            List<MsbWorker> workers = new();

            for (int i = 0; i < Const.THREAD_COUNT; i++)
            {
                int start = i * partition;
                int end = start + partition;
                MsbWorker worker = new(msbs, cachePath, modPath, start, end);
                workers.Add(worker);
            }

            /* Wait for threads to finish */
            while (true)
            {
                bool done = true;
                foreach (MsbWorker worker in workers)
                {
                    done &= worker.IsDone;
                }

                if (done)
                    break;
            }

            Console.WriteLine("## Nice! ##");

        }
    }

    public class ResourcePool
    {
        public int[] id;
        public List<TerrainInfo> terrain;
        public MSBE msb;
        public List<Tuple<string, CollisionInfo>> collisionIndices;

        public ResourcePool(BaseTile tile, MSBE msb, List<Tuple<string, CollisionInfo>> collisionIndices)
        {
            id = new int[]
            {
                    tile.map, tile.coordinate.x, tile.coordinate.y, tile.block
            };
            terrain = new();
            foreach (Tuple<Vector3, TerrainInfo> t in tile.terrain)
            {
                terrain.Add(t.Item2);
            }
            this.msb = msb;
            this.collisionIndices = collisionIndices;
        }

        public ResourcePool(InteriorGroup group, MSBE msb)
        {
            id = new int[]
            {
                    group.map, group.area, group.unk, group.block
            };
            terrain = new();
            this.msb = msb;
            collisionIndices = new();
        }
    }
}
