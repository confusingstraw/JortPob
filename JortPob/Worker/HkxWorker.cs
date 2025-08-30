using JortPob.Common;
using JortPob.Model;
using System;
using System.Collections.Generic;
using System.Threading;

namespace JortPob.Worker
{
    public class HkxWorker : Worker
    {
        private List<CollisionInfo> collisions;

        private int start;
        private int end;

        public HkxWorker(List<CollisionInfo> collisions, int start, int end)
        {
            this.collisions = collisions;

            this.start = start;
            this.end = end;

            _thread = new Thread(Run);
            _thread.Start();
        }

        private void Run()
        {
            ExitCode = 1;

            for (int i = start; i < Math.Min(collisions.Count, end); i++)
            {
                CollisionInfo collisionInfo = collisions[i];
                ModelConverter.OBJtoHKX($"{Const.CACHE_PATH}{collisionInfo.obj}", $"{Const.CACHE_PATH}{collisionInfo.hkx}");

                Lort.TaskIterate(); // Progress bar update
            }

            IsDone = true;
            ExitCode = 0;
        }

        public static void Go(List<CollisionInfo> collisions)
        {
            Lort.Log($"Converting {collisions.Count} collision...", Lort.Type.Main);                 // Egregiously slow, multithreaded to make less terrible
            int partition = (int)Math.Ceiling(collisions.Count / (float)Const.THREAD_COUNT);
            Lort.NewTask("Converting HKX", collisions.Count);
            List<HkxWorker> workers = new();
            for (int i = 0; i < Const.THREAD_COUNT; i++)
            {
                int start = i * partition;
                int end = start + partition;
                HkxWorker worker = new(collisions, start, end);
                workers.Add(worker);
            }

            /* Wait for threads to finish */
            while (true)
            {
                bool done = true;
                foreach (HkxWorker worker in workers)
                {
                    done &= worker.IsDone;
                }

                if (done)
                    break;
            }
        }
    }
}
