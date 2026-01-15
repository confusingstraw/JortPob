using HKLib.hk2018.hk;
using JortPob.Common;
using SoulsFormats;
using SoulsIds;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using static JortPob.Papyrus;

namespace JortPob
{
    public class PapyrusEMEVD
    {
        public static List<Call.Type> UNSUPPORTED_CALL_LIST = new(), UNSUPPORTED_CONDITIONAL_LIST = new(), UNSUPPORTED_SET_LIST = new();

        public static void Compile(ScriptManager scriptManager, Paramanager paramanager, ItemManager itemManager, Script script, Papyrus papyrus, Content content)
        {
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
                if (!varName.Contains("."))  // probably a local var of this object
                {
                    retFlag = scriptManager.GetFlag(Script.Flag.Designation.Local, $"{content.id}.{varName}");
                }
                if (retFlag == null && varName.Contains(".")) // looks like it's actually a local var of a different object
                {
                    retFlag = scriptManager.GetFlag(Script.Flag.Designation.Local, varName); // look for it, if we dont find it we create it
                    if (retFlag == null) { retFlag = scriptManager.common.CreateFlag(Script.Flag.Category.Saved, Script.Flag.Type.Short, Script.Flag.Designation.Local, varName); }
                }
                if (retFlag == null) { retFlag = scriptManager.GetFlag(Script.Flag.Designation.Global, varName); } // maybe its a global var!
                return retFlag;
            }

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
                List<string> post = new(); // generated stuff. not a part of actual papyrus. just things i need to finagle to emulate it cleanly

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
                        if (call.right.type == Call.Type.Literal)
                        {
                            Script.Flag jflag = scriptManager.GetFlag(Script.Flag.Designation.Journal, call.left.parameters[0]); // find journal flag, create it if doesn't exist yet
                            if(jflag == null) { jflag = scriptManager.common.CreateFlag(Script.Flag.Category.Saved, Script.Flag.Type.Byte, Script.Flag.Designation.Journal, call.left.parameters[0]); }
                            lines.Add(ResetConditionGroups());
                            lines.Add($"IfEventValue(OR_01, {jflag.id}, {jflag.Bits()}, {call.OperatorIndex()}, {call.right.parameters[0]});");
                            lines.Add($"SkipIfConditionGroupStateUncompiled({pass.Count()}, FAIL, OR_01);");
                        }
                        else if (call.right.type == Call.Type.Variable)
                        {
                            Script.Flag jflag = scriptManager.GetFlag(Script.Flag.Designation.Journal, call.left.parameters[0]); // find journal flag, create it if doesn't exist yet
                            if (jflag == null) { jflag = scriptManager.common.CreateFlag(Script.Flag.Category.Saved, Script.Flag.Type.Byte, Script.Flag.Designation.Journal, call.left.parameters[0]); }
                            Script.Flag lflag = GetFlagByVariable(call.right.parameters[0]);
                            lines.Add(ResetConditionGroups());
                            lines.Add($"IfCompareEventValues(OR_01, {jflag.id}, {jflag.Bits()}, {call.OperatorIndex()}, {lflag.id}, {lflag.Bits()});");
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
                            if(dflag == null)  // currently we dont have disable code for assets so they dont have disable flags. skip
                            {
                                lines.Add($"SkipUnconditionally({(flagState?0:pass.Count())})");
                                break;
                            }
                            lines.Add($"SkipIfEventFlag({pass.Count()}, {(flagState ? "ON" : "OFF")}, TargetEventFlagType.EventFlag, {dflag.id});");
                        }
                        else { Lort.Log($"## BAD CONDITIONAL ## {papyrus.id}->{call.type} [{call.left.type} ? {call.right.type}]", Lort.Type.Debug); }
                        break;

                    case Call.Type.GetPcRank:
                        if (call.right.type == Call.Type.Literal)
                        {
                            // notably, this call in payrus sometimes targets the player. it is already a PC check so it's always the player but like... idk double player is just as good
                            // additionally: some faction names have spaces in them, like "imperial cult" which means we need to join all parameters because they dont always use quotes around the calls
                            Script.Flag fflag = scriptManager.GetFlag(Script.Flag.Designation.FactionRank, string.Join(" ",  call.left.parameters));
                            lines.Add(ResetConditionGroups());
                            lines.Add($"IfEventValue(OR_01, {fflag.id}, {fflag.Bits()}, {call.OperatorIndex()}, {call.right.parameters[0]});");
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
                            if (pass.Count() > 0) { lines.Add($"SkipUnconditionally({pass.Count()});"); } // returning false for now, no item support yet!
                        }
                        // Checking if a non player character has gold or an item, we will simply assume true here because npcs don't have inventories in ER and assuming true is probably mostly fine
                        else
                        {
                            lines.Add($"SkipUnconditionally(0);"); // this is effectively a do nothing command to put in the spot of the if statment. effectively if(true)
                        }
                        break;

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
                    case Call.Type.Short:
                        {
                            Script.Flag lflag = scriptManager.GetFlag(Script.Flag.Designation.Local, $"{content.id}.{call.parameters[0]}"); // Look for the flag, create it if it doesn't exist
                            if (lflag == null) { lflag = script.CreateFlag(Script.Flag.Category.Saved, Script.Flag.Type.Short, Script.Flag.Designation.Local, $"{content.id}.{call.parameters[0]}"); }
                            break;
                        }

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
                                    operations.Add(new(lastOp, new Call(string.Join(" ", tempParameters))));
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
                            foreach ((string op, Call call) operation in operations)
                            {
                                switch (operation.call.type)
                                {
                                    case Call.Type.Literal:
                                        lines.Add($"EventValueOperation({lflag.id}, {lflag.Bits()}, {int.Parse(operation.call.parameters[0])}, 0, 1, {GetEventValueOperator(operation.op)});");
                                        break;
                                    case Call.Type.Variable:
                                        Script.Flag l2flag = GetFlagByVariable(operation.call.parameters[0]);
                                        lines.Add($"EventValueOperation({lflag.id}, {lflag.Bits()}, 0, {l2flag.id}, {l2flag.Bits()}, {GetEventValueOperator(operation.op)});");
                                        break;
                                    case Call.Type.GetJournalIndex:
                                        Script.Flag jflag = scriptManager.GetFlag(Script.Flag.Designation.Journal, operation.call.parameters[0]); // find journal flag or create it if it does not exist
                                        if(jflag == null) { jflag = scriptManager.common.CreateFlag(Script.Flag.Category.Saved, Script.Flag.Type.Byte, Script.Flag.Designation.Journal, operation.call.parameters[0]); }
                                        lines.Add($"EventValueOperation({lflag.id}, {lflag.Bits()}, 0, {jflag.id}, {jflag.Bits()}, {GetEventValueOperator(operation.op)});");
                                        break;
                                    case Call.Type.GetButtonPressed:
                                        Script.Flag bflag = scriptManager.GetFlag(Script.Flag.Designation.GetButtonPressedValue, content.entity.ToString());
                                        lines.Add($"EventValueOperation({lflag.id}, {lflag.Bits()}, 0, {bflag.id}, {bflag.Bits()}, {GetEventValueOperator(operation.op)});");
                                        lines.Add($"EventValueOperation({bflag.id}, {bflag.Bits()}, {ushort.MaxValue}, 0, 1, 5);"); // reset value after reading it
                                        break;
                                    default: if (!UNSUPPORTED_SET_LIST.Contains(operation.call.type)) { Lort.Log($" ## WARNING ## Unsupported Papyrus->EMEVD set operation call {papyrus.id}->{call.type}->{operation.call.type}", Lort.Type.Debug); UNSUPPORTED_SET_LIST.Add(operation.call.type); }
                                        break;
                                }
                            }
                            break;
                        }

                    case Call.Type.Journal:
                        {
                            Script.Flag jflag = scriptManager.GetFlag(Script.Flag.Designation.Journal, call.parameters[0]); // look for flag, if not found make one
                            if (jflag == null) { jflag = scriptManager.common.CreateFlag(Script.Flag.Category.Saved, Script.Flag.Type.Byte, Script.Flag.Designation.Journal, call.parameters[0]); }
                            Script.Flag mflag = scriptManager.common.GetOrRegisterNotification(paramanager, "Your journal has been updated!");
                            lines.Add($"EventValueOperation({jflag.id}, {jflag.Bits()}, {int.Parse(call.parameters[1])}, 0, 1, 5);");  // 5 is the 'Assign' operation
                            lines.Add($"SetEventFlag(TargetEventFlagType.EventFlag, {mflag.id}, ON);");
                            break;
                        }

                    case Call.Type.AddTopic:
                        {
                            Script.Flag tvar = scriptManager.GetFlag(Script.Flag.Designation.TopicEnabled, call.parameters[0]);
                            lines.Add($"SetEventFlag(TargetEventFlagType.EventFlag, {tvar.id}, ON);");
                            break;
                        }

                    case Call.Type.Disable:
                        {
                            Script.Flag dvar = scriptManager.GetFlag(Script.Flag.Designation.Disabled, content.entity.ToString());
                            if(dvar == null) { break; } // currently, disabilng assets is unsupported and so the flag doesnt exist. meaning we need to pass here. fixme later
                            lines.Add($"SetEventFlag(TargetEventFlagType.EventFlag, {dvar.id}, ON);");
                            lines.Add($"ChangeCharacterEnableState({content.entity.ToString()}, 0);");
                            break;
                        }

                    case Call.Type.Enable:
                        {
                            Script.Flag evar = scriptManager.GetFlag(Script.Flag.Designation.Disabled, content.entity.ToString());
                            if (evar == null) { break; } // currently, disabilng/enabling assets is unsupported and so the flag doesnt exist. meaning we need to pass here. fixme later
                            lines.Add($"SetEventFlag(TargetEventFlagType.EventFlag, {evar.id}, OFF);");
                            lines.Add($"ChangeCharacterEnableState({content.entity.ToString()}, 1);");
                            break;
                        }

                    case Papyrus.Call.Type.AddItem:
                        {
                            // only supporting items/gold added to player rn. will eventually support other stuff
                            if (call.target == "player")
                            {
                                // Gold specifically handled as souls so its diffo from other item checks
                                if (call.parameters[0] == "gold_001")
                                {
                                    // @TODO: need to generate a speff for this to work
                                }
                                // Any other item
                                else
                                {
                                    ItemManager.ItemInfo itemInfo = itemManager.GetItem(call.parameters[0].ToLower());
                                    if (itemInfo == null) { throw new Exception(); }
                                    lines.Add($"DirectlyGivePlayerItem({(int)itemInfo.type}, {itemInfo.row}, 0, 0);");
                                }
                            }
                            break;
                        }

                    case Papyrus.Call.Type.RemoveItem:
                        {
                            // only supporting items/gold added to player rn. will eventually support other stuff
                            if (call.target == "player")
                            {
                                // Gold specifically handled as souls so its diffo from other item checks
                                if (call.parameters[0] == "gold_001")
                                {
                                    // @TODO: need to generate a speff for this to work
                                }
                                // Any other item
                                else
                                {
                                    ItemManager.ItemInfo itemInfo = itemManager.GetItem(call.parameters[0].ToLower());
                                    if (itemInfo == null) { throw new Exception(); }
                                    lines.Add($"RemoveItemFromPlayer({(int)itemInfo.type}, {itemInfo.row}, 1);");
                                }
                            }
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

                    case Call.Type.Return:
                        {
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
            Script.Flag evtFlag = script.CreateFlag(Script.Flag.Category.Event, Script.Flag.Type.Bit, Script.Flag.Designation.Event, $"{content.id}->{papyrus.id}->{content.entity}");
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
                    Script.Flag onActivateEventFlag = script.CreateFlag(Script.Flag.Category.Event, Script.Flag.Type.Bit, Script.Flag.Designation.Event, content.entity.ToString());
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
                    Script.Flag onActivateEventFlag = script.CreateFlag(Script.Flag.Category.Event, Script.Flag.Type.Bit, Script.Flag.Designation.Event, content.entity.ToString());
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

            /* If there is an "Cell Changed" call we need a temp flag created to use as our "run once" flag */
            if (papyrus.HasCall(Call.Type.CellChanged))
            {
                Script.Flag cellChangedFlag = script.CreateFlag(Script.Flag.Category.Temporary, Script.Flag.Type.Bit, Script.Flag.Designation.CellChanged, content.entity.ToString());
            }

            /* Compile papyrus */
            List<string> lines = HandleScope(papyrus.scope);
            if (lines.Count() <= 0) { return; } // this is a minor optimization. some scripts like nolore end up just being blank as they are (effectively) statically resolved. so we discard empty events that would just do nothing but loop

            lines.Add($"EndUnconditionally(EventEndType.Restart);"); // mw scripts always restart
            foreach (string line in lines) { evt.Instructions.Add(script.AUTO.ParseAdd(line)); }
            script.emevd.Events.Add(evt);
            script.init.Instructions.Add(script.AUTO.ParseAdd($"InitializeEvent(0, {evtFlag.id}, 0);"));
        }
    }
}
