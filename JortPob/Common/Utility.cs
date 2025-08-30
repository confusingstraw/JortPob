using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Linq;

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

        /* Sort binderfiles by id */
        /* Yes, for some god forsaken reason this seems to matter */
        /* Shitty slow sort, replace with something better eventually */
        public static void SortBND4(BND4 bnd)
        {
            for (int i = 0; i < bnd.Files.Count() - 1; i++)
            {
                BinderFile file = bnd.Files[i];
                BinderFile next = bnd.Files[i + 1];
                if (next.ID < file.ID)
                {
                    BinderFile temp = file;
                    bnd.Files[i] = next;
                    bnd.Files[i + 1] = temp;
                    i = 0; // slow and bad
                }
            }
        }

        // same garbage as above
        public static void SortPARAM(PARAM param)
        {
            for (int i = 0; i < param.Rows.Count() - 1; i++)
            {
                PARAM.Row row = param.Rows[i];
                PARAM.Row next = param.Rows[i + 1];
                if(next.ID < row.ID)
                {
                    PARAM.Row temp = row;
                    param.Rows[i] = next;
                    param.Rows[i + 1] = temp;
                    i = 0; 
                }
            }
        }

        // yep!
        public static void SortFMG(FMG fmg)
        {
            for (int i = 0; i < fmg.Entries.Count() - 1; i++)
            {
                FMG.Entry entry = fmg.Entries[i];
                FMG.Entry next = fmg.Entries[i + 1];
                if (next.ID < entry.ID)
                {
                    FMG.Entry temp = entry;
                    fmg.Entries[i] = next;
                    fmg.Entries[i + 1] = temp;
                    i = 0;
                }
            }
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
