using HKLib.hk2018;
using HKLib.hk2018.hkaiWorldCommands;
using JortPob.Common;
using JortPob.Model;
using SoulsFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace JortPob
{
    /* Automagically generates water assets, and msb parts */
    public class WaterManager
    {
        /* Creates assetbnd, hkx file, and matbins for water */
        public static List<WaterInfo> Generate(ESM esm, MaterialContext materialContext)
        {
            List<WaterInfo> waters = new();

            /* Further research on water meshes leads me to believe the best approach is a single water mesh for the entire world space. */
            /* Stupid as fuck solution but it is what it is */
            int id = 0; // id for water mesh // just making the single one for now
            {
                /* Make water mesh */
                FLVER2 flver = GenerateFlver(esm, materialContext);

                /* generate obj for uses as water plane collision, these are per tile so its just a square */
                Obj obj = new();
                ObjG g = new();
                g.name = CollisionMaterial.Water.ToString();
                g.mtl = $"hkm_{g.name}_Safe1";

                float half = Const.TILE_SIZE * .5f;
                Vector3[] positions = new Vector3[]
                {
                    new Vector3(half, 0, half), new Vector3(half, 0, -half), new Vector3(-half, 0, -half), new Vector3(-half, 0, half)
                };
                Vector3 normal = new Vector3(0, 1, 0);
                Vector3[] uvs = new Vector3[]
                {
                    new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(0, 1, 0)
                };

                for(int i = 0;i<positions.Length;i++)
                {
                    obj.vs.Add(positions[i]);
                    obj.vns.Add(normal);
                    obj.vts.Add(uvs[i]);
                }
                List<int> indices = new List<int>() { 0, 1, 2, 0, 2, 3 };
                List<ObjV> V = new();
                foreach (int index in indices)
                {
                    ObjV v = new(index, index, index);
                    V.Add(v);

                    if (V.Count >= 3)
                    {
                        ObjF f = new(V[0], V[1], V[2]);
                        g.fs.Add(f);
                        V.Clear();
                    }
                }
                obj.gs.Add(g);

                /* Files happen */
                string name = $"meshes\\water{id}";
                string flverPath = $"{name}.flver";
                string objPath = $"{name}.obj";
                flver.Write($"{Const.CACHE_PATH}{flverPath}");
                obj.write($"{Const.CACHE_PATH}{objPath}");

                /* make a waterinfo class about this generated water */
                WaterInfo waterInfo = new(id, flverPath);
                CollisionInfo collisioInfo = new($"water{id}", objPath);
                waterInfo.collision = collisioInfo;
                
                waters.Add(waterInfo);
            }

            return waters;
        }

        private static FLVER2 GenerateFlver(ESM esm, MaterialContext materialContext)
        {
            FLVER2 EXAMPLE = FLVER2.Read(@"I:\SteamLibrary\steamapps\common\ELDEN RING\Game\asset\aeg\aeg097\aeg097_000-geombnd-dcx\GR\data\INTERROOT_win64\asset\aeg\AEG097\AEG097_000\sib\AEG097_000.flver");

            FLVER2 flver = new();
            flver.Header.Version = 131098; // Elden Ring FLVER Version Number
            flver.Header.Unk5D = 0;        // Unk
            flver.Header.Unk68 = 4;        // Unk

            /* Add bones and nodes for FLVER */
            FLVER.Node rootNode = new();
            FLVER2.SkeletonSet skeletonSet = new();
            FLVER2.SkeletonSet.Bone rootBone = new(0);

            rootNode.Name = Path.GetFileNameWithoutExtension("WaterMesh");
            skeletonSet.AllSkeletons.Add(rootBone);
            skeletonSet.BaseSkeleton.Add(rootBone);
            flver.Nodes.Add(rootNode);
            flver.Skeletons = skeletonSet;

            /* Materials @TODO: */
            MaterialContext.MaterialInfo matinfo = materialContext.GenerateMaterialWater(0);
            flver.Materials.Add(matinfo.material);
            flver.BufferLayouts.Add(matinfo.layout);
            flver.GXLists.Add(matinfo.gx);

            /* make a mesh */
            FLVER2.Mesh mesh = new();
            FLVER2.FaceSet faces = new();
            mesh.FaceSets.Add(faces);
            faces.CullBackfaces = false;
            faces.Unk06 = 1;
            mesh.NodeIndex = 0; // attach to rootnode
            mesh.MaterialIndex = 0;
            FLVER2.VertexBuffer vb = new(0);
            mesh.VertexBuffers.Add(vb);

            /* generic quad vert data */
            float half = Const.CELL_SIZE * .5f;
            Vector3[] positions = new Vector3[]
            {
                new Vector3(half, 0, half), new Vector3(half, 0, -half), new Vector3(-half, 0, -half), new Vector3(-half, 0, half)
            };
            Vector3 normal = new Vector3(0, 1, 0);
            Vector4 tangent = new Vector4(1, 0, 0, -1);
            Vector4 bitangent = new Vector4(0, 0, 0, 0);
            Vector3[] uvs = new Vector3[]
            {
                new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(0, 1, 0)
            };
            FLVER.VertexColor color = new(255, 255, 255, 255);
            List<int> indiceOffsets = new List<int>() { 0, 1, 2, 0, 2, 3 };

            // returns indice if exists, -1 if doesnt
            int GetVertex(Vector3 position)
            {
                for(int i=0;i<mesh.Vertices.Count;i++)
                {
                    FLVER.Vertex vert = mesh.Vertices[i];
                    if (Vector3.Distance(vert.Position, position) < 0.01) { return i; }
                }
                return -1;
            }

            /* Okay here we go lmao */
            for (int y = -Const.WATER_RADIUS; y < Const.WATER_RADIUS; y++)
            {
                for (int x = -Const.WATER_RADIUS; x < Const.WATER_RADIUS; x++)
                {
                    if(Vector2.Distance(new Vector2(x,y), new Vector2(0f)) <= Const.WATER_RADIUS)
                    {
                        Landscape landscape = esm.GetLandscape(new Int2(x, y));
                        if(landscape == null || landscape.hasWater)
                        {
                            /* Offset */
                            Vector3 posOffset = new Vector3(x, 0f, y) * Const.CELL_SIZE;
                            Vector3 uvOffset = new Vector3(x, y, 0f);

                            /* Add vertex data */
                            int[] quad = new int[4];
                            for (int i = 0; i < 4; i++)
                            {
                                Vector3 nextpos = positions[i] + posOffset;
                                int indice = GetVertex(nextpos);

                                if (indice == -1)
                                {
                                    FLVER.Vertex vert = new();
                                    vert.Position = nextpos;
                                    vert.Normal = normal;
                                    vert.Tangents.Add(tangent);
                                    vert.Bitangent = bitangent;

                                    float distToZero = Vector3.Distance(uvs[i] + uvOffset, Vector3.Zero);
                                    float normDistToZero = distToZero / Const.WATER_RADIUS;
                                    Vector3 normalized = (uvs[i] + uvOffset) / Const.WATER_RADIUS;

                                    vert.UVs.Add(normalized * 15f);  // some kind of loop uv layout, between -15,15, @TODO: generate this properly?
                                    vert.UVs.Add(normalized * 2f);   // normal-ish top down flat uv layout, sized so world is within like -2, 2
                                    vert.UVs.Add(new Vector3(normDistToZero * 15f, normDistToZero * 0.2f, 0));   // some kind of value based on distance from center of land
                                    vert.UVs.Add(new Vector3(normalized.X * 15f, 0.1f, 0f)); // no fucking clue
                                    vert.UVs.Add(new Vector3(normalized.X, 0.5f, 0)); // weird but X is normal and normalized between 0,1 and y is just flat aside from a few random verts 
                                    vert.UVs.Add(new Vector3(normalized.X, 0.5f, 0)); // same as last one ????
                                    vert.UVs.Add(new Vector3(normalized.X, 0.5f, 0)); // also same ???
                                    vert.UVs.Add(new Vector3(normalized.X, 0.5f, 0)); // still same ??????????

                                    vert.Colors.Add(color);
                                    mesh.Vertices.Add(vert);
                                    indice = mesh.Vertices.Count - 1;
                                }

                                quad[i] = indice;
                            }

                            /* Define indice */
                            foreach(int i in indiceOffsets)
                            {
                                faces.Indices.Add(quad[i]);
                            }
                        }
                    }
                }
            }

            /* Add mesh */
            flver.Meshes.Add(mesh);

            /* Bounding box solve */
            BoundingBoxSolver.FLVER(flver);

            return flver;
        }
    }
}
