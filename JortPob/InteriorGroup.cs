using JortPob.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using static JortPob.Layout;

namespace JortPob
{
    public class InteriorGroup : IMSBCompilableGroup
    {
        public int Map { get; init; }
        public int Area { get; init; }
        public int Unk { get; init; }
        public int Block { get; init; }

        public readonly List<Chunk> chunks;

        public Int2 Coordinates
        {
            get
            {
                return new Int2(Area, Unk);
            }
        }
        public bool IsInterior { get; } = true;
        public List<IMSBCompilableChunk> Chunks
        {
            get
            {
                return [.. chunks.Cast<IMSBCompilableChunk>()];
            }
        }

        public InteriorGroup(int m, int a, int u, int b)
        {
            /* Interior Data */
            Map = m;
            Area = a;
            Unk = u;
            Block = b;

            chunks = new();
        }

        public int[] IdList()
        {
            return [Map, Area, Unk, Block];
        }

        public bool IsEmpty()
        {
            foreach(Chunk chunk in chunks)
            {
                if (chunk.Assets.Count > 0) { return false; }
            }
            return true;
        }

        public Paramanager.WeatherData GetWeather()
        {
            return Paramanager.INTERIOR_WEATHER_DATA_LIST[1];  // @TODO: actually figure out what kind of cell this is and grab correct weather
        }

        // Fugly code <3
        /* Process an interior cell into a chunk and add it to this group */
        /* This function is awful looking but it does an important bit of math to bound and align the chunk into a grid with other chunks in this group */
        public void AddCell(ScriptManager scriptManager, Cache cache, Cell cell)
        {
            Vector3 root;
            Vector3 bounds = cell.boundsMax - cell.boundsMin;
            if (chunks.Count > 0)
            {
                float x_calc, z_calc;

                if (chunks.Count % Const.CHUNK_PARTITION_SIZE == 0) {
                    x_calc = 0;

                    z_calc = float.MinValue;
                    for (int i = Math.Max(0, chunks.Count - 1 - Const.CHUNK_PARTITION_SIZE); i < chunks.Count; i++)
                    {
                        Chunk c = chunks[i];
                        z_calc = Math.Max(z_calc, c.Root.Z + c.Bounds.Z);
                    }
                    z_calc = z_calc + bounds.Z;
                }
                else
                {
                    Chunk last = chunks[chunks.Count - 1];
                    x_calc = last.Root.X + last.Bounds.X + bounds.X;
                    z_calc = last.Root.Z;
                }
                root = new Vector3(x_calc, 0, z_calc);
            }
            else
            {
                root = new(0, 0, 0);
            }
            Chunk chunk = new(scriptManager, cache, this, cell, root);
            chunks.Add(chunk);
        }

        public void ProcessTravelPositions(ScriptManager scriptManager)
        {
            foreach(InteriorGroup.Chunk chunk in chunks)
            {
                chunk.ProcessTravelPoints(scriptManager);
            }
        }

        public class Chunk : IMSBCompilableChunk
        {
            public readonly InteriorGroup group;
            public readonly Cell cell;
            public List<Cell> Cells
            {
                get { return [cell]; }
            }

            public Vector3 Root { get; init; }
            public Vector3 Bounds { get; init; }
            public Vector3 Offset { get; init; } // size from center

            public Obj nav;  // navmesh repersentation of this msb chunk
            public List<Layout.PathGridPoint> Paths { get; init; } // mw uses these for nav. we are only using them for wander positions

            public List<AssetContent> Assets { get; init; }
            public List<DoorContent> Doors { get; init; }
            public List<LightContent> Lights { get; init; }
            public List<EmitterContent> Emitters { get; init; }
            public List<CreatureContent> Creatures { get; init; }
            public List<NpcContent> NPCs { get; init; }
            public List<ContainerContent> Containers { get; init; }
            public List<PickableContent> Pickables { get; init; }
            public List<ItemContent> Items { get; init; }

            public List<Layout.WarpDestination> Warps { get; init; } // end points for load doors in other cells. also used by travel npcs
            public List<Layout.ScriptedPosition> Positions { get; init; } // used by scripts to target locations EX: 'PositionCell'
            public List<Layout.TravelPoint> TravelPoints { get; init; } // positions directly referenced in AiPackages

            public bool IsInterior { get; } = true;

            public List<Layout.MapPoint> MapPoints
            {
                get
                {
                    // position, radius, and discovered are not used for anything here so they are 0 or null.
                    MapPoint mp = new(cell.name, new Vector3(0), 0, false, null, MapPoint.Icon.None)
                    {
                        relative = Root
                    };
                    return [mp];
                }
            }
            public Chunk(ScriptManager scriptManager, Cache cache, InteriorGroup group, Cell cell, Vector3 root)
            {
                this.group = group;
                this.cell = cell;
                this.Root = root;

                Bounds = cell.boundsMax - cell.boundsMin;
                Offset = Vector3.Lerp(cell.boundsMin, cell.boundsMax, .5f);

                nav = new();
                Paths = new();
                TravelPoints = new();

                Assets = new();
                Doors = new();
                Emitters = new();
                Lights = new();
                Creatures = new();
                NPCs = new();
                Containers = new();
                Pickables = new();
                Items = new();

                Warps = new();
                Positions = new();

                /* Add content */
                foreach (Content content in cell.contents)
                {
                    content.relative = content.position + Root - Offset;
                    AddContent(cache,content);
                }

                /* Add cells pathgrid to the tile */
                BaseScript script = scriptManager.GetScript(group);
                for (int i=0;i<cell.paths.Count;i++)
                {
                    Vector3 path = cell.paths[i];
                    string name = $"PathGrid_{group.Map:D2}{group.Area:D2}{group.Unk:D2}_{group.chunks.Count:D2}_{i:D4}";
                    Vector3 relative = path + Root - Offset;
                    Layout.PathGridPoint point = new(name, relative, script.CreateEntity(Script.EntityType.Region, $"PathGridPoint"));
                    Paths.Add(point);
                }
            }

            public IEnumerable<Content> GetAllContent()
            {
                IEnumerable<IEnumerable<Content>> all = [
                    Assets,
                    Doors,
                    Emitters,
                    Lights,
                    Creatures,
                    NPCs,
                    Containers,
                    Pickables,
                    Items,
                ];

                foreach (IEnumerable<Content> enumerable in all)
                {
                    foreach (Content content in enumerable)
                    {
                        yield return content;
                    }
                }
            }

            public void AddWarp(DoorContent.Warp warp)
            {
                Layout.WarpDestination dest = new(warp.position + Root - Offset, warp.rotation, warp.entity);
                Warps.Add(dest);
            }

            public void AddScriptedPosition(BaseScript script, Vector3 position, float rot)
            {
                Vector3 relative = position + Root - Offset;
                Vector3 rotation = new Vector3(0, rot, 0); // @TODO: THIS IS WRONG!
                uint region = script.CreateEntity(Script.EntityType.Region, $"ScriptedPosition:Region:{position.ToString()}");
                uint player = script.CreateEntity(Script.EntityType.Region, $"ScriptedPosition:Player:{position.ToString()}");
                Positions.Add(new(position, relative, rotation, region, player, group.Map, group.Area, group.Unk, group.Block));
            }

            public void AddNav(Cache cache, Content content)
            {
                if (Const.DEBUG_SKIP_NAVMESH) { return; }
                switch (content)
                {
                    case AssetContent a:
                        ModelInfo modelInfo = cache.GetModel(content.mesh, content.scale);
                        if (!modelInfo.HasCollision()) { break; } // no collision means no nav info
                        nav.add(new Obj(Path.Combine(Const.CACHE_PATH, modelInfo.collision.obj)), content.relative, content.rotation, modelInfo.UseScale() ? (content.scale * 0.01f) : 1f);
                        break;
                    default: break;
                }
            }

            public void AddContent(Cache cache, Content content)
            {
                switch (content)
                {
                    case AssetContent a:
                        Assets.Add(a); break;
                    case DoorContent d:
                        Doors.Add(d); break;
                    case EmitterContent e:
                        Emitters.Add(e); break;
                    case LightContent l:
                        Lights.Add(l); break;
                    case ContainerContent o:
                        Containers.Add(o); break;
                    case ItemContent i:
                        Items.Add(i); break;
                    case PickableContent p:
                        Pickables.Add(p); break;
                    case NpcContent n:
                        NPCs.Add(n); break;
                    case CreatureContent c:
                        Creatures.Add(c); break;
                    default:
                        Lort.Log($" ## WARNING ## Unhandled Content class '{content.type}::{content.id}' fell through AddContent()", Lort.Type.Debug); break;
                }

                AddNav(cache, content);
            }

            /* Add travelpoint */
            public void AddTravelPoint(BaseScript script, Vector3 point, float radius = -1f)
            {
                Vector3 relative = point + Root - Offset;
                uint region = script.CreateEntity(Script.EntityType.Region, $"Travel:Region:{point}");
                TravelPoint travel = new($"Travel_{group.Map:D2}{group.Area:D2}{group.Unk:D2}_{SafeName()}_{TravelPoints.Count:D4}", point, relative, radius == -1f ? Const.PATH_REGION_SIZE : radius, region);
                TravelPoints.Add(travel);
            }

            /* Converts all "Travel" positions to scriptedpositions */
            public void ProcessTravelPoints(ScriptManager scriptManager)
            {
                BaseScript script = scriptManager.GetScript(group);
                void HandleCharacterContent(CharacterContent content)
                {
                    foreach (CharacterContent.AiPackage package in content.packages)
                    {
                        if (package.type == CharacterContent.AiPackage.Type.Travel)
                        {
                            Vector3 relative = package.position + Root - Offset;
                            uint region = script.CreateEntity(Script.EntityType.Region, $"Travel:Region:{package.position}");
                            TravelPoint travel = new($"Travel_{group.Map:D2}{group.Area:D2}{group.Unk:D2}_{SafeName()}_{TravelPoints.Count:D4}", package.position, relative, Const.PATH_REGION_SIZE, region);
                            TravelPoints.Add(travel);
                        }
                    }
                }

                foreach (NpcContent c in NPCs) { HandleCharacterContent(c); }
                foreach (CreatureContent c in Creatures) { HandleCharacterContent(c); }
            }

            private string SafeName()
            {
                return cell.name.Replace(" ", "").Replace(",", "").Replace("-", "").ToLower().Trim();
            }
        }
    }
}
