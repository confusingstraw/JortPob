using HKLib.hk2018.hk;
using HKLib.hk2018.TypeRegistryTest;
using JortPob.Common;
using JortPob.Worker;
using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using static JortPob.FxrManager;
using static SoulsFormats.PARAM;

namespace JortPob
{
    /* Manages params */
    public class Paramanager
    {
        public enum ParamType
        {
            ActionButtonParam, AiSoundParam, AssetEnvironmentGeometryParam, AssetMaterialSfxParam, AssetModelSfxParam, AtkParam_Npc, AtkParam_Pc, AttackElementCorrectParam, 
            AutoCreateEnvSoundParam, BaseChrSelectMenuParam, BehaviorParam, BehaviorParam_PC, BonfireWarpParam, BonfireWarpSubCategoryParam, BonfireWarpTabParam, BuddyParam, 
            BuddyStoneParam, BudgetParam, Bullet, BulletCreateLimitParam, CalcCorrectGraph, Ceremony, CharaInitParam, CharMakeMenuListItemParam, CharMakeMenuTopParam, 
            ChrActivateConditionParam, ChrEquipModelParam, ChrModelParam, ClearCountCorrectParam, CoolTimeParam, CutsceneGparamTimeParam, CutsceneGparamWeatherParam, CutsceneMapIdParam, 
            CutSceneTextureLoadParam, CutsceneTimezoneConvertParam, CutsceneWeatherOverrideGparamConvertParam, DecalParam, DirectionCameraParam, EnemyCommonParam, EnvObjLotParam, 
            EquipMtrlSetParam, EquipParamAccessory, EquipParamCustomWeapon, EquipParamGem, EquipParamGoods, EquipParamProtector, EquipParamWeapon, FaceParam, FaceRangeParam, FeTextEffectParam,
            FinalDamageRateParam, FootSfxParam, GameAreaParam, GameSystemCommonParam, GestureParam, GparamRefSettings, GraphicsCommonParam, GraphicsConfig, GrassLodRangeParam, GrassTypeParam,
            GrassTypeParam_Lv1, GrassTypeParam_Lv2, HitEffectSeParam, HitEffectSfxConceptParam, HitEffectSfxParam, HitMtrlParam, HPEstusFlaskRecoveryParam, ItemLotParam_enemy, 
            ItemLotParam_map, KeyAssignMenuItemParam, KeyAssignParam_TypeA, KeyAssignParam_TypeB, KeyAssignParam_TypeC, KnockBackParam, KnowledgeLoadScreenItemParam, 
            LegacyDistantViewPartsReplaceParam, LoadBalancerDrawDistScaleParam, LoadBalancerDrawDistScaleParam_ps4, LoadBalancerDrawDistScaleParam_ps5, LoadBalancerDrawDistScaleParam_xb1, 
            LoadBalancerDrawDistScaleParam_xb1x, LoadBalancerDrawDistScaleParam_xss, LoadBalancerDrawDistScaleParam_xsx, LoadBalancerNewDrawDistScaleParam_ps4, 
            LoadBalancerNewDrawDistScaleParam_ps5, LoadBalancerNewDrawDistScaleParam_win64, LoadBalancerNewDrawDistScaleParam_xb1, LoadBalancerNewDrawDistScaleParam_xb1x, 
            LoadBalancerNewDrawDistScaleParam_xss, LoadBalancerNewDrawDistScaleParam_xsx, LoadBalancerParam, LockCamParam, Magic, MapDefaultInfoParam, MapGdRegionDrawParam, 
            MapGdRegionInfoParam, MapGridCreateHeightDetailLimitInfo, MapGridCreateHeightLimitInfoParam, MapMimicryEstablishmentParam, MapNameTexParam, MapNameTexParam_m61, 
            MapPieceTexParam, MapPieceTexParam_m61, MaterialExParam, MenuColorTableParam, MenuCommonParam, MenuOffscrRendParam, MenuPropertyLayoutParam, MenuPropertySpecParam,
            MenuValueTableParam, MimicryEstablishmentTexParam, MimicryEstablishmentTexParam_m61, MoveParam, MPEstusFlaskRecoveryParam, MultiHPEstusFlaskBonusParam,
            MultiMPEstusFlaskBonusParam, MultiPlayCorrectionParam, MultiSoulBonusRateParam, NetworkAreaParam, NetworkMsgParam, NetworkParam, NpcAiActionParam, NpcAiBehaviorProbability,
            NpcParam, NpcThinkParam, ObjActParam, PartsDrawParam, PhantomParam, PlayerCommonParam, PlayRegionParam, PostureControlParam_Gender, PostureControlParam_Pro, 
            PostureControlParam_WepLeft, PostureControlParam_WepRight, RandomAppearParam, ReinforceParamProtector, ReinforceParamWeapon, ResistCorrectParam, RideParam, RoleParam,
            RollingObjLotParam, RuntimeBoneControlParam, SeActivationRangeParam, SeMaterialConvertParam, SfxBlockResShareParam, ShopLineupParam, ShopLineupParam_Recipe, SignPuddleParam,
            SignPuddleSubCategoryParam, SignPuddleTabParam, SoundAssetSoundObjEnableDistParam, SoundAutoEnvSoundGroupParam, SoundAutoReverbEvaluationDistParam, SoundAutoReverbSelectParam,
            SoundChrPhysicsSeParam, SoundCommonIngameParam, SoundCutsceneParam, SpeedtreeParam, SpEffectParam, SpEffectSetParam, SpEffectVfxParam, SwordArtsParam, TalkParam,
            ThrowDirectionSfxParam, ThrowParam, ToughnessParam, TutorialParam, WaypointParam, WeatherAssetCreateParam, WeatherAssetReplaceParam, WeatherLotParam, WeatherLotTexParam,
            WeatherLotTexParam_m61, WeatherParam, WepAbsorpPosParam, WetAspectParam, WhiteSignCoolTimeParam, WorldMapLegacyConvParam, WorldMapPieceParam, WorldMapPlaceNameParam,
            WorldMapPointParam, WwiseValueToStrParam_BgmBossChrIdConv, WwiseValueToStrParam_EnvPlaceType, WwiseValueToStrParam_Switch_AttackStrength, WwiseValueToStrParam_Switch_AttackType,
            WwiseValueToStrParam_Switch_DamageAmount, WwiseValueToStrParam_Switch_DeffensiveMaterial, WwiseValueToStrParam_Switch_GrassHitType, WwiseValueToStrParam_Switch_HitStop,
            WwiseValueToStrParam_Switch_OffensiveMaterial, WwiseValueToStrParam_Switch_PlayerEquipmentBottoms, WwiseValueToStrParam_Switch_PlayerEquipmentTops, 
            WwiseValueToStrParam_Switch_PlayerShoes, WwiseValueToStrParam_Switch_PlayerVoiceType
        }

        public enum ParamDefType
        {
            ACTIONBUTTON_PARAM_ST, AI_SOUND_PARAM_ST, ASSET_GEOMETORY_PARAM_ST, ASSET_MATERIAL_SFX_PARAM_ST, ASSET_MODEL_SFX_PARAM_ST, ATK_PARAM_ST, 
            ATTACK_ELEMENT_CORRECT_PARAM_ST, AUTO_CREATE_ENV_SOUND_PARAM_ST, BASECHR_SELECT_MENU_PARAM_ST, BEHAVIOR_PARAM_ST, BONFIRE_WARP_PARAM_ST, 
            BONFIRE_WARP_SUB_CATEGORY_PARAM_ST, BONFIRE_WARP_TAB_PARAM_ST, BUDDY_PARAM_ST, BUDDY_STONE_PARAM_ST, BUDGET_PARAM_ST, BULLET_PARAM_ST, 
            BULLET_CREATE_LIMIT_PARAM_ST, CACL_CORRECT_GRAPH_ST, CEREMONY_PARAM_ST, CHARACTER_INIT_PARAM, CHARMAKEMENU_LISTITEM_PARAM_ST, CHARMAKEMENUTOP_PARAM_ST,
            CHR_ACTIVATE_CONDITION_PARAM_ST, CHR_EQUIP_MODEL_PARAM_ST, CHR_MODEL_PARAM_ST, CLEAR_COUNT_CORRECT_PARAM_ST, COOL_TIME_PARAM_ST, 
            CUTSCENE_GPARAM_TIME_PARAM_ST, CUTSCENE_GPARAM_WEATHER_PARAM_ST, CUTSCENE_MAP_ID_PARAM_ST, CUTSCENE_TEXTURE_LOAD_PARAM_ST, 
            CUTSCENE_TIMEZONE_CONVERT_PARAM_ST, CUTSCENE_WEATHER_OVERRIDE_GPARAM_ID_CONVERT_PARAM_ST, DECAL_PARAM_ST, DIRECTION_CAMERA_PARAM_ST, 
            ENEMY_COMMON_PARAM_ST, ENV_OBJ_LOT_PARAM_ST, EQUIP_MTRL_SET_PARAM_ST, EQUIP_PARAM_ACCESSORY_ST, EQUIP_PARAM_CUSTOM_WEAPON_ST, 
            EQUIP_PARAM_GEM_ST, EQUIP_PARAM_GOODS_ST, EQUIP_PARAM_PROTECTOR_ST, EQUIP_PARAM_WEAPON_ST, FACE_PARAM_ST, FACE_RANGE_PARAM_ST,
            FE_TEXT_EFFECT_PARAM_ST, FINAL_DAMAGE_RATE_PARAM_ST, FOOT_SFX_PARAM_ST, GAME_AREA_PARAM_ST, GAME_SYSTEM_COMMON_PARAM_ST, GESTURE_PARAM_ST,
            GPARAM_REF_SETTINGS_PARAM_ST, GRAPHICS_COMMON_PARAM_ST, CS_GRAPHICS_CONFIG_PARAM_ST, GRASS_LOD_RANGE_PARAM_ST, GRASS_TYPE_PARAM_ST,
            HIT_EFFECT_SE_PARAM_ST, HIT_EFFECT_SFX_CONCEPT_PARAM_ST, HIT_EFFECT_SFX_PARAM_ST, HIT_MTRL_PARAM_ST, ESTUS_FLASK_RECOVERY_PARAM_ST, 
            ITEMLOT_PARAM_ST, CS_KEY_ASSIGN_MENUITEM_PARAM, KEY_ASSIGN_PARAM_ST, KNOCKBACK_PARAM_ST, KNOWLEDGE_LOADSCREEN_ITEM_PARAM_ST,
            LEGACY_DISTANT_VIEW_PARTS_REPLACE_PARAM, LOAD_BALANCER_DRAW_DIST_SCALE_PARAM_ST, LOAD_BALANCER_NEW_DRAW_DIST_SCALE_PARAM_ST,
            LOAD_BALANCER_PARAM_ST, LOCK_CAM_PARAM_ST, MAGIC_PARAM_ST, MAP_DEFAULT_INFO_PARAM_ST, MAP_GD_REGION_DRAW_PARAM, MAP_GD_REGION_ID_PARAM_ST, 
            MAP_GRID_CREATE_HEIGHT_LIMIT_DETAIL_INFO_PARAM_ST, MAP_GRID_CREATE_HEIGHT_LIMIT_INFO_PARAM_ST, MAP_MIMICRY_ESTABLISHMENT_PARAM_ST, 
            MAP_NAME_TEX_PARAM_ST, MAP_NAME_TEX_PARAM_ST_DLC02, MAP_PIECE_TEX_PARAM_ST, MAP_PIECE_TEX_PARAM_ST_DLC02, MATERIAL_EX_PARAM_ST, 
            MENU_PARAM_COLOR_TABLE_ST, MENU_COMMON_PARAM_ST, MENU_OFFSCR_REND_PARAM_ST, MENUPROPERTY_LAYOUT, MENUPROPERTY_SPEC, MENU_VALUE_TABLE_SPEC, 
            MIMICRY_ESTABLISHMENT_TEX_PARAM_ST, MIMICRY_ESTABLISHMENT_TEX_PARAM_ST_DLC02, MOVE_PARAM_ST, MULTI_ESTUS_FLASK_BONUS_PARAM_ST, 
            MULTI_PLAY_CORRECTION_PARAM_ST, MULTI_SOUL_BONUS_RATE_PARAM_ST, NETWORK_AREA_PARAM_ST, NETWORK_MSG_PARAM_ST, NETWORK_PARAM_ST, 
            NPC_AI_ACTION_PARAM_ST, NPC_AI_BEHAVIOR_PROBABILITY_PARAM_ST, NPC_PARAM_ST, NPC_THINK_PARAM_ST, OBJ_ACT_PARAM_ST, PARTS_DRAW_PARAM_ST, 
            PHANTOM_PARAM_ST, PLAYER_COMMON_PARAM_ST, PLAY_REGION_PARAM_ST, POSTURE_CONTROL_PARAM_GENDER_ST, POSTURE_CONTROL_PARAM_PRO_ST, 
            POSTURE_CONTROL_PARAM_WEP_LEFT_ST, POSTURE_CONTROL_PARAM_WEP_RIGHT_ST, RANDOM_APPEAR_PARAM_ST, REINFORCE_PARAM_PROTECTOR_ST, 
            REINFORCE_PARAM_WEAPON_ST, RESIST_CORRECT_PARAM_ST, RIDE_PARAM_ST, ROLE_PARAM_ST, ROLLING_OBJ_LOT_PARAM_ST, RUNTIME_BONE_CONTROL_PARAM_ST,
            SE_ACTIVATION_RANGE_PARAM_ST, SE_MATERIAL_CONVERT_PARAM_ST, SFX_BLOCK_RES_SHARE_PARAM, SHOP_LINEUP_PARAM, SIGN_PUDDLE_PARAM_ST, 
            SIGN_PUDDLE_SUB_CATEGORY_PARAM_ST, SIGN_PUDDLE_TAB_PARAM_ST, SOUND_ASSET_SOUND_OBJ_ENABLE_DIST_PARAM_ST, SOUND_AUTO_ENV_SOUND_GROUP_PARAM_ST,
            SOUND_AUTO_REVERB_EVALUATION_DIST_PARAM_ST, SOUND_AUTO_REVERB_SELECT_PARAM_ST, SOUND_CHR_PHYSICS_SE_PARAM_ST, SOUND_COMMON_INGAME_PARAM_ST,
            SOUND_CUTSCENE_PARAM_ST, SPEEDTREE_MODEL_PARAM_ST, SP_EFFECT_PARAM_ST, SP_EFFECT_SET_PARAM_ST, SP_EFFECT_VFX_PARAM_ST, SWORD_ARTS_PARAM_ST, 
            TALK_PARAM_ST, THROW_DIRECTION_SFX_PARAM_ST, THROW_PARAM_ST, TOUGHNESS_PARAM_ST, TUTORIAL_PARAM_ST, WAYPOINT_PARAM_ST, WEATHER_ASSET_CREATE_PARAM_ST,
            WEATHER_ASSET_REPLACE_PARAM_ST, WEATHER_LOT_PARAM_ST, WEATHER_LOT_TEX_PARAM_ST, WEATHER_LOT_TEX_PARAM_ST_DLC02, WEATHER_PARAM_ST,
            WEP_ABSORP_POS_PARAM_ST, WET_ASPECT_PARAM_ST, WHITE_SIGN_COOL_TIME_PARAM_ST, WORLD_MAP_LEGACY_CONV_PARAM_ST, WORLD_MAP_PIECE_PARAM_ST,
            WORLD_MAP_PLACE_NAME_PARAM_ST, WORLD_MAP_POINT_PARAM_ST, WWISE_VALUE_TO_STR_CONVERT_PARAM_ST
        }

        public Dictionary<ParamType, PARAM> param;

        public short terrainDrawParamID;
        private Dictionary<int, int> lodPartDrawParamIDs; // first int is the index of the array from Const.ASSET_LOD_VALUES, second int is the param row id

        public Paramanager()
        {
            /* Borrowed some files from SmithBox. Credit to that for paramdef xml */
            BND4 paramBnd = SoulsFormats.SFUtil.DecryptERRegulation(Utility.ResourcePath(@"misc\regulation.bin"));
            string[] files = Directory.GetFiles(Utility.ResourcePath(@"misc\paramdefs"));

            Lort.Log("Loading PARAMs...", Lort.Type.Main);
            Lort.NewTask("Loading PARAMs", paramBnd.Files.Count + files.Length);

            Dictionary<ParamDefType, PARAMDEF> paramdefs = new();
            foreach (string file in files)
            {
                PARAMDEF p = PARAMDEF.XmlDeserialize(file);
                Lort.TaskIterate();
                try
                {
                    ParamDefType ty = (ParamDefType)System.Enum.Parse(typeof(ParamDefType), p.ParamType);
                    paramdefs.Add(ty, p);
                }
                catch(Exception ex)
                {
                    Lort.Log($"Skipped unknown paramdef: {p.ParamType}", Lort.Type.Debug);
                    continue;
                }
            }

            int partition = (int)Math.Ceiling(paramBnd.Files.Count / (float)Const.THREAD_COUNT);
            List<ParamWorker> workers = new();
            for (int i = 0; i < Const.THREAD_COUNT; i++)
            {
                int start = i * partition;
                int end = start + partition;
                ParamWorker worker = new(paramBnd.Files, paramdefs, start, end);
                workers.Add(worker);
            }

            /* Wait for threads to finish */
            while (true)
            {
                bool done = true;
                foreach (ParamWorker worker in workers)
                {
                    done &= worker.IsDone;
                }

                if (done)
                    break;
            }

            param = new();
            foreach(ParamWorker worker in workers)
            {
                foreach(KeyValuePair<ParamType, PARAM> kvp in worker.param)
                {
                    param.Add(kvp.Key, kvp.Value);
                }
            }
        }

        // Get a row by id because the row index is NOT the same as the id. guh.
        public PARAM.Row GetRow(int id, PARAM p)
        {
            foreach(PARAM.Row row in p.Rows)
            {
                if (row.ID == id) { return row; }
            }
            return null;
        }

        public PARAM.Row CloneRow(PARAM.Row source, PARAMDEF paramdef, string newName, int newID)
        {
            PARAM.Row row = new(newID, newName, paramdef);
            for (int j = 0; j < source.Cells.Count; j++)
            {
                PARAM.Cell src = source.Cells[j];
                row.Cells[j].Value = src.Value;
            }
            return row;
        }

        public void Write()
        {
            BND4 bnd = new();
            bnd.Compression = SoulsFormats.DCX.Type.DCX_ZSTD;
            bnd.Version = "11601000";
            int i = 0;
            foreach (KeyValuePair<ParamType, PARAM> kvp in param)
            {
                BinderFile file = new();
                file.Bytes = kvp.Value.Write();
                file.Name = $"N:\\GR\\data\\Param\\param\\GameParam\\merged\\DLC02\\{kvp.Key.ToString()}.param";
                file.ID = i++;
                bnd.Files.Add(file);
            }
            SFUtil.EncryptERRegulation($"{Const.OUTPUT_PATH}regulation.bin", bnd);
        }

        /* picks the partdrawparam for an asset based on its size. smaller assets have shorter render distance etc */
        private int AssetPartDrawParamBySize(ModelInfo asset)
        {
            for (int i = 0; i < Const.ASSET_LOD_VALUES.Count(); i++)
            {
                // we do a little cheating here. dynamics can be scaled so im just giving them a huge size mult to compensate.
                // realstically an optimization should be made to calculate this but its a minor concern so very low priority @TODO:
                float[] values = Const.ASSET_LOD_VALUES[i];  
                if ((asset.IsDynamic() ? asset.size * 10f : asset.size) < values[0]) { return lodPartDrawParamIDs[i]; }
            }
            return lodPartDrawParamIDs.Last().Value;
        }

        public void GenerateAssetRows(List<ModelInfo> assets)
        {
            PARAM assetParam = param[ParamType.AssetEnvironmentGeometryParam];
            foreach (ModelInfo asset in assets)
            {
                /* Dynamic */
                if (asset.IsDynamic())
                {
                    // Clone a specific row as our baseline
                    PARAM.Row source = GetRow(7077, assetParam);   // 7077 is a big stone building part in the overworld
                    PARAM.Row row = new(asset.AssetRow(), asset.name, assetParam.AppliedParamdef);
                    for (int i = 0; i < source.Cells.Count; i++)
                    {
                        PARAM.Cell src = source.Cells[i];
                        row.Cells[i].Value = src.Value;
                    }
                    
                    // Set some values and add
                    row.Cells[2].Value = AssetPartDrawParamBySize(asset);        // DrawParamID
                    row.Cells[3].Value = 0;           // Hit type (LO ONLY)
                    row.Cells[4].Value = 0;           // BehaviourType, affects HKX scaling and breakability
                    assetParam.Rows.Add(row);
                }
                /* Static */
                else
                {
                    // Clone a specific row as our baseline
                    PARAM.Row source = GetRow(7077, assetParam);   // 7077 is a big stone building part in the overworld
                    PARAM.Row row = new(asset.AssetRow(), asset.name, assetParam.AppliedParamdef);
                    for (int i = 0; i < source.Cells.Count; i++)
                    {
                        PARAM.Cell src = source.Cells[i];
                        row.Cells[i].Value = src.Value;
                    }

                    // Set some values and add
                    row.Cells[2].Value = AssetPartDrawParamBySize(asset);        // DrawParamID
                    row.Cells[3].Value = 0;           // Hit type (LO ONLY)
                    row.Cells[4].Value = 1;           // BehaviourType, affects HKX scaling and breakability
                    assetParam.Rows.Add(row);
                }
            }
        }

        public void GenerateAssetRows(List<EmitterInfo> assets)
        {
            PARAM assetParam = param[ParamType.AssetEnvironmentGeometryParam];
            foreach (EmitterInfo asset in assets)
            {
                /* We just make all emitters dynamic assets because I can't be asked to sort out baked scaling for them rn */
                /* There aren't that many of them and most will be no-collide so its fine prolly */
                // Clone a specific row as our baseline
                {
                    PARAM.Row source = GetRow(7077, assetParam);   // 7077 is a big stone building part in the overworld
                    PARAM.Row row = new(asset.AssetRow(), asset.record, assetParam.AppliedParamdef);
                    for (int i = 0; i < source.Cells.Count; i++)
                    {
                        PARAM.Cell src = source.Cells[i];
                        row.Cells[i].Value = src.Value;
                    }

                    // Set some values and add
                    row.Cells[2].Value = AssetPartDrawParamBySize(asset.model);        // DrawParamID
                    row.Cells[3].Value = 0;           // Hit type (LO ONLY)
                    row.Cells[4].Value = 0;           // BehaviourType, affects HKX scaling and breakability
                    assetParam.Rows.Add(row);
                }

                /* If the asset has some emitter or attachlight nodes we create an sfx param for it */
                {
                    if (!asset.HasEmitter() && asset.GetAttachLight() == -1) { continue; }  // really shouldnt happen but...

                    int offset = 0;
                    PARAM emitterParam = param[ParamType.AssetModelSfxParam];
                    PARAM.Row row = CloneRow(GetRow(228039000, emitterParam), emitterParam.AppliedParamdef, $"emitter-{asset.record}", asset.AssetRow() * 1000); // 228039000 is a candle in the round table hold
                    /* Quick optimization */
                    /* In Morrowind they comibne multiple effects for some emitter things. Most notably a campfire is like 5 emitters */
                    /* In Elden Ring they jus thave a single simple campfire FXR. So uhhh let's just look and see if a MW emitter has the fire part and then delete the rest to make things easier. */
                    if (asset.model.dummies.ContainsKey("superspray01 emitter"))
                    {
                        asset.model.dummies.Remove("smoke emitter");
                        asset.model.dummies.Remove("sparks emitter");
                    }

                    if (asset.model.dummies.ContainsKey("fire emitter"))
                    {
                        asset.model.dummies.Remove("smoke emitter");
                        asset.model.dummies.Remove("sparks emitter");
                    }
                    foreach (KeyValuePair<string, short> kvp in asset.model.dummies)
                    {
                        string name = kvp.Key;
                        short refid = kvp.Value;
                        int fxrid = FxrManager.GetFXR(name);

                        if (fxrid != -1)
                        {
                            row.Cells[0 + (offset * 3)].Value = fxrid;
                            row.Cells[1 + (offset * 3)].Value = refid;
                            offset++;
                        }
                    }

                    if (asset.GetAttachLight() != -1)
                    {
                        row.Cells[0 + (offset * 3)].Value = FxrManager.GetLightFXR(asset);
                        row.Cells[1 + (offset * 3)].Value = asset.GetAttachLight();
                    }

                    emitterParam.Rows.Add(row);
                }
            }
        }

        public void GenerateAssetRows(List<LiquidInfo> assets)
        {
            PARAM assetParam = param[ParamType.AssetEnvironmentGeometryParam];
            foreach (LiquidInfo asset in assets)
            {
                // Clone a specific row as our baseline
                PARAM.Row source = GetRow(97000, assetParam);   // 097000 is the ocean water around limgrave
                PARAM.Row row = new(asset.AssetRow(), $"water{asset.id}", assetParam.AppliedParamdef);
                for (int i = 0; i < source.Cells.Count; i++)
                {
                    PARAM.Cell src = source.Cells[i];
                    row.Cells[i].Value = src.Value;
                }
                assetParam.Rows.Add(row);
            }
        }

        /* Make some parts draw params for us to use on different types of assets */
        public void GeneratePartDrawParams()
        {
            PARAM assetParam = param[ParamType.PartsDrawParam];
            float NONE = 99999f;
            short drawParamId = Const.PART_DRAW_PARAM;
            lodPartDrawParamIDs = new();

            // Clone a specific row as our baseline
            for (int i=0;i<Const.ASSET_LOD_VALUES.Count();i++)
            {
                float[] values = Const.ASSET_LOD_VALUES[i];

                PARAM.Row source = GetRow(1001, assetParam);   // generic long distance lod drawparam
                PARAM.Row row = new(drawParamId, $"mw | generic | 0lod | size_{values[0]} | static", assetParam.AppliedParamdef);
                for (int j = 0; j < source.Cells.Count; j++)
                {
                    PARAM.Cell src = source.Cells[j];
                    row.Cells[j].Value = src.Value;
                }

                // set some values
                row.Cells[0].Value = NONE;  // border 0
                row.Cells[1].Value = 0;

                row.Cells[13].Value = values[1]; // drawdist
                row.Cells[14].Value = values[2]; // fadeoff

                row.Cells[10].Value = 256; // tex_lv1_borderdist [512]
                row.Cells[11].Value = 32;    // tex_lv1_playdist [10]
                row.Cells[24].Value = 0;    // include lod map level [2]
                row.Cells[26].Value = 1;    // lodtype [1]

                row.Cells[30].Value = NONE; // distant view model border dist [30]
                row.Cells[31].Value = 0;    // distant view model play dist [5]
                assetParam.Rows.Add(row);
                lodPartDrawParamIDs.Add(i, drawParamId++);
            }


            // Clone a specific row as our baseline
            {
                PARAM.Row source = GetRow(1001, assetParam);   // generic long distance lod drawparam
                PARAM.Row row = new(drawParamId, $"mw | terrain | 2lod | static", assetParam.AppliedParamdef);
                for (int i = 0; i < source.Cells.Count; i++)
                {
                    PARAM.Cell src = source.Cells[i];
                    row.Cells[i].Value = src.Value;
                }

                // set some values and add
                row.Cells[0].Value = Const.TERRAIN_LOD_VALUES[0].DISTANCE; // border 0
                row.Cells[1].Value = 16;
                row.Cells[2].Value = Const.TERRAIN_LOD_VALUES[1].DISTANCE; // border 1
                row.Cells[3].Value = 32;
                row.Cells[4].Value = Const.TERRAIN_LOD_VALUES[2].DISTANCE; // border 2
                row.Cells[5].Value = 64;

                row.Cells[13].Value = NONE; // drawdist
                row.Cells[14].Value = 0; //fadeoff

                row.Cells[10].Value = 256; // tex_lv1_borderdist [512]
                row.Cells[11].Value = 32;    // tex_lv1_playdist [10]
                row.Cells[24].Value = 0;    // include lod map level [2]
                row.Cells[26].Value = 1;    // lodtype [1]

                row.Cells[30].Value = NONE; // distant view model border dist [30]
                row.Cells[31].Value = 0;    // distant view model play dist [5]
                assetParam.Rows.Add(row);
                terrainDrawParamID = drawParamId++;
            }
        }

        /* Die */
        public void KillMapHeightParams()
        {
            param[ParamType.MapGridCreateHeightLimitInfoParam].Rows.Clear();
        }
    }
}
