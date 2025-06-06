using JortPob.Common;
using SharpAssimp;
using SharpAssimp.Configs;
using SoulsFormats;
using SoulsFormats.Formats.Other.MWC;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.IO;
using System.Numerics;

namespace JortPob.Model
{
    class ModelConverter
    {
        public static ModelInfo FBXtoFLVER(AssimpContext assimpContext, MaterialContext materialContext, ModelInfo modelInfo, string fbxFilename, string outputFilename)
        {
            //Console.WriteLine($"Converting FBX: {fbxFilename}");

            /* Load FBX file via Assimp */
            Scene fbx = assimpContext.ImportFile(fbxFilename, PostProcessSteps.CalculateTangentSpace);

            /* Create a blank FLVER configured for Elden Ring */
            FLVER2 flver = new();
            flver.Header.Version = 131098; // Elden Ring FLVER Version Number
            flver.Header.Unk5D = 0;        // Unk
            flver.Header.Unk68 = 4;        // Unk
            flver.Header.BoundingBoxMin = new Vector3(-10, -10, -10); // @TODO: DEBUG!
            flver.Header.BoundingBoxMax = new Vector3(10, 10, 10);

            /* Add bones and nodes for FLVER */
            FLVER.Node rootNode = new();
            FLVER2.SkeletonSet skeletonSet = new();
            FLVER2.SkeletonSet.Bone rootBone = new(0);

            rootNode.Name = Path.GetFileNameWithoutExtension(fbxFilename);
            skeletonSet.AllSkeletons.Add(rootBone);
            skeletonSet.BaseSkeleton.Add(rootBone);
            flver.Nodes.Add(rootNode);
            flver.Skeletons = skeletonSet;

            rootNode.BoundingBoxMin = new Vector3(-10, -10, -10); // @TODO: DEBUG!
            rootNode.BoundingBoxMax = new Vector3(10, 10, 10);

            /* Generate material data */
            List<MaterialContext.MaterialInfo> materialInfo = materialContext.GenerateMaterials(fbx.Materials);
            foreach(MaterialContext.MaterialInfo mat in materialInfo)
            {
                flver.Materials.Add(mat.material);
                flver.GXLists.Add(mat.gx);
                flver.BufferLayouts.Add(mat.layout);
                modelInfo.textures.Add(mat.info);
            }

            /* Iterate scene hierarchy and identify and sort collision and render meshes and also identify and collect useful nodes for use as dummies */
            List<Tuple<string, Vector3>> nodes = new(); // FBX nodes that we will use as dummies
            List<Tuple<Node,Mesh>> fbxMeshes = new();
            List<Tuple<Node, Mesh>> fbxCollisions = new();

            void FBXHierarchySearch(Node fbxParentNode, bool isCollision)
            {
                foreach (Node fbxChildNode in fbxParentNode.Children)
                {
                    string nodename = fbxChildNode.Name.ToLower();
                    if (nodename.Trim().ToLower() == "collision")
                    {
                        isCollision = true;
                    }
                    if (nodename.Contains("attachlight") || nodename.Contains("emitter"))
                    {
                        // nodes.Add(new(nodename, fbxComponent.AbsoluteTransform.Translation * GLOBAL_SCALE)); // @TODO: dummies!
                    }
                    if (fbxChildNode.HasMeshes)
                    {
                        foreach(int fbxMeshIndex in fbxChildNode.MeshIndices)
                        {
                            if (isCollision) { fbxCollisions.Add(new Tuple<Node, Mesh>(fbxChildNode, fbx.Meshes[fbxMeshIndex])); }
                            else { fbxMeshes.Add(new Tuple<Node, Mesh>(fbxChildNode, fbx.Meshes[fbxMeshIndex])); }
                        }
                    }
                    if (fbxChildNode.HasChildren)
                    {
                        FBXHierarchySearch(fbxChildNode, isCollision);
                    }
                }
            }
            FBXHierarchySearch(fbx.RootNode, false);

            /* Convert meshes */
            int index = 0;
            foreach (Tuple<Node, Mesh> tuple in fbxMeshes)
            {
                Node node = tuple.Item1;
                Mesh fbxMesh = tuple.Item2;

                /* Generate blank flver mesh and faceset */
                FLVER2.Mesh flverMesh = new();
                FLVER2.FaceSet flverFaces = new();
                flverMesh.FaceSets.Add(flverFaces);
                flverFaces.CullBackfaces = true;
                flverFaces.Unk06 = 1;
                flverMesh.NodeIndex = 0; // attach to rootnode
                flverMesh.MaterialIndex = index++;

                /* Setup Vertex Buffer */
                FLVER2.VertexBuffer flverBuffer = new(0);
                flverMesh.VertexBuffers.Add(flverBuffer);

                /* Spit out some warnings */
                if (fbxMesh.TextureCoordinateChannelCount <= 0) { Console.WriteLine($"## WARNING ## {rootNode.Name}->{fbxMesh.Name} has no UV channels!"); }
                else if (fbxMesh.TextureCoordinateChannelCount > 1) { Console.WriteLine($"## WARNING ## {rootNode.Name}->{fbxMesh.Name} has multiple UV channels!"); }

                /* Convert vert/face data */
                if(fbxMesh.Tangents.Count <= 0)
                {
                    Console.WriteLine($"## WARNING ## {rootNode.Name}->{fbxMesh.Name} has no tangent data!");
                }
                foreach (Face fbxFace in fbxMesh.Faces)          // @TODO: possible optimization of reusing duplicate vertices, for now just making all indices 1 to 1 for simplicity
                {
                    for(int i=0;i<3;i++)
                    {
                        FLVER.Vertex flverVertex = new();

                        /* Grab vertice position + normals/tangents */
                        Vector3 pos = fbxMesh.Vertices[fbxFace.Indices[i]];
                        Vector3 norm = fbxMesh.Normals[fbxFace.Indices[i]];
                        Vector3 tang;
                        Vector3 bitang;
                        if (fbxMesh.Tangents.Count > 0)
                        {
                            tang = fbxMesh.Tangents[fbxFace.Indices[i]];
                            bitang = fbxMesh.BiTangents[fbxFace.Indices[i]];
                        }
                        else
                        {
                            tang = new Vector3(1, 0, 0);
                            bitang = new Vector3(0, 0, 1);
                        }

                        /* Collapse transformations on positions and collapse rotations on normals/tangents */
                        Node parent = node;
                        while(parent != null)
                        {
                            Vector3 translation;
                            Quaternion rotation;
                            Vector3 scale;
                            Matrix4x4.Decompose(parent.Transform, out scale, out rotation, out translation);
                            translation = new Vector3(parent.Transform.M14, parent.Transform.M24, parent.Transform.M34); // Hack

                            rotation = Quaternion.Inverse(rotation);

                            Matrix4x4 ms = Matrix4x4.CreateScale(scale);
                            Matrix4x4 mr = Matrix4x4.CreateFromQuaternion(rotation);
                            Matrix4x4 mt = Matrix4x4.CreateTranslation(translation);

                            pos = Vector3.Transform(pos, ms*mr*mt);
                            norm = Vector3.TransformNormal(norm, mr);
                            tang = Vector3.TransformNormal(tang, mr);
                            bitang = Vector3.TransformNormal(bitang, mr);

                            parent = parent.Parent;
                        }

                        // Fromsoftware lives in the mirror dimension. I do not know why.
                        pos = pos * Const.GLOBAL_SCALE;
                        pos.X *= -1f; 
                        norm.X *= -1f;
                        tang.X *= -1f;
                        bitang.X *= -1f;

                        /* Rotate Y 180 degrees because... */
                        Matrix4x4 rotateY180Matrix = Matrix4x4.CreateRotationY((float)Math.PI);
                        pos = Vector3.Transform(pos, rotateY180Matrix);

                        /* Rotate normals/tangents to match */
                        norm = Vector3.Normalize( Vector3.TransformNormal(norm, rotateY180Matrix));
                        tang = Vector3.Normalize(Vector3.TransformNormal(tang, rotateY180Matrix));
                        bitang = Vector3.Normalize(Vector3.TransformNormal(bitang, rotateY180Matrix));

                        // Set ...
                        flverVertex.Position = pos;
                        flverVertex.Normal = norm;
                        if(fbxMesh.TextureCoordinateChannelCount <= 0)
                        {
                            flverVertex.UVs.Add(new Vector3(0,0,0));
                        }
                        else {
                            Vector3 uvw = fbxMesh.TextureCoordinateChannels[0][fbxFace.Indices[i]];
                            uvw.Y *= -1f;
                            flverVertex.UVs.Add(uvw);
                        }
                        flverVertex.Bitangent = new Vector4(bitang.X, bitang.Y, bitang.Z, 0);
                        flverVertex.Tangents.Add(new Vector4(tang.X, tang.Y, tang.Z, 0));
                        if (fbxMesh.HasVertexColors(0))
                        {
                            Vector4 color = fbxMesh.VertexColorChannels[0][fbxFace.Indices[i]];
                            flverVertex.Colors.Add(new FLVER.VertexColor(color.W, color.X, color.Y, color.Z));
                        }
                        else
                        {
                            flverVertex.Colors.Add(new FLVER.VertexColor(255, 255, 255, 255));
                        }

                        flverMesh.Vertices.Add(flverVertex);
                        flverFaces.Indices.Add(flverMesh.Vertices.Count-1);
                    }
                }

                flver.Meshes.Add(flverMesh);
            }

            /* Calculate bounding boxes */
            float X1 = 0f, X2 = 0f, Y1 = 0f, Y2 = 0f, Z1 = 0f, Z2 = 0f;
            foreach (FLVER2.Mesh mesh in flver.Meshes)
            {
                float x1 = 0f, x2 = 0f, y1 = 0f, y2 = 0f, z1 = 0f, z2 = 0f;
                foreach(FLVER.Vertex vert in mesh.Vertices)
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

            /* Calculate model size */
            float size = Vector3.Distance(rootNode.BoundingBoxMin, rootNode.BoundingBoxMax);
            modelInfo.size = size;

            /* Write to file for testing! */
            flver.Write(outputFilename);

            return modelInfo;
        }

        public static TerrainInfo LANDSCAPEtoFLVER(MaterialContext materialContext, TerrainInfo terrainInfo, Landscape landscape, string outputFilename)
        {
            Console.WriteLine($"Converting Landscape: [{landscape.coordinate.x},{landscape.coordinate.y}]");

            /* Create a blank FLVER configured for Elden Ring */
            FLVER2 flver = new();
            flver.Header.Version = 131098; // Elden Ring FLVER Version Number
            flver.Header.Unk5D = 0;        // Unk
            flver.Header.Unk68 = 4;        // Unk
            flver.Header.BoundingBoxMin = new Vector3(-10, -10, -10); // @TODO: DEBUG!
            flver.Header.BoundingBoxMax = new Vector3(10, 10, 10);

            /* Add bones and nodes for FLVER */
            FLVER.Node rootNode = new();
            FLVER2.SkeletonSet skeletonSet = new();
            FLVER2.SkeletonSet.Bone rootBone = new(0);

            rootNode.Name = Path.GetFileNameWithoutExtension($"terrain-{terrainInfo.path}");
            skeletonSet.AllSkeletons.Add(rootBone);
            skeletonSet.BaseSkeleton.Add(rootBone);
            flver.Nodes.Add(rootNode);
            flver.Skeletons = skeletonSet;

            rootNode.BoundingBoxMin = new Vector3(-10, -10, -10); // @TODO: DEBUG!
            rootNode.BoundingBoxMax = new Vector3(10, 10, 10);

            /* Generate material data */
            List<MaterialContext.MaterialInfo> materialInfo = materialContext.GenerateMaterials(landscape);
            foreach (MaterialContext.MaterialInfo mat in materialInfo)
            {
                flver.Materials.Add(mat.material);
                flver.GXLists.Add(mat.gx);
                flver.BufferLayouts.Add(mat.layout);
                terrainInfo.textures.Add(mat.info);
            }

            /* Generate blank flver mesh and faceset */
                FLVER2.Mesh flverMesh = new();
                FLVER2.FaceSet flverFaces = new();
                flverMesh.FaceSets.Add(flverFaces);
                flverFaces.CullBackfaces = true;
                flverFaces.Unk06 = 1;
                flverMesh.NodeIndex = 0; // attach to rootnode
                flverMesh.MaterialIndex = 0;

                /* DEBUG @TODO: BOUNDING BOX */
                flverMesh.BoundingBox = new FLVER2.Mesh.BoundingBoxes();
                flverMesh.BoundingBox.Min = new Vector3(-10, -10, -10);
                flverMesh.BoundingBox.Max = new Vector3(10, 10, 10);

                /* Setup Vertex Buffer */
                FLVER2.VertexBuffer flverBuffer = new(0);
                flverMesh.VertexBuffers.Add(flverBuffer);

                /* Convert vert/face data */
                foreach (int index in landscape.indices)
                {
                    FLVER.Vertex flverVertex = new();
                    Landscape.Vertex vertex = landscape.vertices[index];

                    /* Grab vertice position + normal */
                    Vector3 pos = new(vertex.position.X, vertex.position.Y, vertex.position.Z);
                    Vector3 norm = new(vertex.normal.X, vertex.normal.Y, vertex.normal.Z);

                    // Fromsoftware lives in the mirror dimension. I do not know why.
                    pos.X *= -1f;
                    norm.X *= -1f;

                    // Set ...
                    flverVertex.Position = pos;
                    flverVertex.Normal = norm;

                    Vector3 uvw = new(vertex.coordinate.X *16f, -vertex.coordinate.Y * 16f, 0);
                    flverVertex.UVs.Add(uvw);

                    flverVertex.Bitangent = new Vector4(0, 0, 1, 1);  // @TODO: WRONG!
                    flverVertex.Tangents.Add(new Vector4(1, 0, 0, 1));  // @TODO: WRONG!
                    flverVertex.Colors.Add(new FLVER.VertexColor(vertex.color.x, vertex.color.y, vertex.color.z, vertex.color.w));

                    flverMesh.Vertices.Add(flverVertex);
                    flverFaces.Indices.Add(flverMesh.Vertices.Count - 1);
                }

                flver.Meshes.Add(flverMesh);

            /* Calculate bounding boxes */
            float X1 = 0f, X2 = 0f, Y1 = 0f, Y2 = 0f, Z1 = 0f, Z2 = 0f;
            foreach (FLVER2.Mesh mesh in flver.Meshes)
            {
                float x1 = 0f, x2 = 0f, y1 = 0f, y2 = 0f, z1 = 0f, z2 = 0f;
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

            /* Write to file for testing! */
            flver.Write(outputFilename);

            return terrainInfo;
        }
    }
}
