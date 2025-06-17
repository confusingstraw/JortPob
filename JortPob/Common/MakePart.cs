using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace JortPob.Common
{
    /* Makes generic parts for MSBE */
    /* Generated parts have standardized fields, you can then set the important bits and gg ez */
    /* The reason I made this it's own class is because generating parts is very bulky in Elden Ring and this is cleaner than doing it inline */
    public class MakePart
    {
        public static Dictionary<ModelInfo, int> AssetInstances = new(); // counts instances of assets
        public static Dictionary<string, int> EnemyInstances = new();      // counts instances of enemies

        /* Makes simple collideable asset */
        /* Values for this generic asset generator are taken from a random stone ruin in the church of elleh area 'AEG007_077' */
        public static MSBE.Part.Asset Asset(ModelInfo modelInfo)
        {
            MSBE.Part.Asset asset = new();

            /* Instance */
            int inst;
            if(AssetInstances.ContainsKey(modelInfo)) { inst = ++AssetInstances[modelInfo]; }
            else { inst = 0; AssetInstances.Add(modelInfo, inst); }
            asset.InstanceID = inst;

            /* Model Stuff */
            asset.Name = $"{modelInfo.AssetName().ToUpper()}_{inst.ToString("D4")}";
            asset.ModelName = modelInfo.AssetName().ToUpper();

            /* Top stuff */
            asset.AssetSfxParamRelativeID = -1;
            asset.MapStudioLayer = 4294967295;
            asset.IsShadowDest = true;

            /* Gparam */
            asset.Gparam.FogParamID = -1;
            asset.Gparam.LightSetID = -1;

            /* Various Unks */
            asset.UnkE0F = 1;
            asset.UnkE3C = -1;
            asset.UnkT12 = 255;
            asset.UnkT1E = -1;
            asset.UnkT24 = -1;
            asset.UnkT30 = -1;
            asset.UnkT34 = -1;

            /* Display Groups */
            asset.Unk1.DisplayGroups[0] = 16;
            asset.Unk1.UnkC4 = -1;

            /* Unk Groups */
            asset.Unk2.Condition = -1;
            asset.Unk2.Unk26 = -1;

            /* TileLoad */
            asset.TileLoad.MapID = new byte[] { 255, 255, 255, 255 };
            asset.TileLoad.CullingHeightBehavior = -1;

            /* Grass */
            asset.Grass.Unk18 = -1;

            /* Asset Partnames */
            asset.UnkT54PartName = asset.Name;
            asset.UnkPartNames[4] = asset.Name;
            asset.UnkPartNames[5] = asset.Name;
            asset.UnkModelMaskAndAnimID = -1;
            asset.UnkT5C = -1;
            asset.UnkT60 = -1;
            asset.UnkT64 = -1;

            /* AssetUnk1 */
            asset.AssetUnk1.Unk1C = -1;
            asset.AssetUnk1.Unk24 = -1;
            asset.AssetUnk1.Unk26 = -1;
            asset.AssetUnk1.Unk28 = -1;
            asset.AssetUnk1.Unk2C = -1;

            /* AssetUnk2 */
            asset.AssetUnk2.Unk04 = 100;
            asset.AssetUnk2.Unk14 = -1f;
            asset.AssetUnk2.Unk1C = 255;
            asset.AssetUnk2.Unk1D = 255;
            asset.AssetUnk2.Unk1E = 255;
            asset.AssetUnk2.Unk1F = 255;

            /* AssetUnk3 */
            asset.AssetUnk3.Unk04 = 64.808716f;
            asset.AssetUnk3.Unk09 = 255;
            asset.AssetUnk3.Unk0B = 255;
            asset.AssetUnk3.Unk0C = -1;
            asset.AssetUnk3.Unk10 = -1f;
            asset.AssetUnk3.DisableWhenMapLoadedMapID = new sbyte[] { -1, -1, -1, -1 };
            asset.AssetUnk3.Unk18 = -1;
            asset.AssetUnk3.Unk1C = -1;
            asset.AssetUnk3.Unk20 = -1;
            asset.AssetUnk3.Unk24 = 255;

            /* AssetUnk4 */
            asset.AssetUnk4.Unk01 = 255;
            asset.AssetUnk4.Unk02 = 255;

            return asset;
        }

        /* Make a map piece for use as terrain */
        public static MSBE.Part.MapPiece MapPiece()
        {
            MSBE.Part.MapPiece map = new();

            /* Some Stuff */
            map.MapStudioLayer = 4294967295;
            map.isUsePartsDrawParamID = 1;
            map.PartsDrawParamID = 1001;

            /* Gparam */
            map.Gparam.FogParamID = -1;
            map.Gparam.LightSetID = -1;

            /* More stuff */
            map.IsShadowDest = true;

            /* TileLoad */
            map.TileLoad.MapID = new byte[] { 255, 255, 255, 255 };
            map.TileLoad.CullingHeightBehavior = -1;
            map.TileLoad.Unk0C = -1;

            /* Display Groups */
            map.Unk1.UnkC4 = -1;

            /* Random Unks */
            map.UnkE0F = 1;
            map.UnkE3C = -1;
            map.UnkE3E = 1;

            return map;
        }

        /* Make a collision for use as terrain */
        public static MSBE.Part.Collision Collision()
        {
            MSBE.Part.Collision collision = new();

            collision.MapStudioLayer = 4294967295;
            collision.PlayRegionID = -1;
            collision.LocationTextID = -1;
            collision.InstanceID = -1;
            collision.TileLoad.CullingHeightBehavior = -1;
            collision.TileLoad.MapID = new byte[] { 255, 255, 255, 255 };
            collision.TileLoad.Unk0C = -1;
            collision.Unk1.UnkC4 = -1;
            collision.Unk2.Condition = -1;
            collision.Unk2.Unk26 = -1;
            collision.UnkE0F = 1;
            collision.UnkE3C = -1;
            collision.UnkT01 = 255;
            collision.UnkT02 = 255;
            collision.UnkT04 = 64.8087158f;
            collision.UnkT14 = -1;
            collision.UnkT1C = -1;
            collision.UnkT24 = -1;
            collision.UnkT30 = -1;
            collision.UnkT35 = 255;
            collision.UnkT3C = -1;
            collision.UnkT3E = -1;
            collision.UnkT4E = -1;

            return collision;
        }
    
        /* Makes a c000 enemy part */
        public static MSBE.Part.Enemy Npc()
        {
            MSBE.Part.Enemy enemy = new();

            /* Instance */
            int inst;
            if (EnemyInstances.ContainsKey("c0000")) { inst = ++EnemyInstances["c0000"]; }
            else { inst = 0; EnemyInstances.Add("c0000", inst); }
            enemy.InstanceID = inst;

            /* Model and Enemy Stuff */
            enemy.Name = $"c0000_{inst.ToString("D4")}";
            enemy.ModelName = "c0000";
            enemy.CharaInitID = 23150;
            enemy.NPCParamID = 523010010;
            enemy.EntityID = 0;
            enemy.PlatoonID = 0;
            enemy.ThinkParamID = 523011000;

            /* In Alphabetical Order... */
            /* Gparam */
            enemy.Gparam.FogParamID = -1;
            enemy.Gparam.LightSetID = -1;

            /* Stuff */
            enemy.IsShadowDest = true;
            enemy.MapStudioLayer = 4294967295;

            /* TileLoad */
            enemy.TileLoad.MapID = new byte[] { 255, 255, 255, 255 };
            enemy.TileLoad.CullingHeightBehavior = -1;
            enemy.TileLoad.Unk0C = -1;

            /* Display Groups */
            enemy.Unk1.DisplayGroups[0] = 16;
            enemy.Unk1.UnkC4 = -1;

            /* Random Unks */
            enemy.UnkE0F = 1;
            enemy.UnkE3C = -1;

            return enemy;
        }

        /* makes a goat */
        public static MSBE.Part.Enemy Creature()
        {
            MSBE.Part.Enemy enemy = new();

            /* Instance */
            int inst;
            if (EnemyInstances.ContainsKey("c0000")) { inst = ++EnemyInstances["c0000"]; }
            else { inst = 0; EnemyInstances.Add("c0000", inst); }
            enemy.InstanceID = inst;

            /* Model and Enemy Stuff */
            enemy.Name = $"c6060_{inst.ToString("D4")}";
            enemy.ModelName = "c6060";
            enemy.NPCParamID = 60600010;
            enemy.EntityID = 0;
            enemy.PlatoonID = 0;
            enemy.ThinkParamID = 60600000;

            /* In Alphabetical Order... */
            /* Gparam */
            enemy.Gparam.FogParamID = -1;
            enemy.Gparam.LightSetID = -1;

            /* Stuff */
            enemy.IsShadowDest = true;
            enemy.MapStudioLayer = 4294967295;

            /* TileLoad */
            enemy.TileLoad.MapID = new byte[] { 255, 255, 255, 255 };
            enemy.TileLoad.CullingHeightBehavior = -1;
            enemy.TileLoad.Unk0C = -1;

            /* Display Groups */
            enemy.Unk1.DisplayGroups[0] = 16;
            enemy.Unk1.UnkC4 = -1;

            /* Random Unks */
            enemy.UnkE0F = 1;
            enemy.UnkE3C = -1;
            enemy.UnkT84 = 1;

            return enemy;
        }

        /* Create generic player starting point. */
        public static MSBE.Part.Player Player()
        {
            MSBE.Part.Player player = new();

            player.Name = "c0000_9001";
            player.ModelName = "c0000";
            player.InstanceID = 9001;
            player.MapStudioLayer = 4294967295;
            player.Unk1.DisplayGroups[0] = 16;
            player.IsShadowDest = true;

            /* TileLoad */
            player.TileLoad.MapID = new byte[] { 255, 255, 255, 255 };
            player.TileLoad.CullingHeightBehavior = -1;
            player.TileLoad.Unk0C = -1;

            /* No idea */
            player.Unk00 = 6;

            /* Display Groups */
            player.Unk1.UnkC4 = -1;

            /* Random Unks */
            player.UnkE0F = 1;
            player.UnkE3C = -1;

            return player;
        }
    }
}


// If ya need to compare some values....
//            //MSBE TESTO = MSBE.Read(@"I:\SteamLibrary\steamapps\common\ELDEN RING\Game\map\mapstudio\m60_42_36_00.msb.dcx");
