using HKLib.hk2018.hkaiNavMeshClearanceCacheSeeding;
using JortPob.Common;
using JortPob.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JortPob.Worker
{
    public class HkxWorker : Worker
    {
        private List<CollisionInfo> collisions;
        private string cachePath;

        private int start;
        private int end;

        public HkxWorker(List<CollisionInfo> collisions, string cachePath, int start, int end)
        {
            this.cachePath = cachePath;
            this.collisions = collisions;

            this.start = start;
            this.end = end;

            _thread = new Thread(Parse);
            _thread.Start();
        }

        private void Parse()
        {
            ExitCode = 1;

            for (int i = start; i < Math.Min(collisions.Count, end); i++)
            {
                CollisionInfo collisionInfo = collisions[i];
                ModelConverter.OBJtoHKX($"{cachePath}{collisionInfo.obj}", $"{cachePath}{collisionInfo.path}");
            }

            IsDone = true;
            ExitCode = 0;
        }
    }
}
