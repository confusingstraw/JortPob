using HKLib.hk2018;
using HKX2;
using JortPob.Common;
using JortPob.Model;
using JortPob.Worker;
using SharpAssimp;
using SoulsFormats;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using static HKLib.hk2018.hkaSkeleton;

namespace JortPob
{
    public class Cache
    {
        public List<TerrainInfo> terrains;
        public List<ModelInfo> maps;        // Map pieces
        public List<ModelInfo> assets;
        public List<EmitterInfo> emitters;
        public List<ObjectInfo> objects;
        public List<LiquidInfo> liquids;
        public List<CutoutInfo> cutouts; // defines collision planes for swamps/lava

        public Cache()
        {
            maps = new();     /// @TODO: deelte deprecated
            assets = new();
            emitters = new();
            objects = new();
            terrains = new();
            liquids = new();
            cutouts = new();
        }

        /* Get a terrain by coordinate */
        public TerrainInfo GetTerrain(Int2 coordinate)
        {
            foreach(TerrainInfo terrain in terrains)
            {
                if(terrain.coordinate == coordinate)
                {
                    return terrain;
                }
            }
            return null;
        }

        /* Get a cutout by coordinate */
        public CutoutInfo GetCutout(Int2 coordinate)
        {
            foreach(CutoutInfo cutout in cutouts)
            {
                if(cutout.coordinate == coordinate)
                {
                    return cutout;
                }
            }
            return null;
        }

        /* Get a modelinfo by the nif name and scale */
        public ModelInfo GetModel(string name)
        {
            return GetModel(name, 100);
        }

        public ModelInfo GetModel(string name, int scale)
        {
            /* If the model doesn't have collision it's static scaleable so we return scale 100 as that's the only version of it */
            if(!ModelHasCollision(name))
            {
                foreach (ModelInfo model in assets)
                {
                    if (model.name == name) { return model; }
                }
                return null;
            }

            /* Otherwise... */
            /* First look for one with a matched scale */
            foreach(ModelInfo model in assets)
            {
                if(model.name == name && model.scale == scale) { return model; }
            }

            /* If not found then we look a dynamic asset */
            foreach (ModelInfo model in assets)
            {
                if (model.name == name && model.scale == Const.DYNAMIC_ASSET) { return model; }
            }

            /* Oh dear.. return null I guess! */
            return null;
        }

        public EmitterInfo GetEmitter(string record)
        {
            foreach(EmitterInfo emitter in emitters)
            {
                if(emitter.record == record)
                {
                    return emitter;
                }
            }

            return null;
        }

        public LiquidInfo GetWater()
        {
            return liquids[0];
        }

        public LiquidInfo GetSwamp()
        {
            return liquids[1];
        }

        public LiquidInfo GetLava()
        {
            return liquids[2];
        }

        public bool ModelHasCollision(string name)
        {
            foreach(ModelInfo model in assets)
            {
                if (model.name == name) { return model.HasCollision(); }
            }
            return false;
        }

        /* Big stupid load function */
        public static Cache Load(ESM esm)
        {
            string manifestPath = Const.CACHE_PATH + @"cache.json";

            /* Cache Exists ? */
            if (File.Exists(manifestPath))
            {
                Lort.Log($"Using cache: {manifestPath}", Lort.Type.Main);
                Lort.Log($"Delete this file if you want to regenerate models/textures/collision and cache!", Lort.Type.Main);
            }
            /* Generate new cache! */
            else
            {
                /* Grab all the models we want to convert */
                List<PreModel> meshes = new();
                PreModel GetMesh(Content content)
                {
                    foreach (PreModel model in meshes)
                    {
                        if(model.mesh == content.mesh)
                        {
                            if (content.type == ESM.Type.Static) { model.forceCollision = true; }
                            return model;
                        }
                    }
                    PreModel m = new(content.mesh, content.type == ESM.Type.Static);
                    meshes.Add(m);
                    return m;
                }

                void ScoopEmUp(List<Cell> cells)
                {
                    foreach (Cell cell in cells)
                    {
                        void Scoop(List<Content> contents)
                        {
                            foreach (Content content in contents)
                            {
                                if(content.mesh == null) { continue; }  // skip content with no mesh
                                LiquidManager.AddCutout(content); // check if this is a lava or swamp mesh and add it to cutouts if it is
                                PreModel model = GetMesh(content);
                                int i = model.scales.ContainsKey(content.scale)? model.scales[content.scale]:0;
                                model.scales.Remove(content.scale);
                                model.scales.Add(content.scale, ++i);  // ?? i guess this works. dumbass solution though tbh
                            }
                        }
                        Scoop(cell.contents);
                    }
                }
                ScoopEmUp(esm.exterior);
                if (!Const.DEBUG_SKIP_INTERIOR) { ScoopEmUp(esm.interior); }

                Lort.Log($"Generating new cache...", Lort.Type.Main);

                Cache nu = new();

                AssimpContext assimpContext = new();
                MaterialContext materialContext = new();

                /* Convert models/textures for terrain */
                nu.terrains = LandscapeWorker.Go(materialContext, esm);

                /* Generate stuff for cutouts */
                nu.cutouts = LiquidManager.GenerateCutouts(esm);

                /* Convert models/textures for models */
                nu.assets = FlverWorker.Go(materialContext, meshes);

                /* Generate stuff for water */
                nu.liquids = LiquidManager.GenerateLiquids(esm, materialContext);

                /* Write textures */
                Lort.Log($"Writing matbins & tpfs...", Lort.Type.Main);
                materialContext.WriteAll();
                assimpContext.Dispose();

                /* Garbage collect after writing material data to file */
                materialContext = null;
                assimpContext = null;
                GC.Collect();

                /* Create emitterinfos */
                foreach(JsonNode json in esm.records[ESM.Type.Light])
                {
                    if (json["mesh"] == null || json["mesh"].ToString().Trim() == "") { continue; }

                    EmitterInfo emitterInfo = new();
                    emitterInfo.record = json["id"].ToString();
                    string mesh = json["mesh"].ToString().ToLower();
                    emitterInfo.model = nu.GetModel(mesh);

                    if(emitterInfo.model == null) { continue; } // discard if we don't find a model for this. should only happen when debug stuff is enabled for cell building

                    int r = int.Parse(json["data"]["color"][0].ToString());
                    int g = int.Parse(json["data"]["color"][1].ToString());
                    int b = int.Parse(json["data"]["color"][2].ToString());
                    int a = int.Parse(json["data"]["color"][3].ToString());
                    emitterInfo.color = new byte[] { (byte)r, (byte)g, (byte)b, (byte)a };  // 0 -> 255 colors

                    emitterInfo.radius = float.Parse(json["data"]["radius"].ToString()) * Const.GLOBAL_SCALE;
                    emitterInfo.weight = float.Parse(json["data"]["weight"].ToString());

                    emitterInfo.value = int.Parse(json["data"]["value"].ToString());
                    emitterInfo.time = int.Parse(json["data"]["time"].ToString());

                    string flags = json["data"]["flags"].ToString();

                    emitterInfo.dynamic = flags.Contains("DYNAMIC");
                    emitterInfo.fire = flags.Contains("FIRE");
                    emitterInfo.negative = flags.Contains("NEGATIVE");
                    emitterInfo.defaultOff = flags.Contains("OFF_BY_DEFAULT");

                    if (flags.Contains("FLICKER_SLOW")) { emitterInfo.mode = LightContent.Mode.FlickerSlow; }
                    else if (flags.Contains("FLICKER")) { emitterInfo.mode = LightContent.Mode.Flicker; }
                    else if (flags.Contains("PULSE_SLOW")) { emitterInfo.mode = LightContent.Mode.PulseSlow; }
                    else if (flags.Contains("PULSE")) { emitterInfo.mode = LightContent.Mode.Pulse; }
                    else { emitterInfo.mode = LightContent.Mode.Default; }

                    nu.emitters.Add(emitterInfo);
                }

                /* Convert collision */
                List<CollisionInfo> collisions = new();
                foreach (ModelInfo modelInfo in nu.assets)
                {
                    if (modelInfo.collision == null) { continue; }
                    collisions.Add(modelInfo.collision);
                }
                foreach (TerrainInfo terrain in nu.terrains)
                {
                    foreach(CollisionInfo collision in terrain.collision)
                    {
                        collisions.Add(collision);
                    }
                }
                foreach(LiquidInfo water in nu.liquids)
                {
                    collisions.AddRange(water.GetCollision());
                }
                foreach (CutoutInfo cutout in nu.cutouts)
                {
                    collisions.Add(cutout.collision);
                }
                HkxWorker.Go(collisions);

                /* Assign resource ID numbers */
                int nextM = 0, nextA = 0, nextO = 5000;
                foreach (TerrainInfo terrainInfo in nu.terrains)
                {
                    terrainInfo.id = nextM++;
                }
                foreach (ModelInfo modelInfo in nu.maps)
                {
                    modelInfo.id = nextM++;
                }
                foreach (ModelInfo modelInfo in nu.assets)
                {
                    modelInfo.id = nextA++;
                }
                foreach(EmitterInfo emitterInfo in nu.emitters)
                {
                    emitterInfo.id = nextA++;
                }
                foreach (ObjectInfo objectInfo in nu.objects)
                {
                    objectInfo.id = nextO++;
                }

                /* Write new cache file */
                string jsonOutput = JsonSerializer.Serialize<Cache>(nu, new JsonSerializerOptions { IncludeFields = true });
                File.WriteAllText(manifestPath, jsonOutput);
                Lort.Log($"Generated new cache: {Const.CACHE_PATH}", Lort.Type.Main);
            }

            /* Load cache manifest */
            string tempRawJson = File.ReadAllText(manifestPath);
            Cache cache = JsonSerializer.Deserialize<Cache>(tempRawJson, new JsonSerializerOptions { IncludeFields = true });
            return cache;
        }
    }

    public class TerrainInfo
    {
        public readonly Int2 coordinate;   // Location in world cell grid
        public readonly string path;
        public List<CollisionInfo> collision;
        public List<TextureInfo> textures; // All generated tpf files
        public bool hasWater, hasLava, hasSwamp; // caching these flags so we dont have to load the landscape to find out

        public int id;
        public TerrainInfo(Int2 coordinate, string path)
        {
            this.coordinate = coordinate;
            this.path = path;
            textures = new();
            collision = new();

            id = -1;

            hasWater = false;
            hasLava = false;
            hasSwamp = false;
        }
    }

    public class ObjectInfo
    {
        public string name; // Original esm ref id
        public ModelInfo model;

        public int id;
        public ObjectInfo(string name, ModelInfo model)
        {
            this.name = name.ToLower();
            this.model = model;
        }
    }

    public class EmitterInfo
    {
        public string record; // record ID
        public ModelInfo model;

        public float radius, weight;
        public int time, value;
        public byte[] color;

        public bool dynamic, fire, negative, defaultOff;
        public LightContent.Mode mode;

        public int id; // asset id

        public EmitterInfo()
        {

        }

        public string AssetPath()
        {
            int v1 = Const.ASSET_GROUP + (int)(id / 1000);
            int v2 = id % 1000;
            return $"aeg{v1.ToString("D3")}\\aeg{v1.ToString("D3")}_{v2.ToString("D3")}";
        }

        public string AssetName()
        {
            int v1 = Const.ASSET_GROUP + (int)(id / 1000);
            int v2 = id % 1000;
            return $"aeg{v1.ToString("D3")}_{v2.ToString("D3")}";
        }

        public int AssetRow()
        {
            int v1 = Const.ASSET_GROUP + (int)(id / 1000);
            int v2 = id % 1000;
            return int.Parse($"{v1.ToString("D3")}{v2.ToString("D3")}");  // yes i know this the wrong way to do this but guh
        }

        public bool HasEmitter()
        {
            foreach (KeyValuePair<string, short> kvp in model.dummies)
            {
                if (kvp.Key.Contains("emitter")) { return true; }
            }
            return false;
        }

        public short GetAttachLight()
        {
            foreach (KeyValuePair<string, short> kvp in model.dummies)
            {
                if (kvp.Key.Contains("attachlight")) { return kvp.Value; }
            }
            return -1; // if no attachlight node we return -1
        }
    }

    public class ModelInfo
    {
        public string name; // Original nif name, for lookup from ESM records
        public readonly string path; // Relative path from the 'cache' folder to the converted flver file
        public CollisionInfo collision; // Generated HKX file or null if no collision exists
        public List<TextureInfo> textures; // All generated tpf files

        public Dictionary<string, short> dummies; // Dummies and their ids

        public int id;  // Model ID number, the last 6 digits in a model filename. EXAMPLE: m30_00_00_00_005521.mapbnd.dcx
        public int scale;  // clamped to an int. 1 = 1%. 100 is 1f. int.MAX_VALUE means it's dynamic

        public float size; // Bounding radius, for use in distant LOD generation

        public ModelInfo(string name, string path, int scale)
        {
            this.name = name.ToLower();
            this.path = path;
            textures = new();
            dummies = new();

            id = -1;
            this.scale = scale;
            size = -1f;
        }

        public string AssetPath()
        {
            int v1 = Const.ASSET_GROUP + (int)(id / 1000);
            int v2 = id % 1000;
            return $"aeg{v1.ToString("D3")}\\aeg{v1.ToString("D3")}_{v2.ToString("D3")}";
        }

        public string AssetName()
        {
            int v1 = Const.ASSET_GROUP + (int)(id / 1000);
            int v2 = id % 1000;
            return $"aeg{v1.ToString("D3")}_{v2.ToString("D3")}";
        }

        public int AssetRow()
        {
            int v1 = Const.ASSET_GROUP + (int)(id / 1000);
            int v2 = id % 1000;
            return int.Parse($"{v1.ToString("D3")}{v2.ToString("D3")}");  // yes i know this the wrong way to do this but guh
        }

        public bool HasCollision()
        {
            return collision != null;
        }

        public bool IsDynamic()
        {
            return scale == Const.DYNAMIC_ASSET;
        }

        public bool UseScale()
        {
            return !HasCollision() || IsDynamic();
        }
    }

    /* contains info on type of water and it's files and filepaths */
    public class LiquidInfo
    {
        public int id;
        public string path;
        public List<Tuple<Int2, CollisionInfo>> collision;   // Not true collision, just used for the water plane to have splashy splashers when you splash through it

        public LiquidInfo(int id, string path)
        {
            this.id = id;
            this.path = path;
            collision = new();
        }

        public string AssetPath()
        {
            int v1 = Const.WATER_ASSET_GROUP + (int)(id / 1000);
            int v2 = id % 1000;
            return $"aeg{v1.ToString("D3")}\\aeg{v1.ToString("D3")}_{v2.ToString("D3")}";
        }

        public string AssetName()
        {
            int v1 = Const.WATER_ASSET_GROUP + (int)(id / 1000);
            int v2 = id % 1000;
            return $"aeg{v1.ToString("D3")}_{v2.ToString("D3")}";
        }

        public int AssetRow()
        {
            int v1 = Const.WATER_ASSET_GROUP + (int)(id / 1000);
            int v2 = id % 1000;
            return int.Parse($"{v1.ToString("D3")}{v2.ToString("D3")}");  // yes i know this the wrong way to do this but guh
        }

        public void AddCollision(Int2 coordinate, CollisionInfo collisionInfo)
        {
            collision.Add(new(coordinate, collisionInfo));
        }

        public List<CollisionInfo> GetCollision()
        {
            List<CollisionInfo> ret = new();
            foreach(Tuple<Int2, CollisionInfo> tuple in collision)
            {
                ret.Add(tuple.Item2);
            }
            return ret;
        }

        public CollisionInfo GetCollision(Int2 coordinate)
        {
            foreach (Tuple<Int2, CollisionInfo> tuple in collision)
            {
                if (tuple.Item1 == coordinate)
                {
                    return tuple.Item2;
                }
            }
            return GetCollision(new Int2(0, 0)); // we use 0,0 as the default no cutout water plane collision. if this is missing it will stack overflow and crash. should never happen but lol
        }
    }

    public class CutoutInfo
    {
        public readonly Int2 coordinate;
        public CollisionInfo collision;

        public CutoutInfo(Int2 coordinate, CollisionInfo collision)
        {
            this.coordinate = coordinate;
            this.collision = collision;
        }
    }

    public class CollisionInfo
    {
        public string name; // Original nif name, for lookup from ESM records
        public string obj;  // Relative path from the 'cache' folder to the converted obj file
        public string hkx; // Relative path from the 'cache' folder to the converted hkx file

        public CollisionInfo(string name, string obj)
        {
            this.name = name.ToLower();
            this.obj = obj;
            this.hkx = obj.Replace(".obj", ".hkx");
        }
    }

    public class TextureInfo
    {
        public readonly string name; // Original dds texture name for lookup
        public readonly string path; // Relative path from the 'cache' folder to the converted tpf file
        public readonly string low;  // same as above but points to low detail texture
        public TextureInfo(string name, string path)
        {
            this.name = name.ToLower();
            this.path = path;
            this.low = path.Replace(".tpf.dcx", "_l.tpf.dcx");
        }
    }

    // little class i'm using for preprocessing scale info on meshes that will become assets.
    // for assets that have a lot of scaled versions placed down we make baked scaled versions
    // for 1 off scales we use dynamic assets instead
    public class PreModel
    {
        public string mesh;
        public Dictionary<int, int> scales;
        public bool forceCollision;           // some morrowind nifs dont have collision meshes despite needing them. in SOME cases we use the visual mesh as collision

        public PreModel(string mesh, bool forceCollision)
        {
            this.mesh = mesh.Trim().ToLower();
            scales = new();
            this.forceCollision = forceCollision;
        }
    }
}
