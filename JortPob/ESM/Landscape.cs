using JortPob.Common;
using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using gfoidl.Base64;


namespace JortPob
{
    public class Landscape
    {
        public readonly Int2 coordinate;

        public readonly string flags;

        public readonly List<Vertex> vertices;
        public readonly List<int> indices;

        public List<Texture> textures;

        public List<Mesh> meshes;

        public Landscape(Int2 coordinate, JsonNode json, Dictionary<ESM.Type, List<JsonNode>> records)
        {
            this.coordinate = coordinate;
            flags = json["landscape_flags"].ToString();

            byte[] b64Height = Base64.Default.Decode(json["vertex_heights"]["data"].ToString());
            byte[] b64Normal = Base64.Default.Decode(json["vertex_normals"]["data"].ToString());
            byte[] b64Color = Base64.Default.Decode(json["vertex_colors"]["data"].ToString());
            byte[] b64Texture = Base64.Default.Decode(json["texture_indices"]["data"].ToString());

            int bA = 0; // Buffer postion reading heights
            int bB = 0; // Buffer position reading normals
            int bC = 0; // Buffer position reading color
            int bD = 0; // Buffer position for texture indices

            /* Checks through all landscape texture data and makes sure there is no duplicate texture index that points to the same texture file. Returns same index if no dupe or a dupe at a higher index, returns dupe index if found and it's a lower value. */
            ushort[,] ltex = new ushort[16, 16];
            for (int yy = 0; yy < 15; yy += 4)
            {
                for (int xx = 0; xx < 15; xx += 4)
                {
                    for (int yyy = 0; yyy < 4; yyy++)
                    {
                        for (int xxx = 0; xxx < 4; xxx++)
                        {
                            ushort texIndex = (ushort)(BitConverter.ToUInt16(new byte[] { b64Texture[bD++], b64Texture[bD++] }, 0) - (ushort)1);
                            //ushort texIndex = Cell.DeDupeTextureIndex(esm, (ushort)(BitConverter.ToUInt16(new byte[] { zstdTexture[bD++], zstdTexture[bD++] }, 0) - (ushort)1));
                            ltex[xx + xxx, yy + yyy] = texIndex;
                        }
                    }
                }
            }

            textures = new();
            textures.Add(new Texture("Missing Texture", Utility.ResourcePath("textures\\tx_missing.dds"), 65535));   // I don't really know what ID 65535 means but I assume it's a missing textures or smth. Guh.
            JsonNode GetLandscapeTextureRecord(int id)
            {
                foreach (JsonNode j in records[ESM.Type.LandscapeTexture])
                {
                    if (int.Parse(j["index"].ToString()) == id)
                    {
                        return j;
                    }
                }
                return null;
            }
            
            bool HasTexture(ushort id)
            {
                foreach(Texture t in textures)
                {
                    if (t.index == id) { return true; }
                }
                return false;
            }

            foreach (ushort id in ltex)
            {
                if (HasTexture(id)) { continue; }

                JsonNode ltjson = GetLandscapeTextureRecord(id);

                if(ltjson != null)
                {
                    textures.Add(new Texture(ltjson["id"].ToString().ToLower(), $"{Const.MORROWIND_PATH}Data Files\\textures\\{ltjson["file_name"].ToString().ToLower().Substring(0, ltjson["file_name"].ToString().Length-4)}.dds", id));
                }
                else
                {
                   Lort.Log($" ## WARNING ## INVALID LANDSCAPE TEXTURE INDEX IN LANDSCAPE DATA: {id}", Lort.Type.Debug);
                }
            }

            //float offset = BitConverter.ToSingle(new byte[] { zstdHeight[bA++], zstdHeight[bA++], zstdHeight[bA++], zstdHeight[bA++] }, 0);
            float offset = float.Parse(json["vertex_heights"]["offset"].ToString());

            /* Vertex Data */
            Vector3 centerOffset = new Vector3((Const.CELL_SIZE / 2f), 0f, -(Const.CELL_SIZE / 2f));
            vertices = new();
            float last = offset;
            float lastEdge = last;
            for (int yy = Const.CELL_GRID_SIZE; yy >= 0; yy--)
            {
                for (int xx = 0; xx < Const.CELL_GRID_SIZE + 1; xx++)
                {
                    sbyte height = (sbyte)(b64Height[bA++]);
                    last += height;
                    if (xx == 0) { lastEdge = last; }

                    float xxx = -xx * (Const.CELL_SIZE / (float)(Const.CELL_GRID_SIZE));
                    float yyy = (Const.CELL_GRID_SIZE - yy) * (Const.CELL_SIZE / (float)(Const.CELL_GRID_SIZE)); // I do not want to talk about this coordinate swap
                    float zzz = last * 8f * Const.GLOBAL_SCALE;
                    Vector3 position = new Vector3(xxx, zzz, yyy) + centerOffset;
                    Int2 grid = new Int2(xx, yy);

                    float iii = (sbyte)b64Normal[bB++];
                    float jjj = (sbyte)b64Normal[bB++];
                    float kkk = (sbyte)b64Normal[bB++];

                    Byte4 color = new Byte4(Byte.MaxValue); // Default
                    if (b64Color != null)
                    {
                        color = new Byte4(b64Color[bC++], b64Color[bC++], b64Color[bC++], byte.MaxValue);
                    }

                    vertices.Add(new Vertex(position, grid, Vector3.Normalize(new Vector3(iii, kkk, jjj)), new Vector2(xx * (1f / Const.CELL_GRID_SIZE), yy * (1f / Const.CELL_GRID_SIZE)), color, ltex[Math.Min((xx) / 4, 15), Math.Min((Const.CELL_GRID_SIZE - yy) / 4, 15)]));
                }
                last = lastEdge;
            }

            indices = new();
            for (int yy = 0; yy < Const.CELL_GRID_SIZE; yy += 4)
            {
                for (int xx = 0; xx < Const.CELL_GRID_SIZE; xx += 4)
                {
                    int[] quad = {
                                (yy * (Const.CELL_GRID_SIZE + 1)) + xx,
                                (yy * (Const.CELL_GRID_SIZE + 1)) + (xx + 4),
                                ((yy + 4) * (Const.CELL_GRID_SIZE + 1)) + (xx + 4),
                                ((yy + 4) * (Const.CELL_GRID_SIZE + 1)) + xx
                            };


                    int[,] tris = {
                                {
                                    quad[(xx + (yy % 2) + 2) % 4],
                                    quad[(xx + (yy % 2) + 1) % 4],
                                    quad[(xx + (yy % 2) + 0) % 4]
                                },
                                {
                                    quad[(xx + (yy % 2) + 0) % 4],
                                    quad[(xx + (yy % 2) + 3) % 4],
                                    quad[(xx + (yy % 2) + 2) % 4]
                                }
                            };

                    for (int t = 0; t < 2; t++)
                    {
                        for (int i = 2; i >= 0; i--)
                        {
                            indices.Add(tris[t, i]);
                        }
                    }
                }
            }

            /* Now that we've built the terrain mesh, let's subdivide it into multiple meshes based on what textures it uses */
            /* Elden Ring shaders can only render like 2 or 3 textures on a mesh, while morrowind can do dozens. So this subdivision is to allow use to do this */

            Mesh GetMesh(Texture a, Texture b)
            {
                foreach (Mesh mesh in meshes)
                {
                    if (mesh.textures.Contains(a) && mesh.textures.Contains(b))
                    {
                        return mesh;
                    }
                }

                Mesh nu = new(a, b);
                meshes.Add(nu);
                return nu;
            }

            Texture GetTexture(ushort id)
            {
                foreach (Texture tex in textures)
                {
                    if (tex.index == id)
                    {
                        return tex;
                    }
                }
                Lort.Log("# ## WARNING ## Missing texture index in landscape mesh!", Lort.Type.Debug);
                return null;
            }

            meshes = new();
            for (int itr = 0; itr < indices.Count; itr += 3)
            {
                int i = indices[itr];
                int j = indices[itr + 1];
                int k = indices[itr + 2];

                Vertex a = vertices[i];
                Vertex b = vertices[j];
                Vertex c = vertices[k];

                List<Texture> texs = new();
                texs.Add(GetTexture(a.texture));
                if (!texs.Contains(GetTexture(b.texture))) { texs.Add(GetTexture(b.texture)); }
                if (!texs.Contains(GetTexture(c.texture))) { texs.Add(GetTexture(c.texture)); }

                Mesh mesh = GetMesh(texs[0], texs[Math.Min(1, texs.Count - 1)]);

                mesh.indices.Add(mesh.indices.Count); mesh.vertices.Add(a);
                mesh.indices.Add(mesh.indices.Count); mesh.vertices.Add(b);
                mesh.indices.Add(mesh.indices.Count); mesh.vertices.Add(c);
            }
        }

        public class Mesh
        {
            public List<Texture> textures;
            public List<int> indices;
            public List<Vertex> vertices;

            public Mesh(Texture a, Texture b)
            {
                textures = new();
                textures.Add(a);
                textures.Add(b);
                indices = new();
                vertices = new();
            }
        }

        public class Vertex
        {
            public Vector3 position;
            public Int2 grid; // position on this cells grid
            public Vector3 normal;
            public Vector2 coordinate;  // UV
            public Byte4 color; // Bytes of a texture that contains the converted vertex color information

            public ushort texture;

            public Vertex(Vector3 position, Int2 grid, Vector3 normal, Vector2 coordinate, Byte4 color, ushort texture)
            {
                this.position = position;
                this.grid = grid;
                this.normal = normal;
                this.coordinate = coordinate;
                this.color = color;
                this.texture = texture;
            }
        }

        public class Texture
        {
            public string name;
            public string path;
            public ushort index;

            public Texture(string name, string path, ushort index)
            {
                this.name = name;  this.path = path; this.index = index;
            }
        }
    }
}
