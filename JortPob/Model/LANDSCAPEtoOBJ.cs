using HKLib.hk2018.hkaiNavVolumeDebugUtils;
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

            Dictionary<CollisionMaterial, ObjG> gs = new();

            ObjG GetWetG()
            {
                if(gs.ContainsKey(CollisionMaterial.Water))
                {
                    return gs[CollisionMaterial.Water];
                }

                ObjG g = new();
                g.name = CollisionMaterial.Water.ToString();
                g.mtl = $"hkm_{g.name}_Safe1";
                gs.Add(CollisionMaterial.Water, g);
                obj.gs.Add(g);
                return g;
            }

            ObjG GetG(string name, string path)
            {
                CollisionMaterial best = CollisionMaterial.None;
                void Guess(string[] guesses, CollisionMaterial material)
                {
                    if(best != CollisionMaterial.None) { return; }
                    foreach(string guess in guesses)
                    {
                        if (name.ToLower().Contains(guess))   // @TODO: there is an actual "name" field in the esm for landscapetexture. we should parse that later
                        {
                            best = material; break;
                        }
                    }
                }

                Guess(new string[] { "wood", "log", "bark" }, CollisionMaterial.Wood);
                Guess(new string[] { "sand" }, CollisionMaterial.Sand);
                Guess(new string[] { "rock", "stone", "boulder" }, CollisionMaterial.Rock);
                Guess(new string[] { "dirt", "soil", "grass", "mud", "moss" }, CollisionMaterial.Dirt);
                Guess(new string[] { "iron", "metal", "steel" }, CollisionMaterial.IronGrate);
                Guess(new string[] { "mushroom", }, CollisionMaterial.ScarletMushroom);
                Guess(new string[] { "statue", "adobe" }, CollisionMaterial.Rock);
                Guess(new string[] { "dwrv", "daed" }, CollisionMaterial.Rock);

                // Give up!
                if (best == CollisionMaterial.None) { best = CollisionMaterial.Stock; }
                
                /* Return objg of this material */
                if(gs.ContainsKey(best))
                {
                    return gs[best];
                }
                else
                {
                    ObjG g = new();
                    g.name = best.ToString();
                    g.mtl = $"hkm_{g.name}_Safe1";
                    gs.Add(best, g);
                    obj.gs.Add(g);
                    return g;
                }

            }

            List<ObjV> V = new();
            List<Landscape.Texture> T = new();
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

                T.Add(landscape.GetTexture(vertex.texture));

                if (V.Count >= 3)
                {
                    ObjF F = new(V[2], V[1], V[0]);

                    /* determine collision material of this triangle */
                    /* first check if its underwater */
                    bool underwater = true;
                    foreach(ObjV w in V)
                    {
                        if (obj.vs[w.v].Y >= Const.WATER_HEIGHT) { underwater = false; }
                    }

                    /* idea here is we count instances of texture. if any texture is used more than 1 time it's the most common since a tri only has 3 verts */
                    int[] c = new int[3];
                    for (int i = 0; i < T.Count; i++)
                    {
                        foreach (Landscape.Texture t in T)
                        {
                            if (t == T[i]) { c[i]++; }
                        }
                    }

                    Landscape.Texture best = null;
                    for (int i = 0; i < c.Length; i++)
                    {
                        if (c[i] > 1) { best = T[i]; }
                    }
                    if (best == null) { best = T[0]; }

                    ObjG g = underwater ? GetWetG() : GetG(best.name, best.path);
                    g.fs.Add(F);

                    V.Clear();
                    T.Clear();
                }
            }

            return obj;
        }
    }
}
