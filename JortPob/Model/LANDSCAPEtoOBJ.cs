using JortPob.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace JortPob.Model
{
    public partial class  ModelConverter
    {
        public static Obj LANDSCAPEtoOBJ(Landscape landscape)
        {
            Obj obj = new();
            ObjG g = new();    // @TODO: currently just doing on collision material. need to subdivide by vertex texture index later and map materials
            g.name = CollisionMaterial.Dirt.ToString();   // @TODO: just defaulting rn because I need to rewrite this if i want to support collision mats properly
            g.mtl = $"hkm_{g.name}_Safe1";

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
