using JortPob.Common;
using SoulsFormats;
using SoulsFormats.KF4;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace JortPob.Model
{
    internal class MaterialContext
    {

        public XmlDocument xmlMaterialInfo;
        public XmlNodeList xmlMaterials, xmlExamples;

        public string cachePath;

        public MaterialContext(string cachePath)
        {
            xmlMaterialInfo = new();
            xmlMaterialInfo.Load(Utility.ResourcePath(@"matbins\MaterialInfo.xml"));

            xmlMaterials = xmlMaterialInfo.ChildNodes[1].ChildNodes[0].ChildNodes;
            xmlExamples = xmlMaterialInfo.ChildNodes[1].ChildNodes[2].ChildNodes;

            this.cachePath = cachePath;

            genTextures = new();
            genMATBINs = new();
        }

        /* If isStatic is true we will skip any bufferlayouts with boneweights/indicies in them */
        private FLVER2.BufferLayout GetLayout(string mtd, bool isStatic)
        {
            foreach (XmlElement xmlElement in xmlMaterials)
            {
                string attrMtd = xmlElement.GetAttribute("mtd");
                if(attrMtd == mtd)
                {
                    foreach(XmlElement xmlBuffer in xmlElement.ChildNodes[0].ChildNodes)
                    {
                        if (isStatic && (xmlBuffer.InnerXml.Contains("BoneIndices") || xmlBuffer.InnerXml.Contains("BoneWeights"))) { continue; }
                        FLVER2.BufferLayout layout = new();

                        foreach(XmlElement xmlBufferValue in xmlBuffer.ChildNodes[0].ChildNodes)
                        {
                            string type = xmlBufferValue.Name;
                            string semantic = xmlBufferValue.InnerText;
                            int index = 0;
                            if(semantic.EndsWith("]")) {
                                index = int.Parse(semantic.Substring(semantic.Length - 2, 1));
                                semantic = semantic.Substring(0, semantic.Length - 3);
                            }

                            FLVER.LayoutType layoutType = Enum.Parse<FLVER.LayoutType>(type);
                            FLVER.LayoutSemantic layoutSemantic = Enum.Parse<FLVER.LayoutSemantic>(semantic);

                            FLVER.LayoutMember member = new(layoutType, layoutSemantic, index, 0, 0);
                            layout.Add(member);
                        }

                        return layout;
                    }
                }
            }

            Console.WriteLine($"## ERROR ## Failed to find bufferlayout for {mtd}");
            return null;
        }

        /* Currently selects first example of a valid GXList for a given material. It might be a good idea to build a better system for this. */
        private FLVER2.GXList GetGXList(string mtd)
        {
            foreach (XmlElement xmlElement in xmlExamples)
            {
                string attrMtd = xmlElement.GetAttribute("mtd");
                if (attrMtd == mtd)
                {

                    FLVER2.GXList gxlist = new();

                    foreach(XmlElement xmlItem in xmlElement.ChildNodes[0].ChildNodes[0].ChildNodes)
                    {
                        string id = xmlItem.GetAttribute("id");
                        string unk04 = xmlItem.GetAttribute("unk04");
                        byte[] data = xmlItem.InnerText.Split().Select(t => byte.Parse(t, NumberStyles.AllowHexSpecifier)).ToArray();

                        FLVER2.GXItem gxitem = new();
                        gxitem.ID = id;
                        gxitem.Unk04 = int.Parse(unk04);
                        gxitem.Data = data;

                        gxlist.Add(gxitem);
                    }

                    return gxlist;
                }
            }

            Console.WriteLine($"## ERROR ## Failed to find gxlists for {mtd}");
            return null;
        }

        /* Currently copies the first example of the given material. It might be a good idea to build a better system for this. */
        private FLVER2.Material GetMaterial(string mtd, int index)
        {
            foreach (XmlElement xmlElement in xmlExamples)
            {
                string attrMtd = xmlElement.GetAttribute("mtd");
                if (attrMtd == mtd)
                {
                    XmlElement xmlMaterial = (XmlElement)xmlElement.ChildNodes[0];
                    FLVER2.Material material = new();

                    material.MTD = mtd;
                    material.Name = xmlMaterial.GetAttribute("name");
                    material.Index = index;
                    material.GXIndex = index;

                    foreach(XmlElement xmlTexture in xmlMaterial.ChildNodes[1].ChildNodes)
                    {
                        FLVER2.Texture texture = new();

                        string unk10 = xmlTexture.GetAttribute("unk10");
                        string unk11 = xmlTexture.GetAttribute("unk11");
                        string unk14 = xmlTexture.GetAttribute("unk14");
                        string unk18 = xmlTexture.GetAttribute("unk18");
                        string unk1c = xmlTexture.GetAttribute("unk1C");

                        string[] scale = xmlTexture.GetAttribute("scale").Split(",");

                        texture.Type = xmlTexture.GetAttribute("type");
                        texture.Path = xmlTexture.GetAttribute("path");
                        texture.Unk10 = byte.Parse(unk10);
                        texture.Unk11 = bool.Parse(unk11);
                        texture.Unk14 = float.Parse(unk14);
                        texture.Unk18 = float.Parse(unk18);
                        texture.Unk1C = float.Parse(unk1c);
                        texture.Scale = new System.Numerics.Vector2(float.Parse(scale[0]), float.Parse(scale[1]));

                        material.Textures.Add(texture);
                    }

                    return material;
                }

            }

            Console.WriteLine($"## ERROR ## Failed to find {mtd}");
            return null;
        }

        /* For the materialcontexts lifetime it collects a list of textures and matbins used by models being converted. */
        /* Once we are done converting models the write() method for the material context should be called and it will write all tpfs and matbins and bind them up into the appropriate bnds */
        /* The reason we collect everything and then write at the end is so we can reuse tps/matbins easily and so we can easily make the bnds at the end of the process */
        public Dictionary<string, MATBIN> genMATBINs;   // template id + texture, matbin
        public Dictionary<string, string> genTextures;  // original texture path, tpf output

        /* List of template matbins which we use as a base for our custom materials */
        /* filenames for templates stored without extension or path. files are in Resources/matbins and the extension is .matbin but is stored as.matxml in flvers so guh */
        /* template matbins are stored with their original filename so that the lookup through the MaterialInfo.xml still matches to it correctly. makes life easier! */
        public Dictionary<string, string> matbinTemplates = new() {
            { "static[a]opaque", "AEG006_030_ID001" },      // simple opaque albedo material
            { "static[a]alpha", null },                     // same but alpha tested
            { "static[a]transparent", null }                // same but transparent
        };

        /* Create a full suite of a custom matbin, textures, and layout/gx/material info for a flver and return them all in a container */
        /* This method will make guesstimations based on the texture files about what type of material to use. For example, if the material has an alpha channel we will make it transparent */
        public List<MaterialInfo> GenerateMaterials(List<SharpAssimp.Material> sourceMaterials)
        {
            int index = 0;
            List<MaterialInfo> materialInfo = new();
            foreach (SharpAssimp.Material sourceMaterial in sourceMaterials)
            {
                // @TOOD: currently writing materials 1 to 1 but should probably check material usage and cull materials that are not needed like collision material
                string diffuseTextureSourcePath = sourceMaterial.TextureDiffuse.FilePath != null ? sourceMaterial.TextureDiffuse.FilePath : Utility.ResourcePath(@"textures\tx_missing.dds");
                string diffuseTexture;
                if (genTextures.ContainsKey(diffuseTextureSourcePath))
                {
                    diffuseTexture = genTextures.GetValueOrDefault(diffuseTextureSourcePath);
                }
                else
                {
                    diffuseTexture = Utility.PathToFileName(diffuseTextureSourcePath);
                    genTextures.Add(diffuseTextureSourcePath, diffuseTexture);
                }

                string matbinTemplate = matbinTemplates["static[a]opaque"];   // @TODO: actually look at values of textures and determine template type
                string matbinName = diffuseTexture.StartsWith("tx_") ? $"mat_{diffuseTexture.Substring(3)}" : $"mat_{diffuseTexture}";

                MATBIN matbin;
                string matbinkey = $"{matbinTemplate}::{matbinName}";
                if (genMATBINs.ContainsKey(matbinkey))
                {
                    matbin = genMATBINs.GetValueOrDefault(matbinkey);
                }
                else
                {
                    matbin = MATBIN.Read(Utility.ResourcePath($"matbins\\{matbinTemplate}.matbin"));
                    matbin.Samplers[0].Path = $"{diffuseTexture}";
                    matbin.SourcePath = $"{matbinName}.matxml";
                    genMATBINs.Add(matbinkey, matbin);
                }

                FLVER2.BufferLayout layout = GetLayout($"{matbinTemplate}.matxml", true);
                FLVER2.GXList gx = GetGXList($"{matbinTemplate}.matxml");
                FLVER2.Material material = GetMaterial($"{matbinTemplate}.matxml", index++);
                material.MTD = matbin.SourcePath;
                material.Name = $"{matbinName}";

                TextureInfo info = new(diffuseTexture, $"textures\\{diffuseTexture}.tpf.dcx");


                materialInfo.Add(new MaterialInfo(material, gx, layout, matbin, info));
            }
            return materialInfo;
        }

        /* Same as above but for terrain */
        public List<MaterialInfo> GenerateMaterials(Landscape landscape)
        {
            int index = 0;
            List<MaterialInfo> materialInfo = new();
                // @TOOD: currently writing materials 1 to 1 but should probably check material usage and cull materials that are not needed like collision material
                string diffuseTextureSourcePath = Utility.ResourcePath(@"textures\tx_missing.dds");
                string diffuseTexture;
                if (genTextures.ContainsKey(diffuseTextureSourcePath))
                {
                    diffuseTexture = genTextures.GetValueOrDefault(diffuseTextureSourcePath);
                }
                else
                {
                    diffuseTexture = Utility.PathToFileName(diffuseTextureSourcePath);
                    genTextures.Add(diffuseTextureSourcePath, diffuseTexture);
                }

                string matbinTemplate = matbinTemplates["static[a]opaque"];   // @TODO: actually look at values of textures and determine template type
                string matbinName = diffuseTexture.StartsWith("tx_") ? $"mat_{diffuseTexture.Substring(3)}" : $"mat_{diffuseTexture}";

                MATBIN matbin;
                string matbinkey = $"{matbinTemplate}::{matbinName}";
                if (genMATBINs.ContainsKey(matbinkey))
                {
                    matbin = genMATBINs.GetValueOrDefault(matbinkey);
                }
                else
                {
                    matbin = MATBIN.Read(Utility.ResourcePath($"matbins\\{matbinTemplate}.matbin"));
                    matbin.Samplers[0].Path = $"{diffuseTexture}";
                    matbin.SourcePath = $"{matbinName}.matxml";
                    genMATBINs.Add(matbinkey, matbin);
                }

                FLVER2.BufferLayout layout = GetLayout($"{matbinTemplate}.matxml", true);
                FLVER2.GXList gx = GetGXList($"{matbinTemplate}.matxml");
                FLVER2.Material material = GetMaterial($"{matbinTemplate}.matxml", index++);
                material.MTD = matbin.SourcePath;
                material.Name = $"{matbinName}";

                TextureInfo info = new(diffuseTexture, $"textures\\{diffuseTexture}.tpf.dcx");


                materialInfo.Add(new MaterialInfo(material, gx, layout, matbin, info));
            return materialInfo;
        }

        /* Write all tpfs and matbins generated by above methods in this context and bnd them appropriately */
        public void WriteAll()
        {
            foreach(KeyValuePair<string, MATBIN> kvp in genMATBINs)
            {
                string outFileName = $"{Utility.PathToFileName(kvp.Value.SourcePath)}.matbin";
                kvp.Value.Write($"{cachePath}materials\\{outFileName}");
            }

            foreach (KeyValuePair<string, string> kvp in genTextures)
            {
                TPF tpf = new TPF();
                tpf.Encoding = 1;
                tpf.Flag2 = 3;
                tpf.Platform = TPF.TPFPlatform.PC;
                tpf.Compression = DCX.Type.DCX_KRAK;

                byte[] data = File.ReadAllBytes(kvp.Key);
                int format = JortPob.Common.DDS.GetTpfFormatFromDdsBytes(data);

                TPF.Texture tex = new($"{kvp.Value}", (byte)format, 0, data, TPF.TPFPlatform.PC);
                tpf.Textures.Add(tex);

                tpf.Write($"{cachePath}textures\\{kvp.Value}.tpf.dcx");
            }
        }

        /* Container class for returning a bunch of stuff at once */
        public class MaterialInfo
        {
            public FLVER2.Material material;
            public FLVER2.GXList gx;
            public FLVER2.BufferLayout layout;
            public MATBIN matbin;
            public TextureInfo info; // used by cache

            public MaterialInfo(FLVER2.Material material, FLVER2.GXList gx, FLVER2.BufferLayout layout, MATBIN matbin, TextureInfo info)
            {
                this.material = material;
                this.gx = gx;
                this.layout = layout;
                this.matbin = matbin;
                this.info = info;
            }
        }
    }
}
