using JortPob.Common;
using SoulsFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static JortPob.Papyrus;
using static JortPob.Script.Flag;

namespace JortPob
{
    public class PapyrusEMEVD
    {
        public static List<Call.Type> UNSUPPORTED_CALL_LIST = new(), UNSUPPORTED_CONDITIONAL_LIST = new(), UNSUPPORTED_SET_LIST = new();

        public static void Compile(ESM esm, Layout layout, MSBE msb, MainSoundBank sound, ScriptManager scriptManager, Paramanager paramanager, ItemManager itemManager, SpeffManager speffManager, BaseScript script, Papyrus papyrus, Content content, Script.Flag subscriptRunFlag = null)
        {
            if (Const.DEBUG_SKIP_ESD) { return; }

            /* DEFINE SOME LOCAL FUNCTIONS FIRST */

            /* Returns a special EMEVD command that resets all condition groups by making a do-nothing call that checks MAIN */
            string ResetConditionGroups()
            {
                return "IfElapsedSeconds(MAIN, 0);"; // does not cause a frame of delay. i checked. effectively a nop
            }

            // Little function to resolve a variable to a flag
            Script.Flag GetFlagByVariable(string varName)
            {
                Script.Flag retFlag = null;
                
                // probably a local var of this object. ex: powerLevel or angryness
                if (!varName.Contains("."))
                {
                    if (script is ScriptCommon)
                    {
                        retFlag = scriptManager.GetFlag(Script.Flag.Designation.Local, $"{papyrus.id}.{varName}");
                    }
                    else
                    {
                        retFlag = scriptManager.GetFlag(Script.Flag.Designation.Local, $"{content.entity}.{varName}");
                    }
                }
                // looks like it's actually a local var of a different object EX: fargoth.sexy or "dagoth ur".dreamy
                else
                {
                    // Find refernce to object that matches the record id of this local var 
                    string recordId = varName.Split(".")[0].Replace("\"", "").Trim();
                    string varId = varName.Split(".")[1].Replace("\"", "").Trim();
                    Content target = layout.FindScriptReference(content, recordId);
                    if (target != null)
                    {
                        retFlag = scriptManager.GetFlag(Script.Flag.Designation.Local, $"{target.entity}.{varId}");
                    }
                }
                // if the above cases failed to turn up anything then lets see if its a global var EX: crimeGold or tutorialDone
                if (retFlag == null) { retFlag = scriptManager.GetFlag(Script.Flag.Designation.Global, varName); }

                // return whatever we found, even if null
                return retFlag;
            }

            List<string> post = new(); // generated stuff. not a part of actual papyrus. gets placed at the end of a script or right before an early return

            List<string> HandlePapyrus(Papyrus.Call call)
            {
                switch (call)
                {
                    case Papyrus.Conditional cif:
                        return HandleIf(cif);
                    case Papyrus.Main cm:
                        return new List<string>() { "## INVALID BEGIN CALL ##" };
                    default:
                        return HandleCall(call);
                }
            }

            List<string> HandleIf(Papyrus.Conditional call)
            {
                List<string> lines = new();
                List<string> pass = HandleScope(call.pass);
                List<string> fail = HandleScope(call.fail);

                if (fail.Count() > 0) { pass.Add($"SkipUnconditionally({fail.Count()});"); } // skip over else scope if true

                switch (call.left.type)
                {
                    case Call.Type.Variable:
                        Script.Flag vflag = GetFlagByVariable(call.left.parameters[0]);
                        if (call.right.type == Call.Type.Literal)
                        {
                            lines.Add(ResetConditionGroups());
                            lines.Add($"IfEventValue(OR_01, {vflag.id}, {vflag.Bits()}, {call.OperatorIndex()}, {call.right.parameters[0]});");
                            lines.Add($"SkipIfConditionGroupStateUncompiled({pass.Count()}, FAIL, OR_01);");
                        }
                        else if (call.right.type == Call.Type.Variable)
                        {
                            Script.Flag lflag = GetFlagByVariable(call.right.parameters[0]);
                            lines.Add(ResetConditionGroups());
                            lines.Add($"IfCompareEventValues(OR_01, {vflag.id}, {vflag.Bits()}, {call.OperatorIndex()}, {lflag.id}, {lflag.Bits()});");
                            lines.Add($"SkipIfConditionGroupStateUncompiled({pass.Count()}, FAIL, OR_01);");
                        }
                        else { Lort.Log($"## BAD CONDITIONAL ## {papyrus.id}->{call.type} [{call.left.type} ? {call.right.type}]", Lort.Type.Debug); }
                        break;

                    case Call.Type.GetJournalIndex:
                        Script.Flag jflag = scriptManager.common.GetOrCreateFlag(Script.Flag.Category.Saved, Script.Flag.Type.Byte, Script.Flag.Designation.Journal, call.left.parameters[0]);
                        if (call.right.type == Call.Type.Literal)
                        {
                            lines.Add(ResetConditionGroups());
                            lines.Add($"IfEventValue(OR_01, {jflag.id}, {jflag.Bits()}, {call.OperatorIndex()}, {call.right.parameters[0]});");
                            lines.Add($"SkipIfConditionGroupStateUncompiled({pass.Count()}, FAIL, OR_01);");
                        }
                        else if (call.right.type == Call.Type.Variable)
                        {
                            Script.Flag lflag = GetFlagByVariable(call.right.parameters[0]);
                            lines.Add(ResetConditionGroups());
                            lines.Add($"IfCompareEventValues(OR_01, {jflag.id}, {jflag.Bits()}, {call.OperatorIndex()}, {lflag.id}, {lflag.Bits()});");
                            lines.Add($"SkipIfConditionGroupStateUncompiled({pass.Count()}, FAIL, OR_01);");
                        }
                        else { Lort.Log($"## BAD CONDITIONAL ## {papyrus.id}->{call.type} [{call.left.type} ? {call.right.type}]", Lort.Type.Debug); }
                        break;

                    case Call.Type.Random:
                        Script.Flag randFlag = scriptManager.common.GetOrRegisterRandom(int.Parse(call.left.parameters[0]));
                        if (call.right.type == Call.Type.Literal)
                        {
                            lines.Add(ResetConditionGroups());
                            lines.Add($"IfEventValue(OR_01, {randFlag.id}, {randFlag.Bits()}, {call.OperatorIndex()}, {call.right.parameters[0]});");
                            lines.Add($"SkipIfConditionGroupStateUncompiled({pass.Count()}, FAIL, OR_01);");
                        }
                        else if (call.right.type == Call.Type.Variable)
                        {
                            Script.Flag lflag = GetFlagByVariable(call.right.parameters[0]);
                            lines.Add(ResetConditionGroups());
                            lines.Add($"IfCompareEventValues(OR_01, {randFlag.id}, {randFlag.Bits()}, {call.OperatorIndex()}, {lflag.id}, {lflag.Bits()});");
                            lines.Add($"SkipIfConditionGroupStateUncompiled({pass.Count()}, FAIL, OR_01);");
                        }
                        else { Lort.Log($"## BAD CONDITIONAL ## {papyrus.id}->{call.type} [{call.left.type} ? {call.right.type}]", Lort.Type.Debug); }
                        break;

                    case Call.Type.OnActivate:
                        if (call.right.type == Call.Type.Literal)
                        {
                            bool flagState = int.Parse(call.right.parameters[0]) == 0;
                            Script.Flag aflag = scriptManager.GetFlag(Script.Flag.Designation.OnActivate, content.entity.ToString());
                            post.Add($"SetEventFlag(TargetEventFlagType.EventFlag, {aflag.id}, OFF);"); // switch on activate flag back off after this conditional resolves
                            lines.Add($"SkipIfEventFlag({pass.Count()}, {(flagState ? "ON" : "OFF")}, TargetEventFlagType.EventFlag, {aflag.id});");
                        }
                        else { Lort.Log($"## BAD CONDITIONAL ## {papyrus.id}->{call.type} [{call.left.type} ? {call.right.type}]", Lort.Type.Debug); }
                        break;

                    case Call.Type.OnDeath:
                        if (call.right.type == Call.Type.Literal)
                        {
                            bool flagState = int.Parse(call.right.parameters[0]) == 0;
                            Script.Flag onDeathFlag = RegisterOnDeath(call.left);
                            if (onDeathFlag == null) // if register fails due to missing target
                            {
                                lines.Add($"SkipUnconditionally({(flagState ? 0 : pass.Count())})");
                                break;
                            }
                            post.Add($"SetEventFlag(TargetEventFlagType.EventFlag, {onDeathFlag.id}, OFF);"); // switch on death flag back off after this conditional resolves
                            lines.Add($"SkipIfEventFlag({pass.Count()}, {(flagState ? "ON" : "OFF")}, TargetEventFlagType.EventFlag, {onDeathFlag.id});");
                        }
                        else { Lort.Log($"## BAD CONDITIONAL ## {papyrus.id}->{call.type} [{call.left.type} ? {call.right.type}]", Lort.Type.Debug); }
                        break;

                    case Call.Type.OnMurder:
                        if (call.right.type == Call.Type.Literal)
                        {
                            bool flagState = int.Parse(call.right.parameters[0]) == 0;
                            Script.Flag onDeathFlag = RegisterOnDeath(call.left);
                            if (onDeathFlag == null) // if register fails due to missing target
                            {
                                lines.Add($"SkipUnconditionally({(flagState ? 0 : pass.Count())})");
                                break;
                            }
                            Script.Flag attackedFlag = scriptManager.GetFlag(Script.Flag.Designation.HasBeenAttacked, onDeathFlag.name);

                            post.Add($"SetEventFlag(TargetEventFlagType.EventFlag, {onDeathFlag.id}, OFF);"); // switch on death flag back off after this conditional resolves
                            lines.Add(ResetConditionGroups());
                            lines.Add($"IfEventFlag(AND_01, ON, TargetEventFlagType.EventFlag, {onDeathFlag.id});");
                            lines.Add($"IfEventFlag(AND_01, ON, TargetEventFlagType.EventFlag, {attackedFlag.id});");
                            lines.Add($"SkipIfConditionGroupStateUncompiled({pass.Count()}, FAIL, AND_01);");
                        }
                        else { Lort.Log($"## BAD CONDITIONAL ## {papyrus.id}->{call.type} [{call.left.type} ? {call.right.type}]", Lort.Type.Debug); }
                        break;

                    case Call.Type.OnKnockout:
                        if (call.right.type == Call.Type.Literal)
                        {
                            bool flagState = int.Parse(call.right.parameters[0]) == 0;

                            // find our target content
                            Content target;
                            if (call.target == null) { target = content; }
                            else { target = layout.FindScriptReference(content, call.target); }
                            if (target == null) { break; } // Failed to find script reference. Should only happen when making partial builds.

                            lines.Add(ResetConditionGroups());
                            lines.Add($"IfCharacterHPRatio(OR_01, {target.entity}, 3, 0.25, 0, 1);");
                            lines.Add($"SkipIfConditionGroupStateUncompiled({pass.Count()}, FAIL, OR_01);");
                        }
                        else { Lort.Log($"## BAD CONDITIONAL ## {papyrus.id}->{call.type} [{call.left.type} ? {call.right.type}]", Lort.Type.Debug); }
                        break;

                    case Call.Type.MenuMode:
                        if (call.right.type == Call.Type.Literal)
                        {
                            bool flagState = int.Parse(call.right.parameters[0]) == 1;
                            if (flagState) // menumode == 1
                            {
                                lines.Add($"SkipUnconditionally({pass.Count()});"); // skip to else
                            }
                            else           // menumode == 0
                            {
                                lines.Add($"SkipUnconditionally(0);"); // no op
                            }
                        }
                        else { Lort.Log($"## BAD CONDITIONAL ## {papyrus.id}->{call.type} [{call.left.type} ? {call.right.type}]", Lort.Type.Debug); }
                        break;

                    case Call.Type.CellChanged:
                        if(call.right.type == Call.Type.Literal)
                        {
                            bool flagState = int.Parse(call.right.parameters[0]) == 1;
                            Script.Flag cflag = scriptManager.GetFlag(Script.Flag.Designation.CellChanged, content.entity.ToString());
                            post.Add($"SetEventFlag(TargetEventFlagType.EventFlag, {cflag.id}, ON);");
                            lines.Add($"SkipIfEventFlag({pass.Count()}, {(flagState ? "ON" : "OFF")}, TargetEventFlagType.EventFlag, {cflag.id});");
                        }
                        else { Lort.Log($"## BAD CONDITIONAL ## {papyrus.id}->{call.type} [{call.left.type} ? {call.right.type}]", Lort.Type.Debug); }
                        break;

                    case Call.Type.GetDisabled:
                        if (call.right.type == Call.Type.Literal)
                        {
                            bool flagState = int.Parse(call.right.parameters[0]) == 0;
                            Script.Flag dflag = scriptManager.GetFlag(Script.Flag.Designation.Disabled, content.entity.ToString());
                            if(dflag == null)  // if flag not found skip. likely just due to partial builds or papyrus parsing
                            {
                                lines.Add($"SkipUnconditionally({(flagState?0:pass.Count())})");
                                break;
                            }
                            lines.Add($"SkipIfEventFlag({pass.Count()}, {(flagState ? "ON" : "OFF")}, TargetEventFlagType.EventFlag, {dflag.id});");
                        }
                        else { Lort.Log($"## BAD CONDITIONAL ## {papyrus.id}->{call.type} [{call.left.type} ? {call.right.type}]", Lort.Type.Debug); }
                        break;

                    case Call.Type.GetDistance:
                        {
                            if (script is ScriptCommon) { throw new Exception("GetDistance() not supported by global scripts"); } // i really hope this is unreachable

                            if (call.right.type == Call.Type.Literal)
                            {
                                // find our target for this distance check, only works if one side of this check is a static object. cant compare 2 moving objects
                                string a = call.left.target;
                                string b = call.left.parameters[0].ToLower();
                                Content targetA, targetB;
                                // 3 possible player based distance checks
                                if (a == null && b == "player") { targetA = content; targetB = null; }                                      // EX: (GetDistance Player < 50)
                                else if (a != null && b == "player") { targetA = layout.FindScriptReference(content, a); targetB = null; }  // EX: (fargoth->GetDistance Player < 50)
                                else if (a?.ToLower() == "player") { targetA = layout.FindScriptReference(content, b); targetB = null; }    // EX: (Player->GetDistance fargoth < 50)
                                // remaining possible results for non-player distance checks
                                else                                                                                                        // EX: (fargoth->GetDistance fargoths_ring < 50)
                                {
                                    if (a == null) { targetA = content; }
                                    else { targetA = layout.FindScriptReference(content, a); }
                                    targetB = layout.FindScriptReference(content, b);

                                    if (targetB == null) { /* Do nothing lol */ }
                                    else if (targetA is CharacterContent && targetB is CharacterContent) { Lort.Log($"## ERROR ## GetDistance cannot measure distance between 2 characters! [{call.RAW}]", Lort.Type.Debug); }
                                    else if (targetB is (StaticContent) || targetB is (ItemContent)) { Content temp = targetA; targetA = targetB; targetB = temp; } // swap so targetA is the static
                                }

                                // find area script for that target content
                                Script areaScript = scriptManager.FindScriptFor(layout, targetA);

                                // create a region for this distance check to use
                                MSBE.Region.Other distanceRegion = new();
                                distanceRegion.Name = $"{papyrus.id}->{call.RAW.Trim()}";
                                distanceRegion.Shape = new MSB.Shape.Sphere(float.Parse(call.right.parameters[0]) * Const.GLOBAL_SCALE);
                                distanceRegion.Position = targetA.relative + Const.MSB_OFFSET;
                                distanceRegion.EntityID = areaScript.CreateEntity(Script.EntityType.Region, $"{papyrus.id}->{call.RAW.Trim()}");
                                msb.Regions.Add(distanceRegion);

                                string inOut = call.op == ">" || call.op == ">=" ? "Inside" : "Outside";
                                lines.Add($"SkipIfInoutsideArea({pass.Count()}, InsideOutsideState.{inOut}, {(targetB == null ? 10000 : targetB.entity)}, {distanceRegion.EntityID}, 1);");
                            }
                            else { Lort.Log($"## BAD CONDITIONAL ## {papyrus.id}->{call.type} [{call.left.type} ? {call.right.type}]", Lort.Type.Debug); }
                            break;
                        }

                    case Call.Type.PcExpelled:
                        if (call.right.type == Call.Type.Literal)
                        {
                            bool flagState = int.Parse(call.right.parameters[0]) == 1;
                            string faction;
                            if (call.left.parameters.Count() > 0) { faction = call.left.parameters[0].ToLower().Trim(); }
                            else { faction = ((CharacterContent)content).faction.ToLower().Trim(); }
                            Script.Flag exFlag = scriptManager.GetFlag(Script.Flag.Designation.FactionExpelled, faction);
                            lines.Add($"SkipIfEventFlag({pass.Count()}, {(flagState ? "OFF" : "ON")}, TargetEventFlagType.EventFlag, {exFlag.id});");
                        }
                        else { Lort.Log($"## BAD CONDITIONAL ## {papyrus.id}->{call.type} [{call.left.type} ? {call.right.type}]", Lort.Type.Debug); }
                        break;

                    case Call.Type.PcVampire:
                        if (call.right.type == Call.Type.Literal)
                        {
                            bool flagState = int.Parse(call.right.parameters[0]) == 1;
                            SpeffManager.SpeffSpell speff = speffManager.GetVampirism();
                            lines.Add($"SkipIfEventFlag({pass.Count()}, {(flagState ? "OFF" : "ON")}, TargetEventFlagType.EventFlag, {speff.flag.id});");
                        }
                        else { Lort.Log($"## BAD CONDITIONAL ## {papyrus.id}->{call.type} [{call.left.type} ? {call.right.type}]", Lort.Type.Debug); }
                        break;

                    case Call.Type.GetPcRank:
                        int rank = int.Parse(call.right.parameters[0]) + 1;  // ranks are shifted +1 in ER. rank 0 is considered "not a member"
                        if (call.right.type == Call.Type.Literal)
                        {
                            // notably, this call in payrus sometimes targets the player. it is already a PC check so it's always the player but like... idk double player is just as good
                            // additionally: some faction names have spaces in them, like "imperial cult" which means we need to join all parameters because they dont always use quotes around the calls
                            Script.Flag fflag = scriptManager.GetFlag(Script.Flag.Designation.FactionRank, string.Join(" ",  call.left.parameters));
                            lines.Add(ResetConditionGroups());
                            lines.Add($"IfEventValue(OR_01, {fflag.id}, {fflag.Bits()}, {call.OperatorIndex()}, {rank});");
                            lines.Add($"SkipIfConditionGroupStateUncompiled({pass.Count()}, FAIL, OR_01);");
                        }
                        else { Lort.Log($"## BAD CONDITIONAL ## {papyrus.id}->{call.type} [{call.left.type} ? {call.right.type}]", Lort.Type.Debug); }
                        break;

                    case Call.Type.GetItemCount:
                        // Checking players gold specifically
                        if (call.left.target == "player" && call.left.parameters[0] == "gold_001")
                        {
                            if (call.right.type == Call.Type.Literal)
                            {
                                Script.Flag gflag = scriptManager.GetFlag(Script.Flag.Designation.PlayerRuneCount, "PlayerRuneCount");
                                lines.Add(ResetConditionGroups());
                                lines.Add($"IfEventValue(OR_01, {gflag.id}, {gflag.Bits()}, {call.OperatorIndex()}, {call.right.parameters[0]});");
                                lines.Add($"SkipIfConditionGroupStateUncompiled({pass.Count()}, FAIL, OR_01);");
                            }
                            else if (call.right.type == Call.Type.Variable)
                            {
                                Script.Flag gflag = scriptManager.GetFlag(Script.Flag.Designation.PlayerRuneCount, "PlayerRuneCount");
                                Script.Flag lflag = GetFlagByVariable(call.right.parameters[0]);
                                lines.Add(ResetConditionGroups());
                                lines.Add($"IfCompareEventValues(OR_01, {gflag.id}, {gflag.Bits()}, {call.OperatorIndex()}, {lflag.id}, {lflag.Bits()});");
                                lines.Add($"SkipIfConditionGroupStateUncompiled({pass.Count()}, FAIL, OR_01);");
                            }
                            else { Lort.Log($"## BAD CONDITIONAL ## {papyrus.id}->{call.type} [{call.left.type} ? {call.right.type}]", Lort.Type.Debug); }
                        }
                        // Checking if player has an item
                        else if (call.left.target == "player")
                        {
                            ItemManager.ItemInfo itemInfo = itemManager.GetItem(call.left.parameters[0].ToLower().Trim());
                            if (itemInfo == null) { throw new Exception("Script failed to find referenced item! This should not happen!"); }
                            Script.Flag itemCountFlag = scriptManager.common.GetOrCreateFlag(Script.Flag.Category.Temporary, Script.Flag.Type.Byte, Script.Flag.Designation.PlayerItemCount, itemInfo.id);

                            lines.Add(ResetConditionGroups());
                            lines.Add($"StoreItemAmountHeldInEventValue({itemInfo.EquipType()}, {itemInfo.row}, {itemCountFlag.id}, {itemCountFlag.Bits()});"); // Get item count and store in flag
                            if (call.right.type == Call.Type.Literal)
                            {
                                lines.Add($"IfEventValue(OR_01, {itemCountFlag.id}, {itemCountFlag.Bits()}, {call.OperatorIndex()}, {call.right.parameters[0]});"); // compare values
                                lines.Add($"SkipIfConditionGroupStateUncompiled({pass.Count()}, FAIL, OR_01);");
                            }
                            else if (call.right.type == Call.Type.Variable)
                            {
                                Script.Flag lflag = GetFlagByVariable(call.right.parameters[0]);
                                lines.Add($"IfCompareEventValues(OR_01, {itemCountFlag.id}, {itemCountFlag.Bits()}, {call.OperatorIndex()}, {lflag.id}, {lflag.Bits()});");  // compare values
                                lines.Add($"SkipIfConditionGroupStateUncompiled({pass.Count()}, FAIL, OR_01);");
                            }
                        }
                        // Checking if a non player character has gold or an item, we will simply assume true here because npcs don't have inventories in ER and assuming true is probably mostly fine
                        else
                        {
                            lines.Add($"SkipUnconditionally(0);"); // this is effectively a do nothing command to put in the spot of the if statment. effectively if(true)
                        }
                        break;

                    case Call.Type.Xbox:
                        if (call.right.type == Call.Type.Literal)
                        {
                            bool flagState = int.Parse(call.right.parameters[0]) == 1;
                            if (flagState) // xbox == 1
                            {
                                lines.Add($"SkipUnconditionally({pass.Count()});"); // skip to else
                            }
                            else           // xbox == 0
                            {
                                lines.Add($"SkipUnconditionally(0);"); // no op
                            }
                        }
                        else { Lort.Log($"## BAD CONDITIONAL ## {papyrus.id}->{call.type} [{call.left.type} ? {call.right.type}]", Lort.Type.Debug); }
                        break;

                    // will be refering to any papyrus script that is managed by StartScript, StopScript, ScriptRunning as "subscript" for clarity
                    case Call.Type.ScriptRunning:
                        {
                            if (call.right.type == Call.Type.Literal)
                            {
                                // Grab subscript papyrus
                                Papyrus subscript = esm.GetPapyrus(call.left.parameters[0]);
                                if (subscript == null) { break; } // failed to find script, this only happens because our papyrus parsing is still not 100% finished

                                // find our target content
                                Content target;
                                if (call.target == null) { target = content; }
                                else { target = layout.FindScriptReference(content, call.target); }
                                if (script is not ScriptCommon && target == null) { break; } // Failed to find script reference. Should only happen when making partial builds.

                                // find area script for this npc
                                BaseScript targetScript;
                                if (script is ScriptCommon) { targetScript = script; }
                                else { targetScript = scriptManager.FindScriptFor(layout, target); }

                                // See if the subscript is already created. this is needed as multiple scripts can potenitlaly start/stop the same subscript.
                                Script.Flag subscriptRunFlag;
                                if (targetScript is ScriptCommon) { subscriptRunFlag = scriptManager.GetFlag(Script.Flag.Designation.RunSubscript, $"Global->{subscript.id}"); }
                                else { subscriptRunFlag = scriptManager.GetFlag(Script.Flag.Designation.RunSubscript, $"{target.entity}->{subscript.id}"); }

                                // If the subscript does not exist yet, we create it
                                if (subscriptRunFlag == null)
                                {
                                    if (targetScript is ScriptCommon) { subscriptRunFlag = targetScript.CreateFlag(Script.Flag.Category.Saved, Script.Flag.Type.Bit, Script.Flag.Designation.RunSubscript, $"Global->{subscript.id}"); }
                                    else { subscriptRunFlag = targetScript.CreateFlag(Script.Flag.Category.Saved, Script.Flag.Type.Bit, Script.Flag.Designation.RunSubscript, $"{target.entity}->{subscript.id}"); }
                                    PapyrusEMEVD.Compile(esm, layout, msb, sound, scriptManager, paramanager, itemManager, speffManager, targetScript, subscript, target, subscriptRunFlag);
                                }

                                // Finally we create an if condition based off the value of the subscript run flag
                                bool flagState = int.Parse(call.right.parameters[0]) == 0;
                                lines.Add($"SkipIfEventFlag({pass.Count()}, {(flagState ? "ON" : "OFF")}, TargetEventFlagType.EventFlag, {subscriptRunFlag.id});");
                            }
                            else { Lort.Log($"## BAD CONDITIONAL ## {papyrus.id}->{call.type} [{call.left.type} ? {call.right.type}]", Lort.Type.Debug); }
                            break;
                        }

                    default:   // unsupported calls will default to a FALSE result using SkipUnconditinoally()
                        if(!UNSUPPORTED_CONDITIONAL_LIST.Contains(call.type)) { Lort.Log($" ## WARNING ## Unsupported Papyrus->EMEVD conditional {papyrus.id}->{call.type} [{call.left.type} ? {call.right.type}]", Lort.Type.Debug); UNSUPPORTED_CONDITIONAL_LIST.Add(call.type); }
                        if (pass.Count() > 0) { lines.Add($"SkipUnconditionally({pass.Count()});"); }
                        break;
                }

                lines.AddRange(pass);
                lines.AddRange(fail);
                lines.AddRange(post);

                return lines;
            }

            List<string> HandleScope(Papyrus.Scope scope)
            {
                List<string> lines = new();
                foreach (Papyrus.Call call in scope.calls)
                {
                    lines.AddRange(HandlePapyrus(call));
                }
                return lines;
            }

            List<string> HandleCall(Papyrus.Call call)
            {
                List<string> lines = new();
                switch (call.type)
                {
                    case Call.Type.Set:
                        {
                            // Parse set command to individual operations
                            List<(string op, Call call)> operations = new();
                            List<string> tempParameters = new(); string lastOp = "=";
                            for (int i=2;i<call.parameters.Length;i++)
                            {
                                string p = call.parameters[i];
                                if (Utility.StringIsOperator(p))
                                {
                                    string rejoin = string.Join(" ", tempParameters);
                                    if(rejoin.Contains(" "))
                                    {
                                        if(rejoin.Contains(".")) { rejoin = $"\"{rejoin.Replace(".", "\".")}"; }  // fix for variables with spaces in their names
                                        else { rejoin = $"\"{rejoin}\""; }
                                    }

                                    operations.Add(new(lastOp, new Call(rejoin)));
                                    tempParameters.Clear();
                                    lastOp = p;
                                }
                                else { tempParameters.Add(p); }
                            }
                            operations.Add(new(lastOp, new Call(string.Join(" ", tempParameters))));

                            // Little function to convert a string of an operator like "+" or "=" to the correct EventValueOperation ID
                            int GetEventValueOperator(string op)
                            {
                                switch(op)
                                {
                                    case "+": return 0;
                                    case "-": return 1;
                                    case "*": return 2;
                                    case "/": return 3;
                                    case "%": return 4;
                                    case "=": return 5;
                                    default: throw new Exception("GUH!");
                                }
                            }

                            // Resolve those operations as best as we can
                            Script.Flag lflag = GetFlagByVariable(call.parameters[0]);
                            if (lflag == null) { break; } // another safety break. there is a tribunal script that fails to declare a var "done" and trys to set it resulting in a null
                            Script.Flag tempFlag = script.GetOrCreateFlag(Script.Flag.Category.Temporary, Script.Flag.Type.Short, Script.Flag.Designation.Hardcode, $"{papyrus.id}->setworkvalue"); // create temp value to do math on
                            lines.Add($"EventValueOperation({tempFlag.id}, {tempFlag.Bits()}, 0, 0, 1, 5);"); // assign temp value to 0 before we start
                            foreach ((string op, Call call) operation in operations)
                            {
                                switch (operation.call.type)
                                {
                                    case Call.Type.Literal:
                                        lines.Add($"EventValueOperation({tempFlag.id}, {tempFlag.Bits()}, {int.Parse(operation.call.parameters[0])}, 0, 1, {GetEventValueOperator(operation.op)});");
                                        break;
                                    case Call.Type.Variable:
                                        Script.Flag l2flag = GetFlagByVariable(operation.call.parameters[0]);
                                        lines.Add($"EventValueOperation({tempFlag.id}, {tempFlag.Bits()}, 0, {l2flag.id}, {l2flag.Bits()}, {GetEventValueOperator(operation.op)});");
                                        break;
                                    case Call.Type.GetJournalIndex:
                                        Script.Flag jflag = scriptManager.common.GetOrCreateFlag(Script.Flag.Category.Saved, Script.Flag.Type.Byte, Script.Flag.Designation.Journal, operation.call.parameters[0]);
                                        lines.Add($"EventValueOperation({tempFlag.id}, {tempFlag.Bits()}, 0, {jflag.id}, {jflag.Bits()}, {GetEventValueOperator(operation.op)});");
                                        break;
                                    case Call.Type.GetButtonPressed:
                                        Script.Flag bflag = scriptManager.GetFlag(Script.Flag.Designation.GetButtonPressedValue, content.entity.ToString());
                                        lines.Add($"EventValueOperation({tempFlag.id}, {tempFlag.Bits()}, 0, {bflag.id}, {bflag.Bits()}, {GetEventValueOperator(operation.op)});");
                                        lines.Add($"EventValueOperation({bflag.id}, {bflag.Bits()}, {ushort.MaxValue}, 0, 1, 5);"); // reset value after reading it
                                        break;
                                    case Call.Type.Random:
                                        Script.Flag randFlag = scriptManager.common.GetOrRegisterRandom(int.Parse(operation.call.parameters[0]));
                                        lines.Add($"EventValueOperation({tempFlag.id}, {tempFlag.Bits()}, 0, {randFlag.id}, {randFlag.Bits()}, {GetEventValueOperator(operation.op)});");
                                        break;
                                    case Call.Type.GetSecondsPassed:
                                        Script.Flag gspFlag = RegisterGetSecondsPassed();
                                        lines.Add($"EventValueOperation({tempFlag.id}, {tempFlag.Bits()}, 0, {gspFlag.id}, {gspFlag.Bits()}, {GetEventValueOperator(operation.op)});");
                                        lines.Add($"EventValueOperation({gspFlag.id}, {gspFlag.Bits()}, 0, 0, 1, 5);"); // reset value after reading it
                                        break;
                                    default: if (!UNSUPPORTED_SET_LIST.Contains(operation.call.type)) { Lort.Log($" ## WARNING ## Unsupported Papyrus->EMEVD set operation call {papyrus.id}->{call.type}->{operation.call.type}", Lort.Type.Debug); UNSUPPORTED_SET_LIST.Add(operation.call.type); }
                                        break;
                                }
                            }
                            lines.Add($"EventValueOperation({lflag.id}, {lflag.Bits()}, 0, {tempFlag.id}, {tempFlag.Bits()}, 5);"); // assign temp value to actual var now that we are done with calculation
                            break;
                        }

                    case Call.Type.Journal:
                        {
                            Script.Flag jflag = scriptManager.common.GetOrCreateFlag(Script.Flag.Category.Saved, Script.Flag.Type.Byte, Script.Flag.Designation.Journal, call.parameters[0]);
                            Script.Flag mflag = scriptManager.common.GetOrRegisterNotification(paramanager, "Your journal has been updated!");
                            lines.Add($"EventValueOperation({jflag.id}, {jflag.Bits()}, {int.Parse(call.parameters[1])}, 0, 1, 5);");  // 5 is the 'Assign' operation
                            lines.Add($"SetEventFlag(TargetEventFlagType.EventFlag, {mflag.id}, ON);");
                            break;
                        }

                    case Call.Type.AddSpell:
                        {
                            SpeffManager.SpeffSpell spell = speffManager.GetSpellSpeff(call.parameters[0]);
                            if (call.target == "player")
                            {
                                if (spell.spellType == SpeffManager.SpeffSpell.SpellType.Spell || spell.spellType == SpeffManager.SpeffSpell.SpellType.Power)
                                {
                                    // @TODO: stub. should give the player the item of a spell. we don't really have those all mapped out yet though so guh
                                }
                                else
                                {
                                    lines.Add($"SetEventFlag(TargetEventFlagType.EventFlag, {spell.flag.id}, ON);");
                                }
                            }
                            break;
                        }

                    case Call.Type.RemoveSpell:
                        {
                            SpeffManager.SpeffSpell spell = speffManager.GetSpellSpeff(call.parameters[0]);
                            if (call.target == "player")
                            {
                                if (spell.spellType == SpeffManager.SpeffSpell.SpellType.Spell || spell.spellType == SpeffManager.SpeffSpell.SpellType.Power)
                                {
                                    // @TODO: stub. this should remove a spell item from a players inventory but we dont have those mapped out yet
                                }
                                else
                                {
                                    lines.Add($"SetEventFlag(TargetEventFlagType.EventFlag, {spell.flag.id}, OFF);");
                                }
                            }
                            break;
                        }

                    case Call.Type.AddTopic:
                        {
                            Script.Flag tvar = scriptManager.GetFlag(Script.Flag.Designation.TopicEnabled, call.parameters[0]);
                            lines.Add($"SetEventFlag(TargetEventFlagType.EventFlag, {tvar.id}, ON);");
                            break;
                        }

                    case Call.Type.Cast:
                        {
                            SpeffManager.SpeffSpell spell = speffManager.GetSpellSpeff(call.parameters[0]);
                            if (call.parameters[1].ToLower().Trim() == "player")
                            {
                                if (spell.spellType == SpeffManager.SpeffSpell.SpellType.Spell || spell.spellType == SpeffManager.SpeffSpell.SpellType.Power)
                                {
                                    string code = $"SetSpEffect(10000, {spell.row});";
                                    lines.Add(code);
                                }
                            }
                            break;
                        }

                    case Call.Type.ChangeWeather:
                        {
                            ScriptCommon.WeatherPapyrus mww = (ScriptCommon.WeatherPapyrus)int.Parse(call.parameters[1]);
                            ScriptCommon.WeatherEMEVD erw;
                            switch(mww)
                            {
                                case ScriptCommon.WeatherPapyrus.Clear: erw = ScriptCommon.WeatherEMEVD.None; break;
                                case ScriptCommon.WeatherPapyrus.Cloudy: erw = ScriptCommon.WeatherEMEVD.PuffyClouds; break;
                                case ScriptCommon.WeatherPapyrus.Foggy: erw = ScriptCommon.WeatherEMEVD.Fog; break;
                                case ScriptCommon.WeatherPapyrus.Overcast: erw = ScriptCommon.WeatherEMEVD.FlatClouds; break;
                                case ScriptCommon.WeatherPapyrus.Rain: erw = ScriptCommon.WeatherEMEVD.Rain; break;
                                case ScriptCommon.WeatherPapyrus.Thunder: erw = ScriptCommon.WeatherEMEVD.WindyRain; break;
                                case ScriptCommon.WeatherPapyrus.Ash: erw = ScriptCommon.WeatherEMEVD.HeavyFog; break;
                                case ScriptCommon.WeatherPapyrus.Blight: erw = ScriptCommon.WeatherEMEVD.HeavyFog; break;
                                case ScriptCommon.WeatherPapyrus.Snow: erw = ScriptCommon.WeatherEMEVD.Snow; break;
                                case ScriptCommon.WeatherPapyrus.Blizzard: erw = ScriptCommon.WeatherEMEVD.SnowyHeavyFog; break;
                                default: throw new Exception("Invalid weather enum"); // unreachable
                            }
                            lines.Add($"ChangeWeather({(int)erw}, 1000, true);");
                            break;
                        }

                    case Call.Type.Enable:
                    case Call.Type.Disable:
                        {
                            // find our target content
                            Content target;
                            if (call.target == null) { target = content; }
                            else { target = layout.FindScriptReference(content, call.target); }
                            if (target == null) { break; } // Failed to find script reference. Should only happen when making partial builds.

                            /* Get flag and/or register script */
                            Script.Flag disabledFlag = scriptManager.GetFlag(Script.Flag.Designation.Disabled, target.entity.ToString());

                            /* Add code */
                            string toggle = call.type == Call.Type.Disable ? "ON" : "OFF";
                            lines.Add($"SetEventFlag(TargetEventFlagType.EventFlag, {disabledFlag.id}, {toggle});");

                            /* For enable/disable we also need to manually set the enable state but contextually based on the objects currrent status */
                            string able = call.type == Call.Type.Disable ? "Disabled" : "Enabled";
                            switch (target)
                            {
                                case CharacterContent c:
                                    {
                                        if (!c.dead)  // dead bodies don't need this alive/dead check. they can be treated like staticcontent but using CharacterEnable instead of AssetEnable
                                        {
                                            Script.Flag deadFlag = scriptManager.GetFlag(Script.Flag.Designation.Dead, target.entity.ToString());
                                            lines.Add($"SkipIfEventFlag(1, ON, TargetEventFlagType.EventFlag, {deadFlag.id});"); // if character is dead skip enable/disable
                                        }
                                        lines.Add($"ChangeCharacterEnableState({target.entity}, {able});");
                                        break;
                                    }
                                case ItemContent i:
                                    {
                                        if(i.treasure == null) { goto default; }  // items that dont have treasure are literally just statics so treat them as such
                                        lines.Add($"SkipIfEventFlag(1, ON, TargetEventFlagType.EventFlag, {i.treasure.id});"); // if item already picked up then skip enable/disable
                                        lines.Add($"ChangeAssetEnableState({target.entity}, {able});");
                                        break;
                                    }
                                default:
                                    {
                                        lines.Add($"ChangeAssetEnableState({target.entity}, {able});"); // just send it, no issues
                                        break;
                                    }
                            }
                            break;
                        }

                    case Call.Type.AddItem:
                        {
                            int amount = int.Parse(call.parameters[1]);

                            // player
                            if (call.target == "player")
                            {
                                // Gold specifically handled as souls
                                if (call.parameters[0] == "gold_001")
                                {
                                    int speffId = speffManager.CreateScriptedEffect(SpeffManager.StatMod.Runes, amount, call.RAW);
                                    lines.Add($"SetSpEffect(10000, {speffId});");
                                }
                                // Any other item
                                else
                                {
                                    ItemManager.ItemInfo itemInfo = itemManager.GetItem(call.parameters[0].ToLower());
                                    if (itemInfo == null) { throw new Exception("Script failed to find referenced item! This should not happen!"); }
                                    int row = paramanager.GenerateAddItemLot(itemInfo, int.Parse(call.parameters[1]));
                                    lines.Add($"AwardItemLot({row});");
                                }
                            }
                            // not the player
                            else
                            {
                                // unsupported
                            }
                            break;
                        }

                    case Call.Type.RemoveItem:
                        {
                            int amount = int.Parse(call.parameters[1]);

                            // player
                            if (call.target == "player")
                            {
                                // Gold specifically handled as souls
                                if (call.parameters[0] == "gold_001")
                                {
                                    int speffId = speffManager.CreateScriptedEffect(SpeffManager.StatMod.Runes, -amount, call.RAW);
                                    lines.Add($"SetSpEffect(10000, {speffId});");
                                }
                                // Any other item
                                else
                                {
                                    ItemManager.ItemInfo itemInfo = itemManager.GetItem(call.parameters[0].ToLower());
                                    if (itemInfo == null) { throw new Exception(); }
                                    lines.Add($"RemoveItemFromPlayer({(int)itemInfo.type}, {itemInfo.row}, {call.parameters[1]});");
                                }
                            }
                            // not the player
                            else
                            {
                                // unsupported
                            }
                            break;
                        }

                    case Call.Type.FadeIn:
                        {
                            float time = float.Parse(call.parameters[0]);
                            lines.Add($"FadeToBlack(0, {time}, false, 0);");
                            break;
                        }

                    case Call.Type.FadeOut:
                        {
                            float time = float.Parse(call.parameters[0]);
                            lines.Add($"FadeToBlack(1, {time}, true, 0);");
                            break;
                        }

                    case Call.Type.MessageBox:
                        {
                            const int nibbleMaxValue = 15;  // loll

                            /* get button count */
                            int varCount = call.parameters[0].Count(c => c == '%');
                            int buttonCount = call.parameters.Count() - varCount - 1;

                            /* Single or no button messagebox is simple */
                            if (buttonCount <= 1)
                            {
                                int messageRow = paramanager.GenerateMessage("Message", call.parameters[0]);
                                lines.Add($"ShowTutorialPopup({messageRow}, true, true);");
                            }
                            /* Very specific case where a message box has buttons that appear to be yes/no */
                            else if(buttonCount == 2 && (call.parameters[varCount + 1].ToLower() == "yes" || call.parameters[varCount + 2].ToLower() == "no"))
                            {
                                int buttonTextId;

                                // If messagebox is kinda long use a full size textbox
                                if (call.parameters[0].Length > 75)
                                {
                                    int messageRow = paramanager.GenerateMessage("Message", call.parameters[0]);
                                    buttonTextId = paramanager.textManager.AddMapEventText($"");
                                    lines.Add($"ShowTutorialPopup({messageRow}, true, true);");
                                }
                                // Otherwise just put it in the prompt
                                else
                                {
                                    buttonTextId = paramanager.textManager.AddMapEventText(call.parameters[0]);
                                }

                                // even though you can have multiple messagebox calls per script, since its blocking they can share temp flags for values
                                Script.Flag buttonChoicePass = scriptManager.GetFlag(Script.Flag.Designation.GetButtonPressedBit, $"{content.entity} pass");
                                Script.Flag buttonChoiceFail = scriptManager.GetFlag(Script.Flag.Designation.GetButtonPressedBit, $"{content.entity} fail");
                                Script.Flag buttonChoiceValue = scriptManager.GetFlag(Script.Flag.Designation.GetButtonPressedValue, content.entity.ToString());
                                if (buttonChoiceValue == null) // if any of these are null then they all are because we create them only in this scope as a group
                                {
                                    buttonChoicePass = script.CreateFlag(Script.Flag.Category.Temporary, Script.Flag.Type.Bit, Script.Flag.Designation.GetButtonPressedBit, $"{content.entity} pass");
                                    buttonChoiceFail = script.CreateFlag(Script.Flag.Category.Temporary, Script.Flag.Type.Bit, Script.Flag.Designation.GetButtonPressedBit, $"{content.entity} fail");
                                    buttonChoiceValue = script.CreateFlag(Script.Flag.Category.Temporary, Script.Flag.Type.Nibble, Script.Flag.Designation.GetButtonPressedValue, content.entity.ToString());
                                }
                                lines.Add($"EventValueOperation({buttonChoiceValue.id}, {buttonChoiceValue.Bits()}, {nibbleMaxValue}, 0, 1, 5);"); // reset value before we show prompts
                                lines.Add($"WaitFixedTimeFrames(1);"); // wait 1 frame before showing prompt to avoid menus overlapping
                                lines.Add(ResetConditionGroups());
                                lines.Add($"IfEventValue(OR_01, {buttonChoiceValue.id}, {buttonChoiceValue.Bits()}, 0, {nibbleMaxValue});");
                                lines.Add($"SkipIfConditionGroupStateUncompiled({8}, FAIL, OR_01);"); // if the player made a choice we skip the rest of the button choices

                                lines.Add($"DisplayGenericDialogAndSetEventFlags({buttonTextId}, 0, 2, {content.entity}, 3, {buttonChoicePass.id}, {buttonChoiceFail.id}, {buttonChoiceFail.id});");
                                lines.Add($"IfEventFlag(OR_02, ON, TargetEventFlagType.EventFlag, {buttonChoicePass.id});");
                                lines.Add($"IfEventFlag(OR_02, ON, TargetEventFlagType.EventFlag, {buttonChoiceFail.id});");
                                lines.Add($"IfConditionGroup(MAIN, PASS, OR_02);");   // INTENTIONAL BLOCKING!
                                lines.Add($"SkipIfEventFlag(2, OFF, TargetEventFlagType.EventFlag, {buttonChoicePass.id});");
                                lines.Add($"EventValueOperation({buttonChoiceValue.id}, {buttonChoiceValue.Bits()}, 0, 0, 1, 5);");
                                lines.Add($"SkipUnconditionally(1);");
                                lines.Add($"EventValueOperation({buttonChoiceValue.id}, {buttonChoiceValue.Bits()}, 1, 0, 1, 5);");
                                lines.Add($"SetEventFlag(TargetEventFlagType.EventFlag, {buttonChoicePass.id}, OFF);");
                                lines.Add($"SetEventFlag(TargetEventFlagType.EventFlag, {buttonChoiceFail.id}, OFF);");
                            }
                            /* If there is a call to MessageBox with more than 1 button we need to do some complex stuff to create button prompts for GetButtonPressed to read */
                            /* This functionality IS BLOCKING! This is okay though because morrowind pauses when you do a message box so blocking here is still parity! */
                            else
                            {
                                int messageRow = paramanager.GenerateMessage("Message", call.parameters[0]);
                                List<int> buttonRow = new();
                                for (int i = varCount + 1; i < call.parameters.Count(); i++)
                                {
                                    buttonRow.Add(paramanager.textManager.AddMapEventText($"Select Option: {call.parameters[i]}"));
                                }

                                lines.Add($"ShowTutorialPopup({messageRow}, true, true);");
                                // even though you can have multiple messagebox calls per script, since its blocking they can share temp flags for values
                                Script.Flag buttonChoicePass = scriptManager.GetFlag(Script.Flag.Designation.GetButtonPressedBit, $"{content.entity} pass");
                                Script.Flag buttonChoiceFail = scriptManager.GetFlag(Script.Flag.Designation.GetButtonPressedBit, $"{content.entity} fail");
                                Script.Flag buttonChoiceValue = scriptManager.GetFlag(Script.Flag.Designation.GetButtonPressedValue, content.entity.ToString());
                                if (buttonChoiceValue == null) // if any of these are null then they all are because we create them only in this scope as a group
                                {
                                    buttonChoicePass = script.CreateFlag(Script.Flag.Category.Temporary, Script.Flag.Type.Bit, Script.Flag.Designation.GetButtonPressedBit, $"{content.entity} pass");
                                    buttonChoiceFail = script.CreateFlag(Script.Flag.Category.Temporary, Script.Flag.Type.Bit, Script.Flag.Designation.GetButtonPressedBit, $"{content.entity} fail");
                                    buttonChoiceValue = script.CreateFlag(Script.Flag.Category.Temporary, Script.Flag.Type.Nibble, Script.Flag.Designation.GetButtonPressedValue, content.entity.ToString());
                                }
                                lines.Add($"EventValueOperation({buttonChoiceValue.id}, {buttonChoiceValue.Bits()}, {nibbleMaxValue}, 0, 1, 5);"); // reset value before we show prompts
                                lines.Add($"WaitFixedTimeFrames(1);"); // wait 1 frame before showing prompt to avoid menus overlapping
                                for (int i = 0; i < buttonRow.Count(); i++) {
                                    int buttonTextId = buttonRow[i];

                                    lines.Add(ResetConditionGroups());
                                    lines.Add($"IfEventValue(OR_01, {buttonChoiceValue.id}, {buttonChoiceValue.Bits()}, 0, {nibbleMaxValue});");
                                    lines.Add($"SkipIfConditionGroupStateUncompiled({8}, FAIL, OR_01);"); // if the player made a choice we skip the rest of the button choices

                                    lines.Add($"DisplayGenericDialogAndSetEventFlags({buttonTextId}, 0, 2, {content.entity}, 3, {buttonChoicePass.id}, {buttonChoiceFail.id}, {buttonChoiceFail.id});");
                                    lines.Add($"IfEventFlag(OR_02, ON, TargetEventFlagType.EventFlag, {buttonChoicePass.id});");
                                    lines.Add($"IfEventFlag(OR_02, ON, TargetEventFlagType.EventFlag, {buttonChoiceFail.id});");
                                    lines.Add($"IfConditionGroup(MAIN, PASS, OR_02);");   // INTENTIONAL BLOCKING!
                                    lines.Add($"SkipIfEventFlag(1, OFF, TargetEventFlagType.EventFlag, {buttonChoicePass.id});");
                                    lines.Add($"EventValueOperation({buttonChoiceValue.id}, {buttonChoiceValue.Bits()}, {i}, 0, 1, 5);");
                                    lines.Add($"SetEventFlag(TargetEventFlagType.EventFlag, {buttonChoicePass.id}, OFF);");
                                    lines.Add($"SetEventFlag(TargetEventFlagType.EventFlag, {buttonChoiceFail.id}, OFF);");
                                }

                                // if player did not make any selection we default to the last option in the list. not doing this results in broken scripts!
                                lines.Add($"IfEventValue(OR_03, {buttonChoiceValue.id}, {buttonChoiceValue.Bits()}, 0, {nibbleMaxValue});");
                                lines.Add($"SkipIfConditionGroupStateUncompiled({1}, FAIL, OR_03);");
                                lines.Add($"EventValueOperation({buttonChoiceValue.id}, {buttonChoiceValue.Bits()}, {buttonRow.Count() - 1}, 0, 1, 5);");

                            }
                            break;
                        }

                    case Call.Type.ModCurrentHealth:
                    case Call.Type.ModCurrentMagicka:
                    case Call.Type.ModCurrentFatigue:
                        {
                            uint entityId;
                            if (call.target == null) { entityId = content.entity; }                      // case 1: no target so current object is target
                            else if (call.target == "player") { entityId = 10000; }                      // case 2: target is player
                            else                                                                         // case 3: target is a direct reference to an object record
                            {
                                entityId = layout.FindScriptReference(content, call.target).entity;
                            }

                            int amount = int.Parse(call.parameters[0]);
                            SpeffManager.StatMod statToMod;
                            switch (call.type)
                            {
                                case Call.Type.ModCurrentHealth: statToMod = SpeffManager.StatMod.CurrentHP; break;
                                case Call.Type.ModCurrentMagicka: statToMod = SpeffManager.StatMod.CurrentMP; break;
                                case Call.Type.ModCurrentFatigue: statToMod = SpeffManager.StatMod.CurrentSP; break;
                                default: throw new Exception("Invalid papyrus call type");  // unreachable
                            }

                            int speffId = speffManager.CreateScriptedEffect(statToMod, amount, call.RAW);

                            lines.Add($"SetSpEffect({entityId}, {speffId});");

                            break;
                        }


                    case Call.Type.ModHealth:
                    case Call.Type.ModMagicka:
                    case Call.Type.ModFatigue:
                    case Call.Type.ModStrength:
                    case Call.Type.ModIntelligence:
                    case Call.Type.ModWillpower:
                    case Call.Type.ModAgility:
                    case Call.Type.ModSpeed:
                    case Call.Type.ModEndurance:
                    case Call.Type.ModPersonality:
                    case Call.Type.ModLuck:
                    case Call.Type.ModAcrobatics:
                    case Call.Type.ModAlchemy:
                    case Call.Type.ModAlteration:
                    case Call.Type.ModArmorer:
                    case Call.Type.ModAthletics:
                    case Call.Type.ModAxe:
                    case Call.Type.ModBlock:
                    case Call.Type.ModBluntWeapon:
                    case Call.Type.ModConjuration:
                    case Call.Type.ModDestruction:
                    case Call.Type.ModEnchant:
                    case Call.Type.ModHandToHand:
                    case Call.Type.ModHeavyArmor:
                    case Call.Type.ModIllusion:
                    case Call.Type.ModLightArmor:
                    case Call.Type.ModLongBlade:
                    case Call.Type.ModMarksman:
                    case Call.Type.ModMediumArmor:
                    case Call.Type.ModMercantile:
                    case Call.Type.ModMysticism:
                    case Call.Type.ModRestoration:
                    case Call.Type.ModSecurity:
                    case Call.Type.ModShortBlade:
                    case Call.Type.ModSneak:
                    case Call.Type.ModSpear:
                    case Call.Type.ModSpeechcraft:
                    case Call.Type.ModUnarmored:
                        {
                            Content target;
                            uint entityId;
                            if (call.target == null) { target = content; entityId = target.entity; }                        // case 1: no target so current object is target
                            else if (call.target == "player") { target = null; entityId = 10000; }                         // case 2: target is player
                            else { target = layout.FindScriptReference(content, call.target); entityId = target.entity; } // case 3: target is a direct reference to an object record

                            /* Not the player, modify stat via SPEFF */
                            if (entityId != 10000)
                            {
                                Script areaScript = scriptManager.FindScriptFor(layout, target);

                                int amount = int.Parse(call.parameters[0]);
                                SpeffManager.StatMod statToMod;
                                switch (call.type)
                                {
                                    /* Stats */
                                    case Call.Type.ModHealth: statToMod = SpeffManager.StatMod.MaxHP; break;
                                    case Call.Type.ModMagicka: statToMod = SpeffManager.StatMod.MaxMP; break;
                                    case Call.Type.ModFatigue: statToMod = SpeffManager.StatMod.MaxSP; break;
                                    /* Attributes */
                                    case Call.Type.ModStrength: statToMod = SpeffManager.StatMod.Strength; break;
                                    case Call.Type.ModIntelligence: statToMod = SpeffManager.StatMod.Intelligence; break;
                                    case Call.Type.ModWillpower: statToMod = SpeffManager.StatMod.Mind; break;
                                    case Call.Type.ModAgility: statToMod = SpeffManager.StatMod.Dexterity; break;
                                    case Call.Type.ModSpeed: statToMod = SpeffManager.StatMod.Dexterity; break;
                                    case Call.Type.ModEndurance: statToMod = SpeffManager.StatMod.Endurance; break;
                                    case Call.Type.ModPersonality: statToMod = SpeffManager.StatMod.Arcane; break;
                                    case Call.Type.ModLuck: statToMod = SpeffManager.StatMod.Arcane; break;
                                    /* Skills */
                                    case Call.Type.ModArmorer:
                                    case Call.Type.ModAcrobatics:
                                    case Call.Type.ModAxe:
                                    case Call.Type.ModBluntWeapon:
                                    case Call.Type.ModLongBlade:
                                        {
                                            amount = (int)(amount * 0.33f); // skill bonuses are reduced
                                            statToMod = SpeffManager.StatMod.Strength;
                                            break;
                                        }
                                    case Call.Type.ModAlchemy:
                                    case Call.Type.ModConjuration:
                                    case Call.Type.ModEnchant:
                                    case Call.Type.ModSecurity:
                                        {
                                            amount = (int)(amount * 0.33f); // skill bonuses are reduced
                                            statToMod = SpeffManager.StatMod.Intelligence;
                                            break;
                                        }
                                    case Call.Type.ModAlteration:
                                    case Call.Type.ModDestruction:
                                    case Call.Type.ModMysticism:
                                        {
                                            amount = (int)(amount * 0.33f); // skill bonuses are reduced
                                            statToMod = SpeffManager.StatMod.Mind;
                                            break;
                                        }
                                    case Call.Type.ModRestoration:
                                        {
                                            amount = (int)(amount * 0.33f); // skill bonuses are reduced
                                            statToMod = SpeffManager.StatMod.Faith;
                                            break;
                                        }
                                    case Call.Type.ModAthletics:
                                    case Call.Type.ModHandToHand:
                                    case Call.Type.ModShortBlade:
                                    case Call.Type.ModUnarmored:
                                    case Call.Type.ModBlock:
                                    case Call.Type.ModLightArmor:
                                    case Call.Type.ModMarksman:
                                    case Call.Type.ModSneak:
                                        {
                                            amount = (int)(amount * 0.33f); // skill bonuses are reduced
                                            statToMod = SpeffManager.StatMod.Dexterity;
                                            break;
                                        }
                                    case Call.Type.ModHeavyArmor:
                                    case Call.Type.ModMediumArmor:
                                    case Call.Type.ModSpear:
                                        {
                                            amount = (int)(amount * 0.33f); // skill bonuses are reduced
                                            statToMod = SpeffManager.StatMod.Endurance;
                                            break;
                                        }
                                    case Call.Type.ModIllusion:
                                    case Call.Type.ModMercantile:
                                    case Call.Type.ModSpeechcraft:
                                        {
                                            amount = (int)(amount * 0.33f); // skill bonuses are reduced
                                            statToMod = SpeffManager.StatMod.Arcane;
                                            break;
                                        }
                                    default: throw new Exception("Invalid papyrus call type");  // unreachable
                                }

                                int speffId = speffManager.CreateScriptedEffect(statToMod, amount, call.RAW);
                                Script.Flag modStatFlag = areaScript.RegisterModStat(entityId, speffId);

                                lines.Add($"SetEventFlag(TargetEventFlagType.EventFlag, {modStatFlag.id}, ON);");  // turn on flag to make this speff permanent on the npc
                                lines.Add($"SetSpEffect({entityId}, {speffId});");                                 // and apply the speff for right now
                            }
                            /* For player, modify stats via HKS nonsense */
                            else
                            {
                                string statFlagName;
                                int amount = int.Parse(call.parameters[0]);

                                switch (call.type)
                                {
                                    /* Stats */
                                    case Call.Type.ModHealth:
                                        amount = (int)(amount * 0.1f); // direct stat values are heavily reducded
                                        statFlagName = "SetVigor";
                                        break;
                                    case Call.Type.ModMagicka:
                                        amount = (int)(amount * 0.1f); // direct stat values are heavily reducded
                                        statFlagName = "SetMind";
                                        break;
                                    case Call.Type.ModFatigue:
                                        amount = (int)(amount * 0.1f); // direct stat values are heavily reducded
                                        statFlagName = "SetEndurance";
                                        break;
                                    /* Attributes */
                                    case Call.Type.ModStrength: statFlagName = "SetStrength"; break;
                                    case Call.Type.ModIntelligence: statFlagName = "SetIntelligence"; break;
                                    case Call.Type.ModWillpower: statFlagName = "SetMind"; break;
                                    case Call.Type.ModAgility: statFlagName = "SetDexterity"; break;
                                    case Call.Type.ModSpeed: statFlagName = "SetDexterity"; break;
                                    case Call.Type.ModEndurance: statFlagName = "SetEndurance"; break;
                                    case Call.Type.ModPersonality: statFlagName = "SetArcane"; break;
                                    case Call.Type.ModLuck: statFlagName = "SetArcane"; break;
                                    /* Skills */
                                    case Call.Type.ModArmorer:
                                    case Call.Type.ModAcrobatics:
                                    case Call.Type.ModAxe:
                                    case Call.Type.ModBluntWeapon:
                                    case Call.Type.ModLongBlade:
                                        {
                                            amount = (int)(amount * 0.33f); // skill bonuses are reduced
                                            statFlagName = "SetStrength";
                                            break;
                                        }
                                    case Call.Type.ModAlchemy:
                                    case Call.Type.ModConjuration:
                                    case Call.Type.ModEnchant:
                                    case Call.Type.ModSecurity:
                                        {
                                            amount = (int)(amount * 0.33f); // skill bonuses are reduced
                                            statFlagName = "SetIntelligence";
                                            break;
                                        }
                                    case Call.Type.ModAlteration:
                                    case Call.Type.ModDestruction:
                                    case Call.Type.ModMysticism:
                                        {
                                            amount = (int)(amount * 0.33f); // skill bonuses are reduced
                                            statFlagName = "SetMind";
                                            break;
                                        }
                                    case Call.Type.ModRestoration:
                                        {
                                            amount = (int)(amount * 0.33f); // skill bonuses are reduced
                                            statFlagName = "SetFaith";
                                            break;
                                        }
                                    case Call.Type.ModAthletics:
                                    case Call.Type.ModHandToHand:
                                    case Call.Type.ModShortBlade:
                                    case Call.Type.ModUnarmored:
                                    case Call.Type.ModBlock:
                                    case Call.Type.ModLightArmor:
                                    case Call.Type.ModMarksman:
                                    case Call.Type.ModSneak:
                                        {
                                            amount = (int)(amount * 0.33f); // skill bonuses are reduced
                                            statFlagName = "SetDexterity";
                                            break;
                                        }
                                    case Call.Type.ModHeavyArmor:
                                    case Call.Type.ModMediumArmor:
                                    case Call.Type.ModSpear:
                                        {
                                            amount = (int)(amount * 0.33f); // skill bonuses are reduced
                                            statFlagName = "SetEndurance";
                                            break;
                                        }
                                    case Call.Type.ModIllusion:
                                    case Call.Type.ModMercantile:
                                    case Call.Type.ModSpeechcraft:
                                        {
                                            amount = (int)(amount * 0.33f); // skill bonuses are reduced
                                            statFlagName = "SetArcane";
                                            break;
                                        }
                                    default: throw new Exception("Invalid papyrus call type");  // unreachable
                                }

                                Script.Flag statFlag = scriptManager.GetFlag(Script.Flag.Designation.Hardcode, statFlagName);
                                lines.Add($"EventValueOperation({statFlag.id}, {statFlag.Bits()}, {100 + amount}, 0, 1, 5);");  // the SetStat hks hack offsets value by 100 to allow lowering stats EX: 100 + (-5)
                            }

                            break;
                        }

                    case Call.Type.ModDisposition:
                        {
                            Content target;
                            if (call.target == null) { target = content; }                      // case 1: no target so current object is target
                            else                                                                // case 2: target is a direct reference to an object record
                            {
                                target = layout.FindScriptReference(content, call.target);
                            }

                            if (target == null) { break; } // during partial builds the reference may not resolve

                            Script.Flag rvar = scriptManager.GetFlag(Script.Flag.Designation.Disposition, target.entity.ToString());
                            lines.Add(ResetConditionGroups());
                            lines.Add($"EventValueOperation({rvar.id}, {rvar.Bits()}, {int.Parse(call.parameters[0])}, 0, 1, 0);");  // add value to disposition
                            lines.Add($"IfEventValue(OR_01, {rvar.id}, {rvar.Bits()}, 4, 100);");                                   // if disposition is greater or equal to 100
                            lines.Add($"SkipIfConditionGroupStateUncompiled(1, FAIL, OR_01);");
                            lines.Add($"EventValueOperation({rvar.id}, {rvar.Bits()}, 100, 0, 1, 0);");                           // set disposition to 100
                            break;
                        }

                    case Call.Type.ModReputation:
                        {
                            Script.Flag rvar = scriptManager.GetFlag(Script.Flag.Designation.Reputation, "Reputation");
                            lines.Add($"EventValueOperation({rvar.id}, {rvar.Bits()}, {int.Parse(call.parameters[0])}, 0, 1, 0);"); // 0 is Add
                            break;
                        }

                    case Call.Type.ModPcFacRep:
                        {
                            string faction;
                            if (call.parameters.Count() > 1) { faction = call.parameters[1].ToLower().Trim(); }
                            else { faction = ((NpcContent)content).faction; }

                            int rep = int.Parse(call.parameters[0]);

                            Script.Flag fvar = scriptManager.GetFlag(Script.Flag.Designation.FactionReputation, faction);
                            lines.Add($"EventValueOperation({fvar.id}, {fvar.Bits()}, {int.Parse(call.parameters[0])}, 0, 1, 0);"); // 0 is Add
                            break;
                        }

                    case Call.Type.ShowMap:
                        {
                            Script.Flag discoverFlag = scriptManager.GetFlag(Script.Flag.Designation.DiscoverLocation, call.parameters[0]);
                            if(discoverFlag != null)
                            {
                                lines.Add($"SetEventFlag(TargetEventFlagType.EventFlag, {discoverFlag.id}, ON);");
                            }
                            break;
                        }

                    case Call.Type.Say:
                        {
                            string id = call.parameters[0].ToLower().Replace("\\", "_").Replace("/", "_").Replace(".mp3", "");
                            string file = Path.Combine($"{Const.MORROWIND_PATH}", @"Data Files\sound", call.parameters[0]);
                            int playId = sound.AddSound(id, MainSoundBank.Sound.Type.Voice, false, true, 1f, 1f, file);

                            uint entityId;
                            if(call.target == null) { entityId = content.entity; }                       // case 1: no target so current object is target
                            else if (call.target == "player") { entityId = 10000; }                      // case 2: target is player
                            else                                                                         // case 3: target is a direct reference to an object record
                            {
                                Content speaker = layout.FindScriptReference(content, call.target);
                                if (speaker == null) { break; } // failed to find speaker for this line, should only happen in a partial build
                                entityId = speaker.entity;
                            }

                            lines.Add($"PlaySE({entityId}, 7, {playId * 10});");  // 7 is "Voice"
                            break;
                        }

                    case Call.Type.PcRaiseRank:
                        {
                            string faction;
                            if(call.parameters.Count() > 0) { faction = call.parameters[0].ToLower().Trim(); }
                            else { faction = ((NpcContent)content).faction; }

                            Script.Flag jvar = scriptManager.GetFlag(Script.Flag.Designation.FactionJoined, faction);
                            Script.Flag rvar = scriptManager.GetFlag(Script.Flag.Designation.FactionRank, faction);
                            lines.Add($"SetEventFlag(TargetEventFlagType.EventFlag, {jvar.id}, ON);");
                            lines.Add($"EventValueOperation({rvar.id}, {rvar.Bits()}, 1, 0, 1, 0);"); // +1 operation
                            break;
                        }

                    case Call.Type.PcExpell:
                        {
                            string faction;
                            if (call.parameters.Count() > 0) { faction = call.parameters[0].ToLower().Trim(); }
                            else { faction = ((NpcContent)content).faction; }

                            Script.Flag fvar = scriptManager.GetFlag(Script.Flag.Designation.FactionExpelled, faction);
                            lines.Add($"SetEventFlag(TargetEventFlagType.EventFlag, {fvar.id}, ON);");
                            break;
                        }

                    case Call.Type.PcClearExpelled:
                        {
                            string faction;
                            if (call.parameters.Count() > 0) { faction = call.parameters[0].ToLower().Trim(); }
                            else { faction = ((NpcContent)content).faction; }

                            Script.Flag fvar = scriptManager.GetFlag(Script.Flag.Designation.FactionExpelled, faction);
                            lines.Add($"SetEventFlag(TargetEventFlagType.EventFlag, {fvar.id}, OFF);");
                            break;
                        }

                    case Call.Type.PlaySound:
                    case Call.Type.PlaySoundVP:
                    case Call.Type.PlaySound3D:
                    case Call.Type.PlaySound3DVP:
                    case Call.Type.PlayLoopSound3D:
                    case Call.Type.PlayLoopSound3DVP:
                        {
                            SoundInfo info = esm.GetSound(call.parameters[0].ToLower().Trim());
                            float volume, pitch;
                            switch (call.type)
                            {
                                case Call.Type.PlaySound:
                                case Call.Type.PlaySound3D:
                                case Call.Type.PlayLoopSound3D:
                                    {
                                        volume = 1f; pitch = 1f; break;
                                    }
                                case Call.Type.PlaySoundVP:
                                case Call.Type.PlaySound3DVP:
                                case Call.Type.PlayLoopSound3DVP:
                                    {
                                        volume = float.Parse(call.parameters[1]);
                                        pitch = float.Parse(call.parameters[2]);
                                        break;
                                    }
                                default: throw new Exception("Invalid PlaySound call type."); // unreachable or else
                            }

                            bool loop = call.type == Call.Type.PlayLoopSound3D || call.type == Call.Type.PlayLoopSound3DVP;
                            bool spatialize = call.type == Call.Type.PlaySound3D || call.type == Call.Type.PlaySound3DVP || call.type == Call.Type.PlayLoopSound3D || call.type == Call.Type.PlayLoopSound3DVP;
                            string file = Path.Combine($"{Const.MORROWIND_PATH}", @"Data Files\sound", info.path);

                            // find our target content
                            Content target;
                            if (call.target == null) { target = content; }
                            else { target = layout.FindScriptReference(content, call.target); }
                            if (target == null) { break; } // Failed to find script reference. Should only happen when making partial builds.

                            // If this is a non spatialized sound effect play it directly off the player
                            uint targetId;
                            if (spatialize) { targetId = target.entity; }
                            else { targetId = 10000; }

                            // Add sound to main bank and get playback id
                            int seId = sound.AddSound(info.id, MainSoundBank.Sound.Type.SFX, loop, spatialize, volume, pitch, file);

                            // Play SE call
                            lines.Add($"PlaySE({targetId}, 5, {seId});");
                            break;
                        }

                    case Call.Type.SetHealth:
                    case Call.Type.SetMagicka:
                    case Call.Type.SetFatigue:
                        {
                            int value = int.Parse(call.parameters[0]);
                            if (value <= 0) { value = -999999; }  // YOU MUST DIE!
                            else { value = 999999; }             // YOU MUST LIVE!

                            uint entityId;
                            if (call.target == null) { entityId = content.entity; }                      // case 1: no target so current object is target
                            else if (call.target == "player") { entityId = 10000; }                      // case 2: target is player
                            else                                                                         // case 3: target is a direct reference to an object record
                            {
                                entityId = layout.FindScriptReference(content, call.target).entity;
                            }

                            SpeffManager.StatMod statToMod;
                            switch (call.type)
                            {
                                case Call.Type.SetHealth: statToMod = SpeffManager.StatMod.CurrentHP; break;
                                case Call.Type.SetMagicka: statToMod = SpeffManager.StatMod.CurrentMP; break;
                                case Call.Type.SetFatigue: statToMod = SpeffManager.StatMod.CurrentSP; break;
                                default: throw new Exception("Invalid papyrus call type");  // unreachable
                            }

                            int speffId = speffManager.CreateScriptedEffect(statToMod, value, call.RAW);
                            lines.Add($"SetSpEffect({entityId}, {speffId});");
                            break;
                        }

                    case Call.Type.SetStrength:
                    case Call.Type.SetIntelligence:
                    case Call.Type.SetWillpower:
                    case Call.Type.SetAgility:
                    case Call.Type.SetSpeed:
                    case Call.Type.SetEndurance:
                    case Call.Type.SetPersonality:
                    case Call.Type.SetLuck:
                    case Call.Type.SetAcrobatics:
                    case Call.Type.SetAlchemy:
                    case Call.Type.SetAlteration:
                    case Call.Type.SetArmorer:
                    case Call.Type.SetAthletics:
                    case Call.Type.SetAxe:
                    case Call.Type.SetBlock:
                    case Call.Type.SetBluntWeapon:
                    case Call.Type.SetConjuration:
                    case Call.Type.SetDestruction:
                    case Call.Type.SetEnchant:
                    case Call.Type.SetHandToHand:
                    case Call.Type.SetHeavyArmor:
                    case Call.Type.SetIllusion:
                    case Call.Type.SetLightArmor:
                    case Call.Type.SetLongBlade:
                    case Call.Type.SetMarksman:
                    case Call.Type.SetMediumArmor:
                    case Call.Type.SetMercantile:
                    case Call.Type.SetMysticism:
                    case Call.Type.SetRestoration:
                    case Call.Type.SetSecurity:
                    case Call.Type.SetShortBlade:
                    case Call.Type.SetSneak:
                    case Call.Type.SetSpear:
                    case Call.Type.SetSpeechcraft:
                    case Call.Type.SetUnarmored:
                        {
                            CharacterContent target;
                            uint entityId;
                            if (call.target == null) { target = (CharacterContent)content; entityId = target.entity; }                          // case 1: no target so current object is target
                            else if (call.target == "player") { break; }                                                                       // case 2: target is player. UNSUPPORTED!
                            else { target = (CharacterContent)layout.FindScriptReference(content, call.target); entityId = target.entity; }   // case 3: target is a direct reference to an object record

                            Script areaScript = scriptManager.FindScriptFor(layout, target);

                            int amount = int.Parse(call.parameters[0]);
                            SpeffManager.StatMod statToMod;
                            switch (call.type)
                            {
                                /* Attributes */
                                case Call.Type.SetStrength: amount = amount - target.stats.Get(CharacterContent.Stats.Attribute.Strength); statToMod = SpeffManager.StatMod.Strength; break;
                                case Call.Type.SetIntelligence: amount = amount - target.stats.Get(CharacterContent.Stats.Attribute.Intelligence); statToMod = SpeffManager.StatMod.Intelligence; break;
                                case Call.Type.SetWillpower: amount = amount - target.stats.Get(CharacterContent.Stats.Attribute.Willpower); statToMod = SpeffManager.StatMod.Mind; break;
                                case Call.Type.SetAgility: amount = amount - target.stats.Get(CharacterContent.Stats.Attribute.Agility); statToMod = SpeffManager.StatMod.Dexterity; break;
                                case Call.Type.SetSpeed: amount = amount - target.stats.Get(CharacterContent.Stats.Attribute.Speed); statToMod = SpeffManager.StatMod.Dexterity; break;
                                case Call.Type.SetEndurance:amount = amount - target.stats.Get(CharacterContent.Stats.Attribute.Endurance); statToMod = SpeffManager.StatMod.Endurance; break;
                                case Call.Type.SetPersonality: amount = amount - target.stats.Get(CharacterContent.Stats.Attribute.Personality); statToMod = SpeffManager.StatMod.Arcane; break;
                                case Call.Type.SetLuck: amount = amount - target.stats.Get(CharacterContent.Stats.Attribute.Luck); statToMod = SpeffManager.StatMod.Arcane; break;
                                /* Skills */
                                case Call.Type.SetArmorer:
                                case Call.Type.SetAcrobatics:
                                case Call.Type.SetAxe:
                                case Call.Type.SetBluntWeapon:
                                case Call.Type.SetLongBlade:
                                    {
                                        CharacterContent.Stats.Skill skill = Enum.Parse<CharacterContent.Stats.Skill>(call.type.ToString()[^3..]);
                                        amount = amount - target.stats.Get(skill);
                                        amount = (int)(amount * 0.33f); // skill bonuses are reduced
                                        statToMod = SpeffManager.StatMod.Strength;
                                        break;
                                    }
                                case Call.Type.SetAlchemy:
                                case Call.Type.SetConjuration:
                                case Call.Type.SetEnchant:
                                case Call.Type.SetSecurity:
                                    {
                                        CharacterContent.Stats.Skill skill = Enum.Parse<CharacterContent.Stats.Skill>(call.type.ToString()[^3..]);
                                        amount = amount - target.stats.Get(skill);
                                        amount = (int)(amount * 0.33f); // skill bonuses are reduced
                                        statToMod = SpeffManager.StatMod.Intelligence;
                                        break;
                                    }
                                case Call.Type.SetAlteration:
                                case Call.Type.SetDestruction:
                                case Call.Type.SetMysticism:
                                    {
                                        CharacterContent.Stats.Skill skill = Enum.Parse<CharacterContent.Stats.Skill>(call.type.ToString()[^3..]);
                                        amount = amount - target.stats.Get(skill);
                                        amount = (int)(amount * 0.33f); // skill bonuses are reduced
                                        statToMod = SpeffManager.StatMod.Mind;
                                        break;
                                    }
                                case Call.Type.SetRestoration:
                                    {
                                        CharacterContent.Stats.Skill skill = Enum.Parse<CharacterContent.Stats.Skill>(call.type.ToString()[^3..]);
                                        amount = amount - target.stats.Get(skill);
                                        amount = (int)(amount * 0.33f); // skill bonuses are reduced
                                        statToMod = SpeffManager.StatMod.Faith;
                                        break;
                                    }
                                case Call.Type.SetAthletics:
                                case Call.Type.SetHandToHand:
                                case Call.Type.SetShortBlade:
                                case Call.Type.SetUnarmored:
                                case Call.Type.SetBlock:
                                case Call.Type.SetLightArmor:
                                case Call.Type.SetMarksman:
                                case Call.Type.SetSneak:
                                    {
                                        CharacterContent.Stats.Skill skill = Enum.Parse<CharacterContent.Stats.Skill>(call.type.ToString()[^3..]);
                                        amount = amount - target.stats.Get(skill);
                                        amount = (int)(amount * 0.33f); // skill bonuses are reduced
                                        statToMod = SpeffManager.StatMod.Dexterity;
                                        break;
                                    }
                                case Call.Type.SetHeavyArmor:
                                case Call.Type.SetMediumArmor:
                                case Call.Type.SetSpear:
                                    {
                                        CharacterContent.Stats.Skill skill = Enum.Parse<CharacterContent.Stats.Skill>(call.type.ToString()[^3..]);
                                        amount = amount - target.stats.Get(skill);
                                        amount = (int)(amount * 0.33f); // skill bonuses are reduced
                                        statToMod = SpeffManager.StatMod.Endurance;
                                        break;
                                    }
                                case Call.Type.SetIllusion:
                                case Call.Type.SetMercantile:
                                case Call.Type.SetSpeechcraft:
                                    {
                                        CharacterContent.Stats.Skill skill = Enum.Parse<CharacterContent.Stats.Skill>(call.type.ToString()[^3..]);
                                        amount = amount - target.stats.Get(skill);
                                        amount = (int)(amount * 0.33f); // skill bonuses are reduced
                                        statToMod = SpeffManager.StatMod.Arcane;
                                        break;
                                    }

                                default: throw new Exception("Invalid papyrus call type");  // unreachable
                            }

                            int speffId = speffManager.CreateScriptedEffect(statToMod, amount, call.RAW);
                            Script.Flag modStatFlag = areaScript.RegisterModStat(entityId, speffId);

                            lines.Add($"SetEventFlag(TargetEventFlagType.EventFlag, {modStatFlag.id}, ON);");  // turn on flag to make this speff permanent on the npc
                            lines.Add($"SetSpEffect({entityId}, {speffId});");                                 // and apply the speff for right now
                            break;
                        }

                    case Call.Type.SetPcCrimeLevel:
                        {
                            Script.Flag cvar = scriptManager.GetFlag(Script.Flag.Designation.CrimeLevel, "CrimeLevel");
                            string to = call.parameters[0];
                            /* Setting to a literal, ez pz */
                            if (Utility.StringIsInteger(to))
                            {
                                lines.Add($"EventValueOperation({cvar.id}, {cvar.Bits()}, {to}, 0, 1, 5);"); // 5 is assign
                            }
                            /* Setting to a var, handley wandley */
                            else
                            {
                                Script.Flag toFlag = GetFlagByVariable(to.ToLower().Trim());
                                lines.Add($"EventValueOperation({cvar.id}, {cvar.Bits()}, 0, {toFlag.id}, {toFlag.Bits()}, 5);"); // 5 is assign
                            }
                            break;
                        }

                    case Call.Type.SetDisposition:
                        {
                            // Find our target content
                            Content target;
                            if (call.target == null) { target = content; }
                            else { target = layout.FindScriptReference(content, call.target); }
                            if (target == null) { break; } // Failed to find script reference. Should only happen when making partial builds.

                            // Set disp value
                            Script.Flag dvar = scriptManager.GetFlag(Script.Flag.Designation.Disposition, target.entity.ToString());
                            if (dvar == null) { break; } // this is here because some base game morrowind scripts try to set disp on creatures which is impossible. thanks todd
                            lines.Add($"EventValueOperation({dvar.id}, {dvar.Bits()}, {call.parameters[0]}, 0, 1, 5);"); // 5 is assign
                            break;
                        }

                    case Call.Type.StartCombat:
                        {
                            // Find our target content A
                            CharacterContent targetA;
                            if (call.target == null) { targetA = (CharacterContent)content; }
                            else { targetA = (CharacterContent)layout.FindScriptReference(content, call.target); }
                            if (targetA == null) { break; } // Failed to find script reference. Should only happen when making partial builds.

                            if (call.parameters[0].Trim() == "player")
                            {
                                // if a guard starts combat with a player its a crime, if its anyone else it's just them being angy at you
                                if (targetA.IsGuard())
                                {
                                    Script.Flag cvar = scriptManager.GetFlag(Script.Flag.Designation.CrimeEvent, targetA.entity.ToString());
                                    Script.Flag lvar = scriptManager.GetFlag(Script.Flag.Designation.CrimeLevel, "CrimeLevel"); // crime gold flag
                                    lines.Add($"EventValueOperation({lvar.id}, {lvar.Bits()}, {Const.CRIME_GOLD_RESIST}, 0, 1, 5);"); // 5 is assign
                                    lines.Add($"SetEventFlag(TargetEventFlagType.EventFlag, {cvar.id}, ON);");
                                }
                                else
                                {
                                    // Is a talkable character of some kind
                                    Script.Flag hvar = scriptManager.GetFlag(Script.Flag.Designation.Hostile, targetA.entity.ToString());
                                    if (hvar != null)
                                    {
                                        lines.Add($"SetEventFlag(TargetEventFlagType.EventFlag, {hvar.id}, ON);"); // simply set player hostility flag to true
                                    }
                                    // Probably a wild creature
                                    else
                                    {
                                        lines.Add($"SetCharacterTeamType({targetA.entity}, 29);");  // set to indiscriminate team type to make it very hostile
                                    }
                                }
                            }
                            else
                            {
                                // Find our target content B
                                CharacterContent targetB = (CharacterContent)layout.FindScriptReference(content, call.parameters[0].ToLower().Trim());
                                if (targetB == null) { break; } // Failed to find script reference. Should only happen when making partial builds.

                                Script areaScriptA = scriptManager.FindScriptFor(layout, targetA);
                                Script areaScriptB = scriptManager.FindScriptFor(layout, targetB);

                                Script.Flag aFlag = areaScriptA.GetOrRegisterInfight(targetA);
                                Script.Flag bFlag = areaScriptB.GetOrRegisterInfight(targetB);

                                lines.Add($"SetEventFlag(TargetEventFlagType.EventFlag, {aFlag.id}, ON);");
                                lines.Add($"SetEventFlag(TargetEventFlagType.EventFlag, {bFlag.id}, ON);");
                            }
                            break;
                        }

                    case Call.Type.StopCombat:
                        {
                            // Find our target content
                            CharacterContent target;
                            if (call.target == null) { target = (CharacterContent)content; }
                            else { target = (CharacterContent)layout.FindScriptReference(content, call.target); }
                            if (target == null) { break; } // Failed to find script reference. Should only happen when making partial builds.

                            Script areaScript = scriptManager.FindScriptFor(layout, target);
                            Script.Flag hostileFlag = scriptManager.GetFlag(Script.Flag.Designation.Hostile, target.entity.ToString());
                            Script.Flag fightFlag = scriptManager.GetFlag(Designation.NpcInfight, content.entity.ToString());
                            if (hostileFlag != null) { lines.Add($"SetEventFlag(TargetEventFlagType.EventFlag, {hostileFlag.id}, OFF);"); }
                            if (fightFlag != null) { lines.Add($"SetEventFlag(TargetEventFlagType.EventFlag, {fightFlag.id}, OFF);"); }
                            if(fightFlag == null && hostileFlag == null) { lines.Add($"SetCharacterTeamType({target.entity}, 26);"); } // fallback, sets team to player friendly
                            break;
                        }

                    // will be refering to any papyrus script that is managed by StartScript, StopScript, ScriptRunning as "subscript" for clarity
                    case Call.Type.StartScript:
                    case Call.Type.StopScript:
                        {
                            // Grab subscript papyrus
                            Papyrus subscript = esm.GetPapyrus(call.parameters[0]);
                            if (subscript == null) { break; } // failed to find script, this only happens because our papyrus parsing is still not 100% finished

                            // find our target content
                            Content target;
                            if (call.target == null) { target = content; }
                            else { target = layout.FindScriptReference(content, call.target); }
                            if (script is not ScriptCommon && target == null) { break; } // Failed to find script reference. Should only happen when making partial builds.

                            // find area script for this npc
                            BaseScript targetScript;
                            if (script is ScriptCommon) { targetScript = script; }
                            else { targetScript = scriptManager.FindScriptFor(layout, target); }

                            // See if the subscript is already created. this is needed as multiple scripts can potenitlaly start/stop the same subscript.
                            Script.Flag subscriptRunFlag;
                            if(targetScript is ScriptCommon) { subscriptRunFlag = scriptManager.GetFlag(Script.Flag.Designation.RunSubscript, $"Global->{subscript.id}"); }
                            else { subscriptRunFlag = scriptManager.GetFlag(Script.Flag.Designation.RunSubscript, $"{target.entity}->{subscript.id}"); }

                            // If the subscript does not exist yet, we create it
                            if (subscriptRunFlag == null)
                            {
                                if (targetScript is ScriptCommon) { subscriptRunFlag = targetScript.CreateFlag(Script.Flag.Category.Saved, Script.Flag.Type.Bit, Script.Flag.Designation.RunSubscript, $"Global->{subscript.id}"); }
                                else { subscriptRunFlag = targetScript.CreateFlag(Script.Flag.Category.Saved, Script.Flag.Type.Bit, Script.Flag.Designation.RunSubscript, $"{target.entity}->{subscript.id}"); }
                                PapyrusEMEVD.Compile(esm, layout, msb, sound, scriptManager, paramanager, itemManager, speffManager, targetScript, subscript, target, subscriptRunFlag);
                            }

                            // Finally we just add some code here to start/stop the subscript
                            string toggle = call.type == Call.Type.StartScript ? "ON" : "OFF";
                            lines.Add($"SetEventFlag(TargetEventFlagType.EventFlag, {subscriptRunFlag.id}, {toggle});");
                            break;
                        }

                    case Call.Type.Return:
                        {
                            lines.AddRange(post);
                            lines.Add($"EndUnconditionally(EventEndType.Restart);");
                            break;
                        }

                    default:
                        if (!UNSUPPORTED_CALL_LIST.Contains(call.type)) { Lort.Log($" ## WARNING ## Unsupported Papyrus->EMEVD call {papyrus.id}->{call.type}", Lort.Type.Debug); UNSUPPORTED_CALL_LIST.Add(call.type); }
                        break;
                }

                return lines;
            }

            /* STUFF ACTUALLY HAPPENS BELOW THIS POINT */

            /* Setup some stuff */
            Script.Flag evtFlag;
            // papyrus script running from main (global)
            if (script is ScriptCommon) { evtFlag = script.CreateFlag(Script.Flag.Category.Event, Script.Flag.Type.Bit, Script.Flag.Designation.Event, $"Global->{papyrus.id}"); }
            // papyrus script on an object (local)
            else { evtFlag = script.CreateFlag(Script.Flag.Category.Event, Script.Flag.Type.Bit, Script.Flag.Designation.Event, $"{content.id}->{papyrus.id}->{content.entity}"); }
            EMEVD.Event evt = new();
            evt.ID = evtFlag.id;

            /* Emulate OnActivate papyrus call if it is present in this script */
            if (papyrus.HasCall(Call.Type.OnActivate))
            {
                Script.Flag treasuerLootFlag = null;
                if (content is ItemContent) { treasuerLootFlag = ((ItemContent)content).treasure; }
                else if (content is ContainerContent) { treasuerLootFlag = ((ContainerContent)content).treasure; }
                else if (content is NpcContent) { treasuerLootFlag = ((NpcContent)content).treasure; }

                /* If the object is an ItemContent or ContainerContent we can emulate OnActivate by simply waiting for the treasure flag to be set */
                if (treasuerLootFlag != null)
                {
                    Script.Flag onActivateFlag = script.CreateFlag(Script.Flag.Category.Temporary, Script.Flag.Type.Bit, Script.Flag.Designation.OnActivate, content.entity.ToString());
                    Script.Flag onActivateEventFlag = script.CreateFlag(Script.Flag.Category.Event, Script.Flag.Type.Bit, Script.Flag.Designation.Event, $"OnActivate->{content.entity.ToString()}");
                    EMEVD.Event onActivateEvent = new();
                    onActivateEvent.ID = onActivateEventFlag.id;
                    onActivateEvent.Instructions.Add(script.AUTO.ParseAdd($"IfEventFlag(MAIN, OFF, TargetEventFlagType.EventFlag, {treasuerLootFlag.id});")); // if the looted flag is initially off
                    onActivateEvent.Instructions.Add(script.AUTO.ParseAdd($"IfEventFlag(MAIN, ON, TargetEventFlagType.EventFlag, {treasuerLootFlag.id});"));  // then switches on...
                    onActivateEvent.Instructions.Add(script.AUTO.ParseAdd($"SetEventFlag(TargetEventFlagType.EventFlag, {onActivateFlag.id}, ON);"));         // it means the player just picked up the item or looted the container!
                    script.emevd.Events.Add(onActivateEvent);
                    script.init.Instructions.Add(script.AUTO.ParseAdd($"InitializeEvent(0, {onActivateEventFlag.id}, 0);"));
                }
                /* If there is an "on activate" call we need a paralell script to handle that so we generate one */
                /* This paralell script emulates the behaviour of MW by awaiting the player hitting A on an object to interact with it, then setting a flag to true */
                /* The actual papyrus code then just reads this flag to determine if the player has activated it. The papyrus script also resets the value after reading it for consistency */
                /* We cannot simply do an onactionbutton check in the main papyrus script because it is blocking and can't be compared to false. This is the best way to emulate behaviour */
                else
                {
                    int actionButtonId = paramanager.GenerateActionButtonInteractParam($"Interact with {content.name}");
                    Script.Flag onActivateFlag = script.CreateFlag(Script.Flag.Category.Temporary, Script.Flag.Type.Bit, Script.Flag.Designation.OnActivate, content.entity.ToString());
                    Script.Flag onActivateEventFlag = script.CreateFlag(Script.Flag.Category.Event, Script.Flag.Type.Bit, Script.Flag.Designation.Event, $"OnActivate->{content.entity.ToString()}");
                    EMEVD.Event onActivateEvent = new();
                    onActivateEvent.ID = onActivateEventFlag.id;
                    onActivateEvent.Instructions.Add(script.AUTO.ParseAdd($"IfEventFlag(MAIN, OFF, TargetEventFlagType.EventFlag, {onActivateFlag.id});"));
                    onActivateEvent.Instructions.Add(script.AUTO.ParseAdd($"IfActionButtonInArea(MAIN, {actionButtonId}, {content.entity});"));
                    onActivateEvent.Instructions.Add(script.AUTO.ParseAdd($"SetEventFlag(TargetEventFlagType.EventFlag, {onActivateFlag.id}, ON);"));
                    onActivateEvent.Instructions.Add(script.AUTO.ParseAdd($"EndUnconditionally(EventEndType.Restart);"));
                    script.emevd.Events.Add(onActivateEvent);
                    script.init.Instructions.Add(script.AUTO.ParseAdd($"InitializeEvent(0, {onActivateEventFlag.id}, 0);"));
                }
            }

            /* Emulate OnDeath papyruys call for a target object */
            /* To make this work we need a paralell script that tracks when the flag for a character dieing is set */
            /* Returns temp flag that is set when target character dies */
            Script.Flag RegisterOnDeath(Papyrus.Call call)
            {
                // Find our target content
                Content target;
                if (call.target == null) { target = content; }
                else { target = layout.FindScriptReference(content, call.target); }
                if (target == null) { return null; } // Failed to find script reference. Should only happen when making partial builds.

                // Grab their death flag
                Script.Flag targetDeathFlag = scriptManager.GetFlag(Script.Flag.Designation.Dead, target.entity.ToString());

                // if this handler is already registered then don't create it again. just return flag
                Script.Flag onDeathFlag = scriptManager.GetFlag(Script.Flag.Designation.OnDeath, target.entity.ToString());
                if (onDeathFlag != null) { return onDeathFlag; }

                // Create handler and return
                onDeathFlag = script.CreateFlag(Script.Flag.Category.Temporary, Script.Flag.Type.Bit, Script.Flag.Designation.OnDeath, target.entity.ToString());
                Script.Flag onDeathEventFlag = script.CreateFlag(Script.Flag.Category.Event, Script.Flag.Type.Bit, Script.Flag.Designation.Event, $"OnDeath->{target.entity.ToString()}");
                EMEVD.Event onDeathEvent = new();
                onDeathEvent.ID = onDeathEventFlag.id;
                onDeathEvent.Instructions.Add(script.AUTO.ParseAdd($"IfEventFlag(MAIN, OFF, TargetEventFlagType.EventFlag, {targetDeathFlag.id});"));  // pause until target is alive
                onDeathEvent.Instructions.Add(script.AUTO.ParseAdd($"IfEventFlag(MAIN, ON, TargetEventFlagType.EventFlag, {targetDeathFlag.id});"));  // pause until target is dead
                onDeathEvent.Instructions.Add(script.AUTO.ParseAdd($"SetEventFlag(TargetEventFlagType.EventFlag, {onDeathFlag.id}, ON);"));          // mark ondeath flag true
                script.emevd.Events.Add(onDeathEvent);
                script.init.Instructions.Add(script.AUTO.ParseAdd($"InitializeEvent(0, {onDeathEventFlag.id}, 0);"));

                return onDeathFlag;
            }

            /* If there is a "Cell Changed" call we need a temp flag created to use as our "run once" flag */
            if (papyrus.HasCall(Call.Type.CellChanged))
            {
                Script.Flag cellChangedFlag = script.CreateFlag(Script.Flag.Category.Temporary, Script.Flag.Type.Bit, Script.Flag.Designation.CellChanged, content.entity.ToString());
            }

            /* If there is a "GetSecondsPassed" call we need to create a paralell event to emulate it's function */
            Script.Flag RegisterGetSecondsPassed()
            {
                Script.Flag secondsFlag = script.GetOrCreateFlag(Script.Flag.Category.Temporary, Script.Flag.Type.Short, Script.Flag.Designation.SecondsPassed, content.entity.ToString());
                script.init.Instructions.Insert(0, script.AUTO.ParseAdd($"InitializeCommonEvent(0, {scriptManager.common.events[ScriptCommon.Event.GetSecondsPassed]}, {secondsFlag.id}, {secondsFlag.Bits()});"));
                return secondsFlag;
            }

            /* Compile papyrus */
            List<string> lines = HandleScope(papyrus.scope);
            lines.AddRange(post);
            if(subscriptRunFlag != null) { lines.Insert(0, $"IfEventFlag(MAIN, ON, TargetEventFlagType.EventFlag, {subscriptRunFlag.id}); "); }  // if this is a subscript, only runs when run flag is set true
            if (lines.Count() <= 0) { return; } // this is a minor optimization. some scripts like nolore end up just being blank as they are (effectively) statically resolved. so we discard empty events that would just do nothing but loop

            lines.Add($"EndUnconditionally(EventEndType.Restart);"); // mw scripts always restart
            foreach (string line in lines) { evt.Instructions.Add(script.AUTO.ParseAdd(line)); }
            script.emevd.Events.Add(evt);
            script.init.Instructions.Add(script.AUTO.ParseAdd($"InitializeEvent(0, {evtFlag.id}, 0);"));

            return;
        }
    
        public static void InitializeLocalVariables(ESM esm, ScriptManager scriptManager, BaseScript script, Papyrus papyrus, Content content)
        {
            void DoStuff(Papyrus p)
            {
                /* Create any local variables in this script */
                List<Papyrus.Call> shorts = p.GetCalls(Call.Type.Short);
                foreach (Papyrus.Call call in shorts)
                {
                    if (script is ScriptCommon)
                    {
                        string flagId = $"{p.id}.{call.parameters[0]}";
                        script.CreateFlag(Script.Flag.Category.Saved, Script.Flag.Type.Short, Script.Flag.Designation.Local, flagId);
                    }
                    else
                    {
                        string flagId = $"{content.entity}.{call.parameters[0]}";
                        script.CreateFlag(Script.Flag.Category.Saved, Script.Flag.Type.Short, Script.Flag.Designation.Local, flagId);
                    }
                }
                /* If this script runs subscripts we recursively repeate this for those as well */
                List<Papyrus.Call> starters = p.GetCalls(Call.Type.StartScript);
                foreach(Papyrus.Call call in starters)
                {
                    Papyrus sub = esm.GetPapyrus(call.parameters[0].ToLower().Trim());
                    if(sub != null) { DoStuff(sub); }
                }
            }

            DoStuff(papyrus);
        }
    }
}
