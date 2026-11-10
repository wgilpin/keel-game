using System;

namespace Keel.Sim.Grid;

public static class StationBlueprint
{
    /// <summary>
    /// Seeds the default prebuilt orbital station:
    /// - Core at (0, 0, 0)
    /// - Longitudinal spine of ArmorBlock cells along Z (-6 to +2)
    /// - Transverse radiator spars along X (-4 to +4) at Z = 0
    /// - Secondary spars along X (-2 to +2) at Z = -4
    /// - Vertical stabilizer tower along Y (-2 to +2) at Z = -2
    /// - Amber-trimmed Refinery block at (0, 0, 3) facing the origin/ship approach (+Z)
    /// Blocks are placed growing outward from the 6-connected core and spine.
    /// </summary>
    public static BlockGrid CreateDefault()
    {
        var grid = new BlockGrid();

        // 1. Core anchor
        grid.TryPlaceBlock(new Int3(0, 0, 0), BlockType.Core, out _);

        // 2. Main longitudinal spine along Z from Z = 1 to 2 and Z = -1 to -6
        for (int z = 1; z <= 2; z++)
        {
            grid.TryPlaceBlock(new Int3(0, 0, z), BlockType.ArmorBlock, out _);
        }
        for (int z = -1; z >= -6; z--)
        {
            grid.TryPlaceBlock(new Int3(0, 0, z), BlockType.ArmorBlock, out _);
        }

        // 3. Port / starboard radiator spars along X at Z = 0 (growing outward from spine)
        for (int x = 1; x <= 4; x++)
        {
            grid.TryPlaceBlock(new Int3(x, 0, 0), BlockType.ArmorBlock, out _);
            grid.TryPlaceBlock(new Int3(-x, 0, 0), BlockType.ArmorBlock, out _);
        }

        // 4. Secondary spars at Z = -4 (growing outward from spine)
        for (int x = 1; x <= 2; x++)
        {
            grid.TryPlaceBlock(new Int3(x, 0, -4), BlockType.ArmorBlock, out _);
            grid.TryPlaceBlock(new Int3(-x, 0, -4), BlockType.ArmorBlock, out _);
        }

        // 5. Vertical tower along Y at Z = -2 (growing outward from spine)
        for (int y = 1; y <= 2; y++)
        {
            grid.TryPlaceBlock(new Int3(0, y, -2), BlockType.ArmorBlock, out _);
            grid.TryPlaceBlock(new Int3(0, -y, -2), BlockType.ArmorBlock, out _);
        }

        // 6. Docking / processing deck collar at Z = +2
        grid.TryPlaceBlock(new Int3(1, 0, 2), BlockType.ArmorBlock, out _);
        grid.TryPlaceBlock(new Int3(-1, 0, 2), BlockType.ArmorBlock, out _);
        grid.TryPlaceBlock(new Int3(0, 1, 2), BlockType.ArmorBlock, out _);
        grid.TryPlaceBlock(new Int3(0, -1, 2), BlockType.ArmorBlock, out _);

        // 7. Refinery block at front (+Z) face: (0, 0, 3)
        // With +Z face completely open and exposed toward origin / approaching ships
        grid.TryPlaceBlock(new Int3(0, 0, 3), BlockType.Refinery, out _);

        return grid;
    }
}
