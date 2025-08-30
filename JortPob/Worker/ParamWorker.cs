using JortPob.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using WitchyFormats;
using static JortPob.Paramanager;

namespace JortPob.Worker
{
    public class ParamWorker : Worker
    {
        private SoulsFormats.BinderFile file;
        private Dictionary<ParamDefType, WitchyFormats.PARAMDEF> paramdefs;

        public Paramanager.ParamType type;
        public FsParam param;

        public ParamWorker(SoulsFormats.BinderFile file, Dictionary<ParamDefType, WitchyFormats.PARAMDEF> paramdefs)
        {
            this.file = file;
            this.paramdefs = paramdefs;
            _thread = new Thread(Run);
            _thread.Start();
        }

        private void Run()
        {
            ExitCode = 1;

            FsParam p = FsParam.Read(file.Bytes);
            ParamDefType ty = (ParamDefType)Enum.Parse(typeof(ParamDefType), p.ParamType);
            ParamType ty2 = (ParamType)Enum.Parse(typeof(ParamType), Utility.PathToFileName(file.Name));
            p.ApplyParamdef(paramdefs[ty]);
            param = p;
            type = ty2;

            Lort.TaskIterate();

            IsDone = true;
            ExitCode = 0;
        }

        public static Dictionary<ParamType, FsParam> Go(SoulsFormats.BND4 paramBnd, Dictionary<ParamDefType, WitchyFormats.PARAMDEF> paramdefs)
        {
            Lort.Log($"Loading {paramBnd.Files.Count()} PARAMs...", Lort.Type.Main);
            Lort.NewTask("Loading PARAMs", paramBnd.Files.Count());

            Dictionary<ParamType, FsParam> param = new();
            List<ParamWorker> workers = new();
            foreach (SoulsFormats.BinderFile file in paramBnd.Files)
            {
                while (workers.Count >= Const.THREAD_COUNT)
                {
                    foreach (ParamWorker worker in workers)
                    {
                        if (worker.IsDone)
                        {
                            workers.Remove(worker);
                            param.Add(worker.type, worker.param);
                            break;
                        }
                    }

                    // wait...
                    Thread.Yield();
                }

                workers.Add(new ParamWorker(file, paramdefs));
            }

            /* Wait for threads to finish */
            for (int i = 0; workers.Count() > 0; i++)
            {
                if (i >= workers.Count()) { i = 0; }
                ParamWorker worker = workers[i];
                if (worker.IsDone)
                {
                    workers.Remove(worker);
                    param.Add(worker.type, worker.param);
                    i--;
                }

                // wait...
                Thread.Yield();
            }

            return param;
        }
    }
}
