using JortPob.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using static JortPob.Layout;

namespace JortPob
{
    /* A Tile is what we call a single square on the Elden Ring cell grid. It's basically the Elden Ring version of a "cell" */
    [DebuggerDisplay("Tile m{map}_{coordinate.x}_{coordinate.y}_{block} :: [{cells.Count}] Cells")]
    public class Tile : BaseTile
    {
        public HugeTile huge;
        public BigTile big;

        public Obj nav;  // navmesh repersentation of this msb tile
        public override List<Layout.PathGridPoint> Paths { get; } // mw uses these for nav. we are only using them for wander positions
        public override List<Layout.TravelPoint> TravelPoints { get; } // positions directly referenced in AiPackages

        public Tile(int m, int x, int y, int b) : base(m, x, y, b)
        {
            nav = new();
            Paths = new();
            TravelPoints = new();
        }

        /* Checks ABSOLUTE POSITION! This is the position of an object from the ESM accounting for the layout offset! */
        public bool PositionInside(Vector3 position)
        {
            Vector3 pos = position + Const.LAYOUT_COORDINATE_OFFSET;

            float x1 = (Coordinates.x * Const.TILE_SIZE) - (Const.TILE_SIZE * 0.5f);
            float y1 = (Coordinates.y * Const.TILE_SIZE) - (Const.TILE_SIZE * 0.5f);
            float x2 = x1 + Const.TILE_SIZE;
            float y2 = y1 + Const.TILE_SIZE;

            if(pos.X >= x1 && pos.X < x2 && pos.Z >= y1 && pos.Z < y2)
            {
                return true;
            }

            return false;
        }

        /* Returns averaged region of this tile. Each cell has a region set so the best we can do is see what region is most common among cells in this tile and return that */
        public string GetRegion()
        {
            Dictionary<string, int> regions = new();
            foreach(Cell cell in Cells)
            {
                if (cell.region == null) { continue; }
                string r = cell.region.Trim().ToLower();
                if (regions.ContainsKey(r)) { regions[r]++; }
                else { regions.Add(r, 1); }
            }

            if (regions.Count <= 0) { return "Default Region"; } // no regions set so guh

            string most = regions.Keys.First();
            foreach(KeyValuePair<string, int> kvp in regions)
            {
                if (regions[most] < kvp.Value)
                {
                    most = kvp.Key;
                }
            }

            /* Red Mountain has priority for skybox */
            string redMountain = "red mountain region"; // Case sensitive
            if (regions.ContainsKey(redMountain))
            {
                if (regions[redMountain] >= 3) { most = redMountain; }
            }

            return most;
        }

        public override void AddCell(ScriptManager scriptManager, Cell cell)
        {
            Cells.Add(cell);

            /* Add cells pathgrid to the tile */
            BaseScript script = scriptManager.GetScript(this);
            for (int i = 0; i < cell.paths.Count; i++)
            {
                Vector3 path = cell.paths[i];
                string name = $"PathGrid_{Map:D2}{Coordinates.x:D2}{Coordinates.y:D2}_{cell.coordinate.x:D2}{cell.coordinate.y:D2}_{i:D4}";
                float x = (Coordinates.x * Const.TILE_SIZE);
                float y = (Coordinates.y * Const.TILE_SIZE);
                Vector3 relative = (path + Const.LAYOUT_COORDINATE_OFFSET) - new Vector3(x, 0, y);
                Layout.PathGridPoint point = new(name,relative, script.CreateEntity(Script.EntityType.Region, $"PathGridPoint"));
                Paths.Add(point);
            }
        }

        public void AddTerrain(Vector3 position, TerrainInfo terrainInfo)
        {
            float x = (Coordinates.x * Const.TILE_SIZE);
            float y = (Coordinates.y * Const.TILE_SIZE);
            Vector3 relative = (position + Const.LAYOUT_COORDINATE_OFFSET) - new Vector3(x, 0, y);
            Terrain.Add(new Tuple<Vector3, TerrainInfo>(relative, terrainInfo));
        }

        public void FinalizeTerrainNav()
        {
            Obj all = new();
            foreach((Vector3 v, TerrainInfo t) in Terrain)
            {
                Obj obj = new Obj(Path.Combine(Const.CACHE_PATH, t.obj));
                all.add(obj, v, Vector3.Zero, 1f);
            }
            all.collapse(Obj.CollisionMaterial.Stock);
            all.optimize();
            all.borderize(1.25f);
            nav.add(all, Vector3.Zero, Vector3.Zero, 1f);
        }

        public override void AddContent(Cache cache, Cell cell, Content content, bool forceFallThrough = false)
        {
            float x = (Coordinates.x * Const.TILE_SIZE);
            float y = (Coordinates.y * Const.TILE_SIZE);
            content.relative = (content.position + Const.LAYOUT_COORDINATE_OFFSET) - new Vector3(x, 0, y);

            AddNav(cache, cell, content);

            base.AddContent(cache, cell, content);
        }

        public void AddNav(Cache cache, Cell cell, Content content)
        {
            if (Const.DEBUG_SKIP_NAVMESH) { return; }

            /* Recalcualte relative for this tile because this content may be coming from a BigTile or HugeTile and the content.relative will not be valid in those cases */
            float x = (Coordinates.x * Const.TILE_SIZE);
            float y = (Coordinates.y * Const.TILE_SIZE);
            Vector3 relative = (content.position + Const.LAYOUT_COORDINATE_OFFSET) - new Vector3(x, 0, y);

            switch (content)
            {
                case AssetContent a:
                    ModelInfo modelInfo = cache.GetModel(content.mesh, content.scale);
                    if (!modelInfo.HasCollision()) { break; } // no collision means no nav info
                    nav.add(new Obj(Path.Combine(Const.CACHE_PATH, modelInfo.collision.obj)), relative, content.rotation, modelInfo.UseScale() ? (content.scale * 0.01f) : 1f);
                    break;
                default: break;
            }
        }

        public void AddWarp(DoorContent.Warp warp)
        {
            float x = (Coordinates.x * Const.TILE_SIZE);
            float y = (Coordinates.y * Const.TILE_SIZE);

            Layout.WarpDestination dest = new((warp.position + Const.LAYOUT_COORDINATE_OFFSET) - new Vector3(x, 0, y), warp.rotation, warp.entity);
            Warps.Add(dest);
        }

        public void AddMapPoint(Layout.MapPoint point)
        {
            float x = (Coordinates.x * Const.TILE_SIZE);
            float y = (Coordinates.y * Const.TILE_SIZE);

            point.relative = (point.position + Const.LAYOUT_COORDINATE_OFFSET) - new Vector3(x, 0, y);
            MapPoints.Add(point);
        }

        public void AddScriptedPosition(BaseScript script, Vector3 position, float rot)
        {
            float x = (Coordinates.x * Const.TILE_SIZE);
            float y = (Coordinates.y * Const.TILE_SIZE);

            Vector3 relative = (position + Const.LAYOUT_COORDINATE_OFFSET) - new Vector3(x, 0, y);
            Vector3 rotation = new Vector3(0, rot, 0); // @TODO: THIS IS WRONG!
            uint region = script.CreateEntity(Script.EntityType.Region, $"ScriptedPosition:Region:{position}");
            uint player = script.CreateEntity(Script.EntityType.Region, $"ScriptedPosition:Player:{position}");
            Positions.Add(new(position, relative, rotation, region, player, Map, Coordinates.x, Coordinates.y, Block));
        }
        
        /* Add travelpoint */
        public void AddTravelPoint(BaseScript script, Vector3 point, float radius = -1f)
        {
            float x = (Coordinates.x * Const.TILE_SIZE);
            float y = (Coordinates.y * Const.TILE_SIZE);
            Vector3 relative = (point + Const.LAYOUT_COORDINATE_OFFSET) - new Vector3(x, 0, y);
            uint region = script.CreateEntity(Script.EntityType.Region, $"Travel:Region:{point}");
            TravelPoint travel = new($"Travel_{Map:D2}{Coordinates.x:D2}{Coordinates.y:D2}_{TravelPoints.Count:D4}", point, relative, radius == -1f ? Const.PATH_REGION_SIZE : radius, region);
            TravelPoints.Add(travel);
        }

        /* Converts all "Travel" positions to travelpoints */
        public void ProcessTravelPoints(ScriptManager scriptManager)
        {
            BaseScript script = scriptManager.GetScript(this);
            void HandleCharacterContent(CharacterContent content)
            {
                foreach (CharacterContent.AiPackage package in content.packages)
                {
                    if (package.type == CharacterContent.AiPackage.Type.Travel)
                    {
                        float x = (Coordinates.x * Const.TILE_SIZE);
                        float y = (Coordinates.y * Const.TILE_SIZE);
                        Vector3 relative = (package.position + Const.LAYOUT_COORDINATE_OFFSET) - new Vector3(x, 0, y);
                        uint region = script.CreateEntity(Script.EntityType.Region, $"Travel:Region:{package.position}");
                        TravelPoint travel = new($"Travel_{Map:D2}{Coordinates.x:D2}{Coordinates.y:D2}_{TravelPoints.Count:D4}", package.position, relative, Const.PATH_REGION_SIZE, region);
                        TravelPoints.Add(travel);
                    }
                }
            }

            foreach (NpcContent c in NPCs) { HandleCharacterContent(c); }
            foreach (CreatureContent c in Creatures) { HandleCharacterContent(c); }
        }
    }



    public abstract class BaseTile : IMSBCompilableGroup, IMSBCompilableChunk
    {
        public int Map { get; init; }
        public Int2 Coordinates { get; init; }
        public int Block { get; init; }

        public List<Cell> Cells { get; init; }

        public List<Tuple<Vector3, TerrainInfo>> Terrain { get; init; }
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
        public List<Layout.MapPoint> MapPoints { get; init; }
        public List<Layout.ScriptedPosition> Positions { get; init; } // used by scripts to target locations EX: 'PositionCell'

        public bool IsInterior { get; } = false;
        public Vector3 Root
        {
            get { return new Vector3(0); }  // not used but needed to satisfy interface
        }
        public Vector3 Bounds
        {
            get { return new Vector3(0); }  // not used but needed to satisfy interface
        }
        public List<IMSBCompilableChunk> Chunks
        {
            get { return [this]; }
        }
        public virtual List<Layout.TravelPoint> TravelPoints
        {
            get { return []; }
        }
        public virtual List<Layout.PathGridPoint> Paths
        {
            get { return []; }
        }

        public BaseTile(int m, int x, int y, int b)
        {
            /* Tile Data */
            Map = m;
            Coordinates = new(x, y);
            Block = b;

            /* Tile Content Data */
            Cells = new();
            Terrain = new();
            Assets = new();
            Doors = new();
            Emitters = new();
            Lights = new();
            Creatures = new();
            NPCs = new();
            Containers = new();
            Pickables = new();
            Items = new();

            Positions = new();
            MapPoints = new();
            Warps = new();
        }

        public int[] IdList()
        {
            return [Map, Coordinates.x, Coordinates.y, Block];
        }

        public bool IsEmpty()
        {
            return Cells.Count <= 0 && Terrain.Count <= 0 && Assets.Count <= 0;
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

        public abstract void AddCell(ScriptManager scriptManager, Cell cell);

        /* Incoming content is in aboslute worldspace from the ESM, when adding content to a tile we convert it's coordiantes to relative space */
        public virtual void AddContent(Cache cache, Cell cell, Content content, bool forceFallThrough = false)
        {
            switch(content)
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
                case PickableContent p:
                    Pickables.Add(p); break;
                case ItemContent i:
                    Items.Add(i); break;
                case NpcContent n:
                    NPCs.Add(n); break;
                case CreatureContent c:
                    Creatures.Add(c); break;
                default:
                    Lort.Log($" ## WARNING ## Unhandled Content class '{content.type}::{content.id}' fell through AddContent()", Lort.Type.Debug); break;
            }
        }
    }
}
