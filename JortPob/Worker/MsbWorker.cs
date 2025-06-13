using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HKLib.hk2018;
using JortPob.Common;
using SoulsFormats;

namespace JortPob.Worker
{
    public class MsbWorker : Worker
    {
        private List<ResourcePool> msbs;
        private int start;
        private int end;

        public MsbWorker(List<ResourcePool> msbs, int start, int end)
        {
            this.msbs = msbs;

            this.start = start;
            this.end = end;

            _thread = new Thread(Run);
            _thread.Start();
        }

        private void Run()
        {
            ExitCode = 1;

            for (int i = start; i < Math.Min(msbs.Count, end); i++)
            {
                ResourcePool pool = msbs[i];

                string map = $"{pool.id[0].ToString("D2")}";
                string name = $"{pool.id[0].ToString("D2")}_{pool.id[1].ToString("D2")}_{pool.id[2].ToString("D2")}_{pool.id[3].ToString("D2")}";

                pool.msb.Write($"{Const.OUTPUT_PATH}map\\mapstudio\\m{name}.msb.dcx");

                /* Write terrain */
                foreach (TerrainInfo t in pool.terrain)
                {
                    FLVER2 flver = FLVER2.Read($"{Const.CACHE_PATH}{t.path}");

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

                    bnd.Write($"{Const.OUTPUT_PATH}map\\m60\\m{name}\\m{name}_{t.id.ToString("D8")}.mapbnd.dcx");
                }

                BXF4 bxfH = new();
                bxfH.Version = "07D7R6";
                BinderFile comH = new();
                comH.CompressionType = SoulsFormats.DCX.Type.Zlib;
                comH.Name = $"m{name}\\h{name}.compendium.dcx";
                comH.Bytes = DCX.Compress(File.ReadAllBytes(Utility.ResourcePath(@"test\test.compendium")), DCX.Type.DCX_KRAK);
                comH.ID = 0;
                bxfH.Files.Add(comH);
                int id = 1;
                foreach (Tuple<string, CollisionInfo> tuple in pool.collisionIndices)
                {
                    string index = tuple.Item1;
                    CollisionInfo collisionInfo = tuple.Item2;

                    BinderFile testH = new();
                    testH.CompressionType = SoulsFormats.DCX.Type.Zlib;
                    testH.Name = $"m{name}\\h{name}_{index}.hkx.dcx";
                    testH.Bytes = DCX.Compress(File.ReadAllBytes($"{Const.CACHE_PATH}{collisionInfo.path}"), DCX.Type.DCX_KRAK);
                    testH.ID = id++;
                    bxfH.Files.Add(testH);
                }
                bxfH.Write($"{Const.OUTPUT_PATH}map\\m60\\m{name}\\h{name}.hkxbhd", $"{Const.OUTPUT_PATH}map\\m{map}\\m{name}\\h{name}.hkxbdt");

                BXF4 bxfL = new();
                bxfL.Version = "07D7R6";
                BinderFile comL = new();
                comL.CompressionType = SoulsFormats.DCX.Type.Zlib;
                comL.Name = $"m{name}\\l{name}.compendium.dcx";
                comL.Bytes = DCX.Compress(File.ReadAllBytes(Utility.ResourcePath(@"test\test.compendium")), DCX.Type.DCX_KRAK);
                comL.ID = 0;
                bxfL.Files.Add(comL);
                id = 1;
                foreach (Tuple<string, CollisionInfo> tuple in pool.collisionIndices)
                {
                    string index = tuple.Item1;
                    CollisionInfo collisionInfo = tuple.Item2;

                    BinderFile testL = new();
                    testL.CompressionType = SoulsFormats.DCX.Type.Zlib;
                    testL.Name = $"m{name}\\l{name}_{index}.hkx.dcx";
                    testL.Bytes = DCX.Compress(File.ReadAllBytes($"{Const.CACHE_PATH}{collisionInfo.path}"), DCX.Type.DCX_KRAK);
                    testL.ID = id++;
                    bxfL.Files.Add(testL);
                }
                bxfL.Write($"{Const.OUTPUT_PATH}map\\m{map}\\m{name}\\l{name}.hkxbhd", $"{Const.OUTPUT_PATH}map\\m{map}\\m{name}\\l{name}.hkxbdt");

                Lort.TaskIterate(); // Progress bar update
            }

            IsDone = true;
            ExitCode = 0;
        }

        public static void Go(List<ResourcePool> msbs)
        {
            Lort.Log($"Writing {msbs.Count} msbs...", Lort.Type.Main); // Multithreaded because insanely slow
            Lort.NewTask("Writing MSB", msbs.Count);
            int partition = (int)Math.Ceiling(msbs.Count / (float)Const.THREAD_COUNT);
            List<MsbWorker> workers = new();

            for (int i = 0; i < Const.THREAD_COUNT; i++)
            {
                int start = i * partition;
                int end = start + partition;
                MsbWorker worker = new(msbs, start, end);
                workers.Add(worker);
            }

            /* Wait for threads to finish */
            while (true)
            {
                bool done = true;
                foreach (MsbWorker worker in workers)
                {
                    done &= worker.IsDone;
                }

                if (done)
                    break;
            }
        }
    }
}
