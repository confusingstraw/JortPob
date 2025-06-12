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
    public class FlverWorker : Worker
    {
        private MaterialContext materialContext;
        private List<string> meshes;

        private string cachePath;
        private string morrowindPath;
        private int start;
        private int end;

        public List<ModelInfo> models;

        public FlverWorker(MaterialContext materialContext, List<string> meshes, string morrowindPath, string cachePath, int start, int end)
        {
            this.materialContext= materialContext;
            this.meshes = meshes;
            this.cachePath = cachePath;
            this.morrowindPath = morrowindPath;

            this.start = start;
            this.end = end;

            models = new();

            _thread = new Thread(Parse);
            _thread.Start();
        }

        private void Parse()
        {
            ExitCode = 1;

            AssimpContext assimpContext = new();
            for (int i = start; i < Math.Min(meshes.Count, end); i++)
            {
                string mesh = meshes[i];

                string meshIn = $"{morrowindPath}meshes\\{mesh.ToLower().Replace(".nif", ".fbx")}";
                string meshOut = $"{cachePath}meshes\\{mesh.ToLower().Replace(".nif", ".flver").Replace(@"\", "_")}";
                ModelInfo modelInfo = new(mesh, $"meshes\\{mesh.ToLower().Replace(".nif", ".flver").Replace(@"\", "_")}");
                modelInfo = ModelConverter.FBXtoFLVER(assimpContext, materialContext, modelInfo, meshIn, meshOut);

                models.Add(modelInfo);
            }
            assimpContext.Dispose();

            IsDone = true;
            ExitCode = 0;
        }

        public static List<ModelInfo> Go(MaterialContext materialContext, List<string> meshes, string morrowindPath, string cachePath)
        {
            Console.WriteLine($"Converting {meshes.Count} models... t[{Const.THREAD_COUNT}]"); // Not that slow but multithreading good

            int partition = (int)Math.Ceiling(meshes.Count / (float)Const.THREAD_COUNT);
            List<FlverWorker> workers = new();
            for (int i = 0; i < Const.THREAD_COUNT; i++)
            {
                int start = i * partition;
                int end = start + partition;
                FlverWorker worker = new(materialContext, meshes, morrowindPath, cachePath, start, end);
                workers.Add(worker);
            }

            /* Wait for threads to finish */
            while (true)
            {
                bool done = true;
                foreach (FlverWorker worker in workers)
                {
                    done &= worker.IsDone;
                }

                if (done)
                    break;
            }

            /* Merge output */
            List<ModelInfo> models = new();
            foreach (FlverWorker worker in workers)
            {
                models.AddRange(worker.models);
            }

            return models;
        }
    }
}
