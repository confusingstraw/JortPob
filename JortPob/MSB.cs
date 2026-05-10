using System.Collections.Generic;
using System.Numerics;
using JortPob.Common;

namespace JortPob
{
    // IMSBCompilableGroup is implemented by InteriorGroup and Tiles to improve code reuse when generating MSBs.
    public interface IMSBCompilableGroup
    {
        int Map { get; }
        Int2 Coordinates { get; }
        int Block { get; }
        int[] IdList();

        bool IsEmpty();
        bool IsInterior { get; }

        List<IMSBCompilableChunk> Chunks { get; }  // Tiles will return a List containing itself only.
    }

    // IMSBCompilableChunk is implemented by Chunks within an InteriorGroup.
    // It is also implemented by Tiles, though they don't contain chunks so each Tile is treated as a single chunk.
    public interface IMSBCompilableChunk
    {
        Vector3 Root { get; }
        Vector3 Bounds { get; }
        List<Cell> Cells { get; }  // Each Chunk in an InteriorGroup will only return 1 cell.

        bool IsInterior { get; }

        List<AssetContent> Assets { get; }
        List<DoorContent> Doors { get; }
        List<LightContent> Lights { get; }
        List<EmitterContent> Emitters { get; }
        List<CreatureContent> Creatures { get; }
        List<NpcContent> NPCs { get; }
        List<ContainerContent> Containers { get; }
        List<PickableContent> Pickables { get; }
        List<ItemContent> Items { get; }
        List<Layout.WarpDestination> Warps { get; } // end points for load doors in other cells. also used by travel npcs
        List<Layout.ScriptedPosition> Positions { get; } // used by scripts to target locations EX: 'PositionCell'
        List<Layout.TravelPoint> TravelPoints { get; } // positions directly referenced in AiPackages
        List<Layout.PathGridPoint> Paths { get; }
        List<Layout.MapPoint> MapPoints { get; }  // Each Chunk in an InteriorGroup will only return 1 MapPoint.
    }
}