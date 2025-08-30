using SoulsFormats;
using System;
using System.Numerics;

namespace JortPob.Model
{
    public class BoundingBoxSolver
    {
        /* Solves bounding box of a flver */
        public static void FLVER(FLVER2 flver)
        {
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
            foreach(FLVER.Node node in flver.Nodes)                  // Some assumptions made here about nodes. probably wrong!
            {
                node.BoundingBoxMin = new Vector3(X1, Y1, Z1);
                node.BoundingBoxMax = new Vector3(X2, Y2, Z2);
            }
            flver.Header.BoundingBoxMin = new Vector3(X1, Y1, Z1);
            flver.Header.BoundingBoxMax = new Vector3(X2, Y2, Z2);
        }
    }
}
