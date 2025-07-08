using HKLib.hk2018;
using HKLib.hk2018.hkaiWorldCommands;
using HKLib.hk2018.hkcdDynamicTree;
using HKLib.hk2018.TypeRegistryTest;
using JortPob.Common;
using JortPob.Model;
using Microsoft.VisualBasic;
using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static HKLib.hk2018.hkaiUserEdgeUtils;
using static HKLib.hk2018.hknpExtendedExternMeshShapeGeometry;

namespace JortPob
{
    /* Automagically generates water assets for  the cache */
    /* Also handles some stuff for swamps and lava */
    /* @TODO: this really shouldnt be a static class anymore. it has evolved a bit */
    public class WaterManager
    {
        struct CBT
        {
            public bool result, same;
            public WetEdge a, b;
            public CBT(bool result, WetEdge a, WetEdge b)
            {
                this.result = result;
                same = a == b;
                this.a = a;
                this.b = b;
            }
        }

        /* Creates assetbnd, hkx file, and matbins for water */
        public static List<WaterInfo> Generate(ESM esm, MaterialContext materialContext)
        {
            List<WaterInfo> waters = new();

            /* Further research on water meshes leads me to believe the best approach is a single water mesh for the entire world space. */
            /* Stupid as fuck solution but it is what it is */
            int id = 0; // id for water mesh // just making the single one for now, maybe later we will generate more for other things
            {
                /* Generate water mesh */
                WetEdge testA = new WetEdge(new Vector3(1, 0, 1), new Vector3(-1, 0, -1));
                WetEdge testB = new WetEdge(new Vector3(1, 0, 0), new Vector3(-1, 0, 0));
                WetEdge testC = new WetEdge(new Vector3(0, 0, 5), new Vector3(0, 0, 6));
                WetEdge testD = new WetEdge(new Vector3(0, 0, 1), new Vector3(0, 0, -1));
                WetEdge testE = new WetEdge(new Vector3(0, 0, 4), new Vector3(0, 0, 5.5f));
                WetEdge testA2 = new WetEdge(new Vector3(-1, 0, -2), new Vector3(0, 0, 0));

                WetEdge testF = new WetEdge(new Vector3(1, 0, 3), new Vector3(15, 0, 22));
                WetEdge testG = new WetEdge(new Vector3(-5, 0, -3), new Vector3(1, 0, 3));


                Cutout testZ = new(Vector3.Zero, new Vector3(0, 0, 0), 10);
                WetFace testZ2 = new(testZ.Points()[0], testZ.Points()[1], testZ.Points()[2]);

                Vector3 test0 = testA.Intersection(testB);
                Vector3 test1 = testA.Intersection(testC);
                Vector3 test2 = testC.Intersection(testD);
                Vector3 test3 = testC.Intersection(testE);

                bool test4 = testZ.IsInside(new Vector3(1, 0, 7) + testZ.position, false);
                bool test5 = testZ.IsInside(Vector3.Zero + testZ.position, false);
                bool test6 = testZ.IsInside(new Vector3(17, 0, 0) + testZ.position, false);
                bool test7 = testZ.IsInside(new Vector3(4, 0, 2) + testZ.position, false);
                bool test8 = testZ.IsInside(new Vector3(4, 8, 2) + testZ.position, false);
                bool test9 = testZ.IsInside(new Vector3(0, 0, 5) + testZ.position, false);

                bool test10 = testZ.IsInside(new Vector3(1, 0, 0) + testZ.position, false);
                bool test11 = testZ.IsInside(new Vector3(-1, 0, 0) + testZ.position, false);
                bool test12 = testZ.IsInside(new Vector3(0, 0, 1) + testZ.position, false);
                bool test13 = testZ.IsInside(new Vector3(0, 0, -1) + testZ.position, false);

                bool test14 = testZ.IsInside(new Vector3(1, 0, 1) + testZ.position, false);
                bool test15 = testZ.IsInside(new Vector3(-1, 0, 1) + testZ.position, false);
                bool test16 = testZ.IsInside(new Vector3(1, 0, -1) + testZ.position, false);
                bool test17 = testZ.IsInside(new Vector3(-1, 0, -1) + testZ.position, false);

                Vector3 test18 = testF.Intersection(testG);
                Vector3 test19 = testF.Intersection(testF);
                Vector3 test20 = testG.Intersection(testF);

                bool test21 = testZ.IsInside(new Vector3(0, 0, 5) + testZ.position, false);
                bool test22 = testZ.IsInside(new Vector3(5, 0, 5) + testZ.position, false);
                bool test23 = testZ.IsInside(new Vector3(-5, 0, 0) + testZ.position, false);
                bool test24 = testZ.IsInside(new Vector3(-5, 0, -5) + testZ.position, false);

                bool test30 = testZ2.IsInside(new Vector3(0, 0, 5) + testZ.position, false);
                bool test31 = testZ2.IsInside(new Vector3(5, 0, 5) + testZ.position, false);
                bool test32 = testZ2.IsInside(new Vector3(-5, 0, 0) + testZ.position, false);
                bool test33 = testZ2.IsInside(new Vector3(-5, 0, -5) + testZ.position, false);

                WetFace testI = new WetFace(new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 0, 1));

                bool test25 = testI.IsIntersect(testI.Edges());
                Vector3 test26 = testA.Intersection(testA2);

                Vector3 X = new Vector3(1, 0, 0);
                Vector3 Z = new Vector3(0, 0, 1);
                WetEdge edge0 = new WetEdge(X+Z, -X-Z); // diags
                WetEdge edge1 = new WetEdge(-X-Z, X+Z);
                WetEdge edge2 = new WetEdge(-X+Z, X-Z);
                WetEdge edge3 = new WetEdge(X-Z, -X+Z);
                WetEdge edge4 = new WetEdge(X, -X); // cards
                WetEdge edge5 = new WetEdge(-X, X);
                WetEdge edge6 = new WetEdge(Z, -Z);
                WetEdge edge7 = new WetEdge(-Z, Z);
                List<WetEdge> edges = new() { edge0, edge1, edge2, edge3, edge4, edge5, edge6, edge7 };
                CBT[] results = new CBT[edges.Count*edges.Count]; int i = 0;
                foreach(WetEdge a in edges)
                {
                   foreach(WetEdge b in edges)
                    {
                        results[i++] = new CBT(!a.Intersection(b).IsNaN(), a, b);
                    }
                }

                WetEdge fuck0 = new WetEdge(new Vector3(-175.342f, 0f, 793.467f), new Vector3(-122.88f, 0f, 778.24f));
                WetEdge fuck1 = new WetEdge(new Vector3(-170.222f, 0f, 788.347f), new Vector3(-170.222f, 0f, 793.467f));
                Vector3 testFUCK0 = fuck0.Intersection(fuck1);
                Vector3 testFUCK1 = fuck1.Intersection(fuck0);

                WetEdge fuck2 = new WetEdge(new Vector3(-122.88f, 0f, 778.24f), new Vector3(-175.342f, 0f, 793.467f));
                WetEdge fuck3 = new WetEdge(new Vector3(-170.222f, 0f, 793.467f), new Vector3(-170.222f, 0f, 788.347f));
                Vector3 testFUCK2 = fuck2.Intersection(fuck3);
                Vector3 testFUCK3 = fuck3.Intersection(fuck2);

                WetMesh wetmesh = new(esm);
                wetmesh.ToObj().write(@"I:\SteamLibrary\steamapps\common\ELDEN RING\wetmesh debug.obj");
                Console.WriteLine("DEBUG BREAK");

                /* Make water meshes */
                FLVER2 flver = GenerateFlver(esm, materialContext);
                Obj obj = GenerateObj();

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
            //FLVER2 EXAMPLE = FLVER2.Read(@"I:\SteamLibrary\steamapps\common\ELDEN RING\Game\asset\aeg\aeg097\aeg097_000-geombnd-dcx\GR\data\INTERROOT_win64\asset\aeg\AEG097\AEG097_000\sib\AEG097_000.flver");

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

            // returns indice if exists, -1 if doesnt // normally i dont caare about optimizing verts/indices but this material really cares about connected verts so we doing it
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

        private static Obj GenerateObj()
        {
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

            for (int i = 0; i < positions.Length; i++)
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

            return obj;
        }

        /* When iterating through static assets, if we see swamp meshes we pop em in here. We need a list of swamp areas so we can cut them out of water gen */
        /* Morrowind water is flat so the swamp is just slightly above the water, but elden ring water is 3d so we have to actually slice the water plane to prevent clipping */
        public static List<Cutout> cutouts = new();
        public static void AddSwamp(Content content)
        {
            float s;
            if (content.mesh == @"f\terrain_bc_scum_01.nif") { s =  20.48f; }  // measured these meshes in blender. could read actual vert data but they are just squares so why bother
            else if (content.mesh == @"f\terrain_bc_scum_02.nif") { s = 10.24f; }
            else { s = 5.12f; } // @"f\terrain_bc_scum_03.nif"
            Cutout cutout = new(content.position + new Vector3(22f, 0, 0), content.rotation, s);
            cutouts.Add(cutout);

        }

        public static void AddLava(Content content)
        {

        }

        public static bool PointInSwamp(Vector3 position)
        {
            foreach(Cutout cutout in cutouts)
            {
                if (cutout.IsInside(position, false) && position.Y <= cutout.position.Y) { return true; }
            }
            return false;
        }

        public static bool PointInLava(Vector3 position)
        {
            return false;
        }

        public class WetMesh
        {
            public List<WetFace> faces;
            public WetMesh(ESM esm)
            {
                /* generic quad vert data */
                float half = Const.CELL_SIZE * .5f;
                Vector3[] positions = new Vector3[]
                {
                    new Vector3(half, 0, half),
                    new Vector3(half, 0, -half),
                    new Vector3(-half, 0, -half),
                    new Vector3(-half, 0, half)
                };

                /* Generate world water mesh */
                faces = new();
                for (int y = -(Const.WATER_RADIUS + (int)Math.Abs(Const.WATER_CENTER.Y)); y < (Const.WATER_RADIUS + (int)Math.Abs(Const.WATER_CENTER.Y)); y++)
                {
                    for (int x = -(Const.WATER_RADIUS + (int)Math.Abs(Const.WATER_CENTER.X)); x < (Const.WATER_RADIUS + (int)Math.Abs(Const.WATER_CENTER.X)); x++)
                    {
                        if (Vector2.Distance(new Vector2(x, y), Const.WATER_CENTER) <= Const.WATER_RADIUS)
                        {
                            Landscape landscape = esm.GetLandscape(new Int2(x, y));
                            if (landscape == null || landscape.hasWater)
                            {
                                /* Offset */
                                Vector3 posOffset = new Vector3(x, 0f, y) * Const.CELL_SIZE;
                                Vector3[] quad = new Vector3[]
                                {
                                    positions[0] + posOffset,
                                    positions[1] + posOffset,
                                    positions[2] + posOffset,
                                    positions[3] + posOffset,
                                };
                                WetFace A = new WetFace(quad[2], quad[1], quad[0]);
                                WetFace B = new WetFace(quad[0], quad[3], quad[2]);
                                faces.Add(A); faces.Add(B);
                            }
                        }
                    }
                }

                /* Begin slicing cutouts... god help me */

                /* We check every triangle in the mesh for intersection with a cutout */
                /* Three possible cases 1) edge intersection, 2) cutout fully inside triangle, 3) triangle fullyinside cutout */

                /* Case #1, cutout fully inside triangle */
                for (int i = 0; i < faces.Count(); i++)
                {
                    WetFace face = faces[i];

                    foreach (Cutout cutout in cutouts)
                    {
                        /* Check */
                        bool inside = true;
                        foreach (Vector3 point in cutout.Points())
                        {
                            if (!face.IsInside(point, false)) { inside = false; break; }
                        }
                        /* Perform slice */
                        if (inside)
                        {
                            /* New triangles */
                            List<WetFace> newFaces = new();

                            /* Collect all points in edge order, using modulo we can safely assume that i+1 is the next edge */
                            List<Vector3> outer = face.Points();
                            List<Vector3> inner = cutout.Points();

                            /* List of all edges for raycasting tests */
                            List<WetEdge> edges = face.Edges();
                            edges.AddRange(cutout.Edges());

                            /* Do a raycast from each outer point to each inner point, collect values to create tris */
                            for (int ii = 0; ii < outer.Count(); ii++)
                            {
                                /* Attempt to create valid triangles */
                                Vector3 op = outer[ii];                           // outer point
                                Vector3 opn = outer[(ii + 1) % outer.Count()];    // next outer point
                                for (int jj = 0; jj < inner.Count(); jj++)
                                {
                                    Vector3 ip = inner[jj];                           // inner point
                                    Vector3 ipn = inner[(jj + 1) % inner.Count()];    // next inner point

                                    /* Triangle Attempts */
                                    WetFace nf1 = new(op, ip, opn);
                                    WetFace nf2 = new(op, ip, ipn);

                                    /* Check if they are valid, then add them if they are */
                                    if (!nf1.IsIntersect(edges) && !nf1.IsDegenerate()) { newFaces.Add(nf1); edges.AddRange(nf1.Edges()); }
                                    if (!nf2.IsIntersect(edges) && !nf2.IsDegenerate()) { newFaces.Add(nf2); edges.AddRange(nf2.Edges()); }
                                }
                            }

                            /* Delete the original triangle, and add the new ones to the mesh */
                            //cutouts.Remove(cutout); // fully handled, not needed anymore! we are also breaking so no issue with foreach enum
                            faces.RemoveAt(i--);
                            faces.AddRange(newFaces);
                            break; // we cant do multiple cutouts at the same time so break and we we will loop back through 
                        }
                    }
                }

                /* Case #2, one or more points of a triangle inside cutout */
                for (int i = 0; i < faces.Count(); i++)
                {
                    WetFace face = faces[i];

                    foreach (Cutout cutout in cutouts)
                    {
                        /* Check */
                        if (cutout.IsInside(face.a, false) || cutout.IsInside(face.b, false) || cutout.IsInside(face.c, false))
                        {
                            /* Go edge by edge finding intersections */
                            /* If we find an intersection we keep anything still inside the triangle, discard the rest */
                            /* if a point of the triangle is inside the cutout it gets discarded */
                            /* Ordering points and edges here may be impossible to manage so just do your best and let the triangulation be whatever it ends up being */

                            List<WetEdge> outline = face.Edges(); // edges of tri

                            /* First deal with the situation where the cutout removes a vert of the triangle */
                            for (int ii = 0; ii < outline.Count(); ii++)
                            {
                                WetEdge edge = outline[ii];

                                /* Both points inside */
                                if (cutout.IsInside(edge.a, false) && cutout.IsInside(edge.b, false))
                                {
                                    outline.RemoveAt(ii--); continue;  // remove edge and continue, fully removed edges dont need any actual splicing
                                }

                                /* One point is inside */
                                else if (cutout.IsInside(edge.a, false) || cutout.IsInside(edge.b, false))
                                {
                                    WetEdge spl = cutout.IsInside(edge.a, false) ? edge.Reverse() : edge;  // flip edge if a is inside, makes things simpler

                                    /* Find nearest intersection for this edge */
                                    Vector3 nearest = Vector3.NaN; // nearest intersected edge point
                                    foreach (WetEdge cutedge in cutout.Edges())
                                    {
                                        Vector3 intersection = spl.Intersection(cutedge);
                                        if (intersection.IsNaN()) { continue; } // no intersection, skip

                                        if (nearest.IsNaN() || (Vector3.Distance(nearest, spl.a) > Vector3.Distance(intersection, spl.a)))
                                        {
                                            nearest = intersection;
                                        }
                                    }

                                    /* Create new edge from nearest intersection to replace one with engulfed point */
                                    WetEdge replaceEdge = new WetEdge(spl.a, nearest);
                                    outline.RemoveAt(ii--);
                                    outline.Add(replaceEdge);
                                }
                            }

                            /* Check if triangle is still sealed, (it's probably a polygon now but uhhh yeah just go ahead. if it's not sealed then seal it */
                            Dictionary<Vector3, int> edgePointCount = new();
                            foreach (WetEdge edge in outline)
                            {
                                if (edgePointCount.ContainsKey(edge.a)) { edgePointCount[edge.a]++; }
                                else { edgePointCount.Add(edge.a, 1); }
                                if (edgePointCount.ContainsKey(edge.b)) { edgePointCount[edge.b]++; }
                                else { edgePointCount.Add(edge.b, 1); }
                            }
                            List<Vector3> openPoints = new();
                            foreach (KeyValuePair<Vector3, int> kvp in edgePointCount)
                            {
                                if (kvp.Value == 1) { openPoints.Add(kvp.Key); }
                            }
                            for (int ii = 0; ii < openPoints.Count; ii += 2)   // this method of sealing is probably fine but could have trouble in some edge cases. potential bugs!
                            {
                                WetEdge sealEdge = new WetEdge(openPoints[ii], openPoints[ii + 1]);
                                outline.Add(sealEdge); // arf arf
                            }

                            /* Now that we are finally done creating the edge outline, lets fill it in with triangles */
                            /* Do a raycast from each edge to every other point and fill in with valid triangles */
                            List<WetFace> newFaces = new();
                            List<WetEdge> edges = new();
                            edges.AddRange(outline);
                            for (int ii = 0; ii < outline.Count(); ii++)
                            {
                                /* Attempt to create valid triangles */
                                WetEdge baseEdge = outline[ii];
                                for (int jj = 0; jj < outline.Count(); jj++)
                                {
                                    if (ii == jj) { continue; } // dont self succ
                                    WetEdge connectingEdge = outline[jj];

                                    /* Triangle Attempts */
                                    WetFace nf1 = new(baseEdge.a, connectingEdge.a, baseEdge.b);
                                    WetFace nf2 = new(baseEdge.a, connectingEdge.b, baseEdge.b);

                                    /* See if this triangle already exists */
                                    foreach (WetFace newFace in newFaces)
                                    {
                                        if (nf1 != null && newFace.TolerantEquals(nf1)) { nf1 = null; }
                                        if (nf2 != null && newFace.TolerantEquals(nf2)) { nf2 = null; }
                                    }

                                    /* Check if they are valid, then add them if they are */
                                    if (nf1 != null && !nf1.IsIntersect(edges) && !nf1.IsDegenerate()) { newFaces.Add(nf1); edges.AddRange(nf1.Edges()); }
                                    else if (nf2 != null && !nf2.IsIntersect(edges) && !nf2.IsDegenerate()) { newFaces.Add(nf2); edges.AddRange(nf2.Edges()); }
                                }
                            }

                            /* Delete the original triangle, and add the new ones to the mesh */
                            faces.RemoveAt(i--);
                            faces.AddRange(newFaces);
                            break; // we cant do multiple cutouts at the same time so break and we we will loop back through 
                        }
                    }
                }

                /* Case #3, cutout edge intersects a triangle edge, no points of the triangle are inside the cutout though */
                for (int i = 0; i < faces.Count(); i++)
                {
                    WetFace face = faces[i];
                    List<WetEdge> outline = face.Edges(); // edges of tri

                    foreach (Cutout cutout in cutouts)
                    {
                        if (face.IsIntersect(cutout.Edges()))
                        {
                            /* Next we loop back through edges and look for intersections between the tri edge and cutout. cutting the triangle edges to the intersection */
                            for (int ii = 0; ii < outline.Count(); ii++)
                            {
                                WetEdge edge = outline[ii];

                                /* Find nearest intersection for both the a and b point of the edge, farthest from a is nearest to b btw */
                                Vector3 nearest = Vector3.NaN;
                                Vector3 farthest = Vector3.NaN;
                                foreach (WetEdge cut in cutout.Edges())
                                {
                                    Vector3 intersection = edge.Intersection(cut);
                                    if (!intersection.IsNaN())
                                    {
                                        if (nearest.IsNaN() || (Vector3.Distance(nearest, edge.a) > Vector3.Distance(intersection, edge.a)))
                                        {
                                            nearest = intersection;
                                        }
                                        if (farthest.IsNaN() || (Vector3.Distance(farthest, edge.a) < Vector3.Distance(intersection, edge.a)))
                                        {
                                            farthest = intersection;
                                        }
                                    }
                                }

                                if (nearest.IsNaN() || farthest.IsNaN()) { continue; } // no intersection found, continue. they will always eithe be both nan or both not nan. the or is just for clarity

                                WetEdge replaceEdge = new WetEdge(edge.a, nearest);
                                WetEdge reverseEdge = new WetEdge(farthest, edge.b);
                                outline.RemoveAt(ii--);
                                outline.Add(replaceEdge);
                                outline.Add(reverseEdge);
                            }

                            /* Next we intersect the cutout edges that are inside the triangle and add them to the outline using the original triangle edges. anything entirely outside the tri is discarded */
                            /* Im going to skip accounting for a specific edge case where the cutout intersects through the entire tri without encompassing any points of it. lazy! */
                            foreach (WetEdge cut in cutout.Edges())
                            {
                                foreach (WetEdge edge in face.Edges())
                                {
                                    // if edge is entirly inside triangle add it
                                    if (face.IsInside(cut.a, false) && face.IsInside(cut.b, false))
                                    {
                                        outline.Add(cut);
                                        continue;
                                    }

                                    // if edge intersects triangle add the part thats inside the triangle
                                    Vector3 intersection = cut.Intersection(edge);
                                    if (!intersection.IsNaN())
                                    {
                                        // point a is inside; segment
                                        if (face.IsInside(cut.a, false))
                                        {
                                            WetEdge newEdge = new(intersection, cut.a);
                                            outline.Add(newEdge);
                                        }
                                        // point b is inside; segment
                                        else if (face.IsInside(cut.b, false))
                                        {
                                            WetEdge newEdge = new(cut.b, intersection);
                                            outline.Add(newEdge);
                                        }
                                        // neither point inside; bisection
                                        else
                                        {
                                            // assuming since neither point inside, we are bisecting the tri entirely. 
                                            Vector3 bisectionPoint = Vector3.NaN;
                                            foreach(WetEdge e in face.Edges())
                                            {
                                                if(edge == e) { continue; }  // looking for the other edge we hit
                                                bisectionPoint = cut.Intersection(e);
                                                if (!bisectionPoint.IsNaN()) { break; } // 99% sure i dont need to test beyond the first positive result
                                            }
                                            if(bisectionPoint.IsNaN())
                                            {
                                                Console.WriteLine("BAD BAD BAD"); // @TODO: REMOVE
                                                continue;
                                            }
                                            WetEdge newEdge = new(intersection, bisectionPoint);
                                            outline.Add(newEdge);
                                        }
                                    }
                                }
                            }

                            /* Detect and seperate islands */
                            List<List<WetEdge>> islands = new();
                            void CheckEdge(WetEdge edge)
                            {
                                // See if edge belongs in an existing island
                                foreach(List<WetEdge> island in islands)
                                {
                                    foreach(WetEdge e in island)
                                    {
                                        if (
                                            edge.a.TolerantEquals(e.a) ||
                                            edge.a.TolerantEquals(e.b) ||
                                            edge.b.TolerantEquals(e.a) ||
                                            edge.b.TolerantEquals(e.b)
                                        )
                                        {
                                            island.Add(edge);
                                            return;
                                        }
                                    }
                                }
                                // New island
                                List<WetEdge> newIsland = new();
                                newIsland.Add(edge);
                                islands.Add(newIsland);
                            }
                            foreach (WetEdge edge in outline) { CheckEdge(edge); }

                            if (islands.Count > 2)
                            {
                                Console.WriteLine("GUH");
                            }

                            /* Discard shit islands */ // @TODO: these are a result of bugs so uhhhhhhhhhh fix bug?
                            for (int ii = 0; ii < islands.Count();ii++)
                            {
                                if (islands[ii].Count() < 3)
                                {
                                    islands[Math.Max(0, ii - 1)].AddRange(islands[ii]); //collapse shit island, guh massvie gay hack @TODO:
                                    islands.RemoveAt(ii--);
                                }
                            }

                            /* Now that we are finally done creating the edge outline, lets fill it in with triangles */
                            /* Do a raycast from each edge to every other point and fill in with valid triangles */
                            List<WetFace> newFaces = new();
                            foreach (List<WetEdge> island in islands)
                            {
                                List<WetEdge> edges = new();
                                edges.AddRange(island);
                                for (int ii = 0; ii < island.Count(); ii++)
                                {
                                    /* Attempt to create valid triangles */
                                    WetEdge baseEdge = island[ii];
                                    for (int jj = 0; jj < island.Count(); jj++)
                                    {
                                        if (ii == jj) { continue; } // dont self succ
                                        WetEdge connectingEdge = island[jj];

                                        /* Triangle Attempts */
                                        WetFace nf1 = new(baseEdge.a, connectingEdge.a, baseEdge.b);
                                        WetFace nf2 = new(baseEdge.a, connectingEdge.b, baseEdge.b);

                                        /* See if this triangle already exists */
                                        foreach (WetFace newFace in newFaces)
                                        {
                                            if (nf1 != null && newFace.TolerantEquals(nf1)) { nf1 = null; }
                                            if (nf2 != null && newFace.TolerantEquals(nf2)) { nf2 = null; }
                                        }

                                        /* See if this newly generated triangle actually falls inside of a cutout */
                                        /* This can happen during triangulation for various reasons */
                                        bool InsideCutout(WetFace f)
                                        {
                                            foreach (Cutout c in cutouts) // @TODO: should only check the one we are looping through, this is dumb
                                            {
                                                c.size += 0.001f; // @TODO: disgusting hack
                                                if (c.IsInside(f.a, true) && c.IsInside(f.b, true) && c.IsInside(f.c, true))
                                                {
                                                    c.size -= 0.001f;
                                                    return true;
                                                }
                                                c.size -= 0.001f;
                                            }
                                            return false;
                                        }

                                        /* test the new edges of this triangle, skip outline edge */
                                        bool BaseSkipIntersectTest(WetFace f)
                                        {
                                            foreach(WetEdge cutedge in cutout.Edges())
                                            {
                                                if (!cutedge.Intersection(new WetEdge(f.a, f.b)).IsNaN()) { return true; }
                                                if (!cutedge.Intersection(new WetEdge(f.c, f.b)).IsNaN()) { return true; }
                                            }
                                            return false;
                                        }

                                        /* Check if they are valid, then add them if they are */
                                        if (nf1 != null && !nf1.IsIntersect(edges) && !nf1.IsDegenerate() && !InsideCutout(nf1) && !BaseSkipIntersectTest(nf1)) { newFaces.Add(nf1); edges.AddRange(nf1.Edges()); }
                                        else if (nf2 != null && !nf2.IsIntersect(edges) && !nf2.IsDegenerate() && !InsideCutout(nf2) && !BaseSkipIntersectTest(nf2)) { newFaces.Add(nf2); edges.AddRange(nf2.Edges()); }
                                    }
                                }
                            }

                            if (newFaces.Count() == 1 && face.TolerantEquals(newFaces[0])) { continue; } // gore hack
                            for (int ii=0;ii<newFaces.Count();ii++)
                            {
                                WetFace nuf = newFaces[ii];
                                if (nuf.Area() < 1f) { newFaces.RemoveAt(ii--); }
                            }

                            /* Delete the original triangle, and add the new ones to the mesh */
                            faces.RemoveAt(i--);
                            faces.AddRange(newFaces);
                            break; // we cant do multiple cutouts at the same time so break and we we will loop back through 
                        }
                    }
                }
            }

            /* DEBUG @TODO: remove later */
            public Obj ToObj()
            {
                Obj obj = new();
                // water mesh
                {
                    ObjG g = new();
                    g.name = "water";
                    g.mtl = g.name;

                    obj.vns.Add(new Vector3(0, 1, 0));
                    obj.vts.Add(new Vector3(0, 0, 0));

                    foreach (WetFace face in faces)
                    {
                        obj.vs.Add(face.a);
                        obj.vs.Add(face.b);
                        obj.vs.Add(face.c);

                        ObjV A = new(obj.vs.Count() - 3, 0, 0);
                        ObjV B = new(obj.vs.Count() - 2, 0, 0);
                        ObjV C = new(obj.vs.Count() - 1, 0, 0);

                        ObjF f = new(A, B, C);
                        g.fs.Add(f);
                    }
                    obj.gs.Add(g);
                }
                // cutout meshes
                {
                    ObjG g = new();
                    g.name = "cutout";
                    g.mtl = g.name;

                    obj.vns.Add(new Vector3(0, 1, 0));
                    obj.vts.Add(new Vector3(0, 0, 0));

                    Vector3 up = new(0, 5f, 0); // offset for debug

                    foreach(Cutout cutout in cutouts)
                    {
                        List<WetFace> faces = cutout.Faces();
                        foreach(WetFace face in faces)
                        {
                            obj.vs.Add(face.a + up);
                            obj.vs.Add(face.b + up);
                            obj.vs.Add(face.c + up);

                            ObjV A = new(obj.vs.Count() - 3, 0, 0);
                            ObjV B = new(obj.vs.Count() - 2, 0, 0);
                            ObjV C = new(obj.vs.Count() - 1, 0, 0);

                            ObjF f = new(A, B, C);
                            g.fs.Add(f);
                        }
                    }
                    obj.gs.Add(g);
                }

                return obj;
            }
        }

        public class Cutout
        {
            public readonly Vector3 position, rotation;
            public float size; // @TODO: reaadonly....
            public Cutout(Vector3 position, Vector3 rotation, float size)
            {
                this.position = position - new Vector3(Const.CELL_SIZE * .5f, 0, Const.CELL_SIZE * .5f);
                this.position.Y = 0; // @TODO: debug just to verify its a nonissue
                this.rotation = rotation;
                this.size = size;
            }

            public List<Vector3> Points()
            {
                Vector3 X = new Vector3(size * .5f, 0f, 0f);
                Vector3 Y = new Vector3(0f, 0f, size * .5f);  // i meant z lmao

                X = Vector3.Transform(X, Matrix4x4.CreateRotationY(rotation.Y * (float)(Math.PI / 180)));
                Y = Vector3.Transform(Y, Matrix4x4.CreateRotationY(rotation.Y * (float)(Math.PI / 180)));

                return new List<Vector3>()
                {
                    position+X+Y, position-X+Y, position-X-Y, position+X-Y
                };
            }

            public List<WetEdge> Edges()
            {
                Vector3 X = new Vector3(size * .5f, 0f, 0f);
                Vector3 Y = new Vector3(0f, 0f, size * .5f);  // i meant z lmao

                X = Vector3.Transform(X, Matrix4x4.CreateRotationY(rotation.Y * (float)(Math.PI / 180)));
                Y = Vector3.Transform(Y, Matrix4x4.CreateRotationY(rotation.Y * (float)(Math.PI / 180)));

                return new List<WetEdge>()
                {
                    new WetEdge(position + X + Y, position - X + Y),
                    new WetEdge(position - X + Y, position - X - Y),
                    new WetEdge(position - X - Y, position + X - Y),
                    new WetEdge(position + X - Y, position + X + Y)
                };
            }

            public List<WetFace> Faces()
            {
                Vector3 X = new Vector3(size * .5f, 0f, 0f);
                Vector3 Y = new Vector3(0f, 0f, size * .5f);

                X = Vector3.Transform(X, Matrix4x4.CreateRotationY(rotation.Y * (float)(Math.PI / 180)));
                Y = Vector3.Transform(Y, Matrix4x4.CreateRotationY(rotation.Y * (float)(Math.PI / 180)));

                Vector3[] quad = new[]
                {
                    position+X+Y, position-X+Y, position-X-Y, position+X-Y
                };

                return new List<WetFace>()
                {
                    new WetFace(quad[2], quad[1], quad[0]),
                    new WetFace(quad[0], quad[3], quad[2])
                };
            }

            // Convex shape test, code adapted from a triangle sameside point inside example
            public bool IsInside(Vector3 v, bool edgeInclusive)
            {
                // checks if point is on same side of edge as another point
                bool SameSide(Vector3 p1, Vector3 p2, WetEdge edge)
                {
                    Vector3 v1 = edge.b - edge.a;
                    Vector3 cp1 = Vector3.Cross(v1, p1 - edge.a);
                    Vector3 cp2 = Vector3.Cross(v1, p2 - edge.a);
                    return edgeInclusive ? Vector3.Dot(cp1, cp2) >= 0 : Vector3.Dot(cp1, cp2) > 0; // edgeinclusive toggles whether we count a point inside if its literally exactly on the edge
                }

                // Iterate through all our edges and make sure the point always falls on the same side of an edge as the next edge endpoint
                List<WetEdge> edges = Edges();
                for(int i=0;i<edges.Count();i++)
                {
                    WetEdge edge = edges[i];
                    WetEdge next = edges[(i+1) % edges.Count];
                    if (!SameSide(v, next.b, edge)) { return false; }
                }

                return true;
            }
        }

        public class WetFace
        {
            public readonly Vector3 a, b, c;
            public WetFace(Vector3 a, Vector3 b, Vector3 c)
            {
                this.a = a;
                this.b = b; 
                this.c = c;
            }

            public List<Vector3> Points()
            {
                return new List<Vector3>() { a, b, c };
            }

            public List<WetEdge> Edges()
            {
                return new List<WetEdge>()
                {
                    new WetEdge(a, b),
                    new WetEdge(b, c),
                    new WetEdge(c, a)
                };
            }

            public bool Equals(WetFace B)
            {
                return
                    (a == B.a && b == B.b && c == B.c) ||
                    (a == B.a && c == B.b && b == B.c) ||
                    (b == B.a && a == B.b && c == B.c) ||
                    (b == B.a && c == B.b && a == B.c) ||
                    (c == B.a && a == B.b && b == B.c) ||
                    (c == B.a && b == B.b && a == B.c);
            }

            public bool TolerantEquals(WetFace B)
            {
                return
                    (a.TolerantEquals(B.a) && b.TolerantEquals(B.b) && c.TolerantEquals(B.c)) ||
                    (a.TolerantEquals(B.a) && c.TolerantEquals(B.b) && b.TolerantEquals(B.c)) ||
                    (b.TolerantEquals(B.a) && a.TolerantEquals(B.b) && c.TolerantEquals(B.c)) ||
                    (b.TolerantEquals(B.a) && c.TolerantEquals(B.b) && a.TolerantEquals(B.c)) ||
                    (c.TolerantEquals(B.a) && a.TolerantEquals(B.b) && b.TolerantEquals(B.c)) ||
                    (c.TolerantEquals(B.a) && b.TolerantEquals(B.b) && a.TolerantEquals(B.c));
            }

            public float Area()
            {
                return 0.5f * Math.Abs((a.X * (b.Z - c.Z) + b.X * (c.Z - a.Z) + c.X * (a.Z - b.Z)));
            }

            /* Degenerate triangles have no surface area, just delete it. 2 smaller sides of a tri should never add up to the largest side. */
            public bool IsDegenerate()
            {
                float[] sides = new[] { Vector3.Distance(a, b), Vector3.Distance(b, c), Vector3.Distance(c, a) };
                Array.Sort(sides);
                return sides[0] + sides[1] <= sides[2];
            }

            // Convex shape test, code adapted from a triangle sameside point inside example
            public bool IsInside(Vector3 v, bool edgeInclusive)
            {
                // checks if point is on same side of edge as another point
                bool SameSide(Vector3 p1, Vector3 p2, WetEdge edge)
                {
                    Vector3 v1 = edge.b - edge.a;
                    Vector3 cp1 = Vector3.Cross(v1, p1 - edge.a);
                    Vector3 cp2 = Vector3.Cross(v1, p2 - edge.a);
                    return edgeInclusive ? Vector3.Dot(cp1, cp2) >= 0 : Vector3.Dot(cp1, cp2) > 0; // edgeinclusive toggles whether we count a point inside if its literally exactly on the edge
                }

                // Iterate through all our edges and make sure the point always falls on the same side of an edge as the next edge endpoint
                List<WetEdge> edges = Edges();
                for (int i = 0; i < edges.Count(); i++)
                {
                    WetEdge edge = edges[i];
                    WetEdge next = edges[(i + 1) % edges.Count];
                    if (!SameSide(v, next.b, edge)) { return false; }
                }

                return true;
            }

            /* Returns true if any edge in the list is intersecting this face, or if any edge is fully within the area of the face */
            public bool IsIntersect(List<WetEdge> B)
            {
                List<WetEdge> A = Edges();
                foreach (WetEdge a in A)
                {
                    foreach (WetEdge b in B)
                    {
                        if (!a.Intersection(b).IsNaN()) {
                            return true;
                        } // intersecting edge test
                        if (IsInside(b.a, false)) {
                            return true;
                        } // edge inside test. only need to test one point because if 1 point is inside, either both are or its an intersction lol.
                    }
                }

                return false;
            }
        }

        public class WetEdge
        {
            public readonly Vector3 a, b;
            public WetEdge(Vector3 a, Vector3 b)
            {
                this.a = a;
                this.b = b;
            }

            public WetEdge Reverse()
            {
                return new WetEdge(b, a);
            }

            // mostly copied from 20xx.io util class because guh
            public Vector3 Intersection(WetEdge B)
            {
                // check if the end points are the intersection and discard if so! since we are working with triangles i am not considering endpoints part of an intersection as it means faces intersect themselves and neighbour faces
                if (a.TolerantEquals(B.a) || a.TolerantEquals(B.b) || b.TolerantEquals(B.a) || b.TolerantEquals(B.b))
                {
                    return Vector3.NaN;
                }

                // check for intersection
                float s1_x, s1_y, s2_x, s2_y;
                float i_x, i_y;
                s1_x = b.X - a.X; s1_y = b.Z - a.Z;
                s2_x = B.b.X - B.a.X; s2_y = B.b.Z - B.a.Z;

                float s, t;
                s = (-s1_y * (a.X - B.a.X) + s1_x * (a.Z - B.a.Z)) / (-s2_x * s1_y + s1_x * s2_y);
                t = (s2_x * (a.Z - B.a.Z) - s2_y * (a.X - B.a.X)) / (-s2_x * s1_y + s1_x * s2_y);

                if (s >= 0 && s <= 1 && t >= 0 && t <= 1)
                {
                    // intersection found
                    i_x = a.X + (t * s1_x);
                    i_y = a.Z + (t * s1_y);
                    Vector3 intersection = new(i_x, 0, i_y);

                    // if the intersection point is exactly on an endpoint, we discard. we dont want that behavriour in this situation
                    if(intersection.TolerantEquals(a) || intersection.TolerantEquals(b) || intersection.TolerantEquals(B.a) || intersection.TolerantEquals(B.b))
                    {
                        return Vector3.NaN;
                    }

                    return intersection;
                }

                return Vector3.NaN; // no intersection
            }
        }
    }
}
