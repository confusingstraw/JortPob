using JortPob.Common;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.Json.Nodes;
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

            if (Const.DEBUG_SKIP_SOUND) { return; }  // yep

            string[] exploreFiles = Directory.GetFiles(Path.Combine(Const.MORROWIND_PATH, "Data Files", "Music", "Explore"));
            string[] battleFiles = Directory.GetFiles(Path.Combine(Const.MORROWIND_PATH, "Data Files", "Music", "Battle"));
            List<string> files = new();
            files.AddRange(exploreFiles);
            files.AddRange(battleFiles);

            foreach (string file in files)
            {
                string ext = Path.GetExtension(file).ToLower();
                string name = Path.GetFileNameWithoutExtension(file).ToLower();
                if (!(ext == ".mp3" || ext == ".wav")) { continue; }  // skip anything thats not a wav or mp3
                if (name.Contains("mw battle")) { AddTrack(Track.Type.Battle, file); continue; } // is battle music, quick continue

                // Predefined list of day and night tracks. MW does not define times for tracks so I'm just vibing it out
                Track.Type trackType;
                switch (name)
                {
                    case "mx_explore_2":
                    case "mx_explore_4":
                    case "mx_explore_6":
                        trackType = Track.Type.Night; break;
                    case "mx_explore_1":
                    case "mx_explore_3":
                    case "mx_explore_5":
                    case "mx_explore_7":
                    case "morrowind title":
                    default:
                        trackType = Track.Type.Day; break;
                }
                AddTrack(trackType, file);                    // convert music track and add to list
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

            Track track = new(type, ids[0], ids[1], ids[2], wem, 5000, globals.NextSourceId());
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

            /* Open the generated json of the cs_smain bank and add all of the morrowind game music in as a big randommusicsequence */
            JsonNode json = JsonNode.Parse(System.IO.File.ReadAllText(bnkJsonPath));
            JsonArray templateJson = JsonNode.Parse(System.IO.File.ReadAllText(Utility.ResourcePath(@"sound\smain_template.json"))).AsArray();
            JsonArray sections = json["sections"].AsArray();
            JsonNode BKHD = sections[0]["body"]["BKHD"];
            JsonNode HIRC = sections[1]["body"]["HIRC"];
            JsonArray objects = HIRC["objects"].AsArray();

            List<JsonNode> musicSegments = new(), musicTracks = new();

            JsonNode templateSegment = templateJson[0];
            JsonNode templateTrackExplore = templateJson[1];
            JsonNode templateTrackCombat = templateJson[2];
            JsonNode templatePlaylist = templateJson[3];

            /* Find limgrave container we will be using */
            JsonNode dayContainer = null, nightContainer = null;
            foreach(JsonNode jsonNode in objects)
            {
                uint id = jsonNode["id"]?["Hash"]?.GetValue<uint>() ?? 0;

                if (id == 936896166) { dayContainer = jsonNode; }
                else if (id == 426838365) { nightContainer = jsonNode; }
                
                if(dayContainer != null && nightContainer != null) { break; }
            }

            /* Delete 398617814 (MusicSegment for limgrave day) */
            /*foreach(JsonNode jsonNode in objects)
            {
                uint id = jsonNode?["id"]?["Hash"].GetValue<uint>() ?? 0;
                if (id == 398617814) { objects.Remove(jsonNode); break; }
            }*/

            /* Remove limgrave track and playlist before we add custom nodes to it */
            dayContainer["body"]["MusicRandomSequenceContainer"]["music_trans_node_params"]["music_node_params"]["children"]["items"].AsArray().Clear();
            dayContainer["body"]["MusicRandomSequenceContainer"]["playlist_items"].AsArray().RemoveAt(1);

            /* Create nodes for our custom music */
            foreach (Track track in tracks)
            {
                JsonNode musicSegment = templateSegment.DeepClone();
                JsonNode trackExplore = templateTrackExplore.DeepClone();
                JsonNode trackCombat = templateTrackCombat.DeepClone();
                JsonNode playlist = templatePlaylist.DeepClone();

                /* Create ids for nodes */
                musicSegment["id"]["Hash"] = globals.NextBnkId();
                trackExplore["id"]["Hash"] = globals.NextBnkId();
                trackCombat["id"]["Hash"] = globals.NextBnkId();
                playlist["playlist_item_id"] = globals.NextBnkId();

                /* Create playlist */
                playlist["segment_id"] = musicSegment["id"]["Hash"].GetValue<uint>();

                /* Add segment and playlist to container */
                dayContainer["body"]["MusicRandomSequenceContainer"]["music_trans_node_params"]["music_node_params"]["children"]["items"].AsArray().Add(musicSegment["id"]["Hash"].GetValue<uint>());
                dayContainer["body"]["MusicRandomSequenceContainer"]["playlist_items"].AsArray().Add(playlist);

                /* Fill out segment */
                musicSegment["body"]["MusicSegment"]["music_node_params"]["children"]["items"][0] = trackExplore["id"]["Hash"].GetValue<uint>();
                musicSegment["body"]["MusicSegment"]["music_node_params"]["children"]["items"][1] = trackCombat["id"]["Hash"].GetValue<uint>();
                musicSegment["body"]["MusicSegment"]["markers"].AsArray()[1]["position"] = track.length; // length of track in millis
                SillyJsonUtils.SortUInt(musicSegment["body"]["MusicSegment"]["music_node_params"]["children"]["items"]);
                musicSegments.Add(musicSegment);

                /* Fill out explore */
                trackExplore["body"]["MusicTrack"]["node_base_params"]["direct_parent_id"] = musicSegment["id"]["Hash"].GetValue<uint>();
                trackExplore["body"]["MusicTrack"]["sources"].AsArray()[0]["media_information"]["source_id"] = track.source;
                trackExplore["body"]["MusicTrack"]["playlist"].AsArray()[0]["source_id"] = track.source;
                musicTracks.Add(trackExplore);

                /* Fill out combat */
                trackCombat["body"]["MusicTrack"]["node_base_params"]["direct_parent_id"] = musicSegment["id"]["Hash"].GetValue<uint>();
                trackCombat["body"]["MusicTrack"]["sources"].AsArray()[0]["media_information"]["source_id"] = track.source;
                trackCombat["body"]["MusicTrack"]["playlist"].AsArray()[0]["source_id"] = track.source;
                musicTracks.Add(trackCombat);

                /* Write wems */
                string wemSrcPath = track.file;
                string wemTgtPath = Path.Combine(dir, @$"wem\{track.source.ToString("D9").Substring(0, 2)}\{track.source:D9}.wem");
                Directory.CreateDirectory(Path.GetDirectoryName(wemTgtPath));
                if (File.Exists(wemTgtPath)) { File.Delete(wemTgtPath); }
                File.Copy(wemSrcPath, wemTgtPath);
            }

            /* Sort container */
            SillyJsonUtils.SortUInt(dayContainer["body"]["MusicRandomSequenceContainer"]["music_trans_node_params"]["music_node_params"]["children"]["items"]);

            /* Modify playlist settings */
            dayContainer["body"]["MusicRandomSequenceContainer"]["playlist_item_count"] = dayContainer["body"]["MusicRandomSequenceContainer"]["playlist_items"].AsArray().Count;
            JsonNode playlistSettings = dayContainer["body"]["MusicRandomSequenceContainer"]["playlist_items"].AsArray()[0];
            playlistSettings["ers_type"] = 2;                // shuffle mode
            playlistSettings["shuffle"] = 1;                // shuffle on
            playlistSettings["loop_base"] = 0;             // 
            playlistSettings["avoid_repeat_count"] = 1;   // 
            playlistSettings["use_weight"] = 1;          // 
            playlistSettings["child_count"] = dayContainer["body"]["MusicRandomSequenceContainer"]["playlist_items"].AsArray().Count-1; // yep

            /* Dump our new nodes into the bnk */
            foreach (JsonNode seg in musicSegments) { objects.Insert(0, seg); }
            foreach (JsonNode trk in musicTracks) { objects.Insert(0, trk); }

            /* Need to move our state nodes (691596679) and (464936633) to the top of the list */
            for(int i=0;i<objects.Count;i++)
            {
                uint id = objects[i]["id"]?["Hash"]?.GetValue<uint>() ?? 0;
                if (id == 691596679 || id == 464936633)
                {
                    JsonNode node = objects[i];
                    objects.RemoveAt(i);
                    objects.Insert(0, node);
                }
            }

            /* Rebuild bnk */
            Directory.CreateDirectory(Path.GetDirectoryName(bnkJsonPath));
            File.WriteAllText(bnkJsonPath, json.ToJsonString());

            ProcessStartInfo recompBnkProcess = new(Utility.ResourcePath(@"tools\Bnk2Json\bnk2json.exe"), $"\"{Path.GetDirectoryName(bnkJsonPath)}\"")
            {
                WorkingDirectory = Utility.ResourcePath(@"tools\Bnk2Json"),
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Utility.ExecuteProcess(recompBnkProcess, false);

            if (File.Exists(bnkPath)) { File.Delete(bnkPath); }
            File.Move(bnkRebuiltPath, bnkPath);
        }

        [DebuggerDisplay("MUSIC [{record}] [{type}] [{file}]")]
        public record Track(
            Track.Type type,
            uint id,                    // id used for script calls to playback this sound @TODO: DEPRECATED AND UNUSED
            uint play,
            uint stop,
            string file,                // wem file
            uint length,                // millis
            uint source                 // source is wem id
        )
        {
            public enum Type { Day, Night, Battle }
        }
    }
}
