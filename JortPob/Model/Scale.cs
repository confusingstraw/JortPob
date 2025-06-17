using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JortPob.Model
{
    public class Scale
    {
        /* opens a flver, scales it, writes it */
        public static void FLVER(string flverPath, string outPath, float scale)
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
    }
}
