using System.Numerics;
using Keel.Sim.Grid;
using Xunit;

namespace Keel.Sim.Tests.Grid;

public class BlockGridTests
{
    [Fact]
    public void TryPlaceBlock_EnforcesUnoccupiedAndSixFaceAdjacencyRules()
    {
        var grid = new BlockGrid();

        // 1. Initial block on empty grid acts as anchor
        bool placedCore = grid.TryPlaceBlock(new Int3(0, 0, 0), BlockType.Core, out string? reason);
        Assert.True(placedCore, reason);
        Assert.Equal(BlockType.Core, grid.GetBlock(new Int3(0, 0, 0)));

        // 2. Reject placing at already occupied cell
        bool replaceSame = grid.TryPlaceBlock(new Int3(0, 0, 0), BlockType.ArmorBlock, out string? occupiedReason);
        Assert.False(replaceSame);
        Assert.Contains("occupied", occupiedReason, System.StringComparison.OrdinalIgnoreCase);

        // 3. Reject None block type
        bool placeNone = grid.TryPlaceBlock(new Int3(1, 0, 0), BlockType.None, out string? noneReason);
        Assert.False(placeNone);

        // 4. Reject diagonal / disconnected cell (e.g. (1, 1, 0) without (1, 0, 0) or (0, 1, 0))
        bool placeDiagonal = grid.TryPlaceBlock(new Int3(1, 1, 0), BlockType.ArmorBlock, out string? diagReason);
        Assert.False(placeDiagonal);
        Assert.Contains("adjacent", diagReason, System.StringComparison.OrdinalIgnoreCase);

        // 5. Accept 6 cardinal neighbors
        var cardinalDirs = new[]
        {
            new Int3(1, 0, 0),
            new Int3(-1, 0, 0),
            new Int3(0, 1, 0),
            new Int3(0, -1, 0),
            new Int3(0, 0, 1),
            new Int3(0, 0, -1),
        };

        foreach (var dir in cardinalDirs)
        {
            bool placed = grid.TryPlaceBlock(dir, BlockType.ArmorBlock, out string? cardReason);
            Assert.True(placed, $"Failed placing at cardinal neighbor {dir}: {cardReason}");
        }

        Assert.Equal(7, grid.Count);
        Assert.True(grid.IsConnected());
    }

    [Fact]
    public void StationBlueprint_SeedsConnectedGridWithCoreArmorAndExposedRefinery()
    {
        BlockGrid station = StationBlueprint.CreateDefault();

        Assert.True(station.Count >= 20, $"Station should have at least 20 blocks, had {station.Count}");
        Assert.True(station.IsConnected(), "Station blocks must form a contiguous 6-connected grid.");

        // Must have Core
        Assert.Equal(BlockType.Core, station.GetBlock(new Int3(0, 0, 0)));

        // Must have at least one Refinery block
        bool foundRefinery = false;
        Int3 refineryPos = default;
        foreach (var kvp in station.Blocks)
        {
            if (kvp.Value == BlockType.Refinery)
            {
                foundRefinery = true;
                refineryPos = kvp.Key;
                break;
            }
        }

        Assert.True(foundRefinery, "Station must include a Refinery block.");

        // Refinery must have at least one exposed face (e.g. +Z) with unoccupied space
        var frontCell = new Int3(refineryPos.X, refineryPos.Y, refineryPos.Z + 1);
        Assert.False(station.HasBlock(frontCell), "Front +Z face of Refinery must be exposed and unoccupied.");
    }

    [Fact]
    public void ResolveHitCell_MapsExactFaceHitsAcrossSignedCoordinatesWithoutSeamFlip()
    {
        var grid = new BlockGrid();
        grid.TryPlaceBlock(new Int3(0, 0, 0), BlockType.Core, out _);
        grid.TryPlaceBlock(new Int3(0, 0, 1), BlockType.Refinery, out _);
        grid.TryPlaceBlock(new Int3(-1, 0, 0), BlockType.ArmorBlock, out _);

        const float half = BlockGrid.CellSize * 0.5f; // 1.25m

        // Test 1: Hit on +Z face of Refinery (local Z = 1 * 2.5 + 1.25 = 3.75), normal = (0, 0, 1)
        // With small positive jitter (+1e-5) outside the face
        Vector3 hitRefinery = new(0f, 0f, (1 * BlockGrid.CellSize) + half + 1e-5f);
        Vector3 normalZ = new(0f, 0f, 1f);

        bool resolvedRef = grid.ResolveHitCell(hitRefinery, normalZ, out Int3 cellRef, out BlockType typeRef);
        Assert.True(resolvedRef);
        Assert.Equal(new Int3(0, 0, 1), cellRef);
        Assert.Equal(BlockType.Refinery, typeRef);

        // Test 2: Hit on -X face of negative ArmorBlock (local X = -1 * 2.5 - 1.25 = -3.75), normal = (-1, 0, 0)
        // With jitter outside face (-1e-5)
        Vector3 hitArmor = new((-1 * BlockGrid.CellSize) - half - 1e-5f, 0f, 0f);
        Vector3 normalNegX = new(-1f, 0f, 0f);

        bool resolvedArmor = grid.ResolveHitCell(hitArmor, normalNegX, out Int3 cellArmor, out BlockType typeArmor);
        Assert.True(resolvedArmor);
        Assert.Equal(new Int3(-1, 0, 0), cellArmor);
        Assert.Equal(BlockType.ArmorBlock, typeArmor);

        // Test 3: Hit in empty space far from any block returns false
        Vector3 emptySpaceHit = new(50f, 50f, 50f);
        bool resolvedEmpty = grid.ResolveHitCell(emptySpaceHit, normalZ, out _, out _);
        Assert.False(resolvedEmpty);
    }
}
