using JortPob.Common;
using System;
using System.Diagnostics;

namespace JortPob.Model
{
    public partial class ModelConverter
    {
        public static void HKXtoNAV(string hkxPath, string navPath)
        {
            string gamePath = @$"{Const.ELDEN_PATH}\Game\eldenring.exe";
            ProcessStartInfo startInfo = new(@$"{AppDomain.CurrentDomain.BaseDirectory}\ERNavmeshGenerator.exe", [gamePath, hkxPath, navPath])
            {
                WorkingDirectory = @$"{AppDomain.CurrentDomain.BaseDirectory}\",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
            };
            Utility.ExecuteProcess(startInfo);
        }
    }
}