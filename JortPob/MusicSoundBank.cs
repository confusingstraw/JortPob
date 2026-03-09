using JortPob.Common;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using static JortPob.SoundManager;

namespace JortPob
{
    public class MusicSoundBank
    {
        private readonly SoundBankGlobals globals;
        public readonly List<Track> tracks;

        public MusicSoundBank(SoundBankGlobals globals)
        {
            this.globals = globals;
            tracks = new();

            string[] files = Directory.GetFiles(Path.Combine(Const.MORROWIND_PATH, "Data Files", "Music", "Explore"));
            foreach(string file in files)
            {
                if (file.ToLower().Contains("morrowind title.mp3")) { continue; } // skip menu music
                AddTrack(Track.Type.Calm, file); // convert music track and add to list
            }
        }

        private void AddTrack(Track.Type type, string file)
        {
            /* Setup some paths */
            string name = Path.GetFileNameWithoutExtension(file);
            string wav = Path.Combine(Const.CACHE_PATH, "music", name, $"{name}.wav");
            string wem = Path.Combine(Const.CACHE_PATH, "music", name, $"{name}.wem");
            Directory.CreateDirectory(Path.GetDirectoryName(wav));

            /* If wem already created then skip conversion */
            if (!File.Exists(wem))
            {
                /* Some files are mp3 and some are wav. Convert if needed, otherwise just copy paste to cache to get ready for wem conversion */
                if (Path.GetExtension(file).ToLower() == ".mp3") { Audio.MP3toWAV(file, wav); }
                else { File.Copy(file, wav); }

                /* Convert wav to wem */
                Audio.WAVtoWEM(wav);
            }

            /* Create play/stop ids and source id for bnk to use */
            uint[] ids = globals.GetEventBnkId("m");

            Track track = new(type, ids[0], ids[1], ids[2], wem, globals.NextSourceId());
            tracks.Add(track);
        }

        public void Write()
        {
            /* Setup some paths */
            string dir = Path.Combine(Const.OUTPUT_PATH, "sd");
            string bnkPath = Path.Combine(dir, "cs_smain.bnk");
            string bnkDir = Path.Combine(dir, "cs_smain");
            string sourcePath = Path.Combine(Const.ELDEN_PATH, "game", "sd", "cs_smain.bnk");
            string bnkJsonPath = Path.Combine(dir, "cs_smain", "soundbank.json");
            string bnkRebuiltPath = Path.Combine(dir, "cs_smain.created.bnk");

            /* Copy base game cs_smain.bnk and then decompile it */
            if (File.Exists(bnkPath)) { File.Delete(bnkPath); }
            if (Directory.Exists(bnkDir)) { Directory.Delete(bnkDir, true); }
            Directory.CreateDirectory(Path.GetDirectoryName(bnkPath));
            File.Copy(sourcePath, bnkPath, true);

            ProcessStartInfo decompBnkProcess = new(Utility.ResourcePath(@"tools\Bnk2Json\bnk2json.exe"), $"\"{bnkPath}\"")
            {
                WorkingDirectory = Utility.ResourcePath(@"tools\Bnk2Json"),
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Utility.ExecuteProcess(decompBnkProcess, false);


        }

        [DebuggerDisplay("MUSIC [{record}] [{type}] [{file}]")]
        public record Track(
            Track.Type type,
            uint id,                    // id used for script calls to playback this sound
            uint play,
            uint stop,
            string file,                // wem file
            uint source                 // source is wem id
        )
        {
            public enum Type { Calm, Battle }
        }
    }
}
