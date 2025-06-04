using JortPob.Common;
using JortPob.Model;
using SharpAssimp;
using SoulsFormats;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JortPob
{
    public class Cache
    {
        public List<TerrainInfo> terrains;
        public List<ModelInfo> maps;        // Map pieces
        public List<ModelInfo> assets;
        public List<ObjectInfo> objects;

        public Cache()
        {
            maps = new();
            assets = new();
            objects = new();
            terrains = new();
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

        /* Get a modelinfo by the nif name */
        public ModelInfo GetModel(string name)
        {
            foreach(ModelInfo model in assets)
            {
                if(model.name == name) { return model; }
            }

            foreach (ModelInfo model in maps)
            {
                if (model.name == name) { return model; }
            }
            return null;
        }

        /* Big stupid load function */
        public static Cache Load(ESM esm, string cachePath, string morrowindPath)
        {
            string manifestPath = cachePath + @"cache.json";

            /* Cache Exists ? */
            if (File.Exists(manifestPath))
            {
                Console.WriteLine($"Using cache: {manifestPath}");
                Console.WriteLine($"Delete this file if you want to regenerate models/textures/collision and cache!");
            }
            else
            {
                /* Generate new cache! */
                string meshPath = cachePath + @"meshes\";
                string terrainPath = cachePath + @"terrain\";
                string texturePath = cachePath + @"textures\";
                string materialPath = cachePath + @"materials\";

                /* Grab all the models we want to convert */
                List<string> meshes = new();
                void ScoopEmUp(List<Cell> cells)
                {
                    foreach (Cell cell in cells)
                    {
                        foreach (AssetContent asset in cell.assets)
                        {
                            if (!meshes.Contains(asset.mesh)) { meshes.Add(asset.mesh); }
                        }
                        foreach (EmitterContent emitter in cell.emitters)
                        {
                            if (!meshes.Contains(emitter.mesh)) { meshes.Add(emitter.mesh); }
                        }
                    }
                }
                ScoopEmUp(esm.exterior);
                ScoopEmUp(esm.interior);

                Console.WriteLine($"Generating new cache... m[{meshes.Count}]");

                Cache nu = new();

                AssimpContext assimpContext = new();
                MaterialContext materialContext = new(cachePath);

                bool ModelExists(string name)
                {
                    foreach (ModelInfo mod in nu.assets)
                    {
                        if (name.ToLower() == mod.name) { return true; }
                    }
                    foreach (ModelInfo mod in nu.maps)
                    {
                        if (name.ToLower() == mod.name) { return true; }
                    }
                    return false;
                }

                /* Convert models/textures for models */
                foreach (string mesh in meshes)
                {
                    if (ModelExists(mesh)) { continue; }

                    string meshIn = $"{morrowindPath}meshes\\{mesh.ToLower().Replace(".nif", ".fbx")}";
                    string meshOut = $"{meshPath}{mesh.ToLower().Replace(".nif", ".flver").Replace(@"\", "_")}";
                    ModelInfo modelInfo = new(mesh, $"meshes\\{mesh.ToLower().Replace(".nif", ".flver").Replace(@"\", "_")}");
                    modelInfo = ModelConverter.FBXtoFLVER(assimpContext, materialContext, modelInfo, meshIn, meshOut);

                    nu.assets.Add(modelInfo);
                }

                /* Convert models/textures for terrain */
                foreach(Cell cell in esm.exterior)
                {
                    Landscape landscape = esm.GetLandscape(cell.coordinate);
                    if(landscape == null) { continue; }

                    TerrainInfo terrainInfo = new(landscape.coordinate, $"terrain\\ext{landscape.coordinate.x},{landscape.coordinate.y}.flver");
                    terrainInfo = ModelConverter.LANDSCAPEtoFLVER(materialContext, terrainInfo, landscape, $"{terrainPath}ext{landscape.coordinate.x},{landscape.coordinate.y}.flver");

                    nu.terrains.Add(terrainInfo);
                }

                materialContext.WriteAll();

                /* Assign resource ID numbers */
                int nextM = 0, nextA = 0, nextO = 5000;
                foreach (TerrainInfo terrainInfo in nu.terrains)
                {
                    terrainInfo.id = nextM++;
                    //terrainInfo.collision.id = -1;
                }
                foreach (ModelInfo modelInfo in nu.maps)
                {
                    modelInfo.id = nextM++;
                    foreach (CollisionInfo collisionInfo in modelInfo.collisions) { collisionInfo.id = -1; }
                }
                foreach (ModelInfo modelInfo in nu.assets)
                {
                    modelInfo.id = nextA++;
                    foreach (CollisionInfo collisionInfo in modelInfo.collisions) { collisionInfo.id = -1; }
                }
                foreach (ObjectInfo objectInfo in nu.objects)
                {
                    objectInfo.id = nextO++;
                }

                /* Write new cache file */
                string jsonOutput = JsonSerializer.Serialize<Cache>(nu, new JsonSerializerOptions { IncludeFields = true });
                File.WriteAllText(manifestPath, jsonOutput);
                Console.WriteLine($"Generated new cache: {cachePath}");
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
        public CollisionInfo collision;
        public List<TextureInfo> textures; // All generated tpf files

        public int id;
        public TerrainInfo(Int2 coordinate, string path)
        {
            this.coordinate = coordinate;
            this.path = path;
            textures = new();

            id = -1;
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

    public class ModelInfo
    {
        public string name; // Original nif name, for lookup from ESM records
        public readonly string path; // Relative path from the 'cache' folder to the converted flver file
         public List<CollisionInfo> collisions; // All generated HKX collision files
        public List<TextureInfo> textures; // All generated tpf files

        public Dictionary<string, short> dummies; // Dummies and their ids

        public int id;  // Model ID number, the last 6 digits in a model filename. EXAMPLE: m30_00_00_00_005521.mapbnd.dcx

        public float size; // Bounding radius, for use in distant LOD generation
        public ModelInfo(string name, string path)
        {
            this.name = name.ToLower();
            this.path = path;
            collisions = new();
            textures = new();
            dummies = new();

            id = -1;
            size = -1f;
        }

        public string AssetPath()
        {
            return $"aeg999\\aeg999_{id.ToString("D4")}";
        }

        public string AssetName()
        {
            return $"aeg999_{id.ToString("D4")}";
        }
    }

    public class CollisionInfo
    {
        public string name; // Original nif name, for lookup from ESM records
        public string obj;  // Relative path from the 'cache' folder to the converted obj file
        public string path; // Relative path from the 'cache' folder to the converted hkx file
        public int scale;   // Scale value of this collision. HKX collision can't be scaled in engine so we hard scale the models and have multiple versions. Using ints for accuracy. 100 = 1.0f

        public int id; // Collision ID number, the last 6 digits in a collision filename. EXAMPLE: h30_00_00_00_000228.hkx.dcx
        public CollisionInfo(string name, string path, int scale)
        {
            this.name = name.ToLower();
            this.obj = path;
            this.path = path.Replace(".obj", ".hkx.dcx");
            this.scale = scale;

            id = -1;
        }
    }

    public class TextureInfo
    {
        public readonly string name; // Original dds texture name for lookup
        public readonly string path; // Relative path from the 'cache' folder to the converted tpf file
        public TextureInfo(string name, string path)
        {
            this.name = name.ToLower();
            this.path = path;
        }
    }
}
