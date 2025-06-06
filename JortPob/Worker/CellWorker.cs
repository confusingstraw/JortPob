using JortPob.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace JortPob.Worker
{
    public class CellWorker : Worker
    {
        private ESM esm;
        private List<JsonNode> json;
        private int start;
        private int end;

        public List<Cell> cells;

        public CellWorker(ESM esm, List<JsonNode> json, int start, int end)
        {
            this.esm = esm;
            this.json = json;
            this.start = start;
            this.end = end;

            cells = new();

            _thread = new Thread(Parse);
            _thread.Start();
        }

        private void Parse()
        {
            ExitCode = 1;

            for (int i = start; i < Math.Min(json.Count, end); i++)
            {
                JsonNode node = json[i];
                if (Const.DEBUG_EXCLUSIVE_CELL_BUILD_BY_NAME != null && !(node["name"] != null && node["name"].ToString() == Const.DEBUG_EXCLUSIVE_CELL_BUILD_BY_NAME)) { continue; }

                Cell cell = new(esm, node);
                cells.Add(cell);
            }

            IsDone = true;
            ExitCode = 0;
        }
    }
}
