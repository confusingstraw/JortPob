using JortPob.Common;
using SharpAssimp;
using SoulsFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace JortPob.Model
{
    public partial class ModelConverter
    {
        public static ModelInfo FBXtoFLVER(AssimpContext assimpContext, MaterialContext materialContext, ModelInfo modelInfo, bool forceCollision, string fbxFilename, string outputFilename)
        {
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
            foreach (MaterialContext.MaterialInfo mat in materialInfo)
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
            List<Tuple<Node, Mesh>> fbxMeshes = new();
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
                        foreach (int fbxMeshIndex in fbxChildNode.MeshIndices)
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
                if (fbxMesh.TextureCoordinateChannelCount <= 0) { Lort.Log($"## WARNING ## {rootNode.Name}->{fbxMesh.Name} has no UV channels!", Lort.Type.Debug); }
                else if (fbxMesh.TextureCoordinateChannelCount > 1) { Lort.Log($"## WARNING ## {rootNode.Name}->{fbxMesh.Name} has multiple UV channels!", Lort.Type.Debug); }

                /* Convert vert/face data */
                if (fbxMesh.Tangents.Count <= 0)
                {
                    Lort.Log($"## WARNING ## {rootNode.Name}->{fbxMesh.Name} has no tangent data!", Lort.Type.Debug);
                }
                foreach (Face fbxFace in fbxMesh.Faces)          // @TODO: possible optimization of reusing duplicate vertices, for now just making all indices 1 to 1 for simplicity
                {
                    for (int i = 0; i < 3; i++)
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
                        norm = Vector3.Normalize(Vector3.TransformNormal(norm, rotateY180Matrix));
                        tang = Vector3.Normalize(Vector3.TransformNormal(tang, rotateY180Matrix));
                        bitang = Vector3.Normalize(Vector3.TransformNormal(bitang, rotateY180Matrix));

                        // Set ...
                        flverVertex.Position = pos;
                        flverVertex.Normal = norm;
                        if (fbxMesh.TextureCoordinateChannelCount <= 0)
                        {
                            flverVertex.UVs.Add(new Vector3(0, 0, 0));
                        }
                        else
                        {
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
                        flverFaces.Indices.Add(flverMesh.Vertices.Count - 1);
                    }
                }

                flver.Meshes.Add(flverMesh);
            }

            /* Calculate bounding boxes */
            BoundingBoxSolver.FLVER(flver);

            /* Optimize flver */
            flver = FLVERUtil.Optimize(flver);

            /* Calculate model size */
            float size = Vector3.Distance(rootNode.BoundingBoxMin, rootNode.BoundingBoxMax);
            modelInfo.size = size;

            /* Write flver */
            flver.Write(outputFilename);

            
            /* Load overrides list for collision */
            JsonNode json = JsonNode.Parse(File.ReadAllText(Utility.ResourcePath(@"overrides\static_collision.json")));
            bool CheckOverride(string name)
            {
                foreach(JsonNode node in json.AsArray())
                {
                    if (node.ToString().ToLower() == name) { return true; }
                }
                return false;
            }

            /* Generate collision obj if the model contains a collision mesh */
            if ((fbxCollisions.Count > 0 || forceCollision) && !CheckOverride(modelInfo.name))
            {
                /* Best guess for collision material */
                CollisionMaterial matguess = CollisionMaterial.None;
                void Guess(string[] keys, CollisionMaterial type)
                {
                    if (matguess != CollisionMaterial.None) { return; }
                    foreach (Material mat in fbx.Materials)
                    {
                        foreach (string key in keys)
                        {
                            if (Utility.PathToFileName(modelInfo.name).ToLower().Contains(key)) { matguess = type; return; }
                            if (mat.Name.ToLower().Contains(key)) { matguess = type; return; }
                            if (mat.TextureDiffuse.FilePath != null && Utility.PathToFileName(mat.TextureDiffuse.FilePath).ToLower().Contains(key)) { matguess = type; return; }
                        }
                    }
                    return;
                }

                /* This is a hierarchy, first found keyword determines collision type, more obvious keywords at the top, niche ones at the bottom */
                Guess(new string[] { "wood", "log", "bark" }, CollisionMaterial.Wood);
                Guess(new string[] { "sand" }, CollisionMaterial.Sand);
                Guess(new string[] { "rock", "stone", "boulder" }, CollisionMaterial.Rock);
                Guess(new string[] { "dirt", "soil", "grass" }, CollisionMaterial.Dirt);
                Guess(new string[] { "iron", "metal", "steel" }, CollisionMaterial.IronGrate);
                Guess(new string[] { "mushroom", }, CollisionMaterial.ScarletMushroom);
                Guess(new string[] { "statue", "adobe" }, CollisionMaterial.Rock);
                Guess(new string[] { "dwrv", "daed" }, CollisionMaterial.Rock);

                // Give up!
                if (matguess == CollisionMaterial.None) { matguess = CollisionMaterial.Stock; }

                /* If the model doesnt have an explicit collision mesh but forceCollision is on because it's a static, we use the visual mesh as a collision mesh */
                Obj obj = COLLISIONtoOBJ(fbxCollisions.Count > 0 ? fbxCollisions : fbxMeshes, matguess);
                if (fbxCollisions.Count <= 0) { Lort.Log($"{modelInfo.name} had forced collision gen...", Lort.Type.Debug); }

                /* Make obj file for collision. These will be converted to HKX later */
                string objPath = outputFilename.Replace(".flver", ".obj");
                CollisionInfo collisionInfo = new(modelInfo.name, $"meshes\\{Utility.PathToFileName(objPath)}.obj");
                modelInfo.collision = collisionInfo;

                obj = obj.optimize();
                obj.write(objPath);
            }

            return modelInfo;
        }
    }
}
