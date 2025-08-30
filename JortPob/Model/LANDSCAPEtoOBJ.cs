using JortPob.Common;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace JortPob.Model
{
    public partial class  ModelConverter
    {
        public static Obj LANDSCAPEtoOBJ(Landscape landscape)
        {
            Obj obj = new();

            Dictionary<Obj.CollisionMaterial, ObjG> gs = new();

            ObjG GetWetG()
            {
                if(gs.ContainsKey(Obj.CollisionMaterial.Water))
                {
                    return gs[Obj.CollisionMaterial.Water];
                }

                ObjG g = new();
                g.name = Obj.CollisionMaterial.Water.ToString();
                g.mtl = $"hkm_{g.name}_Safe1";
                gs.Add(Obj.CollisionMaterial.Water, g);
                obj.gs.Add(g);
                return g;
            }

            ObjG GetSwampG()
            {
                if (gs.ContainsKey(Obj.CollisionMaterial.PoisonSwamp))
                {
                    return gs[Obj.CollisionMaterial.PoisonSwamp];
                }

                ObjG g = new();
                g.name = Obj.CollisionMaterial.PoisonSwamp.ToString();
                g.mtl = $"hkm_{g.name}_Safe1";
                gs.Add(Obj.CollisionMaterial.PoisonSwamp, g);
                obj.gs.Add(g);
                return g;
            }

            ObjG GetLavaG()
            {
                if (gs.ContainsKey(Obj.CollisionMaterial.Lava))
                {
                    return gs[Obj.CollisionMaterial.Lava];
                }

                ObjG g = new();
                g.name = Obj.CollisionMaterial.Lava.ToString();
                g.mtl = $"hkm_{g.name}_Safe1";
                gs.Add(Obj.CollisionMaterial.Lava, g);
                obj.gs.Add(g);
                return g;
            }

            ObjG GetG(string name, string path)
            {
                Obj.CollisionMaterial best = Obj.CollisionMaterial.None;
                void Guess(string[] guesses, Obj.CollisionMaterial material)
                {
                    if(best != Obj.CollisionMaterial.None) { return; }
                    foreach(string guess in guesses)
                    {
                        if (name.ToLower().Contains(guess))   // @TODO: there is an actual "name" field in the esm for landscapetexture. we should parse that later
                        {
                            best = material; break;
                        }
                    }
                }

                Guess(new string[] { "wood", "log", "bark" }, Obj.CollisionMaterial.Wood);
                Guess(new string[] { "sand" }, Obj.CollisionMaterial.Sand);
                Guess(new string[] { "rock", "stone", "boulder" }, Obj.CollisionMaterial.Rock);
                Guess(new string[] { "dirt", "soil", "grass", "mud", "moss" }, Obj.CollisionMaterial.Dirt);
                Guess(new string[] { "iron", "metal", "steel" }, Obj.CollisionMaterial.IronGrate);
                Guess(new string[] { "mushroom", }, Obj.CollisionMaterial.ScarletMushroom);
                Guess(new string[] { "statue", "adobe" }, Obj.CollisionMaterial.Rock);
                Guess(new string[] { "dwrv", "daed" }, Obj.CollisionMaterial.Rock);

                // Give up!
                if (best == Obj.CollisionMaterial.None) { best = Obj.CollisionMaterial.Stock; }
                
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
            List<Landscape.Vertex> lastVerts = new();
            List<Landscape.Texture> T = new();
            foreach (int index in landscape.indices[0])   // use indice set 0 for collision
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
                lastVerts.Add(vertex);

                T.Add(landscape.GetTexture(vertex.texture));

                if (V.Count >= 3)
                {
                    ObjF F = new(V[2], V[1], V[0]);

                    /* determine collision material of this triangle */
                    /* first check if the landscape vertices have a special material flag */
                    bool lava = true, water = true, swamp = true;
                    for(int i=0;i<lastVerts.Count();i++)
                    {
                        Landscape.Vertex last = lastVerts[i];
                        if (!last.underwater) { water = false; }
                        if (!last.lava) { lava = false; }
                        if (!last.swamp) { swamp = false; }
                    }

                    ObjG g;
                    if (lava) { g = GetLavaG(); }
                    else if (swamp) { g = GetSwampG(); }
                    else if (water) { g = GetWetG(); }
                    else {
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

                        g = GetG(best.name, best.path);
                    }
                    g.fs.Add(F);

                    V.Clear();
                    T.Clear();
                    lastVerts.Clear();
                }
            }

            return obj;
        }
    
        public static Obj LANDSCAPEtoOBJ_DEBUG(Landscape landscape)
        {
            Obj obj = new();

            for (int i = 0; i < Const.TERRAIN_LOD_VALUES.Length; i++)
            {
                ObjG g = new();    // @TODO: currently just doing on collision material. need to subdivide by vertex texture index later and map materials
                g.name = Const.TERRAIN_LOD_VALUES[i].FLAG.ToString();
                g.mtl = "default";

                List<ObjV> V = new();
                foreach (int index in landscape.indices[i])
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
            }

            return obj;
        }
    }
}
