using System.Numerics;
using Keel.Sim.Grid;
using Keel.Sim.Inventory;
using Xunit;

namespace Keel.Sim.Tests.Grid;

public sealed class BlockPlacementTests
{
    [Fact]
    public void Evaluate_QuantizesSignedFaceNormalsToAdjacentCandidateCellWithinBuildRange()
    {
        var grid = new BlockGrid();
        grid.TryPlaceBlock(new Int3(0, 0, 0), BlockType.Core, out _);

        const float half = BlockGrid.CellSize * 0.5f; // 1.25m
        const float range = 7.5f;

        // Test all 6 cardinal face normal directions with slight tilt/jitter
        var testCases = new (Vector3 hitOffset, Vector3 normal, Int3 expectedCandidate)[]
        {
            // +X face
            (new Vector3(half + 1e-4f, 0f, 0f), new Vector3(0.98f, 0.1f, 0.05f), new Int3(1, 0, 0)),
            // -X face
            (new Vector3(-half - 1e-4f, 0f, 0f), new Vector3(-0.95f, 0.05f, -0.1f), new Int3(-1, 0, 0)),
            // +Y face
            (new Vector3(0f, half + 1e-4f, 0f), new Vector3(0.02f, 0.99f, -0.05f), new Int3(0, 1, 0)),
            // -Y face
            (new Vector3(0f, -half - 1e-4f, 0f), new Vector3(-0.05f, -0.98f, 0.02f), new Int3(0, -1, 0)),
            // +Z face
            (new Vector3(0f, 0f, half + 1e-4f), new Vector3(0.04f, 0.02f, 0.99f), new Int3(0, 0, 1)),
            // -Z face
            (new Vector3(0f, 0f, -half - 1e-4f), new Vector3(-0.02f, 0.05f, -0.99f), new Int3(0, 0, -1)),
        };

        foreach (var (hitOffset, normal, expectedCandidate) in testCases)
        {
            var eval = BlockPlacementSim.Evaluate(
                grid: grid,
                isFloatingInSuit: true,
                activeSlot: 2,
                steelBlockCount: 5,
                hasStationHit: true,
                localHitPosition: hitOffset,
                localHitNormal: normal,
                hitDistance: 4.0f,
                buildRange: range,
                isModalOpen: false);

            Assert.Equal(PlacementStatus.Valid, eval.Status);
            Assert.True(eval.IsValid);
            Assert.True(eval.ShowGhost);
            Assert.Equal(new Int3(0, 0, 0), eval.StruckCell);
            Assert.Equal(expectedCandidate, eval.CandidateCell);
            Assert.Equal(BlockType.Core, eval.StruckBlockType);
        }

        // Gating tests:
        // 1. Inactive when not in suit mode
        var notInSuit = BlockPlacementSim.Evaluate(
            grid, isFloatingInSuit: false, activeSlot: 2, steelBlockCount: 5,
            hasStationHit: true, localHitPosition: new Vector3(half, 0, 0), localHitNormal: Vector3.UnitX,
            hitDistance: 4.0f, buildRange: range, isModalOpen: false);
        Assert.Equal(PlacementStatus.Inactive, notInSuit.Status);
        Assert.False(notInSuit.ShowGhost);

        // 2. Inactive when wrong slot (slot 1)
        var wrongSlot = BlockPlacementSim.Evaluate(
            grid, isFloatingInSuit: true, activeSlot: 1, steelBlockCount: 5,
            hasStationHit: true, localHitPosition: new Vector3(half, 0, 0), localHitNormal: Vector3.UnitX,
            hitDistance: 4.0f, buildRange: range, isModalOpen: false);
        Assert.Equal(PlacementStatus.Inactive, wrongSlot.Status);
        Assert.False(wrongSlot.ShowGhost);

        // 3. Inactive when modal open
        var modalOpen = BlockPlacementSim.Evaluate(
            grid, isFloatingInSuit: true, activeSlot: 2, steelBlockCount: 5,
            hasStationHit: true, localHitPosition: new Vector3(half, 0, 0), localHitNormal: Vector3.UnitX,
            hitDistance: 4.0f, buildRange: range, isModalOpen: true);
        Assert.Equal(PlacementStatus.Inactive, modalOpen.Status);
        Assert.False(modalOpen.ShowGhost);

        // 4. NoBlocks when steelBlockCount is 0
        var noBlocks = BlockPlacementSim.Evaluate(
            grid, isFloatingInSuit: true, activeSlot: 2, steelBlockCount: 0,
            hasStationHit: true, localHitPosition: new Vector3(half, 0, 0), localHitNormal: Vector3.UnitX,
            hitDistance: 4.0f, buildRange: range, isModalOpen: false);
        Assert.Equal(PlacementStatus.NoBlocks, noBlocks.Status);
        Assert.False(noBlocks.ShowGhost);

        // 5. OutOfRange when hit distance > buildRange
        var outOfRange = BlockPlacementSim.Evaluate(
            grid, isFloatingInSuit: true, activeSlot: 2, steelBlockCount: 5,
            hasStationHit: true, localHitPosition: new Vector3(half, 0, 0), localHitNormal: Vector3.UnitX,
            hitDistance: 8.5f, buildRange: range, isModalOpen: false);
        Assert.Equal(PlacementStatus.OutOfRange, outOfRange.Status);
        Assert.False(outOfRange.ShowGhost);

        // 6. NoTarget when no hit or zero normal
        var noHit = BlockPlacementSim.Evaluate(
            grid, isFloatingInSuit: true, activeSlot: 2, steelBlockCount: 5,
            hasStationHit: false, localHitPosition: Vector3.Zero, localHitNormal: Vector3.Zero,
            hitDistance: 10.0f, buildRange: range, isModalOpen: false);
        Assert.Equal(PlacementStatus.NoTarget, noHit.Status);
        Assert.False(noHit.ShowGhost);

        var zeroNormal = BlockPlacementSim.Evaluate(
            grid, isFloatingInSuit: true, activeSlot: 2, steelBlockCount: 5,
            hasStationHit: true, localHitPosition: new Vector3(half, 0, 0), localHitNormal: Vector3.Zero,
            hitDistance: 4.0f, buildRange: range, isModalOpen: false);
        Assert.Equal(PlacementStatus.NoTarget, zeroNormal.Status);
        Assert.False(zeroNormal.ShowGhost);
    }

    [Fact]
    public void TryPlace_AtomicallyConsumesOneSteelBlockAndChainsOntoNewlyPlacedCells()
    {
        var grid = new BlockGrid();
        grid.TryPlaceBlock(new Int3(0, 0, 0), BlockType.Core, out _);

        var inventory = new SuitInventory(50);
        inventory.Add(ItemId.SteelBlock, 3);
        const float range = 7.5f;

        // 1. Place initial ArmorBlock at +X (1, 0, 0)
        var eval1 = BlockPlacementSim.Evaluate(
            grid: grid,
            isFloatingInSuit: true,
            activeSlot: 2,
            steelBlockCount: inventory.GetCount(ItemId.SteelBlock),
            hasStationHit: true,
            localHitPosition: new Vector3(1.25f, 0f, 0f),
            localHitNormal: Vector3.UnitX,
            hitDistance: 3.5f,
            buildRange: range,
            isModalOpen: false);

        Assert.Equal(PlacementStatus.Valid, eval1.Status);
        bool placed1 = BlockPlacementSim.TryPlace(grid, inventory, in eval1, out string? reason1);
        Assert.True(placed1, reason1);
        Assert.Equal(2, inventory.GetCount(ItemId.SteelBlock));
        Assert.Equal(2, grid.Count);
        Assert.Equal(BlockType.ArmorBlock, grid.GetBlock(new Int3(1, 0, 0)));
        Assert.True(grid.IsConnected());

        // 2. Reject placing onto the same cell (already occupied)
        var evalOccupied = BlockPlacementSim.Evaluate(
            grid: grid,
            isFloatingInSuit: true,
            activeSlot: 2,
            steelBlockCount: inventory.GetCount(ItemId.SteelBlock),
            hasStationHit: true,
            localHitPosition: new Vector3(1.25f, 0f, 0f),
            localHitNormal: Vector3.UnitX,
            hitDistance: 3.5f,
            buildRange: range,
            isModalOpen: false);

        Assert.Equal(PlacementStatus.InvalidCell, evalOccupied.Status);
        Assert.False(evalOccupied.IsValid);
        bool placedOccupied = BlockPlacementSim.TryPlace(grid, inventory, in evalOccupied, out string? reasonOcc);
        Assert.False(placedOccupied);
        Assert.Equal(2, inventory.GetCount(ItemId.SteelBlock)); // zero item loss
        Assert.Equal(2, grid.Count);

        // 3. Chain second block onto the newly placed cell at (2, 0, 0)
        // Hit on the +X face of cell (1, 0, 0) -> local X = 2.5m + 1.25m = 3.75m
        var evalChain = BlockPlacementSim.Evaluate(
            grid: grid,
            isFloatingInSuit: true,
            activeSlot: 2,
            steelBlockCount: inventory.GetCount(ItemId.SteelBlock),
            hasStationHit: true,
            localHitPosition: new Vector3(3.75f, 0f, 0f),
            localHitNormal: Vector3.UnitX,
            hitDistance: 3.0f,
            buildRange: range,
            isModalOpen: false);

        Assert.Equal(PlacementStatus.Valid, evalChain.Status);
        Assert.Equal(new Int3(1, 0, 0), evalChain.StruckCell);
        Assert.Equal(new Int3(2, 0, 0), evalChain.CandidateCell);

        bool placedChain = BlockPlacementSim.TryPlace(grid, inventory, in evalChain, out string? reasonChain);
        Assert.True(placedChain, reasonChain);
        Assert.Equal(1, inventory.GetCount(ItemId.SteelBlock));
        Assert.Equal(3, grid.Count);
        Assert.Equal(BlockType.ArmorBlock, grid.GetBlock(new Int3(2, 0, 0)));
        Assert.True(grid.IsConnected());

        // 4. Place 3rd block exhausting inventory
        var eval3 = BlockPlacementSim.Evaluate(
            grid: grid,
            isFloatingInSuit: true,
            activeSlot: 2,
            steelBlockCount: inventory.GetCount(ItemId.SteelBlock),
            hasStationHit: true,
            localHitPosition: new Vector3(0f, 1.25f, 0f),
            localHitNormal: Vector3.UnitY,
            hitDistance: 4.0f,
            buildRange: range,
            isModalOpen: false);

        Assert.Equal(PlacementStatus.Valid, eval3.Status);
        bool placed3 = BlockPlacementSim.TryPlace(grid, inventory, in eval3, out _);
        Assert.True(placed3);
        Assert.Equal(0, inventory.GetCount(ItemId.SteelBlock));
        Assert.Equal(4, grid.Count);

        // 5. Subsequent placement fails due to zero inventory
        var evalEmpty = BlockPlacementSim.Evaluate(
            grid: grid,
            isFloatingInSuit: true,
            activeSlot: 2,
            steelBlockCount: inventory.GetCount(ItemId.SteelBlock),
            hasStationHit: true,
            localHitPosition: new Vector3(0f, -1.25f, 0f),
            localHitNormal: -Vector3.UnitY,
            hitDistance: 4.0f,
            buildRange: range,
            isModalOpen: false);

        Assert.Equal(PlacementStatus.NoBlocks, evalEmpty.Status);
        bool placedEmpty = BlockPlacementSim.TryPlace(grid, inventory, in evalEmpty, out _);
        Assert.False(placedEmpty);
        Assert.Equal(4, grid.Count);
    }
}
