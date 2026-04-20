using FFMpegCore;
using JortPob.Common;
using Mutagen.Bethesda.Archives;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        }

        /* Handle Morrowind Music */
        public void ImportMorrowindMusic()
        {
            if(!Const.INCLUDE_MORROWIND_MUSIC) { return; }

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

                Track.Type trackType;
                if (name.Contains("mw battle"))
                {
                    // is battle music
                    trackType = Track.Type.Battle;
                }
                else
                {
                    // Predefined list of day and night tracks. MW does not define times for tracks so I'm just vibing it out
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
                }
                AddTrack(trackType, file);                    // convert music track and add to list
            }
        }

        /* Handle Skyrim Music */
        public void ImportSkyrimMusic()
        {
            if (!Const.INCLUDE_SKYRIM_MUSIC || !Directory.Exists(Const.SKYRIM_PATH)) { return; }

            string bsaPath = Path.Combine(Const.SKYRIM_PATH, "Data", "Skyrim - Sounds.bsa");
            string musicDir = Path.Combine(Const.OUTPUT_PATH, "cache", "music");

            Directory.CreateDirectory(musicDir);

            var archive = Archive.CreateReader(Const.SKYRIM_EDITION_ENUM, bsaPath);
            foreach (var file in archive.Files)
            {
                if (file.Path.StartsWith("music", StringComparison.OrdinalIgnoreCase))
                {
                    string pathInfo = file.Path.ToLower();
                    Track.Type trackType;
                    if (pathInfo.Contains("mus_explore_dlc2solstheim")) { trackType = Track.Type.Day; }
                    else if (pathInfo.Contains("mus_explore_day_0")) { trackType = Track.Type.Day; }
                    else if (pathInfo.Contains("mus_explore_dusk_0")) { trackType = Track.Type.Night; }
                    else if (pathInfo.Contains("mus_explore_night_0")) { trackType = Track.Type.Night; }
                    else if (pathInfo.Contains("mus_explore_morning_0")) { trackType = Track.Type.Day; }
                    else if (pathInfo.Contains("mus_combat_0") && !pathInfo.Contains("_finale") && !pathInfo.Contains("_cg")) { trackType = Track.Type.Battle; }
                    else { continue; } // skip the rest

                    string trackDir = Path.Combine(musicDir, Path.GetFileNameWithoutExtension(file.Path));
                    Directory.CreateDirectory(trackDir);

                    string xwmFile = Path.Combine(trackDir, Path.GetFileName(file.Path));
                    if (!File.Exists(xwmFile))
                    {
                        File.WriteAllBytes(xwmFile, file.GetBytes());
                    }

                    string wavFile = Path.Combine(trackDir, Path.GetFileNameWithoutExtension(file.Path) + ".wav");
                    if (!File.Exists(wavFile))
                    {
                        FFMpegArguments
                            .FromFileInput(xwmFile)
                            .OutputToFile(wavFile, false, options => options
                                .WithFastStart()
                                .WithCustomArgument("-c:a pcm_s32le"))
                            .ProcessSynchronously();
                    }

                    AddTrack(trackType, wavFile);
                }
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
                if (File.Exists(wav)) { } // file is already in the right location so whatever
                else if (Path.GetExtension(file).ToLower() == ".mp3") { Audio.MP3toWAV(file, wav); }
                else { File.Copy(file, wav); }

                /* Convert wav to wem */
                Audio.WAVtoWEM(wav);
            }

            /* Create play/stop ids and source id for bnk to use */
            uint[] ids = globals.GetEventBnkId("m");

            double duration = Audio.GetDuration(wav);
            Track track = new(type, ids[0], ids[1], ids[2], wem, duration, globals.NextSourceId());
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

            if (Const.DEBUG_REUSE_FILES && File.Exists(bnkPath)) { return; } // if debug_reuse is on, skip if file already created

            /* Convert music files and add the tracks to our track list*/
            ImportMorrowindMusic();
            ImportSkyrimMusic();

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

            JsonNode templateContainer = templateJson[0];
            JsonNode templateSegment = templateJson[1];
            JsonNode templateTrack = templateJson[2];
            JsonNode templateTree = templateJson[3];
            JsonNode templatePlaylist = templateJson[4];

            /* Get main switch */
            int containerInsertAt = -1;
            JsonNode musicSwitch = null;
            for (int i = 0; i < objects.Count; i++)
            {
                JsonNode jsonNode = objects[i];

                uint id = jsonNode["id"]?["Hash"]?.GetValue<uint>() ?? 0;

                if (id == 618709466) { musicSwitch = jsonNode; }
                else if (id == 936896166) { containerInsertAt = i; }
            }

            /* Create and id containers */
            JsonNode dayContainer = templateContainer.DeepClone();
            JsonNode nightContainer = templateContainer.DeepClone();
            JsonNode battleContainer = templateContainer.DeepClone();
            dayContainer["id"]["Hash"] = globals.NextBnkId();
            nightContainer["id"]["Hash"] = globals.NextBnkId();
            battleContainer["id"]["Hash"] = globals.NextBnkId();

            /* Setup switch to use new containers */
            musicSwitch["body"]["MusicSwitchContainer"]["music_trans_node_params"]["music_node_params"]["children"]["items"].AsArray().Clear(); // switch children
            musicSwitch["body"]["MusicSwitchContainer"]["music_trans_node_params"]["music_node_params"]["children"]["items"].AsArray().Add(dayContainer["id"]["Hash"].GetValue<uint>());
            musicSwitch["body"]["MusicSwitchContainer"]["music_trans_node_params"]["music_node_params"]["children"]["items"].AsArray().Add(nightContainer["id"]["Hash"].GetValue<uint>());
            musicSwitch["body"]["MusicSwitchContainer"]["music_trans_node_params"]["music_node_params"]["children"]["items"].AsArray().Add(battleContainer["id"]["Hash"].GetValue<uint>());
            musicSwitch["body"]["MusicSwitchContainer"]["music_trans_node_params"]["music_node_params"]["children"]["count"] = musicSwitch["body"]["MusicSwitchContainer"]["music_trans_node_params"]["music_node_params"]["children"]["items"].AsArray().Count;
            musicSwitch["body"]["MusicSwitchContainer"]["arguments"].AsArray()[0]["group_id"] = 2670057292;  // decision tree args
            musicSwitch["body"]["MusicSwitchContainer"]["arguments"].AsArray()[1]["group_id"] = 362379552;
            JsonNode decisionTree = templateTree.DeepClone();
            decisionTree["children"].AsArray()[0]["children"].AsArray()[0]["node_id"] = nightContainer["id"]["Hash"].GetValue<uint>();  // decision trees
            decisionTree["children"].AsArray()[0]["children"].AsArray()[1]["node_id"] = dayContainer["id"]["Hash"].GetValue<uint>();
            decisionTree["children"].AsArray()[1]["children"].AsArray()[0]["node_id"] = battleContainer["id"]["Hash"].GetValue<uint>();
            musicSwitch["body"]["MusicSwitchContainer"]["tree"] = decisionTree;
            musicSwitch["body"]["MusicSwitchContainer"]["music_trans_node_params"]["transition_rules"].AsArray().Remove(2);
            musicSwitch["body"]["MusicSwitchContainer"]["music_trans_node_params"]["transition_rules"].AsArray().Remove(1);
            musicSwitch["body"]["MusicSwitchContainer"]["music_trans_node_params"]["transition_rule_count"] = musicSwitch["body"]["MusicSwitchContainer"]["music_trans_node_params"]["transition_rules"].AsArray().Count;

            /* Delete orphaned nodes (?) */
            // @TODO: we should probably do this but its such a huge pain i'm not touching it for now. deleting the orphaned nodes also requires deleting child references and whatever

            /* Create nodes for our custom music */
            foreach (Track track in tracks)
            {
                JsonNode container;
                switch (track.type)
                {
                    case Track.Type.Night: container = nightContainer; break;
                    case Track.Type.Battle: container = battleContainer; break;
                    case Track.Type.Day: container = dayContainer; break;
                    default: throw new System.Exception("I'm going to rip my dick off.");
                }

                JsonNode musicSegment = templateSegment.DeepClone();
                JsonNode musicTrack = templateTrack.DeepClone();
                JsonNode playlistEntry = templatePlaylist.DeepClone();

                /* Create ids for nodes */
                musicSegment["id"]["Hash"] = globals.NextBnkId();
                musicTrack["id"]["Hash"] = globals.NextBnkId();
                playlistEntry["playlist_item_id"] = globals.NextBnkId();

                /* Create playlist */
                playlistEntry["segment_id"] = musicSegment["id"]["Hash"].GetValue<uint>();

                /* Add segment and playlist to container */
                container["body"]["MusicRandomSequenceContainer"]["music_trans_node_params"]["music_node_params"]["children"]["items"].AsArray().Add(musicSegment["id"]["Hash"].GetValue<uint>());
                container["body"]["MusicRandomSequenceContainer"]["playlist_items"].AsArray().Add(playlistEntry);

                /* Fill out segment */
                musicSegment["body"]["MusicSegment"]["music_node_params"]["children"]["items"][0] = musicTrack["id"]["Hash"].GetValue<uint>();
                musicSegment["body"]["MusicSegment"]["markers"].AsArray()[1]["position"] = track.duration; // length of track in millis
                musicSegment["body"]["MusicSegment"]["music_node_params"]["node_base_params"]["direct_parent_id"] = container["id"]["Hash"].GetValue<uint>();
                musicSegments.Add(musicSegment);

                /* Fill out explore */
                musicTrack["body"]["MusicTrack"]["node_base_params"]["direct_parent_id"] = musicSegment["id"]["Hash"].GetValue<uint>();
                musicTrack["body"]["MusicTrack"]["sources"].AsArray()[0]["media_information"]["source_id"] = track.source;
                musicTrack["body"]["MusicTrack"]["playlist"].AsArray()[0]["source_id"] = track.source;
                musicTracks.Add(musicTrack);

                /* Write wems */
                string wemSrcPath = track.file;
                string wemTgtPath = Path.Combine(dir, @$"wem\{track.source.ToString("D9").Substring(0, 2)}\{track.source:D9}.wem");
                Directory.CreateDirectory(Path.GetDirectoryName(wemTgtPath));
                if (File.Exists(wemTgtPath)) { File.Delete(wemTgtPath); }
                File.Copy(wemSrcPath, wemTgtPath);
            }

            /* Sort containers */
            SillyJsonUtils.SortUInt(dayContainer["body"]["MusicRandomSequenceContainer"]["music_trans_node_params"]["music_node_params"]["children"]["items"]);
            SillyJsonUtils.SortUInt(nightContainer["body"]["MusicRandomSequenceContainer"]["music_trans_node_params"]["music_node_params"]["children"]["items"]);
            SillyJsonUtils.SortUInt(battleContainer["body"]["MusicRandomSequenceContainer"]["music_trans_node_params"]["music_node_params"]["children"]["items"]);

            /* Modify playlist settings */
            void SetupPlaylist(JsonNode container)
            {
                container["body"]["MusicRandomSequenceContainer"]["playlist_item_count"] = container["body"]["MusicRandomSequenceContainer"]["playlist_items"].AsArray().Count;
                JsonNode playlistSettings = container["body"]["MusicRandomSequenceContainer"]["playlist_items"].AsArray()[0];
                playlistSettings["child_count"] = container["body"]["MusicRandomSequenceContainer"]["playlist_items"].AsArray().Count - 1; // yep
            }
            SetupPlaylist(dayContainer);
            SetupPlaylist(nightContainer);
            SetupPlaylist(battleContainer);

            /* Dump our new nodes into the bnk */
            objects.Insert(containerInsertAt, dayContainer);
            objects.Insert(containerInsertAt, nightContainer);
            objects.Insert(containerInsertAt, battleContainer);
            foreach (JsonNode seg in musicSegments) { objects.Insert(0, seg); }
            foreach (JsonNode trk in musicTracks) { objects.Insert(0, trk); }

            /* Add new nodes to indirect parent containers */
            // @TODO: not required but we should fix this in the future for consistency

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
            double duration,            // millis
            uint source                 // source is wem id
        )
        {
            public enum Type { Day, Night, Battle }
        }
    }
}
