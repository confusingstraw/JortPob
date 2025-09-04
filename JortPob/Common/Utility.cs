using SoulsFormats;
using SoulsIds;
using System;
using System.Collections.Generic;
using System.Linq;
using WitchyFormats;

namespace JortPob.Common
{
    public static class Utility
    {
        private static readonly char[] _dirSep = { '\\', '/' };

        /* Take a full file path and returns just a file name without directory or extensions */
        public static string PathToFileName(string fileName)
        {
            if (fileName.EndsWith("\\") || fileName.EndsWith("/"))
                fileName = fileName.TrimEnd(_dirSep);

            if (fileName.Contains("\\") || fileName.Contains("/"))
                fileName = fileName.Substring(fileName.LastIndexOfAny(_dirSep) + 1);

            if (fileName.Contains("."))
                fileName = fileName.Substring(0, fileName.LastIndexOf('.'));

            return fileName;
        }

        public static string ResourcePath(string path)
        {
            return $"{AppDomain.CurrentDomain.BaseDirectory}Resources\\{path}";
        }

        public static uint FNV1_32(byte[] data)
        {
            const uint FNV_PRIME = 16777619;
            const uint FNV_OFFSET_BASIS = 2166136261;

            uint hash = FNV_OFFSET_BASIS;

            foreach (byte b in data)
            {
                hash *= FNV_PRIME;
                hash ^= b;
            }

            return hash;
        }

        public static void SortBND4(BND4 bnd)
        {
            bnd.Files = bnd.Files.OrderBy(file => file.ID).ToList();
        }

        public static void SortFsParam(FsParam param)
        {
            param.Rows = param.Rows.AsParallel().OrderBy(row => row.ID).ToList();
        }

        public static void SortPARAM(SoulsFormats.PARAM param)
        {
            param.Rows = param.Rows.AsParallel().OrderBy(row => row.ID).ToList();
        }

        // yep!
        public static void SortFMG(FMG fmg)
        {
            fmg.Entries = fmg.Entries.AsParallel().OrderBy(entry => entry.ID).ToList();
        }
    }

    public static class IListExtensions
    {
        /// <summary>
        /// Shuffles the element order of the specified list.
        /// </summary>
        public static void Shuffle<T>(this IList<T> ts)
        {
            var count = ts.Count;
            var last = count - 1;
            Random rand = new Random();
            for (var i = 0; i < last; ++i)
            {
                var r = rand.Next(i, count);
                var tmp = ts[i];
                ts[i] = ts[r];
                ts[r] = tmp;
            }
        }
    }
}
