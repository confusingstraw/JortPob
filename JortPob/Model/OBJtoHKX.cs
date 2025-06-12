using HKLib;
using HKLib.hk2018;
using HKLib.Reflection.hk2018;
using HKLib.Serialization.hk2018.Binary;
using HKLib.Serialization.hk2018.Xml;
using SoulsFormats;
using System;
using System.Diagnostics;
using System.IO;
using System.Xml.Linq;

/* Code here is courtesy of Dropoff */
/* Also uses some stuff by Hork & 12th I think */
/* This is a modified version of ER_OBJ2HKX */
namespace JortPob.Model
{
    partial class ModelConverter
    {
        public static void OBJtoHKX(string objPath, string hkxPath)
        {
            string tempDir = $"{AppDomain.CurrentDomain.BaseDirectory}Resources\\tools\\ER_OBJ2HKX\\";

            //ClearTempDir(tempDir);
            Console.WriteLine(objPath);

            byte[] hkx = ObjToHkx(tempDir, objPath);
            hkx = UpgradeHKX(tempDir, hkx);

            Console.WriteLine(hkxPath);
            File.WriteAllBytes(hkxPath, hkx);
            //ClearTempDir(tempDir);
        }

        public static void HKXDispose()
        {
            string tempDir = $"{AppDomain.CurrentDomain.BaseDirectory}Resources\\tools\\ER_OBJ2HKX\\";
            ClearTempDir(tempDir);
        }

        private static byte[] ObjToHkx(string tempDir, string objPath)
        {
            string fName = Path.GetFileNameWithoutExtension(objPath);

            File.Copy(objPath, @$"{tempDir}\{fName}.obj", true);

            string srcDir = Path.GetDirectoryName(objPath);
            string mtlPath = @$"{srcDir}\{fName}.mtl";
            if (File.Exists(mtlPath))
            {
                File.Copy(mtlPath, @$"{tempDir}\{fName}.mtl", true);
            }

            var startInfo = new ProcessStartInfo(@$"{tempDir}\obj2fsnp.exe", @$"{tempDir}\{fName}.obj")
            {
                WorkingDirectory = @$"{tempDir}\",
                UseShellExecute = false
            };
            var process = Process.Start(startInfo);
            process.WaitForExit();

            startInfo = new ProcessStartInfo(@$"{tempDir}\AssetCc2_fixed.exe", $@"--strip {tempDir}\{fName}.obj.o2f {tempDir}\{fName}.1")
            {
                WorkingDirectory = @$"{tempDir}\",
                UseShellExecute = false
            };
            process = Process.Start(startInfo);
            process.WaitForExit();

            startInfo = new ProcessStartInfo(@$"{tempDir}\hknp2fsnp.exe", $@"{tempDir}\{fName}.1")
            {
                WorkingDirectory = @$"{tempDir}\",
                UseShellExecute = false
            };
            process = Process.Start(startInfo);
            process.WaitForExit();

            return File.ReadAllBytes($@"{tempDir}\{fName}.1.hkx");
        }

        private static void ClearTempDir(string tempDir)
        {
            foreach (var file in Directory.GetFiles(tempDir))
            {
                if (file.ToLower().EndsWith(".obj") ||
                    file.ToLower().EndsWith(".mtl") ||
                    file.ToLower().EndsWith(".obj.o2f") ||
                    file.ToLower().EndsWith(".hkx") ||
                    file.ToLower().EndsWith(".1"))
                {
                    File.Delete(file);
                }
            }
        }

        private static byte[] UpgradeHKX(string tempDir, byte[] bytes)
        {
            var des = new HKX2.PackFileDeserializer();
            var root = (HKX2.hkRootLevelContainer)des.Deserialize(new BinaryReaderEx(false, bytes));

            hkRootLevelContainer hkx = HkxUpgrader.UpgradehkRootLevelContainer(root);
            HavokTypeRegistry registry = HavokTypeRegistry.Load($"{tempDir}HavokTypeRegistry20180100.xml");

            HavokBinarySerializer binarySerializer = new(registry);
            HavokXmlSerializer xmlSerializer = new(registry);
            using (MemoryStream ms = new MemoryStream())
            {
                //binarySerializer.Write(hkx, ms);
                xmlSerializer.Write(hkx, ms);
                bytes = ms.ToArray();
            }
            return bytes;
        }
    }
}
