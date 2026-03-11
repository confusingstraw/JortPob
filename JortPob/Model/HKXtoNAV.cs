using JortPob.Common;
using System;
using System.Diagnostics;

namespace JortPob.Model
{
    public partial class ModelConverter
    {
        public static void HKXtoNAV(string hkxPath, string navPath, string settingsPath)
        {
            string gamePath = @$"{Const.ELDEN_PATH}\Game\eldenring.exe";
            ProcessStartInfo startInfo = new(Utility.ResourcePath(@"tools\Nav\ERNavmeshGenerator.exe"), $"-g \"{gamePath}\" -i \"{hkxPath}\" -o \"{navPath}\" -s \"{settingsPath}\"" )
            {
                WorkingDirectory = Utility.ResourcePath(@"tools\Nav"),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
            };
            Utility.ExecuteProcess(startInfo, 60000);
        }
    }
}