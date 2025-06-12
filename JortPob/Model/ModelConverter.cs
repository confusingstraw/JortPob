using HKLib.hk2018;
using HKLib.hk2018.hkHashMapDetail;
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
    partial class ModelConverter
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

            /* Add bones and nodes for FLVER */
            FLVER.Node rootNode = new();
            FLVER2.SkeletonSet skeletonSet = new();
            FLVER2.SkeletonSet.Bone rootBone = new(0);

            rootNode.Name = Path.GetFileNameWithoutExtension(fbxFilename);
            skeletonSet.AllSkeletons.Add(rootBone);
            skeletonSet.BaseSkeleton.Add(rootBone);
            flver.Nodes.Add(rootNode);
            flver.Skeletons = skeletonSet;

            /* Generate material data */
            List<MaterialContext.MaterialInfo> materialInfo = materialContext.GenerateMaterials(fbx.Materials);
            foreach(MaterialContext.MaterialInfo mat in materialInfo)
            {
                flver.Materials.Add(mat.material);
                flver.GXLists.Add(mat.gx);
                flver.BufferLayouts.Add(mat.layout);
                foreach (TextureInfo info in mat.info)
                {
                    modelInfo.textures.Add(info);
                }
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

            /* Calculate model size */
            float size = Vector3.Distance(rootNode.BoundingBoxMin, rootNode.BoundingBoxMax);
            modelInfo.size = size;

            /* Write flver */
            flver.Write(outputFilename);

            /* Generate collision obj */
            Obj obj = COLLISIONtoOBJ(fbxCollisions);

            string objPath = outputFilename.Replace(".flver", ".obj");
            CollisionInfo collisionInfo = new(modelInfo.name, objPath);
            modelInfo.collisions.Add(collisionInfo);

            obj.write(objPath);

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
    
        public static Obj COLLISIONtoOBJ(List<Tuple<Node, Mesh>> collisions)
        {
            Obj obj = new();

            foreach(Tuple<Node, Mesh> tuple in collisions)
            {
                Node node = tuple.Item1;
                Mesh mesh = tuple.Item2;

                ObjG g = new();

                /* Convert vert/face data */
                foreach (Face face in mesh.Faces)
                {
                    ObjV[] V = new ObjV[3];
                    for (int i = 0; i < 3; i++)
                    {
                        /* Grab vertice position + normals/tangents */
                        Vector3 pos = mesh.Vertices[face.Indices[i]];
                        Vector3 norm = mesh.Normals[face.Indices[i]];

                        /* Collapse transformations on positions and collapse rotations on normals/tangents */
                        Node parent = node;
                        while (parent != null)
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

                            pos = Vector3.Transform(pos, ms * mr * mt);
                            norm = Vector3.TransformNormal(norm, mr);

                            parent = parent.Parent;
                        }

                        // Fromsoftware lives in the mirror dimension. I do not know why.
                        pos = pos * Const.GLOBAL_SCALE;
                        pos.X *= -1f;
                        norm.X *= -1f;

                        /* Rotate Y 180 degrees because... */
                        Matrix4x4 rotateY180Matrix = Matrix4x4.CreateRotationY((float)Math.PI);
                        pos = Vector3.Transform(pos, rotateY180Matrix);

                        /* Rotate normals/tangents to match */
                        norm = Vector3.Normalize(Vector3.TransformNormal(norm, rotateY180Matrix));

                        /* Get tex coords */
                        Vector3 uvw;
                        if (mesh.TextureCoordinateChannelCount <= 0)
                        {
                            uvw = new Vector3(0, 0, 0);
                        }
                        else
                        {
                            uvw = mesh.TextureCoordinateChannels[0][face.Indices[i]];
                            uvw.Y *= -1f;
                        }

                        /* Set */
                        obj.vs.Add(pos);
                        obj.vns.Add(norm);
                        obj.vts.Add(uvw);

                        V[i] = new(obj.vs.Count-1, obj.vts.Count-1, obj.vns.Count-1);
                    }

                    ObjF F = new(V[0], V[1], V[2]);
                    g.fs.Add(F);
                }
                obj.gs.Add(g);
            }

            return obj;
        }

        public static Obj LANDSCAPEtoOBJ(Landscape landscape)
        {
            Obj obj = new();
            ObjG g = new();    // @TODO: currently just doing on collision material. need to subdivide by vertex texture index later and map materials

            List<ObjV> V = new();
            foreach (int index in landscape.indices)
            {
                Landscape.Vertex vertex = landscape.vertices[index];

                /* Grab vertice position + normal */
                Vector3 pos = new(vertex.position.X, vertex.position.Y, vertex.position.Z);
                Vector3 norm = new(vertex.normal.X, vertex.normal.Y, vertex.normal.Z);

                // Fromsoftware lives in the mirror dimension. I do not know why.
                pos.X *= -1f;
                norm.X *= -1f;

                // Get them tex coords
                Vector3 uvw = new(vertex.coordinate.X, -vertex.coordinate.Y, 0);

                /* Set */
                obj.vs.Add(pos);
                obj.vns.Add(norm);
                obj.vts.Add(uvw);

                ObjV v = new(obj.vs.Count - 1, obj.vts.Count - 1, obj.vns.Count - 1);
                V.Add(new(obj.vs.Count - 1, obj.vts.Count - 1, obj.vns.Count - 1));

                if (V.Count >= 3)
                {
                    ObjF F = new(V[2], V[1], V[0]);
                    g.fs.Add(F);

                    V.Clear();
                }
            }

            obj.gs.Add(g);

            return obj;
        }
    }
}
