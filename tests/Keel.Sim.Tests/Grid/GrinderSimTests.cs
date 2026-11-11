using Keel.Sim.Config;
using Keel.Sim.Grid;
using Keel.Sim.Inventory;
using Xunit;

namespace Keel.Sim.Tests.Grid;

public sealed class GrinderSimTests
{
    [Fact]
    public void Evaluate_ValidatesHitDistanceAndSlotGating()
    {
        var grid = StationBlueprint.CreateDefault();
        var inventory = new SuitInventory(50);
        var targetCell = new Int3(0, 0, 1); // ArmorBlock
        var targetType = grid.GetBlock(targetCell);

        float progress = 0f;
        Int3? lastTarget = null;

        // Wrong slot (e.g. 1 Drill) -> Idle, no beam
        var wrongSlot = GrinderSim.Step(
            grid, inventory, targetCell, targetType, isFiring: true, activeSlot: 1,
            hitDistance: 3.0f, grinderRange: 5.0f, grinderRate: 1.0f, deltaSeconds: 0.1f,
            ref progress, ref lastTarget);

        Assert.Equal(GrinderStatus.Idle, wrongSlot.Status);
        Assert.False(wrongSlot.BeamActive);
        Assert.False(wrongSlot.BlockDeconstructed);

        // Out of range (6.0m > 5.0m) on slot 3 -> OutOfRange
        var outOfRange = GrinderSim.Step(
            grid, inventory, targetCell, targetType, isFiring: true, activeSlot: 3,
            hitDistance: 6.0f, grinderRange: 5.0f, grinderRate: 1.0f, deltaSeconds: 0.1f,
            ref progress, ref lastTarget);

        Assert.Equal(GrinderStatus.OutOfRange, outOfRange.Status);
        Assert.False(outOfRange.BeamActive);

        // No target cell -> NoTarget
        var noTarget = GrinderSim.Step(
            grid, inventory, null, BlockType.None, isFiring: true, activeSlot: 3,
            hitDistance: 3.0f, grinderRange: 5.0f, grinderRate: 1.0f, deltaSeconds: 0.1f,
            ref progress, ref lastTarget);

        Assert.Equal(GrinderStatus.NoTarget, noTarget.Status);
    }

    [Fact]
    public void Step_AccumulatesProgressAndAtomicallyRefundsSteelBlockOnCompletion()
    {
        var grid = StationBlueprint.CreateDefault();
        var inventory = new SuitInventory(50);
        var targetCell = new Int3(1, 0, 2); // ArmorBlock on collar (leaf block)
        var targetType = grid.GetBlock(targetCell);
        int initialGridCount = grid.Count;
        int initialSteelBlocks = inventory.GetCount(ItemId.SteelBlock);

        float progress = 0f;
        Int3? lastTarget = null;

        // Step 1: 0.4s @ 1.0/s -> 0.4 progress, deconstructing, beam active
        var step1 = GrinderSim.Step(
            grid, inventory, targetCell, targetType, isFiring: true, activeSlot: 3,
            hitDistance: 3.0f, grinderRange: 5.0f, grinderRate: 1.0f, deltaSeconds: 0.4f,
            ref progress, ref lastTarget);

        Assert.Equal(GrinderStatus.Deconstructing, step1.Status);
        Assert.Equal(0.4f, step1.Progress, precision: 3);
        Assert.True(step1.BeamActive);
        Assert.False(step1.BlockDeconstructed);
        Assert.Equal(initialGridCount, grid.Count);
        Assert.Equal(initialSteelBlocks, inventory.GetCount(ItemId.SteelBlock));

        // Step 2: 0.7s @ 1.0/s -> 0.4 + 0.7 = 1.1 >= 1.0 -> Block deconstructed, refunded 1 SteelBlock
        var step2 = GrinderSim.Step(
            grid, inventory, targetCell, targetType, isFiring: true, activeSlot: 3,
            hitDistance: 3.0f, grinderRange: 5.0f, grinderRate: 1.0f, deltaSeconds: 0.7f,
            ref progress, ref lastTarget);

        Assert.Equal(GrinderStatus.Idle, step2.Status);
        Assert.True(step2.BlockDeconstructed);
        Assert.Equal(targetCell, step2.DeconstructedCell);
        Assert.Equal(ItemId.SteelBlock, step2.RefundedItem);
        Assert.Equal(1, step2.UnitsRefunded);
        Assert.Equal(0f, step2.Progress);
        Assert.False(step2.BeamActive);

        // Grid count decremented by 1, cell removed
        Assert.Equal(initialGridCount - 1, grid.Count);
        Assert.False(grid.HasBlock(targetCell));

        // Inventory received exactly 1 SteelBlock
        Assert.Equal(initialSteelBlocks + 1, inventory.GetCount(ItemId.SteelBlock));
    }

    [Fact]
    public void Step_ResetsProgressOnTargetChangeOrFireRelease()
    {
        var grid = StationBlueprint.CreateDefault();
        var inventory = new SuitInventory(50);
        var targetCellA = new Int3(1, 0, 2); // collar leaf
        var targetCellB = new Int3(-1, 0, 2); // opposite collar leaf

        float progress = 0f;
        Int3? lastTarget = null;

        // Progress 0.6 on target A
        GrinderSim.Step(
            grid, inventory, targetCellA, grid.GetBlock(targetCellA), isFiring: true, activeSlot: 3,
            hitDistance: 3.0f, grinderRange: 5.0f, grinderRate: 1.0f, deltaSeconds: 0.6f,
            ref progress, ref lastTarget);

        Assert.Equal(0.6f, progress, precision: 3);

        // Switch to target B -> progress must reset to 0
        var switchStep = GrinderSim.Step(
            grid, inventory, targetCellB, grid.GetBlock(targetCellB), isFiring: true, activeSlot: 3,
            hitDistance: 3.0f, grinderRange: 5.0f, grinderRate: 1.0f, deltaSeconds: 0.1f,
            ref progress, ref lastTarget);

        Assert.Equal(0.1f, switchStep.Progress, precision: 3);

        // Release fire -> halts progress, beam off
        var releaseStep = GrinderSim.Step(
            grid, inventory, targetCellB, grid.GetBlock(targetCellB), isFiring: false, activeSlot: 3,
            hitDistance: 3.0f, grinderRange: 5.0f, grinderRate: 1.0f, deltaSeconds: 0.1f,
            ref progress, ref lastTarget);

        Assert.Equal(GrinderStatus.Idle, releaseStep.Status);
        Assert.False(releaseStep.BeamActive);
        Assert.Equal(0.1f, releaseStep.Progress, precision: 3);
    }

    [Fact]
    public void Step_RejectsCoreCargoFullAndGridDisconnectionWithoutMutatingState()
    {
        var grid = StationBlueprint.CreateDefault();
        var inventory = new SuitInventory(50);
        var coreCell = new Int3(0, 0, 0);

        float progress = 0f;
        Int3? lastTarget = null;

        // 1. Core block protection
        var coreStep = GrinderSim.Step(
            grid, inventory, coreCell, BlockType.Core, isFiring: true, activeSlot: 3,
            hitDistance: 2.0f, grinderRange: 5.0f, grinderRate: 1.0f, deltaSeconds: 2.0f,
            ref progress, ref lastTarget);

        Assert.Equal(GrinderStatus.CannotDeconstructCore, coreStep.Status);
        Assert.False(coreStep.BlockDeconstructed);
        Assert.True(grid.HasBlock(coreCell));

        // 2. Full cargo rejection
        var fullInventory = new SuitInventory(2);
        fullInventory.Add(ItemId.IronOre, 2);
        Assert.True(fullInventory.IsFull);

        var armorCell = new Int3(0, 0, 2);
        var fullCargoStep = GrinderSim.Step(
            grid, fullInventory, armorCell, BlockType.ArmorBlock, isFiring: true, activeSlot: 3,
            hitDistance: 2.0f, grinderRange: 5.0f, grinderRate: 1.0f, deltaSeconds: 2.0f,
            ref progress, ref lastTarget);

        Assert.Equal(GrinderStatus.CargoFull, fullCargoStep.Status);
        Assert.False(fullCargoStep.BlockDeconstructed);
        Assert.True(grid.HasBlock(armorCell));

        // 3. Grid split / disconnection rejection
        // In default blueprint, cell (0, 0, 2) connects the collar to the refinery at (0, 0, 3).
        // Removing (0, 0, 2) would orphan (0, 0, 3)!
        Assert.True(grid.WouldDisconnect(new Int3(0, 0, 2)));

        var splitStep = GrinderSim.Step(
            grid, inventory, new Int3(0, 0, 2), BlockType.ArmorBlock, isFiring: true, activeSlot: 3,
            hitDistance: 2.0f, grinderRange: 5.0f, grinderRate: 1.0f, deltaSeconds: 2.0f,
            ref progress, ref lastTarget);

        Assert.Equal(GrinderStatus.WouldSplitGrid, splitStep.Status);
        Assert.False(splitStep.BlockDeconstructed);
        Assert.True(grid.HasBlock(new Int3(0, 0, 2)));
        Assert.True(grid.IsConnected());
    }

    [Fact]
    public void BlockRemoval_UpdatesResolveHitCellAndEnablesSubsequentPlacement()
    {
        var grid = StationBlueprint.CreateDefault();
        var targetCell = new Int3(1, 0, 2); // collar leaf
        var localPos = grid.GetCellCenter(targetCell) + new System.Numerics.Vector3(1.25f, 0, 0);
        var hitNormal = System.Numerics.Vector3.UnitX;

        // Before removal: raycast hit resolves to targetCell
        bool hitBefore = grid.ResolveHitCell(localPos, hitNormal, out var struckCell, out var struckType);
        Assert.True(hitBefore);
        Assert.Equal(targetCell, struckCell);
        Assert.Equal(BlockType.ArmorBlock, struckType);

        // Remove block
        bool removed = grid.RemoveBlock(targetCell, out var removedType);
        Assert.True(removed);
        Assert.Equal(BlockType.ArmorBlock, removedType);

        // After removal: raycast hit does NOT resolve to targetCell (empty space)
        bool hitAfter = grid.ResolveHitCell(localPos, hitNormal, out _, out _);
        Assert.False(hitAfter);

        // Subsequent placement at targetCell succeeds because cell is now free
        bool replaced = grid.TryPlaceBlock(targetCell, BlockType.ArmorBlock, out string? reason);
        Assert.True(replaced, reason);
        Assert.True(grid.HasBlock(targetCell));
    }

    [Fact]
    public void Step_SimulatesFullDeconstructionCycleWithGridRemoval()
    {
        var grid = StationBlueprint.CreateDefault();
        var inventory = new SuitInventory(50);
        var targetCell = new Int3(1, 0, 2);
        int initialGridCount = grid.Count;
        int initialSteel = inventory.GetCount(ItemId.SteelBlock);

        float progress = 0f;
        Int3? lastTarget = null;

        // Step over 10 frames @ 0.1s delta with grinderRate = 1.0 -> 1.0s total
        GrinderStepResult lastResult = default;
        for (int frame = 0; frame < 10; frame++)
        {
            lastResult = GrinderSim.Step(
                grid, inventory, targetCell, grid.GetBlock(targetCell),
                isFiring: true, activeSlot: 3, hitDistance: 2.5f,
                grinderRange: 5.0f, grinderRate: 1.0f, deltaSeconds: 0.1f,
                ref progress, ref lastTarget);
        }

        Assert.True(lastResult.BlockDeconstructed);
        Assert.Equal(targetCell, lastResult.DeconstructedCell);
        Assert.Equal(initialGridCount - 1, grid.Count);
        Assert.Equal(initialSteel + 1, inventory.GetCount(ItemId.SteelBlock));
        Assert.False(grid.HasBlock(targetCell));
    }

    [Fact]
    public void ClosedLoop_PlaceAndDeconstructConservesSteelBlocksAndGridConnectivity()
    {
        var grid = StationBlueprint.CreateDefault();
        var inventory = new SuitInventory(50);
        inventory.Add(ItemId.SteelBlock, 5);

        int initialGridCount = grid.Count;
        int initialSteelCount = inventory.GetCount(ItemId.SteelBlock);
        Assert.Equal(5, initialSteelCount);
        Assert.True(grid.IsConnected());

        // 1. Place a block at (1, 0, 3) adjacent to collar (1, 0, 2)
        var placementCell = new Int3(1, 0, 3);
        bool placed = grid.TryPlaceBlock(placementCell, BlockType.ArmorBlock, out string? reason);
        Assert.True(placed, reason);
        int removedAmount = inventory.Remove(ItemId.SteelBlock, 1);
        Assert.Equal(1, removedAmount);

        Assert.Equal(initialGridCount + 1, grid.Count);
        Assert.Equal(initialSteelCount - 1, inventory.GetCount(ItemId.SteelBlock));
        Assert.True(grid.IsConnected());

        // 2. Select slot 3 (Grinder), deconstruct the newly placed block
        float progress = 0f;
        Int3? lastTarget = null;
        GrinderStepResult result = default;

        // Step over 10 frames @ 0.1s delta with rate = 1.0 -> 1.0s completion
        for (int frame = 0; frame < 10; frame++)
        {
            result = GrinderSim.Step(
                grid, inventory, placementCell, BlockType.ArmorBlock,
                isFiring: true, activeSlot: 3, hitDistance: 3.0f,
                grinderRange: 5.0f, grinderRate: 1.0f, deltaSeconds: 0.1f,
                ref progress, ref lastTarget);
        }

        Assert.True(result.BlockDeconstructed);
        Assert.Equal(placementCell, result.DeconstructedCell);

        // 3. Invariants verified:
        // Item count returned 1-for-1 to initial 5
        Assert.Equal(initialSteelCount, inventory.GetCount(ItemId.SteelBlock));
        // Grid count returned to initial count
        Assert.Equal(initialGridCount, grid.Count);
        // Cell is no longer occupied
        Assert.False(grid.HasBlock(placementCell));
        // Grid remains 6-connected
        Assert.True(grid.IsConnected());
    }
}
