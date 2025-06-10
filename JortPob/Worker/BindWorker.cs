using JortPob.Common;
using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace JortPob.Worker
{
    public class BindWorker : Worker
    {
        private Cache cache;
        private string cachePath;
        private string outPath;
        private int start;
        private int end;

        public BindWorker(Cache cache, string cachePath, string outPath, int start, int end)
        {
            this.cache = cache;
            this.cachePath = cachePath;
            this.outPath = outPath;

            this.start = start;
            this.end = end;

            _thread = new Thread(Parse);
            _thread.Start();
        }

        private void Parse()
        {
            ExitCode = 1;

            for (int i = start; i < Math.Min(cache.assets.Count, end); i++)
            {
                ModelInfo modelInfo = cache.assets[i];
                Bind.BindAsset(modelInfo, cachePath, $"{outPath}asset\\aeg\\{modelInfo.AssetPath()}.geombnd.dcx");
            }

            IsDone = true;
            ExitCode = 0;
        }
    }
}
