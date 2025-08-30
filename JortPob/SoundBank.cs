using JortPob.Common;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json.Nodes;
using static JortPob.SoundManager;

namespace JortPob
{
    public class SoundBank
    {
        private readonly SoundBankGlobals globals;
        public readonly List<Sound> sounds;

        public SoundBank(SoundBankGlobals globals)
        {
            this.globals = globals;
            sounds = new();
        }


        // Returns the row id of the sound you add
        public uint AddSound(string file, uint dialogInfo, string transcript)
        {
            uint[] ids = globals.GetEventBnkId();

            Sound sound = new(dialogInfo, ids[0], ids[1], ids[2], transcript, file);
            sounds.Add(sound);

            return ids[0];
        }

        public uint AddSound(SoundBank.Sound snd)
        {
            sounds.Add(snd);
            return snd.row;
        }

        /* Generates and writes JSON, then calls bnk2json to build the bnk */
        public void Write(string dir, int id)
        {
            JsonNode json = JsonNode.Parse(System.IO.File.ReadAllText(Utility.ResourcePath(@"sound\bnk_template.json")));
            JsonArray sections = json["sections"].AsArray();
            JsonNode BKHD = sections[0]["BKHD"];
            JsonNode HIRC = sections[1]["HIRC"];
            JsonArray objects = HIRC["objects"].AsArray();

            JsonNode templatePlay = objects[0]; objects.RemoveAt(0);
            JsonNode templatePlayCall = objects[0]; objects.RemoveAt(0);
            JsonNode templateStop = objects[0]; objects.RemoveAt(0);
            JsonNode templateStopCall = objects[0]; objects.RemoveAt(0);
            JsonNode templateData = objects[0]; objects.RemoveAt(0);

            BKHD["bank_id"] = globals.NextHeaderId();

            foreach (Sound sound in sounds)
            {
                JsonNode soundPlayEvent = templatePlay.DeepClone();
                JsonNode soundPlayCall = templatePlayCall.DeepClone();
                JsonNode soundStopEvent = templateStop.DeepClone();
                JsonNode soundStopCall = templateStopCall.DeepClone();
                JsonNode soundData = templateData.DeepClone();

                uint sourceId = globals.NextSourceId();
                soundData["id"] = globals.NextBnkId();
                soundData["object"]["Sound"]["bank_source_data"]["media_information"]["source_id"] = sourceId;

                soundPlayCall["id"] = globals.NextBnkId();
                soundPlayCall["object"]["Action"]["initial_values"]["external_id"] = (uint)soundData["id"];
                soundStopCall["id"] = globals.NextBnkId();
                soundStopCall["object"]["Action"]["initial_values"]["external_id"] = (uint)soundData["id"];

                soundPlayEvent["id"] = sound.play;
                soundPlayEvent["object"]["Event"]["actions"][0] = (uint)soundPlayCall["id"];
                soundStopEvent["id"] = sound.stop;
                soundStopEvent["object"]["Event"]["actions"][0] = (uint)soundStopCall["id"];

                objects.Add(soundData);
                objects.Add(soundPlayCall);
                objects.Add(soundStopCall);
                objects.Add(soundPlayEvent);
                objects.Add(soundStopEvent);

                string pythonPath = @"C:\Users\dmtin\AppData\Local\Programs\Python\Python313\python.exe";
                string scriptPath = $"\"{Utility.ResourcePath(@"tools\WemConversionWrapper\wem_conversion_wrapper.py")}\"";

                /* //@TODO: STUB FOR NOW BUT CORRECTISH CODE 
                string wavPath = $"\"{Utility.ResourcePath(@"sound\test_sound.wav")}\"";

                ProcessStartInfo startInfo2 = new(pythonPath, $"{ scriptPath} {wavPath}")
                {
                    WorkingDirectory = Utility.ResourcePath(@"tools\WemConversionWrapper"),
                    UseShellExecute = false,
                    CreateNoWindow = false
                };
                var process2 = Process.Start(startInfo2);
                process2.WaitForExit();

                string wemSrcPath = Utility.ResourcePath(@"sound\test_sound.wem");
                string wemTgtPath = $"{dir}\\wem\\{sourceId.ToString("D9").Substring(0, 2)}\\{sourceId.ToString("D9")}.wem";
                if (!Directory.Exists(Path.GetDirectoryName(wemTgtPath))) { Directory.CreateDirectory(Path.GetDirectoryName(wemTgtPath)); }
                if (File.Exists(wemTgtPath)) { File.Delete(wemTgtPath); }
                System.IO.File.Move(wemSrcPath, wemTgtPath); */

                string wemSrcPath = Utility.ResourcePath(@"sound\page_turn.wem");
                string wemTgtPath = $"{dir}\\wem\\{sourceId.ToString("D9").Substring(0, 2)}\\{sourceId.ToString("D9")}.wem";
                if (!Directory.Exists(Path.GetDirectoryName(wemTgtPath))) { Directory.CreateDirectory(Path.GetDirectoryName(wemTgtPath)); }
                if (File.Exists(wemTgtPath)) { File.Delete(wemTgtPath); }
                System.IO.File.Copy(wemSrcPath, wemTgtPath);
            }

            string jsonData = json.ToJsonString();
            string bnkPath = $"{dir}vc{id.ToString("D3")}.bnk";
            if (!Directory.Exists(Path.GetDirectoryName(bnkPath))) { Directory.CreateDirectory(Path.GetDirectoryName(bnkPath)); }
            System.IO.File.WriteAllText($"{bnkPath}json", jsonData);

            ProcessStartInfo startInfo = new(Utility.ResourcePath(@"tools\Bnk2Json\bnk2json.exe"), $"\"{bnkPath}json\"")
            {
                WorkingDirectory = Utility.ResourcePath(@"tools\Bnk2Json"),
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var process = Process.Start(startInfo);
            process.WaitForExit();

            if (File.Exists(bnkPath)) { File.Delete(bnkPath); }
            System.IO.File.Move($"{bnkPath}.rebuilt", bnkPath);
        }

        public class Sound
        {
            public readonly uint dialogInfo; // id from dialoginfo we use for quicker searching when adding wems
            public readonly uint row, play, stop;
            public readonly string transcript, file;
            public Sound(uint dialogInfo, uint row, uint play, uint stop, string transcript, string file)
            {
                this.dialogInfo = dialogInfo;
                this.row = row;
                this.play = play;
                this.stop = stop;
                this.transcript = transcript;
                this.file = file;
            }
        }
    }
}
