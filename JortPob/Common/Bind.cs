using SoulsFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JortPob.Common
{
    public class Bind
    {
        public static void BindMaterials(string cachePath, string outPath)
        {
            BND4 bnd = BND4.Read(Utility.ResourcePath($"matbins\\allmaterial.matbinbnd.dcx"));

            /* Grab all matbin files */
            string[] fileList = Directory.GetFiles($"{cachePath}materials");
            int i = 15102; // appending our new file indexes after all the base game ones
            foreach (string file in fileList)
            {
                MATBIN matbin = MATBIN.Read(file);
                BinderFile bind = new();
                bind.CompressionType = SoulsFormats.DCX.Type.Zlib;
                bind.Flags = SoulsFormats.Binder.FileFlags.Flag1;
                bind.ID = ++i;
                bind.Name = $"N:\\GR\\data\\INTERROOT_win64\\material\\matbin\\Morrowind\\matxml\\{Utility.PathToFileName(file)}.matbin";
                bind.Bytes = matbin.Write();
                bnd.Files.Add(bind);
            }

            bnd.Write(outPath);
        }

        public static void BindAsset(ModelInfo modelInfo, string cachePath, string outPath)
        {
            BND4 bnd = new();
            bnd.Compression = SoulsFormats.DCX.Type.DCX_DFLT_11000_44_9;
            bnd.Extended = 4;
            bnd.Format = SoulsFormats.Binder.Format.IDs | SoulsFormats.Binder.Format.Names1 | SoulsFormats.Binder.Format.Names2 | SoulsFormats.Binder.Format.Compression;
            bnd.Unicode = true;
            bnd.Version = "07D7R6";

            FLVER2 flver = FLVER2.Read($"{cachePath}{modelInfo.path}");

            BinderFile file = new();
            file.CompressionType = SoulsFormats.DCX.Type.Zlib;
            file.Flags = SoulsFormats.Binder.FileFlags.Flag1;
            file.ID = 200;
            file.Name = $"N:\\GR\\data\\INTERROOT_win64\\asset\\aeg\\{modelInfo.AssetPath()}\\sib\\{modelInfo.AssetName()}.flver";
            file.Bytes = flver.Write();

            bnd.Files.Add(file);

            bnd.Write(outPath);
        }

        public static void BindTPF(Cache cache, string cachePath, string outPath)
        {
            /* Collect all textures, kind of brute force, could optimize later */
            List<TextureInfo> textures = new();
            bool TextureExists(TextureInfo t)
            {
                foreach (TextureInfo tex in textures)
                {
                    if (t.name == tex.name) { return true; }
                }
                return false;
            }

            foreach (ModelInfo mod in cache.assets)
            {
                foreach(TextureInfo tex in mod.textures)
                {
                    if (TextureExists(tex)) { continue; }
                    textures.Add(tex);
                }
            }

            /* Bind all textures */
            BXF4 tpfbdt = new();
            tpfbdt.Extended = 4;
            tpfbdt.Format = SoulsFormats.Binder.Format.IDs | SoulsFormats.Binder.Format.Names1 | SoulsFormats.Binder.Format.Names2 | SoulsFormats.Binder.Format.Compression;
            tpfbdt.Unicode = true;
            tpfbdt.Version = "25E14X35";
            int index = 0;
            foreach (TextureInfo tex in textures)
            {
                TPF tpf = TPF.Read($"{cachePath}{tex.path}");

                BinderFile bf = new();
                bf.CompressionType = DCX.Type.None;
                bf.Flags = SoulsFormats.Binder.FileFlags.Flag1;
                bf.ID = index++;
                bf.Name = $"{tex.name}.tpf.dcx";
                bf.Bytes = tpf.Write();
                tpfbdt.Files.Add(bf);
            }

            /* Write bind */
            tpfbdt.Write($"{outPath}.tpfbhd", $"{outPath}.tpfbdt");
        }
    }
}
