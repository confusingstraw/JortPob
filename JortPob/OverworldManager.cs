using JortPob.Common;
using PortJob;
using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JortPob
{
    public class OverworldManager
    {
        /* Makes a modified version  of m60_00_00_99 */
        /* This msb is the "super overworld" an lod msb that is always visible at any distance in the overworld */
        /* It also contains things like the sky and water so yee */
        public static MSBE Generate(ESM esm, WaterInfo waterInfo)
        {
            MSBE msb = MSBE.Read(Utility.ResourcePath(@"msb\m60_00_00_99.msb.dcx"));

            /* Delete all vanilla map parts */
            msb.Parts.MapPieces.Clear();

            /* Delete all vanilla assets except the skyboxs */
            List<MSBE.Part.Asset> keep = new();
            foreach(MSBE.Part.Asset asset in msb.Parts.Assets)
            {
                int id1 = int.Parse(asset.Name.Substring(3, 3));
                int id2 = int.Parse(asset.Name.Substring(7, 3));

                // Sky stuff
                if (id1 == 96) { keep.Add(asset); continue; }
            }

            msb.Parts.Assets = keep;

            /* Calculate actual 0,0 of morrowind against the superoverworld root offset (DUMB!) */
            Cell cell = esm.GetCellByGrid(new Int2(0, 0));
            float x = (10 * 4f * Const.TILE_SIZE) + (Const.TILE_SIZE * 1.5f);
            float y = (8 * 4f * Const.TILE_SIZE) + (Const.TILE_SIZE * 1.5f);
            Vector3 center = (cell.center + Const.LAYOUT_COORDINATE_OFFSET) - new Vector3(x, 0, y);

            /* Add water */
            Vector3 TEST_OFFSET1 = new(0, 200, 0); // just shifting vertical position a bit so the morrowind map isn't super far down
            Vector3 TEST_OFFSET2 = new(0, -15, 0);
            MSBE.Part.Asset water = MakePart.Asset(waterInfo);
            water.Position = center + TEST_OFFSET1 + TEST_OFFSET2;
            msb.Parts.Assets.Add(water);

            msb.Models.Assets.Clear();
            msb.Models.MapPieces.Clear();
            AutoResource.Generate(60, 0, 0, 99, msb);

            return msb;
        }
    }
}
