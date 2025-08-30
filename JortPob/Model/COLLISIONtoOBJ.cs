using JortPob.Common;
using SharpAssimp;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace JortPob.Model
{
    public partial class ModelConverter
    {
        public static Obj COLLISIONtoOBJ(List<Tuple<Node, Mesh>> collisions, Obj.CollisionMaterial material)
        {
            Obj obj = new();

            foreach (Tuple<Node, Mesh> tuple in collisions)
            {
                Node node = tuple.Item1;
                Mesh mesh = tuple.Item2;

                ObjG g = new();
                g.name = material.ToString();
                g.mtl = $"hkm_{g.name}_Safe1";

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

                        V[i] = new(obj.vs.Count - 1, obj.vts.Count - 1, obj.vns.Count - 1);
                    }

                    ObjF F = new(V[2], V[1], V[0]);  // reverse indices going into collision. i don't know *why* but it works
                    g.fs.Add(F);
                }
                obj.gs.Add(g);
            }

            return obj;
        }
    }
}
