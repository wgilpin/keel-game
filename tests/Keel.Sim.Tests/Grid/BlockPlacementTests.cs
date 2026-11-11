using System.Numerics;
using Keel.Sim.Config;
using Keel.Sim.Grid;
using Keel.Sim.Inventory;
using Keel.Sim.Mining;
using Keel.Sim.Pilot;
using Keel.Sim.Refinery;
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

    [Fact]
    public void EvaluateAndTryPlace_RejectCellsOverlappingSuitOrShipBoundsWithoutFlushNeighborFalsePositives()
    {
        var grid = new BlockGrid();
        grid.TryPlaceBlock(new Int3(0, 0, 0), BlockType.Core, out _);
        var inventory = new SuitInventory(50);
        inventory.Add(ItemId.SteelBlock, 10);

        // Candidate cell (1, 0, 0) has center (2.5, 0, 0), spanning [1.25, 3.75] along X
        // 1. Obstacle directly overlapping candidate cell interior: e.g. Suit box [2.0, 2.8] along X
        var overlappingObstacle = (Min: new Vector3(2.0f, -0.4f, -0.4f), Max: new Vector3(2.8f, 0.4f, 0.4f));

        var evalBlocked = BlockPlacementSim.Evaluate(
            grid: grid,
            isFloatingInSuit: true,
            activeSlot: 2,
            steelBlockCount: inventory.GetCount(ItemId.SteelBlock),
            hasStationHit: true,
            localHitPosition: new Vector3(1.25f, 0f, 0f),
            localHitNormal: Vector3.UnitX,
            hitDistance: 3.0f,
            buildRange: 7.5f,
            isModalOpen: false,
            dynamicObstacles: new[] { overlappingObstacle });

        Assert.Equal(PlacementStatus.InvalidCell, evalBlocked.Status);
        Assert.False(evalBlocked.IsValid);
        Assert.True(evalBlocked.ShowGhost);
        bool placedBlocked = BlockPlacementSim.TryPlace(grid, inventory, in evalBlocked, out string? blockedReason);
        Assert.False(placedBlocked);
        Assert.Equal(10, inventory.GetCount(ItemId.SteelBlock)); // zero item loss

        // 2. Obstacle flush against adjacent face or just outside candidate skin tolerance:
        // Candidate cell extends to X = 3.75. An obstacle at [3.76, 4.5] is outside candidate cell
        var clearObstacle = (Min: new Vector3(3.76f, -0.4f, -0.4f), Max: new Vector3(4.5f, 0.4f, 0.4f));

        var evalClear = BlockPlacementSim.Evaluate(
            grid: grid,
            isFloatingInSuit: true,
            activeSlot: 2,
            steelBlockCount: inventory.GetCount(ItemId.SteelBlock),
            hasStationHit: true,
            localHitPosition: new Vector3(1.25f, 0f, 0f),
            localHitNormal: Vector3.UnitX,
            hitDistance: 3.0f,
            buildRange: 7.5f,
            isModalOpen: false,
            dynamicObstacles: new[] { clearObstacle });

        Assert.Equal(PlacementStatus.Valid, evalClear.Status);
        Assert.True(evalClear.IsValid);
        Assert.True(evalClear.ShowGhost);
        bool placedClear = BlockPlacementSim.TryPlace(grid, inventory, in evalClear, out _);
        Assert.True(placedClear);
        Assert.Equal(9, inventory.GetCount(ItemId.SteelBlock));
    }

    [Fact]
    public void EvaluateAndTryPlace_ProtectRefineryFrontPortWhileAllowingOtherRefineryFaces()
    {
        // Setup Station with default blueprint (Refinery at (0, 0, 3) facing +Z)
        BlockGrid station = StationBlueprint.CreateDefault();
        var inventory = new SuitInventory(50);
        inventory.Add(ItemId.SteelBlock, 5);

        // Refinery is at (0, 0, 3).
        // 1. Aiming at +Z face of Refinery: candidate cell is (0, 0, 4).
        // This would seal the refinery front port! Must return InvalidCell and block placement.
        float refineryCenterZ = 3 * BlockGrid.CellSize; // 7.5m
        float refineryFaceZ = refineryCenterZ + (BlockGrid.CellSize * 0.5f); // 8.75m

        var evalPort = BlockPlacementSim.Evaluate(
            grid: station,
            isFloatingInSuit: true,
            activeSlot: 2,
            steelBlockCount: inventory.GetCount(ItemId.SteelBlock),
            hasStationHit: true,
            localHitPosition: new Vector3(0f, 0f, refineryFaceZ),
            localHitNormal: Vector3.UnitZ,
            hitDistance: 3.5f,
            buildRange: 7.5f,
            isModalOpen: false);

        Assert.Equal(PlacementStatus.InvalidCell, evalPort.Status);
        Assert.False(evalPort.IsValid);
        Assert.True(evalPort.ShowGhost);
        Assert.Equal(new Int3(0, 0, 4), evalPort.CandidateCell);

        bool placedPort = BlockPlacementSim.TryPlace(station, inventory, in evalPort, out _);
        Assert.False(placedPort);
        Assert.Equal(5, inventory.GetCount(ItemId.SteelBlock)); // zero item loss

        // 2. Aiming at +X face of Refinery: candidate cell is (1, 0, 3).
        // Non-port side face is unobstructed and valid to build on.
        var evalSide = BlockPlacementSim.Evaluate(
            grid: station,
            isFloatingInSuit: true,
            activeSlot: 2,
            steelBlockCount: inventory.GetCount(ItemId.SteelBlock),
            hasStationHit: true,
            localHitPosition: new Vector3(BlockGrid.CellSize * 0.5f, 0f, refineryCenterZ),
            localHitNormal: Vector3.UnitX,
            hitDistance: 3.5f,
            buildRange: 7.5f,
            isModalOpen: false);

        Assert.Equal(PlacementStatus.Valid, evalSide.Status);
        Assert.True(evalSide.IsValid);
        Assert.True(evalSide.ShowGhost);
        Assert.Equal(new Int3(1, 0, 3), evalSide.CandidateCell);

        bool placedSide = BlockPlacementSim.TryPlace(station, inventory, in evalSide, out string? sideReason);
        Assert.True(placedSide, sideReason);
        Assert.Equal(4, inventory.GetCount(ItemId.SteelBlock));
        Assert.Equal(BlockType.ArmorBlock, station.GetBlock(new Int3(1, 0, 3)));
    }

    [Fact]
    public void Evaluate_ShowsGhostOnlyWhenSlotTwoHasStockWithinRangeAndModalClosed()
    {
        var grid = new BlockGrid();
        grid.TryPlaceBlock(new Int3(0, 0, 0), BlockType.Core, out _);
        const float range = 7.5f;

        // Valid base state: ShowGhost is true
        var evalValid = BlockPlacementSim.Evaluate(
            grid, isFloatingInSuit: true, activeSlot: 2, steelBlockCount: 3,
            hasStationHit: true, localHitPosition: new Vector3(1.25f, 0, 0), localHitNormal: Vector3.UnitX,
            hitDistance: 3.5f, buildRange: range, isModalOpen: false);
        Assert.True(evalValid.ShowGhost);

        // InvalidCell (candidate cell is already occupied in corner): ShowGhost is still true (red ghost)
        grid.TryPlaceBlock(new Int3(1, 0, 0), BlockType.ArmorBlock, out _);
        grid.TryPlaceBlock(new Int3(1, 1, 0), BlockType.ArmorBlock, out _);
        var evalOccupied = BlockPlacementSim.Evaluate(
            grid, isFloatingInSuit: true, activeSlot: 2, steelBlockCount: 3,
            hasStationHit: true, localHitPosition: new Vector3(2.5f, 1.25f, 0f), localHitNormal: Vector3.UnitY,
            hitDistance: 3.5f, buildRange: range, isModalOpen: false);
        Assert.Equal(PlacementStatus.InvalidCell, evalOccupied.Status);
        Assert.Equal(new Int3(1, 1, 0), evalOccupied.CandidateCell);
        Assert.True(evalOccupied.ShowGhost);

        // OutOfRange: ShowGhost is false
        var evalOutOfRange = BlockPlacementSim.Evaluate(
            grid, isFloatingInSuit: true, activeSlot: 2, steelBlockCount: 3,
            hasStationHit: true, localHitPosition: new Vector3(1.25f, 0, 0), localHitNormal: Vector3.UnitX,
            hitDistance: 9.0f, buildRange: range, isModalOpen: false);
        Assert.False(evalOutOfRange.ShowGhost);

        // Slot 1 (Drill): ShowGhost is false
        var evalSlot1 = BlockPlacementSim.Evaluate(
            grid, isFloatingInSuit: true, activeSlot: 1, steelBlockCount: 3,
            hasStationHit: true, localHitPosition: new Vector3(1.25f, 0, 0), localHitNormal: Vector3.UnitX,
            hitDistance: 3.5f, buildRange: range, isModalOpen: false);
        Assert.False(evalSlot1.ShowGhost);

        // Zero SteelBlock count: ShowGhost is false
        var evalZeroStock = BlockPlacementSim.Evaluate(
            grid, isFloatingInSuit: true, activeSlot: 2, steelBlockCount: 0,
            hasStationHit: true, localHitPosition: new Vector3(1.25f, 0, 0), localHitNormal: Vector3.UnitX,
            hitDistance: 3.5f, buildRange: range, isModalOpen: false);
        Assert.False(evalZeroStock.ShowGhost);

        // Modal terminal open: ShowGhost is false
        var evalModal = BlockPlacementSim.Evaluate(
            grid, isFloatingInSuit: true, activeSlot: 2, steelBlockCount: 3,
            hasStationHit: true, localHitPosition: new Vector3(1.25f, 0, 0), localHitNormal: Vector3.UnitX,
            hitDistance: 3.5f, buildRange: range, isModalOpen: true);
        Assert.False(evalModal.ShowGhost);

        // Off-station miss: ShowGhost is false
        var evalMiss = BlockPlacementSim.Evaluate(
            grid, isFloatingInSuit: true, activeSlot: 2, steelBlockCount: 3,
            hasStationHit: false, localHitPosition: Vector3.Zero, localHitNormal: Vector3.Zero,
            hitDistance: 4.0f, buildRange: range, isModalOpen: false);
        Assert.False(evalMiss.ShowGhost);
    }

    [Fact]
    public void ClosedLoop_MineRefineWithdrawPlaceAndBoardConservesItemsAndGridConnectivity()
    {
        var tuning = TuningConfig.Default;
        var station = StationBlueprint.CreateDefault();
        int initialGridCount = station.Count;
        Assert.True(station.IsConnected());

        var pilotMachine = new PilotModeStateMachine();
        // Exit ship into suit
        pilotMachine.ExitCockpit(Vector3.Zero, Quaternion.Identity, Vector3.Zero);
        Assert.Equal(PilotMode.FloatingInSuit, pilotMachine.Mode);

        var deposit = new OreDepositState(10, ItemId.IronOre);
        var refinery = new RefineryState();
        float drillProgress = 0.0f;

        // 1. Mine 4 units of IronOre from deposit with [1] Drill (drillRate = 1.5/s, need 4 units -> ~2.67s)
        var drillRes = DrillSim.Step(
            deposit,
            pilotMachine.SuitInventory,
            isFiring: true,
            activeSlot: 1,
            hitDistance: 3.0f,
            tuning: tuning,
            deltaSeconds: 4.0f / tuning.DrillRate,
            ref drillProgress);

        Assert.Equal(4, drillRes.UnitsExtracted);
        Assert.Equal(6, deposit.RemainingUnits);
        Assert.Equal(4, pilotMachine.SuitInventory.GetCount(ItemId.IronOre));
        Assert.Equal(0, pilotMachine.SuitInventory.GetCount(ItemId.SteelBlock));

        // 2. Deposit IronOre into station refinery
        int deposited = RefinerySim.DepositFromSuit(refinery, pilotMachine.SuitInventory);
        Assert.Equal(4, deposited);
        Assert.Equal(0, pilotMachine.SuitInventory.GetCount(ItemId.IronOre));
        Assert.Equal(4, refinery.InputInventory.GetCount(ItemId.IronOre));

        // 3. Step refinery for 2 full cycles (4.0s at 2.0s/cycle, 2 ore per block -> 2 blocks)
        RefinerySim.Step(refinery, tuning, 4.0f);
        Assert.Equal(0, refinery.InputInventory.GetCount(ItemId.IronOre));
        Assert.Equal(2, refinery.OutputInventory.GetCount(ItemId.SteelBlock));

        // 4. Withdraw refined SteelBlock items into suit cargo
        int withdrawn = RefinerySim.WithdrawToSuit(refinery, pilotMachine.SuitInventory);
        Assert.Equal(2, withdrawn);
        Assert.Equal(2, pilotMachine.SuitInventory.GetCount(ItemId.SteelBlock));
        Assert.Equal(0, refinery.OutputInventory.GetCount(ItemId.SteelBlock));

        // 5. Place 1st block at +X of Refinery: (1, 0, 3)
        float refZ = 3 * BlockGrid.CellSize;
        var eval1 = BlockPlacementSim.Evaluate(
            grid: station,
            isFloatingInSuit: true,
            activeSlot: 2,
            steelBlockCount: pilotMachine.SuitInventory.GetCount(ItemId.SteelBlock),
            hasStationHit: true,
            localHitPosition: new Vector3(BlockGrid.CellSize * 0.5f, 0f, refZ),
            localHitNormal: Vector3.UnitX,
            hitDistance: 3.5f,
            buildRange: tuning.BuildRange,
            isModalOpen: false);

        Assert.True(eval1.IsValid);
        Assert.True(eval1.ShowGhost);
        bool placed1 = BlockPlacementSim.TryPlace(station, pilotMachine.SuitInventory, in eval1, out _);
        Assert.True(placed1);
        Assert.Equal(initialGridCount + 1, station.Count);
        Assert.Equal(1, pilotMachine.SuitInventory.GetCount(ItemId.SteelBlock));
        Assert.True(station.IsConnected());

        // 6. Chain 2nd block onto (2, 0, 3)
        var eval2 = BlockPlacementSim.Evaluate(
            grid: station,
            isFloatingInSuit: true,
            activeSlot: 2,
            steelBlockCount: pilotMachine.SuitInventory.GetCount(ItemId.SteelBlock),
            hasStationHit: true,
            localHitPosition: new Vector3(BlockGrid.CellSize * 1.5f, 0f, refZ),
            localHitNormal: Vector3.UnitX,
            hitDistance: 3.5f,
            buildRange: tuning.BuildRange,
            isModalOpen: false);

        Assert.True(eval2.IsValid);
        bool placed2 = BlockPlacementSim.TryPlace(station, pilotMachine.SuitInventory, in eval2, out _);
        Assert.True(placed2);
        Assert.Equal(initialGridCount + 2, station.Count);
        Assert.Equal(0, pilotMachine.SuitInventory.GetCount(ItemId.SteelBlock));
        Assert.True(station.IsConnected());

        // 7. Exhausted stock: subsequent evaluation shows no ghost
        var evalZero = BlockPlacementSim.Evaluate(
            grid: station,
            isFloatingInSuit: true,
            activeSlot: 2,
            steelBlockCount: pilotMachine.SuitInventory.GetCount(ItemId.SteelBlock),
            hasStationHit: true,
            localHitPosition: new Vector3(BlockGrid.CellSize * 2.5f, 0f, refZ),
            localHitNormal: Vector3.UnitX,
            hitDistance: 3.5f,
            buildRange: tuning.BuildRange,
            isModalOpen: false);

        Assert.Equal(PlacementStatus.NoBlocks, evalZero.Status);
        Assert.False(evalZero.ShowGhost);

        // 8. Board ship and exit: inventory and station blocks remain intact
        bool boarded = pilotMachine.TryBoardShip(Vector3.Zero, Vector3.Zero, tuning.ShipInteractRange);
        Assert.True(boarded);
        Assert.Equal(PilotMode.SeatedInCockpit, pilotMachine.Mode);

        pilotMachine.ExitCockpit(Vector3.Zero, Quaternion.Identity, Vector3.Zero);
        Assert.Equal(PilotMode.FloatingInSuit, pilotMachine.Mode);
        Assert.Equal(0, pilotMachine.SuitInventory.GetCount(ItemId.SteelBlock));
        Assert.Equal(initialGridCount + 2, station.Count);
        Assert.True(station.IsConnected());
    }
}
