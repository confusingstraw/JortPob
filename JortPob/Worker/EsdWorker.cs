using JortPob.Common;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace JortPob.Worker
{
    public class EsdWorker : Worker
    {
        private NpcManager.EsdInfo esdInfo;

        public EsdWorker(NpcManager.EsdInfo esdInfo)
        {
            this.esdInfo = esdInfo;
            _thread = new Thread(Run);
            _thread.Start();
        }

        private void Run()
        {
            ExitCode = 1;

            ProcessStartInfo startInfo = new(Utility.ResourcePath(@"tools\ESDTool\esdtool.exe"), $"-er -basedir \"{Const.ELDEN_PATH}Game\" -moddir \"{Const.ELDEN_PATH}Game\\empty\" -i \"{esdInfo.py}\" -writeloose \"{esdInfo.esd}\"")
            {
                WorkingDirectory = Utility.ResourcePath(@"tools\ESDTool"),
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var process = Process.Start(startInfo);
            process.WaitForExit();

            Lort.TaskIterate();

            IsDone = true;
            ExitCode = 0;
        }

        public static void Go(List<NpcManager.EsdInfo> esds)
        {
            Lort.Log($"Compiling {esds.Count} ESDs...", Lort.Type.Main); // Very slow! Calling python sub programs to do stuff
            Lort.NewTask("Compiling ESDs", esds.Count);

            List<EsdWorker> workers = new();
            foreach (NpcManager.EsdInfo esdInfo in esds)
            {
                while (workers.Count >= Const.THREAD_COUNT)
                {
                    foreach (EsdWorker worker in workers)
                    {
                        if (worker.IsDone) { workers.Remove(worker); break; }
                    }

                    // wait...
                    Thread.Yield();
                }

                workers.Add(new EsdWorker(esdInfo));
            }

            /* Wait for threads to finish */
            while (true)
            {
                bool done = true;
                foreach (EsdWorker worker in workers)
                {
                    done &= worker.IsDone;
                }

                if (done)
                    break;

                // wait...
                Thread.Yield();
            }
        }
    }
}
