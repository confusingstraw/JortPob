using JortPob.Common;
using JortPob.Model;
using SharpAssimp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JortPob.Worker
{
    public class LandscapeWorker : Worker
    {
        private MaterialContext materialContext;
        private ESM esm;

        private string cachePath;
        private string morrowindPath;
        private int start;
        private int end;

        public List<TerrainInfo> terrains;

        public LandscapeWorker(MaterialContext materialContext, ESM esm, string cachePath, int start, int end)
        {
            this.materialContext = materialContext;
            this.esm = esm;
            this.cachePath = cachePath;

            this.start = start;
            this.end = end;

            terrains = new();

            _thread = new Thread(Parse);
            _thread.Start();
        }

        private void Parse()
        {
            ExitCode = 1;

            for (int i = start; i < Math.Min(esm.exterior.Count, end); i++)
            {
                Cell cell = esm.exterior[i];
                Landscape landscape = esm.GetLandscape(cell.coordinate);
                if (landscape == null) { continue; }

                TerrainInfo terrainInfo = new(landscape.coordinate, $"terrain\\ext{landscape.coordinate.x},{landscape.coordinate.y}.flver");
                terrainInfo = ModelConverter.LANDSCAPEtoFLVER(materialContext, terrainInfo, landscape, $"{cachePath}terrain\\ext{landscape.coordinate.x},{landscape.coordinate.y}.flver");

                terrains.Add(terrainInfo);
            }

            IsDone = true;
            ExitCode = 0;
        }

        public static List<TerrainInfo> Go(MaterialContext materialContext, ESM esm, string cachePath)
        {
            Console.WriteLine($"Converting {esm.exterior.Count} landscapes... t[{Const.THREAD_COUNT}]"); // Not that slow but multithreading good

            int partition = (int)Math.Ceiling(esm.exterior.Count / (float)Const.THREAD_COUNT);
            List<LandscapeWorker> workers = new();
            for (int i = 0; i < Const.THREAD_COUNT; i++)
            {
                int start = i * partition;
                int end = start + partition;
                LandscapeWorker worker = new(materialContext, esm, cachePath, start, end);
                workers.Add(worker);
            }

            /* Wait for threads to finish */
            while (true)
            {
                bool done = true;
                foreach (LandscapeWorker worker in workers)
                {
                    done &= worker.IsDone;
                }

                if (done)
                    break;
            }

            /* Merge output */
            List<TerrainInfo> terrains = new();
            foreach (LandscapeWorker worker in workers)
            {
                terrains.AddRange(worker.terrains);
            }

            return terrains;
        }
    }
}
