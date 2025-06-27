using JortPob.Common;
using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static JortPob.Paramanager;

namespace JortPob.Worker
{


    public class ParamWorker : Worker
    {
        private List<BinderFile> files;
        private Dictionary<Paramanager.ParamDefType, PARAMDEF> paramdefs;  //inputs
        private int start;
        private int end;

        public Dictionary<Paramanager.ParamType, PARAM> param; //output

        public ParamWorker(List<BinderFile> files, Dictionary<Paramanager.ParamDefType, PARAMDEF> paramdefs, int start, int end)
        {
            this.files = files;
            this.paramdefs = paramdefs;
            this.start = start;
            this.end = end;

            param = new();

            _thread = new Thread(Run);
            _thread.Start();
        }

        public void Run()
        {
            ExitCode = 1;

            for (int i = start; i < Math.Min(files.Count, end); i++)
            {
                BinderFile file = files[i];

                PARAM p = PARAM.Read(file.Bytes);
                ParamDefType ty = (ParamDefType)Enum.Parse(typeof(ParamDefType), p.ParamType);
                ParamType ty2 = (ParamType)Enum.Parse(typeof(ParamType), Utility.PathToFileName(file.Name));
                p.ApplyParamdef(paramdefs[ty]);
                param.Add(ty2, p);
                Lort.TaskIterate();
            }

            IsDone = true;
            ExitCode = 0;
        }
    }
}
