using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Linq;

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
            for(int i=0;i<flver.Materials.Count();i++)
            {
                // Check if unused
                FLVER2.Material material = flver.Materials[i];
                bool unused = true;
                foreach(FLVER2.Mesh mesh in flver.Meshes)
                {
                    if (mesh.MaterialIndex == material.Index) { unused = false; break; }
                }
                // Delete and resolve indices
                if (unused)
                {
                    flver.Materials.RemoveAt(i);
                    foreach(FLVER2.Material mat in flver.Materials)
                    {
                        if(mat.Index > i) { mat.Index--; }
                    }
                    foreach(FLVER2.Mesh mesh in flver.Meshes)
                    {
                        if(mesh.MaterialIndex > i) { mesh.MaterialIndex--; }
                    }
                    i--;
                }
            }

            /* Delete unused bufferlayouts */
            for (int i = 0; i < flver.BufferLayouts.Count(); i++)
            {
                // Check if unused
                FLVER2.BufferLayout layout = flver.BufferLayouts[i];
                bool unused = true;
                foreach (FLVER2.Mesh mesh in flver.Meshes)
                {
                    foreach(FLVER2.VertexBuffer buffer in mesh.VertexBuffers)
                    {
                        if(buffer.LayoutIndex == i) { unused = false; break; }
                    }
                    if(!unused) { break; }
                }
                // Delete and resolve indices
                if (unused)
                {
                    flver.BufferLayouts.RemoveAt(i);
                    foreach (FLVER2.Mesh mesh in flver.Meshes)
                    {
                        foreach(FLVER2.VertexBuffer vb in mesh.VertexBuffers)
                        {
                            if (vb.LayoutIndex > i) { vb.LayoutIndex--; }
                        }
                    }
                    i--;
                }
            }

            /* Delete unused gxlists */
            for (int i = 0; i < flver.GXLists.Count(); i++)
            {
                // Check if unused
                FLVER2.GXList gxlist = flver.GXLists[i];
                bool unused = true;
                foreach (FLVER2.Material mat in flver.Materials)
                {
                    if (mat.GXIndex == i) { unused = false; break; }
                }
                // Delete and resolve indices
                if (unused)
                {
                    flver.GXLists.RemoveAt(i);
                    foreach (FLVER2.Material mat in flver.Materials)
                    {
                        if(mat.GXIndex > i) { mat.GXIndex--; }
                    }
                    i--;
                }
            }

            /* Re-index duplicate vertices */
            foreach(FLVER2.Mesh mesh in flver.Meshes)
            {
                List<FLVER.Vertex> verts = mesh.Vertices; // original vertices
                mesh.Vertices = new();

                int GetIndex(FLVER.Vertex v)
                {
                    for(int i=0;i<mesh.Vertices.Count();i++)
                    {
                        FLVER.Vertex vert = mesh.Vertices[i];
                        // check if vertex is a match
                        if
                        (
                            vert.Position == v.Position &&   // @TODO: probably not good enough tbh, should look into rounding and comparison of values directly
                            vert.Normal == v.Normal &&
                            vert.UVs[0] == v.UVs[0] &&
                            (vert.UVs.Count() > 1 ? vert.UVs[1] == v.UVs[1] : true)
                        )
                        {
                            return i;
                        }
                    }

                    mesh.Vertices.Add(v);
                    return mesh.Vertices.Count - 1;
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
    }
}
