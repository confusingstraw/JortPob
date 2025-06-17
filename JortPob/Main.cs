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
using System.Linq;
using System.Net.Mime;
using System.Numerics;
using System.Reflection.Metadata;
using System.Text.Json;

namespace JortPob
{
    public class Main
    {
        public static void Convert()
        {
            /* Startup logging */
            Lort.Initialize();

            /* Loading stuff */
            ESM esm = new ESM($"{Const.MORROWIND_PATH}\\Data Files\\Morrowind.json");      // Morrowind ESM parse and partial serialization
            Cache cache = Cache.Load(esm);                                                  // Load existing cache (FAST!) or generate a new one (SLOW!)
            Layout layout = new(cache, esm);                                                 // Subdivides all content data from ESM into a more elden ring friendly format
            Paramanager param = new();                                                        // Class for managing PARAM files

            /* Generate exterior msbs from layout */
            Vector3 TEST_OFFSET1 = new(0, 200, 0); // just shifting vertical position a bit so the morrowind map isn't super far down
            Vector3 TEST_OFFSET2 = new(0, -15, 0);
            List<ResourcePool> msbs = new();

            Lort.Log($"Generating {layout.tiles.Count} exterior msbs...", Lort.Type.Main);
            Lort.NewTask("Generating MSB", layout.tiles.Count);
            foreach (BaseTile tile in layout.all)
            {
                if (tile.assets.Count <= 0 && tile.terrain.Count <= 0) { continue; }   // Skip empty tiles.

                /* Generate msb from tile */
                MSBE msb = new();
                msb.Compression = SoulsFormats.DCX.Type.DCX_KRAK;

                /* Collision index */
                int nextcol = 0;
                List<Tuple<string, CollisionInfo>> collisionIndices = new();

                /* Add terrain */
                foreach (Tuple<Vector3, TerrainInfo> tuple in tile.terrain)
                {
                    Vector3 position = tuple.Item1;
                    TerrainInfo terrainInfo = tuple.Item2;

                    /* Terrain and terrain collision */  // Render goes in hugetile for long view distance. Collision goes in tile for optimization
                    if (tile.GetType() == typeof(HugeTile))
                    {
                        /* @TODO: This system for grabbing and packing terrain parts sucks, we should rework it at some point */
                        MSBE.Part.MapPiece map = MakePart.MapPiece();
                        map.Name = $"m{terrainInfo.id.ToString("D8")}_test";
                        map.ModelName = $"m{terrainInfo.id.ToString("D8")}";
                        map.Position = position + TEST_OFFSET1 + TEST_OFFSET2;

                        msb.Parts.MapPieces.Add(map);
                    }
                    else if (tile.GetType() == typeof(Tile))
                    {
                        string collisionIndex = $"{tile.coordinate.x.ToString("D2")}{tile.coordinate.y.ToString("D2")}{nextcol++.ToString("D2")}";

                        MSBE.Part.Collision collision = MakePart.Collision();
                        collision.Name = $"h{collisionIndex}_test";
                        collision.ModelName = $"h{collisionIndex}";
                        collision.Position = position + TEST_OFFSET1 + TEST_OFFSET2;

                        msb.Parts.Collisions.Add(collision);
                        collisionIndices.Add(new Tuple<string, CollisionInfo>(collisionIndex, terrainInfo.collision));
                    }
                }

                /* Add assets */
                foreach (AssetContent content in tile.assets)
                {
                    /* Grab ModelInfo */
                    ModelInfo modelInfo = cache.GetModel(content.mesh, content.scale);

                    /* Make part */
                    MSBE.Part.Asset asset = MakePart.Asset(modelInfo);
                    asset.Position = content.relative + TEST_OFFSET1 + TEST_OFFSET2;
                    asset.Rotation = content.rotation;
                    asset.Scale = new Vector3(modelInfo.UseScale()?(content.scale*0.01f):1f);

                    /* Asset tileload config */
                    if (tile.GetType() == typeof(HugeTile) || tile.GetType() == typeof(BigTile))
                    {
                        Tile tt = tile.GetContentTrueTile(content);

                        asset.TileLoad.MapID = new byte[] { (byte)tt.block, (byte)tt.coordinate.y, (byte)tt.coordinate.x, (byte)tt.map };
                        asset.TileLoad.Unk04 = 13;
                        asset.TileLoad.CullingHeightBehavior = -1;
                    }

                    msb.Parts.Assets.Add(asset);
                }

                /* TEST NPCs */  // make some c0000 npcs where humanoid npcs would spawn as a test
                foreach (NpcContent npc in tile.npcs)
                {
                    MSBE.Part.Enemy enemy = MakePart.Npc();
                    enemy.Position = npc.relative + TEST_OFFSET1 + TEST_OFFSET2;
                    enemy.Rotation = npc.rotation;

                    msb.Parts.Enemies.Add(enemy);
                }

                /* TEST Creatures */  // make some goats where enemies would spawn just as a test
                foreach (CreatureContent creature in tile.creatures)
                {
                    MSBE.Part.Enemy enemy = MakePart.Creature();
                    enemy.Position = creature.relative + TEST_OFFSET1 + TEST_OFFSET2;
                    enemy.Rotation = creature.rotation;

                    msb.Parts.Enemies.Add(enemy);
                }

                /* TEST players */  // Generic player spawn point at the center of the cell for testing purposes
                if (tile.GetType() == typeof(Tile))
                {
                    MSBE.Part.Player player = MakePart.Player();
                    player.Position = tile.creatures.Count > 1 ? tile.creatures[0].relative : TEST_OFFSET1;
                    msb.Parts.Players.Add(player);
                }

                /* Auto resource */
                AutoResource.Generate(tile.map, tile.coordinate.x, tile.coordinate.y, tile.block, msb);

                /* Done */
                msbs.Add(new ResourcePool(tile, msb, collisionIndices));
                Lort.TaskIterate(); // Progress bar update
            }

            /* Generate interior msbs from interiorgroups */
            Lort.Log($"Generating {layout.interiors.Count} interior msbs...", Lort.Type.Main);
            Lort.NewTask("Generating MSB", layout.interiors.Count);
            foreach (InteriorGroup group in layout.interiors)
            {
                if (Const.DEBUG_SKIP_INTERIOR) { break; }

                if (group.chunks.Count <= 0 && group.chunks.Count <= 0) { continue; }   // Skip empty groups.

                /* Generate msb from group */
                MSBE msb = new();
                msb.Compression = SoulsFormats.DCX.Type.DCX_KRAK;

                /* Handle chunks */
                foreach (InteriorGroup.Chunk chunk in group.chunks)
                {
                    /* Add assets */
                    foreach (AssetContent content in chunk.assets)
                    {
                        // Grab da thing
                        ModelInfo modelInfo = cache.GetModel(content.mesh, content.scale);

                        // Make da thing
                        MSBE.Part.Asset asset = new();
                        asset.Name = $"{modelInfo.AssetName().ToUpper()}_test";
                        asset.ModelName = modelInfo.AssetName().ToUpper();
                        asset.MapStudioLayer = 4294967295;

                        asset.Unk1.DisplayGroups[0] = 16;

                        //asset.InstanceID = INSTANCETEST++;

                        asset.Position = content.relative + TEST_OFFSET1 + TEST_OFFSET2;
                        asset.Rotation = content.rotation;
                        asset.Scale = new Vector3(modelInfo.IsDynamic() ? (content.scale * 0.01f) : 1f);
                        msb.Parts.Assets.Add(asset);
                    }

                    /* TEST NPCs */  // make some c0000 npcs where humanoid npcs would spawn as a test
                    foreach (NpcContent npc in chunk.npcs)
                    {
                        MSBE.Part.Enemy enemy = MakePart.Npc();
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
                        MSBE.Part.Enemy enemy = MakePart.Creature();
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
                MSBE.Part.Player player = MakePart.Player();
                player.Position = TEST_OFFSET1;

                msb.Parts.Players.Add(player);

                /* Auto resource */
                AutoResource.Generate(group.map, group.area, group.unk, group.block, msb);

                /* Done */
                msbs.Add(new ResourcePool(group, msb));
                Lort.TaskIterate(); // Progress bar update
            }

            /* Generate some params and write to file */
            Lort.Log($"Creating PARAMs...", Lort.Type.Main);
            param.GenerateAssetRows(cache.assets);
            param.Write();

            /* Bind and write all materials and textures */
            Lort.Log($"Binding materials...", Lort.Type.Main);
            Lort.NewTask("Binding Materials", 1);
            Bind.BindMaterials($"{Const.OUTPUT_PATH}material\\allmaterial.matbinbnd.dcx");
            Lort.Log($"Binding textures...", Lort.Type.Main);
            Lort.NewTask("Binding Textures", 1);
            Bind.BindTPF(cache, $"{Const.OUTPUT_PATH}map\\m60\\common\\m60_0000");

            /* Bind all assets */    // Multithreaded because slow
            Lort.Log($"Binding {cache.assets.Count} assets...", Lort.Type.Main);
            Lort.NewTask("Binding Assets", cache.assets.Count);
            Bind.BindAssets(cache);

            /* Write msbs */
            MsbWorker.Go(msbs);

            /* Debug print thing */
            if(Const.DEBUG_PRINT_LOCATION_INFO != null)
            {
                Cell cell = esm.GetCellByName(Const.DEBUG_PRINT_LOCATION_INFO);
                foreach(Tile tile in layout.tiles)
                {
                    if(tile.PositionInside(cell.center))
                    {
                        Lort.Log($" ## DEBUG ## Found cell '{cell.name}' in m{tile.map}_{tile.coordinate.x}_{tile.coordinate.y}_{tile.block}", Lort.Type.Debug);
                        break;
                    }
                }
            }

            /* Donezo */
            Lort.Log("Done!", Lort.Type.Main);
            Lort.NewTask("Done!", 1);
            Lort.TaskIterate();
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
