using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace JortPob.Model
{
    public class FLVERUtil
    {
        /* opens a flver, scales it, writes it */
        public static void Scale(string flverPath, string outPath, float scale)
        {
            /* Load flver */
            FLVER2 flver = FLVER2.Read(flverPath);

            /* Scale vertices... */
            foreach(FLVER2.Mesh mesh in flver.Meshes)
            {
                foreach(FLVER.Vertex vertex in mesh.Vertices)
                {
                    vertex.Position *= scale;
                }
            }

            /* Resolve bounding boxes after scaling */
            BoundingBoxSolver.FLVER(flver);

            /* Write to file */
            flver.Write(outPath);
        }

        /* takes a flver already loaded in memory and scans through it, removing anything that is not needed. */
        /* Examples: unused materials, duplicate vertices */
        public static FLVER2 Optimize(FLVER2 flver)
        {
            /* Delete unused materials */
            HashSet<int> usedMaterials = new();
            foreach (FLVER2.Mesh mesh in flver.Meshes)
            {
                usedMaterials.Add(mesh.MaterialIndex);
            }
            for (int i = flver.Materials.Count - 1; i >= 0; i--)
            {
                if (!usedMaterials.Contains(i))
                {
                    flver.Materials.RemoveAt(i);
                    foreach (FLVER2.Material mat in flver.Materials)
                    {
                        if (mat.Index > i) { mat.Index--; }
                    }
                    foreach (FLVER2.Mesh mesh in flver.Meshes)
                    {
                        if (mesh.MaterialIndex > i) { mesh.MaterialIndex--; }
                    }
                }
            }

            /* Delete unused bufferlayouts */
            HashSet<int> usedLayouts = new();
            foreach (FLVER2.Mesh mesh in flver.Meshes)
            {
                foreach (FLVER2.VertexBuffer buffer in mesh.VertexBuffers)
                {
                    usedLayouts.Add(buffer.LayoutIndex);
                }
            }

            for (int i = flver.BufferLayouts.Count - 1; i >= 0; i--)
            {
                if (!usedLayouts.Contains(i))
                {
                    flver.BufferLayouts.RemoveAt(i);
                    foreach (FLVER2.Mesh mesh in flver.Meshes)
                    {
                        foreach (FLVER2.VertexBuffer vb in mesh.VertexBuffers)
                        {
                            if (vb.LayoutIndex > i) { vb.LayoutIndex--; }
                        }
                    }
                }
            }


            /* Delete unused gxlists */
            HashSet<int> usedGXLists = new();
            foreach (FLVER2.Material mat in flver.Materials)
            {
                usedGXLists.Add(mat.GXIndex);
            }
            for (int i = flver.GXLists.Count - 1; i >= 0; i--)
            {
                if (!usedGXLists.Contains(i))
                {
                    flver.GXLists.RemoveAt(i);
                    foreach (FLVER2.Material mat in flver.Materials)
                    {
                        if (mat.GXIndex > i) { mat.GXIndex--; }
                    }
                }
            }

            /* Re-index duplicate vertices */
            foreach(FLVER2.Mesh mesh in flver.Meshes)
            {
                List<FLVER.Vertex> verts = mesh.Vertices; // original vertices
                mesh.Vertices = new();

                Dictionary<VertexKey, int> vertexLookup = new();

                int GetIndex(FLVER.Vertex v)
                {
                    VertexKey key = new VertexKey(v);

                    if (vertexLookup.TryGetValue(key, out int existingIndex))
                    {
                        return existingIndex;
                    }

                    int newIndex = mesh.Vertices.Count;
                    mesh.Vertices.Add(v);
                    vertexLookup[key] = newIndex;
                    return newIndex;
                }

                foreach (FLVER2.FaceSet faceSet in mesh.FaceSets)
                {
                    List<int> inds = faceSet.Indices; // original indices
                    faceSet.Indices = new();

                    for(int i = 0; i < inds.Count; i++)
                    {
                        FLVER.Vertex v = verts[inds[i]]; // get original vert from this index
                        int index = GetIndex(v);
                        faceSet.Indices.Add(index);
                    }
                }
                Console.WriteLine("DEBUG");
            }

            return flver;
        }

        private struct VertexKey : IEquatable<VertexKey>
        {
            private readonly Vector3 position;
            private readonly Vector3 normal;
            private readonly Vector3 uv0;
            private readonly Vector3? uv1;
            private readonly int hashCode;

            public VertexKey(FLVER.Vertex vertex)
            {
                position = vertex.Position;
                normal = vertex.Normal;
                uv0 = vertex.UVs[0];
                uv1 = vertex.UVs.Count > 1 ? vertex.UVs[1] : null;

                hashCode = HashCode.Combine(position, normal, uv0, uv1);
            }

            public bool Equals(VertexKey other)
            {
                // @TODO: probably not good enough tbh, should look into rounding and comparison of values directly
                return position == other.position &&
                       normal == other.normal &&
                       uv0 == other.uv0 &&
                       uv1 == other.uv1;
            }

            public override bool Equals(object obj)
            {
                return obj is VertexKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return hashCode;
            }
        }
    }
}
