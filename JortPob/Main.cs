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
            Vector3 forward = Vector3.UnitY;
            Vector3 left = Vector3.UnitX;

            Vector3 a = new Vector3(1, 0, 0);
            Vector3 b = new Vector3(-1, 0, 0);
            Vector3 c = new Vector3(0, 1, 0);
            Vector3 d = new Vector3(0, -1, 0);

            double val1 = (Math.Acos(Vector3.Dot(a, left)) > Math.PI / 2 ? -1 : 1) * Math.Acos(Vector3.Dot(a, forward));
            double val2 = (Math.Acos(Vector3.Dot(b, left)) > Math.PI / 2 ? -1 : 1) * Math.Acos(Vector3.Dot(b, forward));
            double val3 = (Math.Acos(Vector3.Dot(c, left)) > Math.PI / 2 ? -1 : 1) * Math.Acos(Vector3.Dot(c, forward));
            double val4 = (Math.Acos(Vector3.Dot(d, left)) > Math.PI / 2 ? -1 : 1) * Math.Acos(Vector3.Dot(d, forward));

            /* Startup logging */
            Lort.Initialize();

            /* Loading stuff */
            ESM esm = new ESM($"{Const.MORROWIND_PATH}\\Data Files\\Morrowind.json");      // Morrowind ESM parse and partial serialization
            Cache cache = Cache.Load(esm);                                                  // Load existing cache (FAST!) or generate a new one (SLOW!)
            Layout layout = new(cache, esm);                                                 // Subdivides all content data from ESM into a more elden ring friendly format
            Paramanager param = new();                                                        // Class for managing PARAM files

            /* Generate exterior msbs from layout */
            List<ResourcePool> msbs = new();

            Lort.Log($"Generating {layout.tiles.Count} exterior msbs...", Lort.Type.Main);
            Lort.NewTask("Generating MSB", layout.tiles.Count);
            foreach (BaseTile tile in layout.all)
            {
                if (tile.assets.Count <= 0 && tile.terrain.Count <= 0) { continue; }   // Skip empty tiles.

                /* Generate msb from tile */
                MSBE msb = new();
                LightManager lightManager = new(tile.map, tile.coordinate, tile.block);
                ResourcePool pool = new(tile, msb, lightManager);
                msb.Compression = SoulsFormats.DCX.Type.DCX_KRAK;

                /* Collision Indices */
                int nextC = 0;

                /* Add terrain */
                foreach (Tuple<Vector3, TerrainInfo> tuple in tile.terrain)
                {
                    Vector3 position = tuple.Item1;
                    TerrainInfo terrainInfo = tuple.Item2;

                    /* Terrain and terrain collision */  // Render goes in superoverworld for long view distance. Collision goes in tile for optimization
                    // superoverowrld msb is  handled by its own class -> OverworldManager
                    if (tile.GetType() == typeof(Tile))
                    {
                        foreach (CollisionInfo collisionInfo in terrainInfo.collision)
                        {
                            string collisionIndex = $"{tile.coordinate.x.ToString("D2")}{tile.coordinate.y.ToString("D2")}{nextC++.ToString("D2")}";

                            MSBE.Part.Collision collision = MakePart.Collision();
                            collision.Name = $"h{collisionIndex}_0000";
                            collision.ModelName = $"h{collisionIndex}";
                            collision.Position = position + Const.TEST_OFFSET1 + Const.TEST_OFFSET2;

                            msb.Parts.Collisions.Add(collision);
                            pool.collisionIndices.Add(new Tuple<string, CollisionInfo>(collisionIndex, collisionInfo));
                        }

                        /* Add water collision if terrain 'hasWater' */
                        /* Water is done on a cell by cell basis so I am simply tieing it to the terrain data here */
                        if (terrainInfo.hasWater)
                        {
                            WaterInfo waterInfo = cache.GetWater();
                            CollisionInfo waterCollisionInfo = waterInfo.GetCollision(terrainInfo.coordinate);

                            /* Make collision for water splashing */
                            string collisionIndex = $"{tile.coordinate.x.ToString("D2")}{tile.coordinate.y.ToString("D2")}{nextC++.ToString("D2")}";
                            MSBE.Part.Collision collision = MakePart.WaterCollision();
                            collision.Name = $"h{collisionIndex}_0000";
                            collision.ModelName = $"h{collisionIndex}";
                            collision.Position = position + Const.TEST_OFFSET1 + Const.TEST_OFFSET2;

                            msb.Parts.Collisions.Add(collision);
                            pool.collisionIndices.Add(new Tuple<string, CollisionInfo>(collisionIndex, waterCollisionInfo));
                        }
                        
                        /* Add collision for cutouts. */
                        if(terrainInfo.hasSwamp || terrainInfo.hasLava) // minor hack, no cell has both swamp and lava so we don't actually differentiate here
                        {
                            CutoutInfo cutoutInfo = cache.GetCutout(terrainInfo.coordinate);
                            if(cutoutInfo != null)
                            {
                                /* Make collision for swamp or lava splashy splashing */
                                string collisionIndex = $"{tile.coordinate.x.ToString("D2")}{tile.coordinate.y.ToString("D2")}{nextC++.ToString("D2")}";
                                MSBE.Part.Collision collision = MakePart.WaterCollision(); // also works for lava and poison
                                collision.Name = $"h{collisionIndex}_0000";
                                collision.ModelName = $"h{collisionIndex}";
                                collision.Position = position + Const.TEST_OFFSET1 + Const.TEST_OFFSET2;

                                msb.Parts.Collisions.Add(collision);
                                pool.collisionIndices.Add(new Tuple<string, CollisionInfo>(collisionIndex, cutoutInfo.collision));
                            }
                        }
                    }
                }

                /* Add assets */
                foreach (AssetContent content in tile.assets)
                {
                    /* Grab ModelInfo */
                    ModelInfo modelInfo = cache.GetModel(content.mesh, content.scale);

                    /* Make part */
                    MSBE.Part.Asset asset = MakePart.Asset(modelInfo);
                    asset.Position = content.relative + Const.TEST_OFFSET1 + Const.TEST_OFFSET2;
                    asset.Rotation = content.rotation;
                    asset.Scale = new Vector3(modelInfo.UseScale()?(content.scale*0.01f):1f);

                    /* Asset tileload config */
                    if (tile.GetType() == typeof(HugeTile) || tile.GetType() == typeof(BigTile))
                    {
                        asset.TileLoad.MapID = new byte[] { (byte)0, (byte)content.load.y, (byte)content.load.x, (byte)tile.map };
                        asset.TileLoad.Unk04 = 13;
                        asset.TileLoad.CullingHeightBehavior = -1;
                    }

                    msb.Parts.Assets.Add(asset);
                }

                /* Add lights */
                foreach(LightContent light in tile.lights)
                {
                    lightManager.CreateLight(light);
                }

                /* TEST NPCs */  // make some c0000 npcs where humanoid npcs would spawn as a test
                foreach (NpcContent npc in tile.npcs)
                {
                    MSBE.Part.Enemy enemy = MakePart.Npc();
                    enemy.Position = npc.relative + Const.TEST_OFFSET1 + Const.TEST_OFFSET2;
                    enemy.Rotation = npc.rotation;

                    msb.Parts.Enemies.Add(enemy);
                }

                /* TEST Creatures */  // make some goats where enemies would spawn just as a test
                foreach (CreatureContent creature in tile.creatures)
                {
                    MSBE.Part.Enemy enemy = MakePart.Creature();
                    enemy.Position = creature.relative + Const.TEST_OFFSET1 + Const.TEST_OFFSET2;
                    enemy.Rotation = creature.rotation;

                    msb.Parts.Enemies.Add(enemy);
                }

                /* TEST players */  // Generic player spawn point at the center of the cell for testing purposes
                if (tile.GetType() == typeof(Tile))
                {
                    MSBE.Part.Player player = MakePart.Player();
                    player.Position = tile.npcs.Count > 1 ? tile.npcs[0].relative : Const.TEST_OFFSET1;
                    msb.Parts.Players.Add(player);
                }

                /* Auto resource */
                AutoResource.Generate(tile.map, tile.coordinate.x, tile.coordinate.y, tile.block, msb);

                /* Done */
                msbs.Add(pool);
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
                LightManager lightManager = new(group.map, group.area, group.unk, group.block);
                ResourcePool pool = new(group, msb, lightManager);
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

                        asset.Position = content.relative + Const.TEST_OFFSET1 + Const.TEST_OFFSET2;
                        asset.Rotation = content.rotation;
                        asset.Scale = new Vector3(modelInfo.IsDynamic() ? (content.scale * 0.01f) : 1f);
                        msb.Parts.Assets.Add(asset);
                    }

                    /* Add lights */
                    foreach (LightContent light in chunk.lights)
                    {
                        lightManager.CreateLight(light);
                    }

                    /* TEST NPCs */  // make some c0000 npcs where humanoid npcs would spawn as a test
                    foreach (NpcContent npc in chunk.npcs)
                    {
                        MSBE.Part.Enemy enemy = MakePart.Npc();
                        enemy.Name = $"c0000_{npc.id.Replace(" ", "")}";
                        enemy.Position = new Vector3(npc.relative.X, 3, npc.relative.Z) + Const.TEST_OFFSET1;
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
                        enemy.Position = new Vector3(creature.relative.X, 3, creature.relative.Z) + Const.TEST_OFFSET1;
                        enemy.Rotation = creature.rotation;
                        enemy.InstanceID = -1;
                        enemy.ModelName = "c6060";
                        enemy.WalkRouteName = "";

                        msb.Parts.Enemies.Add(enemy);
                    }
                }

                /* TEST players */  // Generic player spawn point at the center of the cell for testing purposes
                MSBE.Part.Player player = MakePart.Player();
                player.Position = Const.TEST_OFFSET1;

                msb.Parts.Players.Add(player);

                /* Auto resource */
                AutoResource.Generate(group.map, group.area, group.unk, group.block, msb);

                /* Done */
                msbs.Add(pool);
                Lort.TaskIterate(); // Progress bar update
            }

            /* Generate some params and write to file */
            Lort.Log($"Creating PARAMs...", Lort.Type.Main);
            param.GeneratePartDrawParams();
            param.GenerateAssetRows(cache.assets);
            param.GenerateAssetRows(cache.waters);
            param.KillMapHeightParams();    // murder kill
            param.Write();

            /* Bind and write all materials and textures */
            Bind.BindMaterials($"{Const.OUTPUT_PATH}material\\allmaterial.matbinbnd.dcx");
            Bind.BindTPF(cache, $"{Const.OUTPUT_PATH}map\\m60\\common\\m60_0000");

            /* Bind all assets */    // Multithreaded because slow
            Lort.Log($"Binding {cache.assets.Count} assets...", Lort.Type.Main);
            Lort.NewTask("Binding Assets", cache.assets.Count);
            Bind.BindAssets(cache);
            foreach(WaterInfo water in cache.waters)  // bind up them waters toooooo
            {
                Bind.BindAsset(water, $"{Const.OUTPUT_PATH}asset\\aeg\\{water.AssetPath()}.geombnd.dcx");
            }

            /* Generate overworld */
            ResourcePool overworld = OverworldManager.Generate(cache, esm, param);
            msbs.Insert(0, overworld); // this one takes the longest so we put it first so that the thread working on it has plenty of time to finish

            /* Debug print thing */
            if (Const.DEBUG_PRINT_LOCATION_INFO != null)
            {
                Cell cell = esm.GetCellByName(Const.DEBUG_PRINT_LOCATION_INFO);
                foreach (Tile tile in layout.tiles)
                {
                    if (tile.PositionInside(cell.center))
                    {
                        Lort.Log($" ## DEBUG ## Found cell '{cell.name}' in m{tile.map}_{tile.coordinate.x}_{tile.coordinate.y}_{tile.block}", Lort.Type.Debug);
                        break;
                    }
                }
            }

            /* Write msbs */
            esm = null;  // free some memory here
            param = null;
            GC.Collect();
            MsbWorker.Go(msbs);

            /* Donezo */
            Lort.Log("Done!", Lort.Type.Main);
            Lort.NewTask("Done!", 1);
            Lort.TaskIterate();
        }
    }

    public class ResourcePool
    {
        public int[] id;
        public List<Tuple<int, string>> mapIndices;
        public MSBE msb;
        public LightManager lights;
        public List<Tuple<string, CollisionInfo>> collisionIndices;

        /* Exterior cells */
        public ResourcePool(BaseTile tile, MSBE msb, LightManager lights)
        {
            id = new int[]
            {
                    tile.map, tile.coordinate.x, tile.coordinate.y, tile.block
            };
            mapIndices = new();
            collisionIndices = new();
            this.msb = msb;
            this.lights = lights;
        }

        /* Interior cells */
        public ResourcePool(InteriorGroup group, MSBE msb, LightManager lights)
        {
            id = new int[]
            {
                    group.map, group.area, group.unk, group.block
            };
            mapIndices = new();
            this.msb = msb;
            this.lights = lights;
            collisionIndices = new();
        }

        /* Super overworld */
        public ResourcePool(MSBE msb, LightManager lights)
        {
            id = new int[]
            {
                    60, 00, 00, 99
            };
            mapIndices = new();
            this.msb = msb;
            this.lights = lights;
            collisionIndices = new();
        }

        public void Add(TerrainInfo terrain)
        {
            mapIndices.Add(new Tuple<int, string>(terrain.id, terrain.path));
        }

        public void Add(string index, CollisionInfo collision)
        {
            collisionIndices.Add(new Tuple<string, CollisionInfo>(index, collision));
        }
    }
}
