using JortPob.Common;
using JortPob.Model;
using SharpAssimp;
using SoulsFormats;
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

        private int start;
        private int end;

        public List<ModelInfo> models;

        public FlverWorker(MaterialContext materialContext, List<string> meshes, int start, int end)
        {
            this.materialContext= materialContext;
            this.meshes = meshes;

            this.start = start;
            this.end = end;

            models = new();

            _thread = new Thread(Run);
            _thread.Start();
        }

        private void Run()
        {
            ExitCode = 1;

            AssimpContext assimpContext = new();
            for (int i = start; i < Math.Min(meshes.Count, end); i++)
            {
                string mesh = meshes[i];

                string meshIn = $"{Const.MORROWIND_PATH}Data Files\\meshes\\{mesh.ToLower().Replace(".nif", ".fbx")}";
                string meshOut = $"{Const.CACHE_PATH}meshes\\{mesh.ToLower().Replace(".nif", ".flver").Replace(@"\", "_")}";
                ModelInfo modelInfo = new(mesh, $"meshes\\{mesh.ToLower().Replace(".nif", ".flver").Replace(@"\", "_")}");
                modelInfo = ModelConverter.FBXtoFLVER(assimpContext, materialContext, modelInfo, meshIn, meshOut);

                models.Add(modelInfo);

                Lort.TaskIterate(); // Progress bar update
            }
            assimpContext.Dispose();

            IsDone = true;
            ExitCode = 0;
        }

        public static List<ModelInfo> Go(MaterialContext materialContext, List<string> meshes)
        {
            Lort.Log($"Converting {meshes.Count} models...", Lort.Type.Main); // Not that slow but multithreading good
            Lort.NewTask("Converting FBX", meshes.Count);

            int partition = (int)Math.Ceiling(meshes.Count / (float)Const.THREAD_COUNT);
            List<FlverWorker> workers = new();
            for (int i = 0; i < Const.THREAD_COUNT; i++)
            {
                int start = i * partition;
                int end = start + partition;
                FlverWorker worker = new(materialContext, meshes, start, end);
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
