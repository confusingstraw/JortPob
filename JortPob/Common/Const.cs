using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace JortPob.Common
{
    public static class Const
    {
        #region Paths
        public static readonly string MORROWIND_PATH = @"I:\SteamLibrary\steamapps\common\Morrowind\";
        public static readonly string OUTPUT_PATH = @"I:\SteamLibrary\steamapps\common\ELDEN RING\Game\mod\";
        #endregion

        #region Optimization
        public static readonly int THREAD_COUNT = 16;
        #endregion

        #region General
        public static readonly float GLOBAL_SCALE = 0.01f;
        public static readonly int CELL_EXTERIOR_BOUNDS = 30;
        public static readonly float CELL_SIZE = 8192f * GLOBAL_SCALE;
        public static readonly float TILE_SIZE = 256f;
        public static readonly int CELL_GRID_SIZE = 64;    // terrain vertices

        /* Calculated... ESM lowest cell is [-20,-20]~ on the grid. MSB lowest value is [+33,+40]~. Offset so they overlap */
        public static readonly Vector3 LAYOUT_COORDINATE_OFFSET = new((20*CELL_SIZE)+(35*TILE_SIZE), 0, (20*CELL_SIZE)+(38*TILE_SIZE));

        public static int CHUNK_PARTITION_SIZE = 6;

        public static readonly float CONTENT_SIZE_BIG = 7f;
        public static readonly float CONTENT_SIZE_HUGE = 20f;
        #endregion

        #region Debug
        public static readonly string DEBUG_EXCLUSIVE_CELL_BUILD_BY_NAME = "Seyda Neen"; // set to "null" to build entire map.
        public static readonly bool DEBUG_SKIP_INTERIOR = true;


        #endregion
    }
}
