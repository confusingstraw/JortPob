using System.Collections.Generic;
using System.Numerics;
using JortPob.Common;
using static JortPob.Layout;

namespace JortPob
{
    // IMSBCompilableGroup is implemented by InteriorGroup and Tiles to improve code reuse when generating MSBs.
    public interface IMSBCompilableGroup
    {
        int GetMap();
        Int2 GetCoordinates();
        int GetBlock();
        int[] IdList();

        bool IsEmpty();
        bool IsInterior();

        List<IMSBCompilableChunk> GetChunks();  // Tiles will return a List containing itself only.
    }

    // IMSBCompilableChunk is implemented by Chunks within an InteriorGroup.
    // It is also implemented by Tiles, though they don't contain chunks so each Tile is treated as a single chunk.
    public interface IMSBCompilableChunk
    {
        Vector3 GetRoot();
        Vector3 GetBounds();
        List<Cell> GetCells();  // Each Chunk in an InteriorGroup will only return 1 cell.

        bool IsInterior();

        List<AssetContent> GetAssets();
        List<DoorContent> GetDoors();
        List<LightContent> GetLights();
        List<EmitterContent> GetEmitters();
        List<CreatureContent> GetCreatures();
        List<NpcContent> GetNPCs();
        List<ContainerContent> GetContainers();
        List<PickableContent> GetPickables();
        List<ItemContent> GetItems();
        List<WarpDestination> GetWarps(); // end points for load doors in other cells. also used by travel npcs
        List<ScriptedPosition> GetPositions(); // used by scripts to target locations EX: 'PositionCell'
        List<TravelPoint> GetTravelPoints(); // positions directly referenced in AiPackages
        List<PathGridPoint> GetPaths();
        List<MapPoint> GetMapPoints();  // Each Chunk in an InteriorGroup will only return 1 MapPoint.
    }
}