using JortPob.Common;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Numerics;
using static JortPob.InteriorGroup;
using static JortPob.ItemManager;
using static JortPob.NpcContent;
using static JortPob.Script;
using static SoulsFormats.MSBAC4.Event;

namespace JortPob
{

    /* Takes the Morrowind ESM cell grid and re-subdivides it into the Elden Ring tile grid */
    public class Layout
    {
        public List<BaseTile> all;
        public List<HugeTile> huges;
        public List<BigTile> bigs;
        public List<Tile> tiles;

        public List<InteriorGroup> interiors;

        public Layout(Cache cache, ESM esm, Paramanager param, TextManager text, ScriptManager scriptManager)
        {
            all = new();
            huges = new();
            bigs = new();
            tiles = new();

            interiors = new();

            /* Generate tiles based off base game msb info... */
            string msbdata = File.ReadAllText(Utility.ResourcePath(@"msb\msblist.txt"));
            string[] msblist = msbdata.Split(";");

            Lort.Log("Generating layout...", Lort.Type.Main);
            Lort.NewTask("Generating Layout", msblist.Length+esm.exterior.Count+esm.interior.Count);

            foreach (string msb in msblist)
            {
                string[] split = msb.Split(",");
                int m = int.Parse(split[0]);
                int x = int.Parse(split[1]);
                int y = int.Parse(split[2]);
                int b = int.Parse(split[3]);

                if(m == 60 && b == 0)
                {
                    Tile tile = new Tile(m, x, y, b);
                    tiles.Add(tile);
                    all.Add(tile);
                }

                Lort.TaskIterate(); // Progress bar update
            }

            /* Generate BigTiles... */
            foreach (string msb in msblist)
            {
                string[] split = msb.Split(",");
                int m = int.Parse(split[0]);
                int x = int.Parse(split[1]);
                int y = int.Parse(split[2]);
                int b = int.Parse(split[3]);

                if (m == 60 && b == 1)
                {
                    BigTile big = new BigTile(m, x, y, b);

                    foreach (Tile tile in tiles)
                    {
                        int x1 = x * 2;
                        int y1 = y * 2;
                        int x2 = x1 + 2;
                        int y2 = y1 + 2;
                        if (tile.coordinate.x >= x1 && tile.coordinate.x < x2 && tile.coordinate.y >= y1 && tile.coordinate.y < y2)
                        {
                            big.AddTile(tile);
                        }
                    }

                    bigs.Add(big);
                    all.Add(big);
                }

                Lort.TaskIterate(); // Progress bar update
            }

            /* Generate HugeTiles... */
            foreach (string msb in msblist)
            {
                string[] split = msb.Split(",");
                int m = int.Parse(split[0]);
                int x = int.Parse(split[1]);
                int y = int.Parse(split[2]);
                int b = int.Parse(split[3]);

                if (m == 60 && b == 2)
                {
                    HugeTile huge = new HugeTile(m, x, y, b);

                    foreach(BigTile big in bigs)
                    {
                        int x1 = x * 2;
                        int y1 = y * 2;
                        int x2 = x1 + 2;
                        int y2 = y1 + 2;
                        if (big.coordinate.x >= x1 && big.coordinate.x < x2 && big.coordinate.y >= y1 && big.coordinate.y < y2)
                        {
                            huge.AddBig(big);
                        }
                    }

                    foreach(Tile tile in tiles)
                    {
                        int x1 = x * 4;
                        int y1 = y * 4;
                        int x2 = x1 + 4;
                        int y2 = y1 + 4;
                        if(tile.coordinate.x >= x1 && tile.coordinate.x < x2 && tile.coordinate.y >= y1 && tile.coordinate.y < y2)
                        {
                            huge.AddTile(tile);
                        }
                    }

                    huges.Add(huge);
                    all.Add(huge);
                }

                Lort.TaskIterate(); // Progress bar update
            }

            /* Generate Interior Groups */
            foreach (string msb in msblist)
            {
                string[] split = msb.Split(",");
                int m = int.Parse(split[0]);
                int a = int.Parse(split[1]);
                int u = int.Parse(split[2]);
                int b = int.Parse(split[3]);

                int[] validMaps = new int[]
                {
                    12, 13, 14, 15, 16, 19, 20, 21, 22, 25, 28, 30, 31, 32, 34, 35, 39, 40, 41, 42, 43
                };

                if (validMaps.Contains(m) && u == 0 && b == 0)
                {
                    InteriorGroup group = new InteriorGroup(m, a, u, b);
                    interiors.Add(group);
                }

                Lort.TaskIterate(); // Progress bar update
            }

            /* Part 1 of papyrus pre-process */
            /* Find all objects targeted by script calls so we can make sure they are placd in regular tiles. Objects in Big/Huge tiles can't have script data */
            List<Papyrus.Call> allCalls = new();
            foreach (Papyrus papyrus in esm.scripts)
            {
                allCalls.AddRange(papyrus.GetCalls());
            }

            foreach (Dialog.DialogRecord dialog in esm.dialog)
            {
                allCalls.AddRange(dialog.GetCalls());
            }

            List<string> allReferences = new(), ableReferences = new();  // able refs is objects targeted by Enable, Disable, and GetDisabled
            foreach (Papyrus.Call call in allCalls)
            {
                // Grab record reference from target if exists
                string target = call.target?.ToLower().Trim();
                if (target != null && target != "player")
                {
                    if(!allReferences.Contains(target)) { allReferences.Add(target); }

                    switch (call.type)
                    {
                        case Papyrus.Call.Type.Enable:
                        case Papyrus.Call.Type.Disable:
                        case Papyrus.Call.Type.GetDisabled:
                            if (!ableReferences.Contains(target)) { ableReferences.Add(target); }
                            break;
                        default: break;
                    }
                }

                // Grab record reference from arguments if they exist
                switch (call.type)
                {
                    case Papyrus.Call.Type.Cast:
                        {
                            string reference = call.parameters[1].ToLower().Trim();
                            if (reference == "player") { break; }
                            allReferences.Add(reference);
                            break;
                        }
                    case Papyrus.Call.Type.GetDistance:
                        {
                            string reference = call.parameters[0].ToLower().Trim();
                            if (reference == "player") { break; }
                            allReferences.Add(reference);
                            break;
                        }
                    default: break;
                }
            }

            void PreProcessSelfDisableCalls(Cell cell)
            {
                foreach(Content content in cell.contents)
                {
                    Papyrus papyrus = esm.GetPapyrus(content.papyrus);
                    if(
                        papyrus != null &&
                        (
                            papyrus.HasCall(Papyrus.Call.Type.Disable) ||
                            papyrus.HasCall(Papyrus.Call.Type.Enable) ||
                            papyrus.HasCall(Papyrus.Call.Type.GetDisabled)
                        )
                    )
                    {
                        string contentId = content.id.ToLower().Trim();
                        if (!ableReferences.Contains(contentId)) { ableReferences.Add(contentId); }
                    }
                }
            }

            foreach(Cell cell in esm.exterior) { PreProcessSelfDisableCalls(cell); }
            foreach (Cell cell in esm.interior) { PreProcessSelfDisableCalls(cell); }

            /* Subdivide all cell content into tiles */
            Content EmitterConversionCheck(Content content)
            {
                if(content.GetType() != typeof(AssetContent)) { return content; }
                AssetContent assetContent = content as AssetContent;

                /* If an assetcontent has emitter nodes, we convert it to an emittercontent */
                /* We can't really do this earlier than this point sadly because we need both the ESM loaded an cache built to be able to catch this corner case */
                /* So we do it here */
                ModelInfo modelInfo = cache.GetModel(assetContent.mesh);
                if (!modelInfo.HasEmitter()) { return content; }

                EmitterContent emitterContent = assetContent.ConvertToEmitter();
                cache.AddConvertedEmitter(emitterContent);

                return emitterContent;
            }

            foreach (Cell cell in esm.exterior)
            {
                HugeTile huge = GetHugeTile(cell.center);
                TerrainInfo terrain = cache.GetTerrain(cell.coordinate);
                if (terrain != null)
                {
                    if (huge != null) { huge.AddTerrain(cell.center, terrain); }
                    else { Lort.Log($" ## WARNING ## Terrain fell outside of reality [{cell.coordinate.x}, {cell.coordinate.y}] -- {cell.region} :: B02", Lort.Type.Debug); }
                }

                if (huge != null)
                {
                    huge.AddCell(cell);

                    foreach (Content content in cell.contents)
                    {
                        Content c = EmitterConversionCheck(content); // checks if we need to convert an assetcontent into an emittercontent due to it having emitter nodes but no light data

                        huge.AddContent(cache, cell, c, allReferences.Contains(content.id));
                    }
                }
                else { Lort.Log($" ## WARNING ## Cell fell outside of reality [{cell.coordinate.x}, {cell.coordinate.y}] -- {cell.name} :: B02", Lort.Type.Debug); }
                Lort.TaskIterate(); // Progress bar update
            }

            /* Render an ASCII image of the tiles for verification! */
            Lort.Log("Drawing ASCII art of worldspace map...", Lort.Type.Debug);
            for (int y = 28; y < 66; y++)
            {
                string line = "";
                for (int x = 30; x < 64; x++)
                {
                    Tile tile = GetTile(new Int2(x, y));
                    if (tile == null) { line += "-"; }
                    else
                    {
                        line += tile.assets.Count > 0 ? "X" : "~";
                    }
                }
                Lort.Log(line, Lort.Type.Debug);
            }


            /* Subdivide all interior cells into groups */
            int partition = (int)Math.Ceiling(esm.interior.Count / (float)interiors.Count);
            int start = 0, end = partition;
            foreach (InteriorGroup group in interiors)
            {
                for(int i=start; i<Math.Min(end, esm.interior.Count); i++)
                {
                    Cell cell = esm.interior[i];
                    group.AddCell(cell);

                    Lort.TaskIterate(); // Progress bar update
                }

                start += partition;
                end += partition;
            }

            /* Resolve load doors and travel npcs */
            InteriorGroup.Chunk FindChunk(string name) // find a chunk that contains the named cell
            {
                foreach(InteriorGroup group in interiors)
                {
                    foreach (InteriorGroup.Chunk chunk in group.chunks)
                    {
                        if (chunk.cell.name == name) { return chunk; }
                    }
                }

                return null; // may happen if debug options are enabled to build only some cells
            }

            Tile FindTile(Vector3 position) // find a tile based on coords
            {
                foreach (Tile tile in tiles)
                {
                    if(tile.PositionInside(position))
                    {
                        return tile;
                    }
                }

                return null; // may happen if debug options are enabled to build only some cells
            }

            Cell FindCell(Vector3 position)  // getting an ext cell by morrowind coordinates. used to find exterior cell name
            {
                int x = (int)Math.Floor(position.X / Const.CELL_SIZE);
                int y = (int)Math.Floor(position.Z / Const.CELL_SIZE);
                return esm.GetCellByGrid(new Int2(x,y));
            }

            void HandleDoor(DoorContent door)
            {
                if (door.warp != null)
                {
                    // Door goes to interior cell
                    if (door.warp.cell != null)
                    {
                        InteriorGroup.Chunk to = FindChunk(door.warp.cell);
                        if (to == null) { door.warp = null; return; }      // caused by debug sometimes
                        door.warp.map = to.group.map;
                        door.warp.x = to.group.area;
                        door.warp.y = to.group.unk;
                        door.warp.block = to.group.block;
                        door.warp.entity = scriptManager.GetScript(to.group).CreateEntity(Script.EntityType.Region, $"DoorExit::{door.cell.name}->{door.warp.cell}");
                        string areaName;
                        if (to.cell.name.Contains(",")) { areaName = to.cell.name.Split(",")[^1].Trim(); }
                        else { areaName = to.cell.name; }
                        door.warp.prompt = $"Enter {areaName}";
                        to.AddWarp(door.warp);
                    }
                    // Door goes to exterior cell
                    else
                    {
                        Tile to = FindTile(door.warp.position);  // does not respect cell borders in tile msbs. likely a non-issue but kinda sketch ... @TODO:
                        if (to == null) { door.warp = null; return; }     // caused by debug sometimes
                        door.warp.map = to.map;
                        door.warp.x = to.coordinate.x;
                        door.warp.y = to.coordinate.y;
                        door.warp.block = to.block;
                        door.warp.entity = scriptManager.GetScript(to).CreateEntity(Script.EntityType.Region, $"DoorExit::{door.cell.name}->exterior[{door.warp.x},{door.warp.y}]");
                        door.warp.prompt = $"Exit";
                        to.AddWarp(door.warp);
                    }
                }
            }

            void HandleTravel(NpcContent npc, Tile from = null)
            {
                if (npc.travel.Count() > 0)
                {
                    // Travel goes to interior cell
                    for (int i = 0; i < npc.travel.Count; i++)
                    {
                        CharacterContent.Travel travel = npc.travel[i];
                        if (travel.cell != null)
                        {
                            InteriorGroup.Chunk to = FindChunk(travel.cell);
                            if (to == null) { npc.travel.RemoveAt(i--); continue; }      // caused by debug sometimes
                            travel.map = to.group.map;
                            travel.x = to.group.area;
                            travel.y = to.group.unk;
                            travel.block = to.group.block;
                            travel.entity = scriptManager.GetScript(to.group).CreateEntity(Script.EntityType.Region, $"TravelDestination::{npc.cell.name}->{travel.cell}");
                            travel.name = to.cell.name;
                            travel.cost = Const.TRAVEL_DEFAULT_COST;

                            to.AddWarp(travel);
                        }
                        // Travel goes to exterior cell
                        else
                        {
                            Tile to = FindTile(travel.position);  // does not respect cell borders in tile msbs. likely a non-issue but kinda sketch ... @TODO:
                            Cell cell = FindCell(travel.position);
                            if (to == null || cell == null) { npc.travel.RemoveAt(i--); continue; }     // caused by debug sometimes
                            travel.map = to.map;
                            travel.x = to.coordinate.x;
                            travel.y = to.coordinate.y;
                            travel.block = to.block;
                            travel.entity = scriptManager.GetScript(to).CreateEntity(Script.EntityType.Region, $"TravelDestination::{npc.cell.name}->exterior[{travel.x},{travel.y}]");
                            travel.name = cell.name;
                            // calculate distance for cost of travel
                            if (from != null)
                            {
                                Vector2 a = new(from.coordinate.x, from.coordinate.y);
                                Vector2 b = new(to.coordinate.x, to.coordinate.y);
                                travel.cost = (int)(Const.TRAVEL_DISTANCE_COST * Math.Max(1, Vector2.Distance(a, b)));
                            }
                            else { travel.cost = Const.TRAVEL_DEFAULT_COST; }
                                to.AddWarp(travel);
                        }
                    }
                }
            }

            foreach (InteriorGroup group in interiors)
            {
                foreach(InteriorGroup.Chunk chunk in group.chunks)
                {
                    foreach(DoorContent door in chunk.doors)
                    {
                        HandleDoor(door);
                        if(door.warp != null) { door.entity = scriptManager.GetScript(group).CreateEntity(Script.EntityType.Asset, $"DoorEntry::{door.cell.name}->{door.warp.cell}"); }
                    }

                    foreach(NpcContent npc in chunk.npcs)
                    {
                        HandleTravel(npc);
                    }
                }
            }

            foreach (Tile tile in tiles)
            {
                foreach (DoorContent door in tile.doors)
                {
                    HandleDoor(door);
                    if (door.warp != null) { door.entity = scriptManager.GetScript(tile).CreateEntity(Script.EntityType.Asset, $"DoorExit::{door.cell.name}->exterior[{door.warp.x},{door.warp.y}]"); }
                }

                foreach (NpcContent npc in tile.npcs)
                {
                    HandleTravel(npc, tile);
                }
            }

            // default location name value for interiors
            foreach(InteriorGroup group in interiors)
            {
                int textId = int.Parse($"{group.map:D2}{group.area:D2}0");
                text.SetLocation(textId, "Interior");
            }

            /* Handle npc hasWitness flag */ // this check is very similar to and patially entwined with Script.GenerateCrimeEvents() and the ESD state HANDLECRIME
            /* We can't determine witnesses at runtime so we just do some test now and determine if an npc has witneses to report crimes to */
            void CheckWitnesses(List<NpcContent> npcs)
            {
                foreach (NpcContent npc in npcs)
                {
                    if (npc.alarm >= 50 || npc.IsGuard()) { npc.hasWitness = true; continue; } // will report crimes themselves
                    foreach (NpcContent other in npcs)
                    {
                        if (npc == other) { continue; } // dont' self succ

                        // guards get bonus range because I said so
                        if (other.IsGuard() && System.Numerics.Vector3.Distance(npc.position, other.position) < 23) { npc.hasWitness = true; break; }
                        if (other.alarm >= 50 && System.Numerics.Vector3.Distance(npc.position, other.position) < 12) { npc.hasWitness = true; break; }
                    }
                }
            }

            foreach (Tile tile in tiles) { CheckWitnesses(tile.npcs); }
            foreach (InteriorGroup group in interiors) {
                foreach (InteriorGroup.Chunk chunk in group.chunks) { CheckWitnesses(chunk.npcs); }
            }

            /* Statically resolve shop inventories for npcs (also creatures too) */
            void ResolveShop(CharacterContent npc)
            {
                if (!npc.HasBarter()) { return; } // nope!

                bool WillBarter(CharacterContent npc, ESM.Type type)
                {
                    switch(type)
                    {
                        case ESM.Type.Armor:
                            return npc.services.Contains(CharacterContent.Service.BartersArmor);
                        case ESM.Type.Book:
                            return npc.services.Contains(CharacterContent.Service.BartersBooks);
                        case ESM.Type.Clothing:
                            return npc.services.Contains(CharacterContent.Service.BartersClothing);
                        case ESM.Type.Ingredient:
                            return npc.services.Contains(CharacterContent.Service.BartersIngredients);
                        case ESM.Type.Light:
                            return npc.services.Contains(CharacterContent.Service.BartersLights);
                        case ESM.Type.MiscItem:
                            return npc.services.Contains(CharacterContent.Service.BartersMiscItems);
                        case ESM.Type.Weapon:
                            return npc.services.Contains(CharacterContent.Service.BartersWeapons);
                        case ESM.Type.Probe:
                            return npc.services.Contains(CharacterContent.Service.BartersProbes);
                        case ESM.Type.Lockpick:
                            return npc.services.Contains(CharacterContent.Service.BartersLockpicks);
                        case ESM.Type.RepairItem:
                            return npc.services.Contains(CharacterContent.Service.BartersRepairItems);
                        case ESM.Type.Alchemy:
                            return npc.services.Contains(CharacterContent.Service.BartersAlchemy);
                        case ESM.Type.Apparatus:
                            return npc.services.Contains(CharacterContent.Service.BartersApparatus);
                        default:
                            return false;
                    }
                }

                Cell cell = npc.cell;
                List<(string id, int quantity)> shopInv = new();

                void AddOrIncrement(List<(string id, int quantity)> list, (string id, int quantity) tuple)
                {
                    for(int i=0;i<list.Count();i++)
                    {
                        (string id, int quantity) entry = list[i];
                        if(entry.id.ToLower() == tuple.id.ToLower()) {
                            list.RemoveAt(i);
                            list.Add((entry.id, entry.quantity + tuple.quantity)); // can't increment value in a tuple because fuck
                            return;
                        }
                    }
                    list.Add(tuple);
                }

                foreach (ItemContent item in cell.items) // add loose items this npc owns
                {
                    if (item.ownerNpc == npc.id)
                    {
                        if (WillBarter(npc, item.type))
                        {
                            AddOrIncrement(shopInv, (item.id, 1));
                        }
                    }
                }
                foreach (ContainerContent container in cell.containers) // add containers this npc owns
                {
                    if (container.ownerNpc == npc.id)
                    {
                        foreach ((string id, int quantity) tuple in container.inventory)
                        {
                            Record record = esm.FindRecordById(tuple.id);
                            if (WillBarter(npc, record.type))
                            {
                                AddOrIncrement(shopInv, tuple);
                            }
                        }
                    }
                }

                foreach((string id, int quantity) tuple in npc.inventory) // add own inventory to potential barter
                {
                    Record record = esm.FindRecordById(tuple.id);
                    if (WillBarter(npc, record.type))
                    {
                        AddOrIncrement(shopInv, tuple);
                    }
                }
                if (shopInv.Count() > 0) { npc.barter = shopInv; }
            }

            foreach (Tile tile in tiles)
            {
                foreach (NpcContent npc in tile.npcs) { ResolveShop(npc); }
                foreach(CreatureContent creature in tile.creatures) { ResolveShop(creature); }
            }
            foreach (InteriorGroup group in interiors)
            {
                foreach (InteriorGroup.Chunk chunk in group.chunks)
                {
                    foreach (NpcContent npc in chunk.npcs) { ResolveShop(npc); }
                    foreach (CreatureContent creature in chunk.creatures) { ResolveShop(creature); }
                }
            }

            /* Pat 2 of Papyrus preprocess */
            /* This assigns entity ids to objects that have scripts referencing them */
            /* It also in some cases creates flags and events for objects that require them. */
            /* Also notably we setup disable/enable flags here as well */
            int debugCount = 0;
            void PreprocessContent(Script areaScript, IEnumerable<Content> contents)
            {
                foreach(Content content in contents)
                {
                    string contentId = content.id.ToLower().Trim();
                    if (allReferences.Contains(contentId) || content is CharacterContent || content is ItemContent || content is DoorContent || content.papyrus != null)
                    {
                        // Create an entity ID for this object so that it can be interacted with via scripts
                        Script.EntityType entityType;
                        switch (content)
                        {
                            case ItemContent ic: entityType = Script.EntityType.Asset; break;
                            case CharacterContent cc: entityType = Script.EntityType.Enemy; break;
                            case StaticContent sc: entityType = Script.EntityType.Asset; break;
                            default: throw new Exception("Invalid content type for script preprocess");
                        }
                        content.entity = areaScript.CreateEntity(entityType, $"{content.type}::{content.id}");

                        if(ableReferences.Contains(contentId))
                        {
                            // Object disabled flag
                            Script.Flag disableFlag = areaScript.CreateFlag(Script.Flag.Category.Saved, Script.Flag.Type.Bit, Script.Flag.Designation.Disabled, content.entity.ToString());
                        }

                        debugCount++;
                    }

                    switch (content)
                    {
                        case LightContent lc:
                            {
                                // BTL lights likely can't have scripts (havent checked) so just continue
                                break;
                            }
                        case ItemContent item:
                            {
                                // Item scripts are registered during MSB generation. This is due to the fact that they are tied in closely with params that are generated at that point.
                                break;
                            }
                        case StaticContent statik:
                            {
                                areaScript.RegisterStaticDisable(statik);
                                break;
                            }
                        case CharacterContent character:
                            {
                                // Dead by type list.
                                // So morrowind uses a weird system where it keeps a count of each "type" of npc/creature is killed
                                // For most npcs this count will only ever be 0 or 1 since there is only one of that npc in the world
                                // But for like rats and shit it keeps count so it knows you've killed 10 rats or whatever
                                // So to emulate this system we will have a seperate counter flag for each record type of creature or npc
                                Script.Flag countFlag = scriptManager.common.GetOrCreateFlag(Script.Flag.Category.Saved, Script.Flag.Type.Byte, Script.Flag.Designation.DeadCount, character.id);

                                // Humanoid NPCs or creatures that can talk
                                if (character is NpcContent || (character is CreatureContent && !character.dead && esm.HasDialog((CreatureContent)character)))
                                {
                                    // Pre-Dead npcs get a special script to have their body just lay there. They do not get any flags or ESD stuff built
                                    if (character is NpcContent && character.dead) { areaScript.RegisterDeadNpc((NpcContent)character); }
                                    // Create a bunch of stuff needed for NPCs to work
                                    else
                                    {
                                        /* Create various flags requried for NPCs */
                                        Script.Flag firstGreet = areaScript.CreateFlag(Script.Flag.Category.Saved, Script.Flag.Type.Bit, Script.Flag.Designation.TalkedToPc, character.entity.ToString());
                                        Script.Flag disposition = areaScript.CreateFlag(Script.Flag.Category.Saved, Script.Flag.Type.Byte, Script.Flag.Designation.Disposition, character.entity.ToString(), (uint)character.disposition);
                                        Script.Flag pickpocketedFlag = areaScript.CreateFlag(Script.Flag.Category.Temporary, Script.Flag.Type.Bit, Script.Flag.Designation.Pickpocketed, character.entity.ToString());
                                        Script.Flag thiefFlag = areaScript.CreateFlag(Script.Flag.Category.Temporary, Script.Flag.Type.Bit, Script.Flag.Designation.ThiefCrime, character.entity.ToString());

                                        /* Register some scripts for NPCs */
                                        areaScript.RegisterCharacter(param, character, countFlag);
                                        areaScript.RegisterNpcHostility(character);  // setup hostility flag/event
                                        areaScript.RegisterNpcHello(character);      // setup hello flags and turntoplayer script
                                    }
                                }
                                // Regular creatures
                                else
                                {
                                    // Dead creatures not supported rn
                                    CreatureContent creature = content as CreatureContent;
                                    if (creature.dead) { throw new Exception("Pre-Dead creatures not supported yet!"); }
                                    else { areaScript.RegisterCharacter(param, creature, countFlag); }
                                }
                                break;
                            }
                        default: throw new Exception("Invalid content type for script preprocess");
                    }
                }
            }

            foreach (Tile tile in tiles) { PreprocessContent(scriptManager.GetScript(tile), tile.GetAllContent()); }
            foreach (InteriorGroup group in interiors)
            {
                foreach (InteriorGroup.Chunk chunk in group.chunks) { PreprocessContent(scriptManager.GetScript(group), chunk.GetAllContent()); }
            }

            /* Generate map point placements */
            Dictionary<string, List<Vector3>> mapPoints = new();

            // collect em all
            foreach (Cell cell in esm.exterior)
            {
                if(!string.IsNullOrEmpty(cell.name))
                {
                    Landscape landscape = esm.GetLandscape(cell.coordinate);
                    Vector3 center;
                    if (landscape == null) { center = new(); }
                    else { center = cell.center + new Vector3(0f, landscape.GetHeightAverage(), 0f); }

                    if (mapPoints.ContainsKey(cell.name)) { mapPoints[cell.name].Add(center); }
                    else { mapPoints.Add(cell.name, new() { center }); }
                }
            }

            // merge similar
            List<string> pointNames = new();
            List<MapPoint> importants = new();
            foreach(var kvp in mapPoints)
            {
                string name = kvp.Key;                // name
                Vector3 center = new();               // average of all points with same name
                float radius = Const.CELL_SIZE / 2;   // minimum size for radius of map point is 1 cell

                Vector3 first = kvp.Value.First();
                foreach(Vector3 pos in kvp.Value)
                {
                    radius = Math.Max(radius, Vector3.Distance(first, pos));
                    center += pos;
                }

                center = center * (1f / kvp.Value.Count());

                MapPoint.Icon icon = Override.GetMapIcon(name);
                if (icon == MapPoint.Icon.None) { continue; }  // skip these
                Script.Flag discoverFlag = scriptManager.common.CreateFlag(Script.Flag.Category.Saved, Script.Flag.Type.Bit, Script.Flag.Designation.DiscoverLocation, name);
                Layout.MapPoint mapPoint = new(name, center, radius, true, discoverFlag, icon);
                Tile tile = GetTile(center);
                tile.AddMapPoint(mapPoint);
                importants.Add(mapPoint);
                pointNames.Add(name.ToLower().Trim());
            }

            // Search for interior doors to add markers for
            bool IsInsideImportant(Vector3 position)  // checks if a position is inside of one of the important map points we created above. returns true if it is
            {
                foreach(MapPoint important in importants)
                {
                    if (Vector3.Distance(position, important.position) <= important.radius) { return true; }
                }
                return false;
            }

            bool AlreadyExists(string name)  // see if map point has already been made. some areas have multiple entrances or exits
            {
                if(pointNames.Contains(name.ToLower().Trim())) { return true; }
                pointNames.Add(name.ToLower().Trim());
                return false;
            }

            foreach(Tile tile in tiles)
            {
                foreach(DoorContent door in tile.doors)
                {
                    if(door.warp != null)
                    {
                        string name = door.warp.cell;
                        if (name.Contains(",")) { name = name.Split(",")[0].Trim(); } // Split area sub names so we just have the main area name. Changes things like "Shipwreck, Upper Level" to just "Shipwreck"
                        MapPoint.Icon icon = Override.GetMapIcon(name);

                        if (icon == MapPoint.Icon.None) { continue; }  // skip these
                        if (IsInsideImportant(door.position)) { continue; } // skip these too!
                        if (AlreadyExists(name)) { continue; }    // skip this one as well!

                        const float UNIMPORTANT_SIZE_MODIFIER = 0.3f;
                        Script.Flag discoverFlag = scriptManager.common.CreateFlag(Script.Flag.Category.Saved, Script.Flag.Type.Bit, Script.Flag.Designation.DiscoverLocation, name); // if 2 doors go to the same interior we share the flag
                        Layout.MapPoint mapPoint = new(name, door.position, Const.CELL_SIZE * UNIMPORTANT_SIZE_MODIFIER, false, discoverFlag, icon);
                        tile.AddMapPoint(mapPoint);
                    }
                }
            }

        }

        public HugeTile GetHugeTile(Vector3 position)
        {
            foreach (HugeTile huge in huges)
            {
                if (huge.PositionInside(position))
                {
                    return huge;
                }
            }
            return null;
        }

        public HugeTile GetHugeTile(Int2 coordinate)
        {
            foreach (HugeTile huge in huges)
            {
                if (huge.coordinate == coordinate)
                {
                    return huge;
                }
            }
            return null;
        }

        public BigTile GetBigTile(Vector3 position)
        {
            foreach (BigTile big in bigs)
            {
                if (big.PositionInside(position))
                {
                    return big;
                }
            }
            return null;
        }

        public Tile GetTile(Vector3 position)
        {
            foreach(Tile tile in tiles)
            {
                if (tile.PositionInside(position))
                {
                    return tile;
                }
            }
            return null;
        }

        public Tile GetTile(Int2 coordinate)
        {
            foreach(Tile tile in tiles)
            {
                if(tile.coordinate == coordinate)
                {
                    return tile;
                }
            }
            return null;
        }

        /* Get an overworld tile that contains the cell of the given name */
        public Tile GetTile(string cellName)
        {
            foreach(Tile tile in tiles)
            {
                foreach(Cell cell in tile.cells)
                {
                    if(cell.name == cellName)
                    {
                        return tile;
                    }
                }
            }
            return null;
        }

        /* Generates a list of map ids that are being used */
        /* This is the first number in the msb name. m60 for example is overworld */
        public List<int> ListCommon()
        {
            List<int> list = new();
            list.Add(60);
            foreach(InteriorGroup group in interiors)
            {
                if (!list.Contains(group.map)) { list.Add(group.map); }
            }

            return list;
        }


        /* These 2 funcs search by reference. It's finding where this specific content object is located */
        public Tile FindTile(Content source)
        {
            foreach(Tile tile in tiles)
            {
                foreach (Content content in tile.GetAllContent())
                {
                    if(content == source)
                    {
                        return tile;
                    }
                }
            }
            return null;
        }

        public Chunk FindChunk(Content source)
        {
            foreach(InteriorGroup group in interiors)
            {
                foreach(InteriorGroup.Chunk chunk in group.chunks)
                {
                    foreach (Content content in chunk.GetAllContent())
                    {
                        if(content == source)
                        {
                            return chunk;
                        }
                    }
                }
            }
            return null;
        }

        /* Called by script compilers in DialogESD.cs and PapyrusEMEVD.cs */
        /* When a Papyrus script references an object by it's record name we need to find that object to do operations on it */
        /* Example is like "fargoth"->disable */
        /* In that case we search through all content to find the first Content with the record id of "fargoth" */
        /* In this search we return the first result even if there are multiple references using that same record */
        /* Morrowind prioritizes loaded cells in this search so we will try to emulate this behaviour */
        /* This function takes a content "source" as an input parameter so we can search the area around the origination of this script call first before searching the full world */
        /* Source can also be null and we skip the local search. Reason for this is that global scripts coming from the papyrus 'Main' do not have a local object they are attached to so it's null */
        public Content FindScriptReference(Content source, string reference)
        {
            if (source != null)
            {
                IEnumerable<Content> local;
                Tile tile = FindTile(source);
                if (tile != null)
                {
                    local = tile.GetAllContent();
                }
                else
                {
                    Chunk chunk = FindChunk(source);
                    local = chunk.GetAllContent();
                }

                foreach (Content content in local)
                {
                    if (content.id.ToLower() == reference.ToLower())
                    {
                        return content;
                    }
                }
            }

            // not found in local area, search whole world now
            foreach(Tile t in tiles)
            {
                foreach(Content c in t.GetAllContent())
                {
                    if(c.id.ToLower() == reference.ToLower())
                    {
                        return c;
                    }
                }
            }

            foreach(InteriorGroup group in interiors)
            {
                foreach(InteriorGroup.Chunk chunk in group.chunks)
                {
                    foreach(Content c in chunk.GetAllContent())
                    {
                        if(c.id.ToLower() == reference.ToLower())
                        {
                            return c;
                        }
                    }
                }
            }

            return null; // not found anywhere!
        }

        public class MapPoint
        {
            public enum Icon
            {
                None = 0,                 // discards map point completely!
                Auto = 1,                 // Take a wild guess
                StoneBuilding = 3,        // Gnisis, Suran, Maar Gan
                LargeStoneCity = 51,      // Balmora & Ald-ruhn
                Tomb = 4,
                RuinPillars = 5,
                WoodShack = 6,
                WoodTownA = 7,            // Caldera
                RuinTower = 8,
                Circle = 9,               // Default if not specified
                LargeStoneGate = 10,
                LargeBridge = 11,
                CaveEntrance = 13,
                MineEntrance = 14,
                TombSpeical = 16,
                MageTower = 17,
                Fort = 18,
                Farm = 19,
                RuinStoneLarge = 20,
                Encampment = 22,
                StoneTown = 35,         // Pelagiad, Ald Velothi
                WoodTownB = 37,         // Dagon Fel
                WoodTownC = 38,         // Seyda Neen, Gnaar Mok
                WoodTownD = 39,         // Khuul
                WoodTownE = 40,         // Hla Oad
                TombOrnate = 47,
                Canton = 15,            // All vivec cantons
                TreeTown = 55,          // Sadrith Mora and other mushroom towns
                VolcanoTown = 58,       // Molag Mar
                SwampFort = 27,
                Tree = 30,
                TowerTemple = 65,          // Vivec palace
                TreeFort = 26,             // Vos
                SeasideCastle = 28,        // Ebonheart
                DwarvenRuin = 25,          // Ye
                DaedricRuin = 59           // Ye
            }

            public readonly string name;
            public readonly Vector3 position;
            public Vector3 relative;
            public readonly float radius;
            public readonly bool important; // if true then displays text on screen when you enter the area, otherwise just marks it on your map. EX: cities are important, cave entrances are not.
            public Script.Flag discovered;  // 1 bit flag that is flipped when you discover an area. marks it on your map. preston garvey would be proud
            public MapPoint.Icon icon;

            public MapPoint(string name, Vector3 position, float radius, bool important, Script.Flag discovered, MapPoint.Icon icon)
            {
                this.name = name;
                this.position = position;
                this.radius = radius;
                this.important = important;
                this.discovered = discovered;
                if(icon == MapPoint.Icon.Auto)
                {
                    string n = name.ToLower().Trim();
                    if (n.Contains("cave") || n.Contains("grotto")) { this.icon = MapPoint.Icon.CaveEntrance; }
                    else if (n.Contains("farm") || n.Contains("plantation")) { this.icon = MapPoint.Icon.Farm; }
                    else if (n.Contains("shack") || n.Contains("house")) { this.icon = MapPoint.Icon.WoodShack; }
                    else { this.icon = MapPoint.Icon.Circle; }  // default result
                }
                else
                {
                    this.icon = icon;
                }
            }
        }

        public class WarpDestination
        {
            public readonly Vector3 position, rotation;
            public readonly uint id;  // entity id

            public WarpDestination(Vector3 position, Vector3 rotation, uint id)
            {
                this.position = position - new Vector3(0f, Const.NPC_ROOT_OFFSET, 0f);  // fix load door offset
                this.rotation = rotation;
                this.id = id;
            }
        }
    }
}
