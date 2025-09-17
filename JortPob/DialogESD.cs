using HKLib.hk2018.hkaiCollisionAvoidance;
using HKX2;
using JortPob.Common;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using static JortPob.Dialog;
using static JortPob.Faction;
using static JortPob.NpcManager.TopicData;
using static SoulsFormats.DRB.Shape;
using static SoulsFormats.MSB3.Region;
using static SoulsFormats.MSBS.Event;

namespace JortPob
{
    /* Handles python state machine code generation for a dialog ESD */
    public class DialogESD
    {
        private readonly ESM esm;
        private readonly ScriptManager scriptManager;
        private readonly TextManager textManager;
        private readonly Script areaScript;
        private readonly NpcContent npcContent;

        private readonly List<string> defs;
        private readonly List<string> generatedStates;
        private int nxtGenStateId;

        public DialogESD(ESM esm, ScriptManager scriptManager, TextManager textManager, Script areaScript, uint id, NpcContent npcContent, List<NpcManager.TopicData> topicData)
        {
            this.esm = esm;
            this.scriptManager = scriptManager;
            this.textManager = textManager;
            this.areaScript = areaScript;
            this.npcContent = npcContent;

            defs = new();

            // Create flags for this character's disposition and first greeting
            Script.Flag firstGreet = areaScript.CreateFlag(Script.Flag.Category.Saved, Script.Flag.Type.Bit, Script.Flag.Designation.TalkedToPc, npcContent.entity.ToString());
            Script.Flag disposition = areaScript.CreateFlag(Script.Flag.Category.Saved, Script.Flag.Type.Byte, Script.Flag.Designation.Disposition, npcContent.entity.ToString(), (uint)npcContent.disposition);

            // Split up talk data by type
            NpcManager.TopicData greeting = GetTalk(topicData, DialogRecord.Type.Greeting)[0];
            NpcManager.TopicData hit = GetTalk(topicData, DialogRecord.Type.Hit)[0];
            NpcManager.TopicData attack = GetTalk(topicData, DialogRecord.Type.Attack)[0];
            List<NpcManager.TopicData> talk = GetTalk(topicData, DialogRecord.Type.Topic);

            generatedStates = new();
            nxtGenStateId = 50;

            generatedStates.Add(GeneratedState_ModDisposition(id, Common.Const.ESD_STATE_HARDCODE_MODDISPOSITION));
            generatedStates.Add(GeneratedState_ModFacRep(id, Common.Const.ESD_STATE_HARDCODE_MODFACREP));
            if (npcContent.faction != null)
            {
                generatedStates.Add(GeneratedState_RankReq(id, Common.Const.ESD_STATE_HARDCODE_RANKREQUIREMENT, npcContent));
            }

            defs.Add($"# dialog esd : {npcContent.id}\r\n");

            defs.Add(State_1(id));

            defs.Add(State_1000(id));
            defs.Add(State_1001(id));
            defs.Add(State_1101(id));
            defs.Add(State_1102(id));
            defs.Add(State_1103(id));
            defs.Add(State_2000(id));

            defs.Add(State_x0(id));
            defs.Add(State_x1(id));
            defs.Add(State_x2(id));
            defs.Add(State_x3(id));
            defs.Add(State_x4(id));
            defs.Add(State_x5(id));
            defs.Add(State_x6(id));
            defs.Add(State_x7(id));
            defs.Add(State_x8(id));
            defs.Add(State_x9(id));

            defs.Add(State_x10(id));
            defs.Add(State_x11(id));
            defs.Add(State_x12(id));
            defs.Add(State_x13(id));
            defs.Add(State_x14(id));
            defs.Add(State_x15(id));
            defs.Add(State_x16(id));
            defs.Add(State_x17(id));
            defs.Add(State_x18(id));
            defs.Add(State_x19(id));

            defs.Add(State_x20(id));
            defs.Add(State_x21(id));
            defs.Add(State_x22(id));
            defs.Add(State_x23(id));
            defs.Add(State_x24(id));
            defs.Add(State_x25(id));
            defs.Add(State_x26(id));
            defs.Add(State_x27(id));
            defs.Add(State_x28(id));
            defs.Add(State_x29(id, greeting));

            defs.Add(State_x30(id));
            defs.Add(State_x31(id));
            defs.Add(State_x32(id));
            defs.Add(State_x33(id));
            defs.Add(State_x34(id));
            defs.Add(State_x35(id));
            defs.Add(State_x36(id));
            defs.Add(State_x37(id));
            defs.Add(State_x38(id, hit.talks[0].talkRow));
            defs.Add(State_x39(id, hit));

            defs.Add(State_x40(id, attack.talks[0].talkRow));
            defs.Add(State_x41(id, hit.talks[0].talkRow));
            defs.Add(State_x42(id));
            defs.Add(State_x43(id));
            defs.Add(State_x44(id, npcContent.services, talk));

            foreach (string genState in generatedStates)
            {
                defs.Add(genState);
            }
        }

        /* Returns all topics that match the given type. */
        private List<NpcManager.TopicData> GetTalk(List<NpcManager.TopicData> topicData, DialogRecord.Type type)
        {
            List<NpcManager.TopicData> matches = new();
            foreach(NpcManager.TopicData topic in topicData)
            {
                if (topic.dialog.type == type) { matches.Add(topic); }
            }

            return matches;
        }

        public void Write(string pyPath)
        {
            if (!Directory.Exists(Path.GetDirectoryName(pyPath))) { Directory.CreateDirectory(Path.GetDirectoryName(pyPath)); }
            System.IO.File.WriteAllLines(pyPath, defs);
        }



        /* WARNING: SHITCODE BELOW! YOU WERE WARNED! */



        /* Starting state, top level */
        private string State_1(uint id)
        {
            string id_s = id.ToString("D9");
            Script.Flag hostile = scriptManager.GetFlag(Script.Flag.Designation.Hostile, npcContent.entity.ToString());
            return $"def t{id_s}_1():\r\n    \"\"\"State 0,1\"\"\"\r\n    # actionbutton:6000:\"Talk\"\r\n    t{id_s}_x5(flag6=4743, flag7={hostile.id}, val1=5, val2=10, val3=12, val4=10, val5=12, actionbutton1=6000,\r\n                  flag9=6000, flag10=6001, flag11=6000, flag12=6000, flag13=6000, z1=1, z2=1000000, z3=1000000,\r\n                  z4=1000000, mode1=1, mode2=1)\r\n    Quit()\r\n";
        }

        private string State_1000(uint id)
        {
            string id_s = id.ToString("D9");
            return $"def t{id_s}_1000():\r\n    \"\"\"State 0,2,3\"\"\"\r\n    assert t{id_s}_x37()\r\n    \"\"\"State 1\"\"\"\r\n    EndMachine(1000)\r\n    Quit()\r\n";
        }

        private string State_1001(uint id)
        {
            string id_s = id.ToString("D9");
            return $"def t{id_s}_1001():\r\n    \"\"\"State 0,2,3\"\"\"\r\n    assert t{id_s}_x38()\r\n    \"\"\"State 1\"\"\"\r\n    EndMachine(1001)\r\n    Quit()\r\n";
        }

        private string State_1101(uint id)
        {
            string id_s = id.ToString("D9");
            return $"def t{id_s}_1101():\r\n    \"\"\"State 0,2\"\"\"\r\n    assert t{id_s}_x39()\r\n    \"\"\"State 1\"\"\"\r\n    EndMachine(1101)\r\n    Quit()\r\n";
        }

        private string State_1102(uint id)
        {
            string id_s = id.ToString("D9");
            int hostileFlag = 1043339205;
            return $"def t{id_s}_1102():\r\n    \"\"\"State 0,2\"\"\"\r\n    t{id_s}_x40(flag4={hostileFlag})\r\n    Quit()\r\n";
        }

        private string State_1103(uint id)
        {
            string id_s = id.ToString("D9");
            return $"def t{id_s}_1103():\r\n    \"\"\"State 0,2\"\"\"\r\n    assert t{id_s}_x41()\r\n    \"\"\"State 1\"\"\"\r\n    EndMachine(1103)\r\n    Quit()\r\n";
        }

        private string State_2000(uint id)
        {
            string id_s = id.ToString("D9");
            int unk0Flag = 1043332706;
            int unk1Flag = 1043332707;
            return $"def t{id_s}_2000():\r\n    \"\"\"State 0,2,3\"\"\"\r\n    assert t{id_s}_x42(flag2={unk0Flag}, flag3={unk1Flag})\r\n    \"\"\"State 1\"\"\"\r\n    EndMachine(2000)\r\n    Quit()\r\n";
        }

        private string State_x0(uint id)
        {
            string id_s = id.ToString("D9");
            return $"def t{id_s}_x0(actionbutton1=6000, flag10=6001, flag14=6000, flag15=6000, flag16=6000, flag17=6000, flag9=6000):\r\n    \"\"\"State 0\"\"\"\r\n    while True:\r\n        \"\"\"State 1\"\"\"\r\n        assert not GetOneLineHelpStatus() and not IsClientPlayer() and not IsPlayerDead() and not IsCharacterDisabled()\r\n        \"\"\"State 3\"\"\"\r\n        assert (GetEventFlag(flag10) or GetEventFlag(flag14) or GetEventFlag(flag15) or GetEventFlag(flag16) or\r\n                GetEventFlag(flag17))\r\n        \"\"\"State 4\"\"\"\r\n        assert not GetEventFlag(flag9)\r\n        \"\"\"State 2\"\"\"\r\n        if (GetEventFlag(flag9) or not (not GetOneLineHelpStatus() and not IsClientPlayer() and not IsPlayerDead()\r\n            and not IsCharacterDisabled()) or (not GetEventFlag(flag10) and not GetEventFlag(flag14) and not GetEventFlag(flag15)\r\n            and not GetEventFlag(flag16) and not GetEventFlag(flag17))):\r\n            pass\r\n        # actionbutton:6000:\"Talk\"\r\n        elif CheckActionButtonArea(actionbutton1):\r\n            break\r\n    \"\"\"State 5\"\"\"\r\n    return 0\r\n";
        }

        private string State_x1(uint id)
        {
            string id_s = id.ToString("D9");
            return $"def t{id_s}_x1():\r\n    \"\"\"State 0,1\"\"\"\r\n    if not CheckSpecificPersonTalkHasEnded(0):\r\n        \"\"\"State 7\"\"\"\r\n        ClearTalkProgressData()\r\n        StopEventAnimWithoutForcingConversationEnd(0)\r\n        \"\"\"State 6\"\"\"\r\n        ReportConversationEndToHavokBehavior()\r\n    else:\r\n        pass\r\n    \"\"\"State 2\"\"\"\r\n    if CheckSpecificPersonGenericDialogIsOpen(0):\r\n        \"\"\"State 3\"\"\"\r\n        ForceCloseGenericDialog()\r\n    else:\r\n        pass\r\n    \"\"\"State 4\"\"\"\r\n    if CheckSpecificPersonMenuIsOpen(-1, 0) and not CheckSpecificPersonGenericDialogIsOpen(0):\r\n        \"\"\"State 5\"\"\"\r\n        ForceCloseMenu()\r\n    else:\r\n        pass\r\n    \"\"\"State 8\"\"\"\r\n    return 0\r\n";
        }

        private string State_x2(uint id)
        {
            string id_s = id.ToString("D9");
            return $"def t{id_s}_x2():\r\n    \"\"\"State 0,1\"\"\"\r\n    ClearTalkProgressData()\r\n    StopEventAnimWithoutForcingConversationEnd(0)\r\n    ForceCloseGenericDialog()\r\n    ForceCloseMenu()\r\n    ReportConversationEndToHavokBehavior()\r\n    \"\"\"State 2\"\"\"\r\n    return 0\r\n";
        }

        private string State_x3(uint id)
        {
            string id_s = id.ToString("D9");
            return $"def t{id_s}_x3(val2=10, val3=12):\r\n    \"\"\"State 0,1\"\"\"\r\n    assert GetDistanceToPlayer() < val2 and GetCurrentStateElapsedFrames() > 1\r\n    \"\"\"State 2\"\"\"\r\n    if PlayerDiedFromFallInstantly() == False and PlayerDiedFromFallDamage() == False:\r\n        \"\"\"State 3,6\"\"\"\r\n        call = t{id_s}_x19()\r\n        if call.Done():\r\n            pass\r\n        elif GetDistanceToPlayer() > val3 or GetTalkInterruptReason() == 6:\r\n            \"\"\"State 5\"\"\"\r\n            assert t{id_s}_x1()\r\n    else:\r\n        \"\"\"State 4,7\"\"\"\r\n        call = t{id_s}_x32()\r\n        if call.Done():\r\n            pass\r\n        elif GetDistanceToPlayer() > val3 or GetTalkInterruptReason() == 6:\r\n            \"\"\"State 8\"\"\"\r\n            assert t{id_s}_x1()\r\n    \"\"\"State 9\"\"\"\r\n    return 0\r\n";
        }

        private string State_x4(uint id)
        {
            string id_s = id.ToString("D9");
            return $"def t{id_s}_x4():\r\n    \"\"\"State 0,1\"\"\"\r\n    assert t{id_s}_x1()\r\n    \"\"\"State 2\"\"\"\r\n    return 0\r\n";
        }

        private string State_x5(uint id)
        {
            string id_s = id.ToString("D9");
            Script.Flag hostile = scriptManager.GetFlag(Script.Flag.Designation.Hostile, npcContent.entity.ToString());
            return $"def t{id_s}_x5(flag6=4743, flag7={hostile.id}, val1=5, val2=10, val3=12, val4=10, val5=12, actionbutton1=6000,\r\n                  flag9=6000, flag10=6001, flag11=6000, flag12=6000, flag13=6000, z1=1, z2=1000000, z3=1000000,\r\n                  z4=1000000, mode1=1, mode2=1):\r\n    \"\"\"State 0\"\"\"\r\n    assert GetCurrentStateElapsedTime() > 1.5\r\n    while True:\r\n        \"\"\"State 2\"\"\"\r\n        call = t{id_s}_x22(flag6=flag6, flag7=flag7, val1=val1, val2=val2, val3=val3, val4=val4,\r\n                              val5=val5, actionbutton1=actionbutton1, flag9=flag9, flag10=flag10, flag11=flag11,\r\n                              flag12=flag12, flag13=flag13, z1=z1, z2=z2, z3=z3, z4=z4, mode1=mode1, mode2=mode2)\r\n        assert IsClientPlayer()\r\n        \"\"\"State 1\"\"\"\r\n        call = t{id_s}_x21()\r\n        assert not IsClientPlayer()\r\n";
        }

        private string State_x6(uint id)
        {
            string id_s = id.ToString("D9");
            return $"def t{id_s}_x6(val1=5, val2=10, val3=12, val4=10, val5=12, actionbutton1=6000, flag9=6000, flag10=6001, flag11=6000,\r\n                  flag12=6000, flag13=6000, z1=1, z2=1000000, z3=1000000, z4=1000000, mode1=1, mode2=1):\r\n    \"\"\"State 0\"\"\"\r\n    while True:\r\n        \"\"\"State 2\"\"\"\r\n        call = t{id_s}_x9(actionbutton1=actionbutton1, flag9=flag9, flag10=flag10, z2=z2, z3=z3, z4=z4)\r\n        def WhilePaused():\r\n            RemoveMyAggroIf(IsAttackedBySomeone() and (DoesSelfHaveSpEffect(9626) and DoesSelfHaveSpEffect(9627)))\r\n            GiveSpEffectToPlayerIf(not CheckSpecificPersonTalkHasEnded(0), 9640)\r\n        if call.Done():\r\n            \"\"\"State 4\"\"\"\r\n            Label('L0')\r\n            ChangeCamera(1000000)\r\n            call = t{id_s}_x13(val1=val1, z1=z1)\r\n            def WhilePaused():\r\n                ChangeCameraIf(GetDistanceToPlayer() > 2.5, -1)\r\n                RemoveMyAggroIf(IsAttackedBySomeone() and (DoesSelfHaveSpEffect(9626) and DoesSelfHaveSpEffect(9627)))\r\n                GiveSpEffectToPlayer(9640)\r\n                SetLookAtEntityForTalkIf(mode1 == 1, -1, 0)\r\n                SetLookAtEntityForTalkIf(mode2 == 1, 0, -1)\r\n            def ExitPause():\r\n                ChangeCamera(-1)\r\n            if call.Done():\r\n                continue\r\n            elif IsAttackedBySomeone():\r\n                pass\r\n        elif IsAttackedBySomeone() and not DoesSelfHaveSpEffect(9626) and not DoesSelfHaveSpEffect(9627):\r\n            pass\r\n        elif GetEventFlag(flag13):\r\n            Goto('L0')\r\n        elif GetEventFlag(flag11) and not GetEventFlag(flag12) and GetDistanceToPlayer() < val4:\r\n            \"\"\"State 5\"\"\"\r\n            call = t{id_s}_x15(val5=val5)\r\n            if call.Done():\r\n                continue\r\n            elif IsAttackedBySomeone():\r\n                pass\r\n        elif ((GetDistanceToPlayer() > val5 or GetTalkInterruptReason() == 6) and not CheckSpecificPersonTalkHasEnded(0)\r\n              and not DoesSelfHaveSpEffect(9625)):\r\n            \"\"\"State 6\"\"\"\r\n            assert t{id_s}_x26() and CheckSpecificPersonTalkHasEnded(0)\r\n            continue\r\n        elif GetEventFlag(9000):\r\n            \"\"\"State 1\"\"\"\r\n            assert not GetEventFlag(9000)\r\n            continue\r\n        \"\"\"State 3\"\"\"\r\n        def ExitPause():\r\n            RemoveMyAggro()\r\n        assert t{id_s}_x11(val2=val2, val3=val3)\r\n";
        }

        private string State_x7(uint id)
        {
            string id_s = id.ToString("D9");
            return $"def t{id_s}_x7(val2=10, val3=12):\r\n    \"\"\"State 0,1\"\"\"\r\n    call = t{id_s}_x17(val2=val2, val3=val3)\r\n    assert IsPlayerDead()\r\n    \"\"\"State 2\"\"\"\r\n    t{id_s}_x3(val2=val2, val3=val3)\r\n    Quit()\r\n";
        }

        private string State_x8(uint id)
        {
            string id_s = id.ToString("D9");
            return $"def t{id_s}_x8(flag6=4743, val2=10, val3=12):\r\n    \"\"\"State 0,8\"\"\"\r\n    assert t{id_s}_x36()\r\n    \"\"\"State 1\"\"\"\r\n    if GetEventFlag(flag6):\r\n        \"\"\"State 2\"\"\"\r\n        pass\r\n    else:\r\n        \"\"\"State 3\"\"\"\r\n        if GetDistanceToPlayer() < val2:\r\n            \"\"\"State 4,6\"\"\"\r\n            call = t{id_s}_x20()\r\n            if call.Done():\r\n                pass\r\n            elif GetDistanceToPlayer() > val3 or GetTalkInterruptReason() == 6:\r\n                \"\"\"State 7\"\"\"\r\n                assert t{id_s}_x1()\r\n        else:\r\n            \"\"\"State 5\"\"\"\r\n            pass\r\n    \"\"\"State 9\"\"\"\r\n    return 0\r\n";
        }

        private string State_x9(uint id)
        {
            string id_s = id.ToString("D9");
            return $"def t{id_s}_x9(actionbutton1=6000, flag9=6000, flag10=6001, z2=1000000, z3=1000000, z4=1000000):\r\n    \"\"\"State 0,1\"\"\"\r\n    call = t{id_s}_x10(machine1=2000, val6=2000)\r\n    if call.Get() == 1:\r\n        \"\"\"State 2\"\"\"\r\n        assert (t{id_s}_x0(actionbutton1=actionbutton1, flag10=flag10, flag14=6000, flag15=6000, flag16=6000,\r\n                flag17=6000, flag9=flag9))\r\n    elif call.Done():\r\n        pass\r\n    \"\"\"State 3\"\"\"\r\n    return 0\r\n";
        }

        private string State_x10(uint id)
        {
            string id_s = id.ToString("D9");
            return $"def t{id_s}_x10(machine1=_, val6=_):\r\n    \"\"\"State 0,1\"\"\"\r\n    if MachineExists(machine1):\r\n        \"\"\"State 2\"\"\"\r\n        assert GetCurrentStateElapsedFrames() > 1\r\n        \"\"\"State 4\"\"\"\r\n        def WhilePaused():\r\n            RunMachine(machine1)\r\n        assert GetMachineResult() == val6\r\n        \"\"\"State 5\"\"\"\r\n        return 0\r\n    else:\r\n        \"\"\"State 3,6\"\"\"\r\n        return 1\r\n";
        }

        private string State_x11(uint id)
        {
            string id_s = id.ToString("D9");
            return $"def t{id_s}_x11(val2=10, val3=12):\r\n    \"\"\"State 0\"\"\"\r\n    assert GetCurrentStateElapsedFrames() > 1\r\n    \"\"\"State 5\"\"\"\r\n    assert t{id_s}_x1()\r\n    \"\"\"State 3\"\"\"\r\n    if GetDistanceToPlayer() < val2:\r\n        \"\"\"State 1\"\"\"\r\n        if IsPlayerAttacking():\r\n            \"\"\"State 6\"\"\"\r\n            call = t{id_s}_x12()\r\n            if call.Done():\r\n                pass\r\n            elif GetDistanceToPlayer() > val3 or GetTalkInterruptReason() == 6:\r\n                \"\"\"State 7\"\"\"\r\n                assert t{id_s}_x27()\r\n        else:\r\n            \"\"\"State 4\"\"\"\r\n            pass\r\n    else:\r\n        \"\"\"State 2\"\"\"\r\n        pass\r\n    \"\"\"State 8\"\"\"\r\n    return 0\r\n";
        }

        private string State_x12(uint id)
        {
            string id_s = id.ToString("D9");
            return $"def t{id_s}_x12():\r\n    \"\"\"State 0,1\"\"\"\r\n    assert t{id_s}_x10(machine1=1101, val6=1101)\r\n    \"\"\"State 2\"\"\"\r\n    return 0\r\n";
        }

        private string State_x13(uint id)
        {
            string id_s = id.ToString("D9");
            return $"def t{id_s}_x13(val1=5, z1=1):\r\n    \"\"\"State 0,2\"\"\"\r\n    assert t{id_s}_x23()\r\n    \"\"\"State 1\"\"\"\r\n    call = t{id_s}_x14()\r\n    if call.Done():\r\n        pass\r\n    elif (GetDistanceToPlayer() > val1 or GetTalkInterruptReason() == 6) and not DoesSelfHaveSpEffect(9625):\r\n        \"\"\"State 3\"\"\"\r\n        assert t{id_s}_x25()\r\n    \"\"\"State 4\"\"\"\r\n    return 0\r\n";
        }

        private string State_x14(uint id)
        {
            string id_s = id.ToString("D9");
            return $"def t{id_s}_x14():\r\n    \"\"\"State 0,1\"\"\"\r\n    assert t{id_s}_x10(machine1=1000, val6=1000)\r\n    \"\"\"State 2\"\"\"\r\n    return 0\r\n";
        }

        private string State_x15(uint id)
        {
            string id_s = id.ToString("D9");
            return $"def t{id_s}_x15(val5=12):\r\n    \"\"\"State 0,1\"\"\"\r\n    call = t{id_s}_x16()\r\n    if call.Done():\r\n        pass\r\n    elif GetDistanceToPlayer() > val5 or GetTalkInterruptReason() == 6:\r\n        \"\"\"State 2\"\"\"\r\n        assert t{id_s}_x26()\r\n    \"\"\"State 3\"\"\"\r\n    return 0\r\n";
        }

        private string State_x16(uint id)
        {
            string id_s = id.ToString("D9");
            return $"def t{id_s}_x16():\r\n    \"\"\"State 0,1\"\"\"\r\n    assert t{id_s}_x10(machine1=1100, val6=1100)\r\n    \"\"\"State 2\"\"\"\r\n    return 0\r\n";
        }

        private string State_x17(uint id)
        {
            string id_s = id.ToString("D9");
            return $"def t{id_s}_x17(val2=10, val3=12):\r\n    \"\"\"State 0,5\"\"\"\r\n    assert t{id_s}_x36()\r\n    \"\"\"State 2\"\"\"\r\n    assert not GetEventFlag(3000)\r\n    while True:\r\n        \"\"\"State 1\"\"\"\r\n        assert GetDistanceToPlayer() < val2\r\n        \"\"\"State 3\"\"\"\r\n        call = t{id_s}_x18()\r\n        if call.Done():\r\n            pass\r\n        elif GetDistanceToPlayer() > val3 or GetTalkInterruptReason() == 6:\r\n            \"\"\"State 4\"\"\"\r\n            assert t{id_s}_x28()\r\n";
        }

        private string State_x18(uint id)
        {
            string id_s = id.ToString("D9");
            return $"def t{id_s}_x18():\r\n    \"\"\"State 0,2\"\"\"\r\n    call = t{id_s}_x10(machine1=1102, val6=1102)\r\n    if call.Get() == 1:\r\n        \"\"\"State 1\"\"\"\r\n        Quit()\r\n    elif call.Done():\r\n        \"\"\"State 3\"\"\"\r\n        return 0\r\n";
        }

        private string State_x19(uint id)
        {
            string id_s = id.ToString("D9");
            return $"def t{id_s}_x19():\r\n    \"\"\"State 0,1\"\"\"\r\n    assert t{id_s}_x10(machine1=1001, val6=1001)\r\n    \"\"\"State 2\"\"\"\r\n    return 0\r\n";
        }

        private string State_x20(uint id)
        {
            string id_s = id.ToString("D9");
            return $"def t{id_s}_x20():\r\n    \"\"\"State 0,1\"\"\"\r\n    assert t{id_s}_x10(machine1=1103, val6=1103)\r\n    \"\"\"State 2\"\"\"\r\n    return 0\r\n";
        }

        private string State_x21(uint id)
        {
            string id_s = id.ToString("D9");
            return $"def t{id_s}_x21():\r\n    \"\"\"State 0\"\"\"\r\n    Quit()\r\n";
        }

        private string State_x22(uint id)
        {
            string id_s = id.ToString("D9");
            Script.Flag hostile = scriptManager.GetFlag(Script.Flag.Designation.Hostile, npcContent.entity.ToString());
            return $"def t{id_s}_x22(flag6=4743, flag7={hostile.id}, val1=5, val2=10, val3=12, val4=10, val5=12, actionbutton1=6000,\r\n                   flag9=6000, flag10=6001, flag11=6000, flag12=6000, flag13=6000, z1=1, z2=1000000, z3=1000000,\r\n                   z4=1000000, mode1=1, mode2=1):\r\n    \"\"\"State 0\"\"\"\r\n    while True:\r\n        \"\"\"State 1\"\"\"\r\n        RemoveMyAggro()\r\n        call = t{id_s}_x6(val1=val1, val2=val2, val3=val3, val4=val4, val5=val5, actionbutton1=actionbutton1,\r\n                             flag9=flag9, flag10=flag10, flag11=flag11, flag12=flag12, flag13=flag13, z1=z1, z2=z2,\r\n                             z3=z3, z4=z4, mode1=mode1, mode2=mode2)\r\n        if CheckSelfDeath() or GetEventFlag(flag6):\r\n            \"\"\"State 3\"\"\"\r\n            Label('L0')\r\n            call = t{id_s}_x8(flag6=flag6, val2=val2, val3=val3)\r\n            if not CheckSelfDeath() and not GetEventFlag(flag6):\r\n                continue\r\n            elif GetEventFlag(9000):\r\n                pass\r\n        elif GetEventFlag(flag7):\r\n            \"\"\"State 2\"\"\"\r\n            call = t{id_s}_x7(val2=val2, val3=val3)\r\n            if CheckSelfDeath() or GetEventFlag(flag6):\r\n                Goto('L0')\r\n            elif not GetEventFlag(flag7):\r\n                continue\r\n            elif GetEventFlag(9000):\r\n                pass\r\n        elif GetEventFlag(9000) or IsPlayerDead():\r\n            pass\r\n        \"\"\"State 4\"\"\"\r\n        assert t{id_s}_x35() and not GetEventFlag(9000)\r\n";
        }

        private string State_x23(uint id)
        {
            string id_s = id.ToString("D9");
            return $"def t{id_s}_x23():\r\n    \"\"\"State 0,1\"\"\"\r\n    assert t{id_s}_x24()\r\n    \"\"\"State 2\"\"\"\r\n    return 0\r\n";
        }

        private string State_x24(uint id)
        {
            string id_s = id.ToString("D9");
            return $"def t{id_s}_x24():\r\n    \"\"\"State 0,1\"\"\"\r\n    assert t{id_s}_x10(machine1=1104, val6=1104)\r\n    \"\"\"State 2\"\"\"\r\n    return 0\r\n";
        }

        private string State_x25(uint id)
        {
            string id_s = id.ToString("D9");
            return $"def t{id_s}_x25():\r\n    \"\"\"State 0,1\"\"\"\r\n    call = t{id_s}_x10(machine1=1201, val6=1201)\r\n    if call.Get() == 1:\r\n        \"\"\"State 2\"\"\"\r\n        assert t{id_s}_x4()\r\n    elif call.Done():\r\n        pass\r\n    \"\"\"State 3\"\"\"\r\n    return 0\r\n";
        }

        private string State_x26(uint id)
        {
            string id_s = id.ToString("D9");
            return $"def t{id_s}_x26():\r\n    \"\"\"State 0,1\"\"\"\r\n    call = t{id_s}_x10(machine1=1300, val6=1300)\r\n    if call.Get() == 1:\r\n        \"\"\"State 2\"\"\"\r\n        assert t{id_s}_x4()\r\n    elif call.Done():\r\n        pass\r\n    \"\"\"State 3\"\"\"\r\n    return 0\r\n";
        }

        private string State_x27(uint id)
        {
            string id_s = id.ToString("D9");
            return $"def t{id_s}_x27():\r\n    \"\"\"State 0,1\"\"\"\r\n    call = t{id_s}_x10(machine1=1301, val6=1301)\r\n    if call.Get() == 1:\r\n        \"\"\"State 2\"\"\"\r\n        assert t{id_s}_x4()\r\n    elif call.Done():\r\n        pass\r\n    \"\"\"State 3\"\"\"\r\n    return 0\r\n";
        }

        private string State_x28(uint id)
        {
            string id_s = id.ToString("D9");
            return $"def t{id_s}_x28():\r\n    \"\"\"State 0,1\"\"\"\r\n    call = t{id_s}_x10(machine1=1302, val6=1302)\r\n    if call.Get() == 1:\r\n        \"\"\"State 2\"\"\"\r\n        assert t{id_s}_x4()\r\n    elif call.Done():\r\n        pass\r\n    \"\"\"State 3\"\"\"\r\n    return 0\r\n";
        }

        private string State_x29(uint id, NpcManager.TopicData greeting)
        {
            string id_s = id.ToString("D9");
            string s = $"def t{id_s}_x29(mode6=1):\r\n    \"\"\"State 0,4\"\"\"\r\n    assert t{id_s}_x2() and CheckSpecificPersonTalkHasEnded(0)\r\n    ShuffleRNGSeed(100)\r\n    SetRNGSeed()\r\n";

            if (npcContent.faction != null)
            {
                s += $"    # rankreq call: \"{npcContent.faction}\"\r\n";
                s += $"    assert t{id_s}_x{Common.Const.ESD_STATE_HARDCODE_RANKREQUIREMENT}()\r\n";
            }

            // Build an if-else tree for each possible greeting and its conditions
            if (greeting.talks.Count > 1)
            {
                string ifop = "if";
                for (int i = 0; i < greeting.talks.Count(); i++)
                {
                    NpcManager.TopicData.TalkData talkData = greeting.talks[i];

                    string filters = $" {talkData.dialogInfo.GenerateCondition(scriptManager, npcContent)}";
                    string greetLine = "";
                    if (filters == " " || !(i < greeting.talks.Count() - 1)) { ifop = "else"; filters = ""; }

                    greetLine += $"    {ifop}{filters}:\r\n";
                    greetLine += $"        # greeting: \"{Common.Utility.SanitizeTextForComment(talkData.dialogInfo.text)}\"\r\n";
                    greetLine += $"        TalkToPlayer({talkData.talkRow}, -1, -1, 0)\r\n        assert CheckSpecificPersonTalkHasEnded(0)\r\n";

                    foreach (DialogRecord dialog in talkData.dialogInfo.unlocks)
                    {
                        greetLine += $"        SetEventFlag({dialog.flag.id}, FlagState.On)\r\n";
                    }

                    if(talkData.dialogInfo.script != null)
                    {
                        greetLine += talkData.dialogInfo.script.GenerateEsdSnippet(scriptManager, npcContent, id, 8);
                    }

                    s += greetLine;

                    if (ifop == "if") { ifop = "elif"; }
                    if (ifop == "else") { break; }
                }
            }
            // Or if there is just a single possible greeting just stick there and call it done
            else
            {
                string greetLine = $"    TalkToPlayer({greeting.talks[0].talkRow}, -1, -1, 0)\r\n    assert CheckSpecificPersonTalkHasEnded(0)\r\n";
                foreach (DialogRecord dialog in greeting.talks[0].dialogInfo.unlocks)
                {
                    greetLine += $"    SetEventFlag({dialog.flag.id}, FlagState.On)\r\n";
                }
                s += greetLine;
            }
            s += "    \"\"\"State 3\"\"\"\r\n    if mode6 == 0:\r\n        pass\r\n    else:\r\n        \"\"\"State 2\"\"\"\r\n        ReportConversationEndToHavokBehavior()\r\n    \"\"\"State 5\"\"\"\r\n";
            // Also make sure to flag the TalkedToPC flag as it should be marked true once the player has finished the greeting with an npc for the first time
            s += $"    SetEventFlag({scriptManager.GetFlag(Script.Flag.Designation.TalkedToPc, npcContent.entity.ToString()).id}, FlagState.On)\r\n";
            s += "    return 0\r\n";
            return s;
        }

        private string State_x30(uint id)
        {
            string id_s = id.ToString("D9");
            return $"def t{id_s}_x30(text3=_, mode5=1):\r\n    \"\"\"State 0,4\"\"\"\r\n    assert t{id_s}_x31() and CheckSpecificPersonTalkHasEnded(0)\r\n    \"\"\"State 1\"\"\"\r\n    TalkToPlayer(text3, -1, -1, 1)\r\n    \"\"\"State 3\"\"\"\r\n    if mode5 == 0:\r\n        pass\r\n    else:\r\n        \"\"\"State 2\"\"\"\r\n        ReportConversationEndToHavokBehavior()\r\n    \"\"\"State 5\"\"\"\r\n    return 0\r\n";
        }

        private string State_x31(uint id)
        {
            string id_s = id.ToString("D9");
            return $"def t{id_s}_x31():\r\n    \"\"\"State 0,1\"\"\"\r\n    ClearTalkProgressData()\r\n    StopEventAnimWithoutForcingConversationEnd(0)\r\n    ReportConversationEndToHavokBehavior()\r\n    \"\"\"State 2\"\"\"\r\n    return 0\r\n";
        }

        private string State_x32(uint id)
        {
            string id_s = id.ToString("D9");
            return $"def t{id_s}_x32():\r\n    \"\"\"State 0,1\"\"\"\r\n    assert t{id_s}_x10(machine1=1002, val6=1002)\r\n    \"\"\"State 2\"\"\"\r\n    return 0\r\n";
        }

        private string State_x33(uint id)
        {
            string id_s = id.ToString("D9");
            return $"def t{id_s}_x33(text2=_, mode4=1):\r\n    assert t{id_s}_x2() and CheckSpecificPersonTalkHasEnded(0)\r\n    TalkToPlayer(text2, -1, -1, 0)\r\n    assert CheckSpecificPersonTalkHasEnded(0)\r\n    if mode4 == 0:\r\n        pass\r\n    else:\r\n        ReportConversationEndToHavokBehavior()\r\n    return 0\r\n";
        }

        private string State_x34(uint id)
        {
            string id_s = id.ToString("D9");
            return $"def t{id_s}_x34(text1=_, flag3=_, mode3=1):\r\n    \"\"\"State 0,5\"\"\"\r\n    assert t{id_s}_x31() and CheckSpecificPersonTalkHasEnded(0)\r\n    \"\"\"State 2\"\"\"\r\n    SetEventFlag(flag3, FlagState.On)\r\n    \"\"\"State 1\"\"\"\r\n    TalkToPlayer(text1, -1, -1, 1)\r\n    \"\"\"State 4\"\"\"\r\n    if mode3 == 0:\r\n        pass\r\n    else:\r\n        \"\"\"State 3\"\"\"\r\n        ReportConversationEndToHavokBehavior()\r\n    \"\"\"State 6\"\"\"\r\n    return 0\r\n";
        }

        private string State_x35(uint id)
        {
            string id_s = id.ToString("D9");
            return $"def t{id_s}_x35():\r\n    \"\"\"State 0,1\"\"\"\r\n    assert t{id_s}_x1()\r\n    \"\"\"State 2\"\"\"\r\n    return 0\r\n";
        }

        private string State_x36(uint id)
        {
            string id_s = id.ToString("D9");
            return $"def t{id_s}_x36():\r\n    \"\"\"State 0,1\"\"\"\r\n    if CheckSpecificPersonGenericDialogIsOpen(0):\r\n        \"\"\"State 2\"\"\"\r\n        ForceCloseGenericDialog()\r\n    else:\r\n        pass\r\n    \"\"\"State 3\"\"\"\r\n    if CheckSpecificPersonMenuIsOpen(-1, 0) and not CheckSpecificPersonGenericDialogIsOpen(0):\r\n        \"\"\"State 4\"\"\"\r\n        ForceCloseMenu()\r\n    else:\r\n        pass\r\n    \"\"\"State 5\"\"\"\r\n    return 0\r\n";
        }

        private string State_x37(uint id)
        {
            string id_s = id.ToString("D9");
            return $"def t{id_s}_x37():\r\n    \"\"\"State 0,1\"\"\"\r\n    assert t{id_s}_x43()\r\n    \"\"\"State 2\"\"\"\r\n    assert t{id_s}_x44()\r\n    \"\"\"State 3\"\"\"\r\n    return 0\r\n";
        }

        private string State_x38(uint id, int killTalk)
        {
            string id_s = id.ToString("D9");
            //int killTalk = 80181200;
            return $"def t{id_s}_x38():\r\n    \"\"\"State 0,1\"\"\"\r\n    # talk:80181200:\"Stay away, Us wanderers have had enough.\"\r\n    assert t{id_s}_x30(text3={killTalk}, mode5=1)\r\n    \"\"\"State 2\"\"\"\r\n    return 0\r\n";
        }

        // Character gets hurt by player
        private string State_x39(uint id, NpcManager.TopicData hurts)
        {
            string id_s = id.ToString("D9");

            string s = $"def t{id_s}_x39():\r\n    ShuffleRNGSeed(100)\r\n    SetRNGSeed()\r\n";

            /* decide if we go hostile from being hit */
            Script.Flag hostileFlag = scriptManager.GetFlag(Script.Flag.Designation.Hostile, npcContent.entity.ToString());
            Script.Flag friendHitCounter = scriptManager.GetFlag(Script.Flag.Designation.FriendHitCounter, npcContent.entity.ToString());
            Script.Flag disposition = scriptManager.GetFlag(Script.Flag.Designation.Disposition, npcContent.entity.ToString());
            s += $"    # decide if we are going hostile from this attack\r\n";
            s += $"    if GetEventFlagValue({friendHitCounter.id}, {friendHitCounter.Bits()}) == 1 and GetEventFlagValue({disposition.id}, {disposition.Bits()}) < 15:\r\n";
            s += $"        SetEventFlag({hostileFlag.id}, FlagState.On)\r\n";
            s += $"    elif GetEventFlagValue({friendHitCounter.id}, {friendHitCounter.Bits()}) == 2 and GetEventFlagValue({disposition.id}, {disposition.Bits()}) < 25:\r\n";
            s += $"        SetEventFlag({hostileFlag.id}, FlagState.On)\r\n";
            s += $"    elif GetEventFlagValue({friendHitCounter.id}, {friendHitCounter.Bits()}) == 3 and GetEventFlagValue({disposition.id}, {disposition.Bits()}) < 35:\r\n";
            s += $"        SetEventFlag({hostileFlag.id}, FlagState.On)\r\n";
            s += $"    elif GetEventFlagValue({friendHitCounter.id}, {friendHitCounter.Bits()}) >= 4:\r\n";
            s += $"        SetEventFlag({hostileFlag.id}, FlagState.On)\r\n";
            s += $"    else:\r\n";
            s += $"        pass\r\n\r\n";

            /* lower disposition */
            Script.Flag dvar = scriptManager.GetFlag(Script.Flag.Designation.Disposition, npcContent.entity.ToString());
            s += $"    # lower disposition from being hit\r\n";
            s += $"    assert t{id_s}_x{Const.ESD_STATE_HARDCODE_MODDISPOSITION}(dispositionflag={dvar.id}, value={-10})\r\n\r\n";

            /* pick voice line to play */
            s += $"    # play a voice line in response to being hit\r\n";
            string ifop = "if";
            for (int i = 0; i < hurts.talks.Count(); i++)
            {
                NpcManager.TopicData.TalkData hurt = hurts.talks[i];

                string filters = hurt.dialogInfo.GenerateCondition(scriptManager, npcContent);
                if (filters == "") { filters = "True"; }

                s += $"    {ifop} {filters}:\r\n";
                s += $"        # hurt: \"{Common.Utility.SanitizeTextForComment(hurt.dialogInfo.text)}\"\r\n";
                s += $"        assert t{id_s}_x33(text2={hurt.talkRow}, mode4=1)\r\n";

                if(ifop == "if") { ifop = "elif"; }
            }

            s += $"    else:\r\n        pass\r\n\r\n";
            s += $"    # increment hit counter\r\n";
            s += $"    SetEventFlagValue({friendHitCounter.id}, {friendHitCounter.Bits()}, GetEventFlagValue({friendHitCounter.id}, {friendHitCounter.Bits()}) + 1 )\r\n"; // increment friend hit value
            s += $"    return 0\r\n";
            return s;
        }

        private string State_x40(uint id, int hostileTalk)
        {
            string id_s = id.ToString("D9");
            //int hostileTalk = 80181100;
            return $"def t{id_s}_x40(flag4=1043339205):\r\n    \"\"\"State 0,2\"\"\"\r\n    if not GetEventFlag(flag4):\r\n        \"\"\"State 3,5\"\"\"\r\n        # talk:80181100:\"That's the last straw, you bloody thief!\"\r\n        assert t{id_s}_x34(text1={hostileTalk}, flag3=flag4, mode3=1)\r\n    else:\r\n        \"\"\"State 4\"\"\"\r\n        pass\r\n    \"\"\"State 1\"\"\"\r\n    Quit()\r\n";
        }

        private string State_x41(uint id, int deathTalk)
        {
            string id_s = id.ToString("D9");
            //int deathTalk = 80181300;
            return $"def t{id_s}_x41():\r\n    \"\"\"State 0,1\"\"\"\r\n    # talk:80181300:\"How dare you trample us.\"\r\n    # talk:80181301:\"You filthy thief.\"\r\n    assert t{id_s}_x30(text3={deathTalk}, mode5=1)\r\n    \"\"\"State 2\"\"\"\r\n    return 0\r\n";
        }

        private string State_x42(uint id)
        {
            string id_s = id.ToString("D9");
            int unk0Flag = 1043332706;
            int unk1Flag = 1043332707;
            return $"def t{id_s}_x42(flag2={unk0Flag}, flag3={unk1Flag}):\r\n    \"\"\"State 0\"\"\"\r\n    while True:\r\n        \"\"\"State 1\"\"\"\r\n        # actionbutton:6000:\"Talk\"\r\n        call = t{id_s}_x0(actionbutton1=6000, flag10=6001, flag14=6000, flag15=6000, flag16=6000, flag17=6000,\r\n                             flag9=6000)\r\n        if call.Done():\r\n            break\r\n        elif GetEventFlag(flag2) and not GetEventFlag(flag3):\r\n            \"\"\"State 2\"\"\"\r\n            # talk:80181010:\"What are you playing at! Stop this!\"\r\n            assert t{id_s}_x34(text1=80181010, flag3=flag3, mode3=1)\r\n    \"\"\"State 3\"\"\"\r\n    return 0\r\n";
        }

        private string State_x43(uint id)
        {
            string id_s = id.ToString("D9");
            string s = $"def t{id_s}_x43():\r\n    \"\"\"State 0,1\"\"\"\r\n    # talk:80105100:\"Ah, back again are we?\"\r\n    # talk:80105101:\"Not everyone can tell how good my wares are. You've a discerning eye, you have.\"\r\n    assert t{id_s}_x29(mode6=1)\r\n    \"\"\"State 4\"\"\"\r\n    return 0\r\n";
            return s;
        }

        // Some ESD calls have custom state machine calls because they are complex
        // These are hardcoded in Const the Papyrus region
        // All states 50 and beyond are Choice calls
        private string State_x44(uint id, bool hasShop, List<NpcManager.TopicData> topics)
        {
            string id_s = id.ToString("D9");
            int shop1 = 100625;
            int shop2 = 100649;

            string s = $"def t{id_s}_x44():\r\n    \"\"\"State 0\"\"\"\r\n    while True:\r\n        \"\"\"State 1\"\"\"\r\n        ClearPreviousMenuSelection()\r\n        ClearTalkActionState()\r\n        ClearTalkListData()\r\n        \"\"\"State 2\"\"\"\r\n";

            if (npcContent.faction != null)
            {
                s += $"        # rankreq call: \"{npcContent.faction}\"\r\n";
                s += $"        assert t{id_s}_x{Common.Const.ESD_STATE_HARDCODE_RANKREQUIREMENT}()";
            }

            int listCount = 1; // starts at 1 idk
            if(hasShop)
            {
                s += $"        # action:20000010:\"Purchase\"\r\n        AddTalkListData({listCount++}, 20000010, -1)\r\n        # action:20000011:\"Sell\"\r\n        AddTalkListData({listCount++}, 20000011, -1)\r\n";
            }

            for (int i = 0; i < topics.Count(); i++)
            {
                NpcManager.TopicData topic = topics[i];
                if (topic.IsOnlyChoice()) { continue; } // skip these as they aren't valid or reachable

                List<string> filters = new();
                foreach(NpcManager.TopicData.TalkData talk in topic.talks)
                {
                    string filter = talk.dialogInfo.GenerateCondition(scriptManager, npcContent);
                    if(filter == "") { filters.Clear(); break; }
                    filters.Add(filter);
                }
                string combinedFilters = "";
                for(int j = 0;j<filters.Count();j++)
                {
                    string filter = filters[j];
                    combinedFilters += $"({filter})";
                    if(j<filters.Count()-1) { combinedFilters += " or "; }
                }
                if(combinedFilters != "") { combinedFilters = $" and ({combinedFilters})"; }

                s += $"        # topic: \"{topic.dialog.id}\"\r\n        if GetEventFlag({topic.dialog.flag.id}){combinedFilters}:\r\n            AddTalkListData({i+listCount}, {topic.topicText}, -1)\r\n        else:\r\n            pass\r\n";
            }

            s += $"        # action:20000009:\"Leave\"\r\n        AddTalkListData(99, 20000009, -1)\r\n        \"\"\"State 3\"\"\"\r\n        ShowShopMessage(TalkOptionsType.Regular)\r\n        \"\"\"State 4\"\"\"\r\n        assert not (CheckSpecificPersonMenuIsOpen(1, 0) and not CheckSpecificPersonGenericDialogIsOpen(0))\r\n        \"\"\"State 5\"\"\"\r\n";

            listCount = 1; // reset
            if (hasShop)
            {
                s += $"        if GetTalkListEntryResult() == {listCount++}:\r\n            \"\"\"State 6\"\"\"\r\n            OpenRegularShop({shop1}, {shop2})\r\n            \"\"\"State 7\"\"\"\r\n            assert not (CheckSpecificPersonMenuIsOpen(5, 0) and not CheckSpecificPersonGenericDialogIsOpen(0))\r\n        elif GetTalkListEntryResult() == {listCount++}:\r\n            \"\"\"State 9\"\"\"\r\n            OpenSellShop(-1, -1)\r\n            \"\"\"State 8\"\"\"\r\n            assert not (CheckSpecificPersonMenuIsOpen(6, 0) and not CheckSpecificPersonGenericDialogIsOpen(0))\r\n";
            }

            string ifopA = hasShop ? "elif" : "if";
            for (int i = 0; i<topics.Count();i++)
            {
                NpcManager.TopicData topic = topics[i];
                if (topic.IsOnlyChoice()) { continue; } // skip these as they aren't valid or reachable

                s += $"        {ifopA} GetTalkListEntryResult() == {i + listCount}:\r\n";
                s += $"            # topic: \"{topic.dialog.id}\"\r\n";

                string ifopB = "if";
                foreach (NpcManager.TopicData.TalkData talk in topic.talks)
                {
                    if (talk.IsChoice()) { continue; } // choice dialogs are unreachable from this context, discard

                    string filters = talk.dialogInfo.GenerateCondition(scriptManager, npcContent);
                    if (filters == "") { filters = "True"; }

                    s += $"            {ifopB} {filters}:\r\n";
                    s += $"                # talk: \"{Common.Utility.SanitizeTextForComment(talk.dialogInfo.text)}\"\r\n";
                    s += $"                assert t{id_s}_x33(text2={talk.talkRow}, mode4=1)\r\n";

                    foreach (DialogRecord dialog in talk.dialogInfo.unlocks)
                    {
                        s += $"                SetEventFlag({dialog.flag.id}, FlagState.On)\r\n";
                    }

                    if (talk.dialogInfo.script != null)
                    {
                        if (talk.dialogInfo.script.calls.Count() > 0)
                        {
                            s += talk.dialogInfo.script.GenerateEsdSnippet(scriptManager, npcContent, id, 16);
                        }
                        if(talk.dialogInfo.script.choice != null)
                        {
                            string genState = GeneratedState_Choice(id, nxtGenStateId, talk, topic);
                            generatedStates.Add(genState);
                            s += $"                assert t{id_s}_x{nxtGenStateId}()\r\n";
                            nxtGenStateId++;
                        }
                    }
                    if (ifopB == "if") { ifopB = "elif"; }
                }

                s += $"            else:\r\n                pass\r\n";  // not needed?
                if (ifopA == "if") { ifopA = "elif"; }
            }

            s += $"        else:\r\n            return 0\r\n\r\n";

            return s;
        }

        private string GeneratedState_ModDisposition(uint id, int x)
        {
            string id_s = id.ToString("D9");
            string s = $"def t{id_s}_x{x}(dispositionflag=_, value=_):\r\n    if GetEventFlagValue(dispositionflag, 8) + value < 0:\r\n        # disposition change will result in a negative number so set to 0\r\n        SetEventFlagValue(dispositionflag, 8, 0)\r\n    elif GetEventFlagValue(dispositionflag, 8) + value > 100:\r\n        # disposition change will result in a value higher than 100\r\n        SetEventFlagValue(dispositionflag, 8, 100)\r\n    else:\r\n        # disposition change is within acceptable bounds!\r\n        SetEventFlagValue(dispositionflag, 8, GetEventFlagValue(dispositionflag, 8) + value)\r\n    return 0\r\n\r\n";
            return s;
        }

        private string GeneratedState_ModFacRep(uint id, int x)
        {
            string id_s = id.ToString("D9");
            string s = $"def t{id_s}_x{x}(facrepflag=_, value=_):\r\n    if GetEventFlagValue(facrepflag, 8) + value < 0:\r\n        # faction reputation change will result in a negative number so set to 0\r\n        SetEventFlagValue(facrepflag, 8, 0)\r\n    elif GetEventFlagValue(facrepflag, 8) + value > 255:\r\n        # faction reputation change will result in a value higher than 255\r\n        SetEventFlagValue(facrepflag, 8, 255)\r\n    else:\r\n        # faction reputation change is within acceptable bounds!\r\n        SetEventFlagValue(facrepflag, 8, GetEventFlagValue(facrepflag, 8) + value)\r\n    return 0\r\n\r\n";
            return s;
        }

        private string GeneratedState_RankReq(uint id, int x, NpcContent npcContent)
        {
            string id_s = id.ToString("D9");
            string s = "";
            s += $"def t{id_s}_x{x}():\r\n";
            s += $"    #rankreq state: \"{npcContent.faction}\"\r\n";

            Script.Flag repFlag = scriptManager.GetFlag(Script.Flag.Designation.FactionReputation, npcContent.faction);
            Script.Flag rankFlag = scriptManager.GetFlag(Script.Flag.Designation.FactionRank, npcContent.faction);
            Script.Flag returnValue = scriptManager.GetFlag(Script.Flag.Designation.ReturnValueRankReq, npcContent.entity.ToString()); // find return val flag or create new one
            if (returnValue == null) { returnValue = areaScript.CreateFlag(Script.Flag.Category.Temporary, Script.Flag.Type.Nibble, Script.Flag.Designation.ReturnValueRankReq, npcContent.entity.ToString()); }
            Faction faction = esm.GetFaction(npcContent.faction);

            // First rank
            s += $"    if GetEventFlagValue({rankFlag.id}, {rankFlag.Bits()}) == 0:\r\n";
            s += $"         SetEventFlagValue({returnValue.id}, {returnValue.Bits()}, 3)\r\n";

            for (int i = 0; i < faction.ranks.Count()-1; i++)
            {
                Faction.Rank rank = faction.ranks[i];
                Faction.Rank nextRank = faction.ranks[i + 1];

                // Not max rank
                if (rank != nextRank)
                {
                    s += $"    elif GetEventFlagValue({rankFlag.id}, {rankFlag.Bits()}) == {rank.level}:\r\n";
                    s += $"        if GetEventFlagValue({repFlag.id}, {repFlag.Bits()}) >= {nextRank.reputation}:\r\n";
                    s += $"            SetEventFlagValue({returnValue.id}, {returnValue.Bits()}, 3)\r\n";
                    s += $"        else:\r\n";
                    s += $"            SetEventFlagValue({returnValue.id}, {returnValue.Bits()}, 1)\r\n";
                }
            }

            // Max rank
            s += $"    else:\r\n";
            s += $"        SetEventFlagValue({returnValue.id}, {returnValue.Bits()}, 0)\r\n";
            s += $"    return 0\r\n\r\n";

            return s;
        }

        private string GeneratedState_Choice(uint id, int x, NpcManager.TopicData.TalkData talk, NpcManager.TopicData topic)
        {
            string id_s = id.ToString("D9");
            DialogPapyrus.PapyrusChoice choice = talk.dialogInfo.script.choice;

            string s = "";
            s += $"def t{id_s}_x{x}():\r\n    \"\"\"State 0\"\"\"\r\n    while True:\r\n        \"\"\"State 1\"\"\"\r\n        ClearPreviousMenuSelection()\r\n        ClearTalkActionState()\r\n        ClearTalkListData()\r\n        \"\"\"State 2\"\"\"\r\n";

            string createList = "";
            string executeList = "";

            string ifop = "if";
            foreach (Tuple<int, string> tuple in choice.choices)
            {
                int choiceId = tuple.Item1;
                string choiceText = tuple.Item2;

                for (int i = 0; i < topic.talks.Count(); i++)
                {
                    NpcManager.TopicData.TalkData talkData = topic.talks[i];
                    if (talkData.dialogInfo.type != DialogRecord.Type.Choice) { continue; }

                    // check if talkdata has the correct choice index
                    bool match = false;
                    foreach(Dialog.DialogFilter filter in talkData.dialogInfo.filters)
                    {
                        if(filter.type == DialogFilter.Type.Function && filter.function == DialogFilter.Function.Choice && filter.value == choiceId)
                        {
                            match = true; break;
                        } 
                    }
                    if (!match) { continue; }

                    int choiceTextId = textManager.AddChoice(choiceText);

                    createList += $"        # action:{choiceTextId}:\"{choiceText}\"\r\n        AddTalkListData({choiceId}, {choiceTextId}, -1)\r\n";
                    executeList += $"        {ifop} GetTalkListEntryResult() == {choiceId}:\r\n            assert t{id_s}_x33(text2={talkData.talkRow}, mode4=1)\r\n";

                    foreach (DialogRecord dialog in talkData.dialogInfo.unlocks)
                    {
                        executeList += $"            SetEventFlag({dialog.flag.id}, FlagState.On)\r\n";
                    }

                    if (talkData.dialogInfo.script != null)
                    {
                        executeList += talkData.dialogInfo.script.GenerateEsdSnippet(scriptManager, npcContent, id, 12);
                    }

                    if (ifop == "if") { ifop = "elif"; }

                    break;
                }
            }

            // Bethesda bug workaround!
            // It is possible for a choice to have no valid options. Bethesda has some bugs like this in the base game
            // In this stupid case we just return a blank-ish def that just returns 0.
            if(createList == "")
            {
                return $"def t{id_s}_x{x}():\r\n    \"\"\"State 0\"\"\"\r\n    return 0\r\n\r\n";
            }

            s += createList;
            s += $"        \"\"\"State 3\"\"\"\r\n        ShowShopMessage(TalkOptionsType.Regular)\r\n        \"\"\"State 4\"\"\"\r\n        assert not (CheckSpecificPersonMenuIsOpen(1, 0) and not CheckSpecificPersonGenericDialogIsOpen(0))\r\n        \"\"\"State 5\"\"\"\r\n";
            s += executeList;
            s += "        else:\r\n            pass\r\n";
            s += $"        \"\"\"State 10,11\"\"\"\r\n        return 0\r\n\r\n";

            return s;
        }
    }
}
