using JortPob.Common;
using JortPob.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace JortPob.Worker
{
    public class NavWorker
    {
        public static void Go(List<string> objs)
        {
            Lort.Log($"Converting {objs.Count} navmeshes...", Lort.Type.Main);     // Egregiously slow, multithreaded to make less terrible
            Lort.NewTask("Converting NAVs", objs.Count());

            /* Write navmesh settings */
            string navmeshSettingsPath = $@"{Const.CACHE_PATH}\NavmeshSettings.json";
            NavmeshUtilities.SaveNavmeshGenerationSettings(navmeshSettingsPath);

            var options = new ParallelOptions { MaxDegreeOfParallelism = Const.THREAD_COUNT }; // Crashes unless set to 1 ???

            Parallel.ForEach(Partitioner.Create(0, objs.Count()), options, range =>
            {
                ProcessNavs(navmeshSettingsPath, objs, range.Item1, range.Item2);
            });
        }

        protected static void ProcessNavs(string nvmSettings, List<string> objs, int start, int end)
        {
            int limit = Math.Min(objs.Count(), end);
            for (int i = start; i < limit; i++)
            {
                string objPath = objs[i];
                string hkxPath = Path.ChangeExtension(objPath, ".hkx");
                string navPath = Path.ChangeExtension(hkxPath, ".nav");
                Model.ModelConverter.OBJtoHKX(objPath, hkxPath);
                Model.ModelConverter.HKXtoNAV(hkxPath, navPath, nvmSettings);
                Lort.TaskIterate(); // Progress bar update
            }
        }
    }
}
