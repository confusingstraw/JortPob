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
        public static ResourcePool Generate(Cache cache, ESM esm, Paramanager param)
        {
            MSBE msb = MSBE.Read(Utility.ResourcePath(@"msb\m60_00_00_99.msb.dcx"));
            LightManager lightManager = new(60, 00, 00, 99);
            ResourcePool pool = new(msb, lightManager);

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
            Vector3 center;
            if (cell != null)
            {
                float x = (10 * 4f * Const.TILE_SIZE) + (Const.TILE_SIZE * 1.5f);
                float y = (8 * 4f * Const.TILE_SIZE) + (Const.TILE_SIZE * 1.5f);
                center = (cell.center + Const.LAYOUT_COORDINATE_OFFSET) - new Vector3(x, 0, y);
            }
            else
            {
                // if we have the const debug flag set for building a specific cell or group of cells the esm 0,0 cell may not be loaded
                // in that case here is the correct value under normal circumstances, its better to calc it for safety but its debug so w/e
                center = new Vector3(15.360352f, 0f, 2831.3604f);
            }

            /* Add water */
            MSBE.Part.Asset water = MakePart.Asset(cache.GetWater());
            water.Position = center + Const.TEST_OFFSET1 + Const.TEST_OFFSET2;
            msb.Parts.Assets.Add(water);

            /* Add swamp */
            MSBE.Part.Asset swamp = MakePart.Asset(cache.GetSwamp());
            swamp.Position = center + Const.TEST_OFFSET1 + Const.TEST_OFFSET2;
            msb.Parts.Assets.Add(swamp);

            /* Add lava */
            MSBE.Part.Asset lava = MakePart.Asset(cache.GetLava());
            lava.Position = center + Const.TEST_OFFSET1 + Const.TEST_OFFSET2 + new Vector3(0f, Const.LAVA_VISUAL_OFFSET, 0f);
            msb.Parts.Assets.Add(lava);

            /* Add terrain */
            foreach (TerrainInfo terrainInfo in cache.terrains)
            {
                Vector3 position = new Vector3(terrainInfo.coordinate.x * Const.CELL_SIZE, 0, terrainInfo.coordinate.y * Const.CELL_SIZE) + center;

                MSBE.Part.MapPiece map = MakePart.MapPiece();
                map.Name = $"m{terrainInfo.id.ToString("D8")}_0000";
                map.ModelName = $"m{terrainInfo.id.ToString("D8")}";
                map.Position = position + Const.TEST_OFFSET1 + Const.TEST_OFFSET2;
                map.PartsDrawParamID = param.terrainDrawParamID;

                msb.Parts.MapPieces.Add(map);
                pool.mapIndices.Add(new Tuple<int, string>(terrainInfo.id, terrainInfo.path));
            }

            /* Regenerate resources */
            msb.Models.Assets.Clear();
            msb.Models.MapPieces.Clear();
            AutoResource.Generate(60, 0, 0, 99, msb);

            return pool;
        }
    }
}
