using JortPob.Common;
using SoulsFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace JortPob.Model
{
    public partial class ModelConverter
    {
        public static TerrainInfo LANDSCAPEtoFLVER(MaterialContext materialContext, TerrainInfo terrainInfo, Landscape landscape, string outputFilename)
        {
            /* Create a blank FLVER configured for Elden Ring */
            FLVER2 flver = new();
            flver.Header.Version = 131098; // Elden Ring FLVER Version Number
            flver.Header.Unk5D = 0;        // Unk
            flver.Header.Unk68 = 4;        // Unk

            /* Add bones and nodes for FLVER */
            FLVER.Node rootNode = new();
            FLVER2.SkeletonSet skeletonSet = new();
            FLVER2.SkeletonSet.Bone rootBone = new(0);

            rootNode.Name = Path.GetFileNameWithoutExtension($"terrain-{terrainInfo.path}");
            skeletonSet.AllSkeletons.Add(rootBone);
            skeletonSet.BaseSkeleton.Add(rootBone);
            flver.Nodes.Add(rootNode);
            flver.Skeletons = skeletonSet;

            /* Generate material data */
            List<MaterialContext.MaterialInfo> materialInfo = materialContext.GenerateMaterials(landscape);
            foreach (MaterialContext.MaterialInfo mat in materialInfo)
            {
                flver.Materials.Add(mat.material);
                flver.GXLists.Add(mat.gx);
                flver.BufferLayouts.Add(mat.layout);
                foreach (TextureInfo info in mat.info)
                {
                    terrainInfo.textures.Add(info);
                }
            }

            /* Generate blank flver mesh and faceset */
            int i = 0;
            foreach (Landscape.Mesh landMesh in landscape.meshes)
            {
                FLVER2.Mesh flverMesh = new();
                FLVER2.FaceSet flverFaces = new();
                flverMesh.FaceSets.Add(flverFaces);
                flverFaces.CullBackfaces = true;
                flverFaces.Unk06 = 1;
                flverMesh.NodeIndex = 0; // attach to rootnode
                flverMesh.MaterialIndex = i++;

                /* Setup Vertex Buffer */
                FLVER2.VertexBuffer flverBuffer = new(0);
                flverMesh.VertexBuffers.Add(flverBuffer);

                /* Convert vert/face data */
                foreach (int index in landMesh.indices)
                {
                    FLVER.Vertex flverVertex = new();
                    Landscape.Vertex vertex = landMesh.vertices[index];

                    /* Grab vertice position + normal */
                    Vector3 pos = new(vertex.position.X, vertex.position.Y, vertex.position.Z);
                    Vector3 norm = new(vertex.normal.X, vertex.normal.Y, vertex.normal.Z);

                    // Fromsoftware lives in the mirror dimension. I do not know why.
                    pos.X *= -1f;
                    norm.X *= -1f;

                    // Set ...
                    flverVertex.Position = pos;
                    flverVertex.Normal = norm;

                    Vector3 uvw = new(vertex.coordinate.X, -vertex.coordinate.Y, 0);
                    float blend = vertex.texture == landMesh.textures[0].index ? 0f : 1f;  // @TODO: could calculate this earlier and do more gradual blends over multiple verts
                    Vector3 uvw_blend = new(blend, 0, 0);
                    Vector3 blank = new(0, 0, 0);
                    flverVertex.UVs.Add(uvw);
                    flverVertex.UVs.Add(uvw_blend);  // Second UV channel is just used as a blender for the multimaterial.
                    flverVertex.UVs.Add(blank);      // I don't know why we need a third channel but SoulsFormat complains if it's not there so here ya go!

                    flverVertex.Bitangent = new Vector4(0, 0, 1, 1);  // @TODO: WRONG!
                    flverVertex.Tangents.Add(new Vector4(1, 0, 0, 1));  // @TODO: WRONG!

                    FLVER.VertexColor color = new(vertex.color.w, vertex.color.x, vertex.color.y, vertex.color.z); // Doesn't seem to do anything @TODO: replace with mult overlay
                    flverVertex.Colors.Add(color);

                    flverMesh.Vertices.Add(flverVertex);
                    flverFaces.Indices.Add(flverMesh.Vertices.Count - 1);
                }

                flver.Meshes.Add(flverMesh);
            }

            /* Calculate bounding boxes */
            float X1 = float.MaxValue, X2 = float.MinValue, Y1 = float.MaxValue, Y2 = float.MinValue, Z1 = float.MaxValue, Z2 = float.MinValue;
            foreach (FLVER2.Mesh mesh in flver.Meshes)
            {
                float x1 = float.MaxValue, x2 = float.MinValue, y1 = float.MaxValue, y2 = float.MinValue, z1 = float.MaxValue, z2 = float.MinValue;
                foreach (FLVER.Vertex vert in mesh.Vertices)
                {
                    x1 = Math.Min(vert.Position.X, x1);
                    y1 = Math.Min(vert.Position.Y, y1);
                    z1 = Math.Min(vert.Position.Z, z1);

                    x2 = Math.Max(vert.Position.X, x2);
                    y2 = Math.Max(vert.Position.Y, y2);
                    z2 = Math.Max(vert.Position.Z, z2);

                    X1 = Math.Min(vert.Position.X, X1);
                    Y1 = Math.Min(vert.Position.Y, Y1);
                    Z1 = Math.Min(vert.Position.Z, Z1);

                    X2 = Math.Max(vert.Position.X, X2);
                    Y2 = Math.Max(vert.Position.Y, Y2);
                    Z2 = Math.Max(vert.Position.Z, Z2);
                }
                mesh.BoundingBox = new();
                mesh.BoundingBox.Min = new Vector3(x1, y1, z1);
                mesh.BoundingBox.Max = new Vector3(x2, y2, z2);
            }
            rootNode.BoundingBoxMin = new Vector3(X1, Y1, Z1);
            rootNode.BoundingBoxMax = new Vector3(X2, Y2, Z2);
            flver.Header.BoundingBoxMin = rootNode.BoundingBoxMin;
            flver.Header.BoundingBoxMax = rootNode.BoundingBoxMax;

            /* Write flver */
            flver.Write(outputFilename);

            /* Generate collision obj */
            Obj obj = LANDSCAPEtoOBJ(landscape);

            string objPath = outputFilename.Replace(".flver", ".obj");
            CollisionInfo collisionInfo = new($"ext{landscape.coordinate.x},{landscape.coordinate.y}", $"terrain\\ext{landscape.coordinate.x},{landscape.coordinate.y}.obj");
            terrainInfo.collision = collisionInfo;

            obj.write(objPath);

            return terrainInfo;
        }
    }
}
