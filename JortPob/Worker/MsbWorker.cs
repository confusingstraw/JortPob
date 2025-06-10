using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SoulsFormats;

namespace JortPob.Worker
{
    public class MsbWorker : Worker
    {
        private string cachePath;
        private List<ResourcePool> msbs;
        private string modPath;
        private int start;
        private int end;

        public MsbWorker(List<ResourcePool> msbs, string cachePath, string modPath, int start, int end)
        {
            this.msbs = msbs;
            this.cachePath = cachePath;
            this.modPath = modPath;

            this.start = start;
            this.end = end;

            _thread = new Thread(Parse);
            _thread.Start();
        }

        private void Parse()
        {
            ExitCode = 1;

            for (int i = start; i < Math.Min(msbs.Count, end); i++)
            {
                ResourcePool pool = msbs[i];

                string map = $"{pool.id[0].ToString("D2")}";
                string name = $"{pool.id[0].ToString("D2")}_{pool.id[1].ToString("D2")}_{pool.id[2].ToString("D2")}_{pool.id[3].ToString("D2")}";

                //Console.WriteLine($"Writing files for -> m{name}");

                pool.msb.Write($"{modPath}map\\mapstudio\\m{name}.msb.dcx");

                /* Write terrain */
                foreach (TerrainInfo t in pool.terrain)
                {
                    FLVER2 flver = FLVER2.Read($"{cachePath}{t.path}");

                    BND4 bnd = new();
                    bnd.Compression = SoulsFormats.DCX.Type.DCX_KRAK;
                    bnd.Version = "07D7R6";

                    BinderFile file = new();
                    file.CompressionType = SoulsFormats.DCX.Type.Zlib;
                    file.Flags = SoulsFormats.Binder.FileFlags.Flag1;
                    file.ID = 200;
                    file.Name = $"N:\\GR\\data\\INTERROOT_win64\\map\\m{name}\\m{name}_{t.id.ToString("D8")}\\Model\\m{name}_{t.id.ToString("D8")}.flver";
                    file.Bytes = flver.Write();
                    bnd.Files.Add(file);

                    bnd.Write($"{modPath}map\\m60\\m{name}\\m{name}_{t.id.ToString("D8")}.mapbnd.dcx");
                }

                /* Write TEST hkx binds */
                BXF4 TEST = BXF4.Read(@"I:\SteamLibrary\steamapps\common\ELDEN RING\Game\map\m60\m60_43_36_00\h60_43_36_00.hkxbhd", @"I:\SteamLibrary\steamapps\common\ELDEN RING\Game\map\m60\m60_43_36_00\h60_43_36_00.hkxbdt");
                foreach (BinderFile file in TEST.Files)
                {
                    file.Name = file.Name.Replace("43_36", $"{pool.id[1].ToString("D2")}_{pool.id[2].ToString("D2")}");
                    file.Name = file.Name.Replace("4336", $"{pool.id[1].ToString("D2")}{pool.id[2].ToString("D2")}");
                    file.Name = file.Name.Replace("m60", $"m{map}");
                    file.Name = file.Name.Replace("h60", $"h{map}");
                }
                TEST.Write($"{modPath}map\\m60\\m{name}\\h{name}.hkxbhd", $"{modPath}map\\m{map}\\m{name}\\h{name}.hkxbdt");

                BXF4 TEST2 = BXF4.Read(@"I:\SteamLibrary\steamapps\common\ELDEN RING\Game\map\m60\m60_43_36_00\l60_43_36_00.hkxbhd", @"I:\SteamLibrary\steamapps\common\ELDEN RING\Game\map\m60\m60_43_36_00\l60_43_36_00.hkxbdt");
                foreach (BinderFile file in TEST2.Files)
                {
                    file.Name = file.Name.Replace("43_36", $"{pool.id[1].ToString("D2")}_{pool.id[2].ToString("D2")}");
                    file.Name = file.Name.Replace("4336", $"{pool.id[1].ToString("D2")}{pool.id[2].ToString("D2")}");
                    file.Name = file.Name.Replace("m60", $"m{map}");
                    file.Name = file.Name.Replace("h60", $"l{map}");
                }
                TEST2.Write($"{modPath}map\\m{map}\\m{name}\\l{name}.hkxbhd", $"{modPath}map\\m{map}\\m{name}\\l{name}.hkxbdt");
            }

            IsDone = true;
            ExitCode = 0;
        }
    }
}
