using System;
using System.Collections.Generic;
using System.Numerics;
using Keel.Sim.Config;
using Keel.Sim.Grid;
using Keel.Sim.Hud;
using Keel.Sim.Inventory;
using Keel.Sim.Mining;
using Keel.Sim.Persistence;
using Keel.Sim.Pilot;
using Keel.Sim.Power;
using Keel.Sim.Refinery;
using Keel.Sim.Storage;
using Xunit;

namespace Keel.Sim.Tests.Storage;

public sealed class CargoTerminalTests
{
    [Fact]
    public void ViewModel_ComputesActionAvailabilityPowerStateAndCellSummary()
    {
        // AC-1: Given CargoTerminalModel.Compute(cell, containerInventory, suitInventory, powerSnapshot, isOpen),
        // when evaluated across empty, partial, and full suit and container states, then it computes accurate
        // per-item counts, container fill ratio, station power status text, and action enablement flags
        // that are true if and only if source stock > 0 and destination RemainingCapacity > 0.

        var cell = new Int3(5, 0, 0);
        var container = new SuitInventory(500);
        var suit = new SuitInventory(50);
        var power = new StationPowerSnapshot(
            GeneratedKw: 125.0f,
            DemandKw: 100.0f,
            PowerEfficiency: 1.0f,
            Status: PowerStatus.Nominal,
            SolarPanelCount: 2,
            CargoContainerCount: 1);

        // 1. Initial State: Container empty, suit has 20 IronOre and 10 SteelBlock (30 / 50)
        suit.Add(ItemId.IronOre, 20);
        suit.Add(ItemId.SteelBlock, 10);

        var vm1 = CargoTerminalModel.Compute(cell, container, suit, power, isOpen: true);

        Assert.True(vm1.IsOpen);
        Assert.Equal(cell, vm1.Cell);
        Assert.Equal(0, vm1.ContainerTotalCount);
        Assert.Equal(500, vm1.ContainerCapacity);
        Assert.Equal(0.0f, vm1.ContainerFillRatio);
        Assert.Equal("CONTAINER EMPTY", vm1.StatusMessage);
        Assert.Contains("PWR  125 / 100 kW (NOMINAL)", vm1.PowerMessage);

        Assert.Equal(20, vm1.SuitOreCount);
        Assert.Equal(10, vm1.SuitBlockCount);
        Assert.Equal(30, vm1.SuitTotalCount);
        Assert.Equal(20, vm1.SuitRemainingCapacity);

        // Action flags
        Assert.True(vm1.CanDepositOre);
        Assert.False(vm1.CanWithdrawOre);
        Assert.True(vm1.CanDepositBlocks);
        Assert.False(vm1.CanWithdrawBlocks);
        Assert.True(vm1.CanDepositAll);
        Assert.False(vm1.CanWithdrawAll);

        // 2. Partial container (150 IronOre, 50 SteelBlock = 200 / 500) and empty suit
        suit.Clear();
        container.Add(ItemId.IronOre, 150);
        container.Add(ItemId.SteelBlock, 50);

        var vm2 = CargoTerminalModel.Compute(cell, container, suit, power, isOpen: true);

        Assert.Equal(200, vm2.ContainerTotalCount);
        Assert.Equal(0.4f, vm2.ContainerFillRatio, precision: 2);
        Assert.Equal("STORED 200 / 500", vm2.StatusMessage);
        Assert.Equal(0, vm2.SuitTotalCount);

        Assert.False(vm2.CanDepositOre);
        Assert.True(vm2.CanWithdrawOre);
        Assert.False(vm2.CanDepositBlocks);
        Assert.True(vm2.CanWithdrawBlocks);
        Assert.False(vm2.CanDepositAll);
        Assert.True(vm2.CanWithdrawAll);

        // 3. Full container (500 / 500), suit with 10 IronOre
        container.Add(ItemId.IronOre, 300); // 200 + 300 = 500
        Assert.True(container.IsFull);
        suit.Add(ItemId.IronOre, 10);

        var vm3 = CargoTerminalModel.Compute(cell, container, suit, power, isOpen: true);

        Assert.True(vm3.CanWithdrawOre);
        Assert.False(vm3.CanDepositOre); // Container is full
        Assert.False(vm3.CanDepositBlocks);
        Assert.False(vm3.CanDepositAll);
        Assert.Equal(1.0f, vm3.ContainerFillRatio);
        Assert.Equal("CONTAINER FULL", vm3.StatusMessage);

        // 4. Full suit (50 / 50) cannot withdraw anything
        suit.Add(ItemId.IronOre, 40); // 10 + 40 = 50
        Assert.True(suit.IsFull);

        var vm4 = CargoTerminalModel.Compute(cell, container, suit, power, isOpen: true);

        Assert.False(vm4.CanWithdrawOre);
        Assert.False(vm4.CanWithdrawBlocks);
        Assert.False(vm4.CanWithdrawAll);
    }

    [Fact]
    public void FullLoop_MineStorePowerRefineBuildAndPersistConservesAllItems()
    {
        // AC-3: Given a complete headless session across DrillSim, StationCargoStore, CargoTransferSim,
        // StationPowerSim, RefinerySim, BlockPlacementSim, GrinderSim, and WorldSaveSerializer,
        // when mining multiple 50-unit suit loads into a 500-unit CargoContainer, refining SteelBlock items,
        // placing SolarPanel blocks to reach 100% power efficiency, withdrawing stored ore/blocks, saving to JSON,
        // mutating world state, and reloading, then every IronOre and SteelBlock unit, power ratio,
        // grid cell, and container stack is conserved with zero drift.

        var tuning = TuningConfig.Default with
        {
            CorePowerKw = 25.0f,
            SolarPanelPowerKw = 50.0f,
            RefineryPowerDemandKw = 100.0f,
            CargoContainerPowerDemandKw = 10.0f,
            CargoContainerCapacity = 500.0f,
            RefineryCycleSeconds = 1.0f,
            RefineryOrePerBlock = 2.0f,
        };

        var grid = StationBlueprint.CreateDefault();
        var refinery = new RefineryState();
        var cargoStore = new StationCargoStore((int)MathF.Round(tuning.CargoContainerCapacity));
        var suit = new SuitInventory(50);
        var deposit = new OreDepositState(200, ItemId.IronOre, "asteroid-01-ore-seam");

        // Player starts with 2 SteelBlocks to place a CargoContainer
        suit.Add(ItemId.SteelBlock, 2);

        // 1. Place a CargoContainer at (5, 0, 0)
        var cargoCell = new Int3(5, 0, 0);
        var placeEval = new PlacementEvaluation(
            Status: PlacementStatus.Valid,
            CandidateCell: cargoCell,
            CandidateBlockType: BlockType.CargoContainer);

        bool placedCargo = BlockPlacementSim.TryPlace(grid, suit, in placeEval, out _);
        Assert.True(placedCargo);
        Assert.Equal(0, suit.GetCount(ItemId.SteelBlock));
        Assert.Equal(BlockType.CargoContainer, grid.GetBlock(cargoCell));

        var container = cargoStore.RegisterContainer(cargoCell, (int)MathF.Round(tuning.CargoContainerCapacity));
        Assert.NotNull(container);

        // 2. Mining Trip 1: Drill 50 IronOre into suit
        float drillProgress = 0f;
        while (!suit.IsFull && !deposit.IsDepleted)
        {
            var res = DrillSim.Step(deposit, suit, isFiring: true, activeSlot: 1, hitDistance: 3.0f, tuning, deltaSeconds: 0.5f, ref drillProgress);
        }
        Assert.Equal(50, suit.GetCount(ItemId.IronOre));
        Assert.Equal(150, deposit.RemainingUnits);

        // Bulk transfer into container
        int transferredTrip1 = CargoTransferSim.DepositAll(suit, container);
        Assert.Equal(50, transferredTrip1);
        Assert.Equal(0, suit.TotalCount);
        Assert.Equal(50, container.GetCount(ItemId.IronOre));

        // 3. Mining Trip 2: Drill 50 more IronOre into suit and deposit
        drillProgress = 0f;
        while (!suit.IsFull && !deposit.IsDepleted)
        {
            DrillSim.Step(deposit, suit, isFiring: true, activeSlot: 1, hitDistance: 3.0f, tuning, deltaSeconds: 0.5f, ref drillProgress);
        }
        Assert.Equal(50, suit.GetCount(ItemId.IronOre));
        Assert.Equal(100, deposit.RemainingUnits);

        int transferredTrip2 = CargoTransferSim.DepositAll(suit, container);
        Assert.Equal(50, transferredTrip2);
        Assert.Equal(100, container.GetCount(ItemId.IronOre)); // Exceeds 50-unit suit capacity!

        // 4. Power check: Core only (25 kW), CargoContainer standby (10 kW), Refinery idle -> Nominal
        var powerInitial = StationPowerSim.Evaluate(grid, refinery, tuning);
        Assert.Equal(25.0f, powerInitial.GeneratedKw);
        Assert.Equal(10.0f, powerInitial.DemandKw);
        Assert.Equal(PowerStatus.Nominal, powerInitial.Status);

        // 5. Transfer 20 ore to suit, then deposit into refinery
        CargoTransferSim.Withdraw(suit, container, ItemId.IronOre, 20);
        Assert.Equal(20, suit.GetCount(ItemId.IronOre));
        Assert.Equal(80, container.GetCount(ItemId.IronOre));

        RefinerySim.DepositFromSuit(refinery, suit);
        Assert.Equal(0, suit.GetCount(ItemId.IronOre));
        Assert.Equal(20, refinery.InputInventory.GetCount(ItemId.IronOre));

        // Station power is now under Brownout: 25 kW gen vs 110 kW demand (100 kW refinery + 10 kW cargo)
        var powerBrownout = StationPowerSim.Evaluate(grid, refinery, tuning);
        Assert.Equal(PowerStatus.Brownout, powerBrownout.Status);
        float brownoutEfficiency = 25.0f / 110.0f;
        Assert.Equal(brownoutEfficiency, powerBrownout.PowerEfficiency, precision: 3);

        // Run refinery under brownout until 4 SteelBlocks are produced (consuming 8 ore)
        while (refinery.OutputInventory.GetCount(ItemId.SteelBlock) < 4)
        {
            var pSnap = StationPowerSim.Evaluate(grid, refinery, tuning);
            RefinerySim.Step(refinery, tuning, deltaSeconds: 0.1f, powerEfficiency: pSnap.PowerEfficiency);
        }
        Assert.Equal(4, refinery.OutputInventory.GetCount(ItemId.SteelBlock));
        Assert.Equal(12, refinery.InputInventory.GetCount(ItemId.IronOre));

        // Withdraw 4 SteelBlocks into suit
        RefinerySim.WithdrawToSuit(refinery, suit);
        Assert.Equal(4, suit.GetCount(ItemId.SteelBlock));
        Assert.Equal(0, refinery.OutputInventory.GetCount(ItemId.SteelBlock));

        // 6. Build 2 SolarPanels to eliminate brownout
        var solarCell1 = new Int3(-5, 0, 0);
        var solarCell2 = new Int3(5, 0, 1);

        var solarPlaceEval1 = new PlacementEvaluation(Status: PlacementStatus.Valid, CandidateCell: solarCell1, CandidateBlockType: BlockType.SolarPanel);
        var solarPlaceEval2 = new PlacementEvaluation(Status: PlacementStatus.Valid, CandidateCell: solarCell2, CandidateBlockType: BlockType.SolarPanel);

        Assert.True(BlockPlacementSim.TryPlace(grid, suit, in solarPlaceEval1, out _));
        Assert.True(BlockPlacementSim.TryPlace(grid, suit, in solarPlaceEval2, out _));
        Assert.Equal(0, suit.GetCount(ItemId.SteelBlock));

        // Power is now Nominal: 125 kW gen vs 110 kW demand -> 100% efficiency
        var powerNominal = StationPowerSim.Evaluate(grid, refinery, tuning);
        Assert.Equal(125.0f, powerNominal.GeneratedKw);
        Assert.Equal(110.0f, powerNominal.DemandKw);
        Assert.Equal(PowerStatus.Nominal, powerNominal.Status);
        Assert.Equal(1.0f, powerNominal.PowerEfficiency);

        // 7. Non-empty grinder guard: trying to grind container (holding 80 ore) is refused
        float grindProgress = 0f;
        Int3? lastGrind = null;
        var grindBlocked = GrinderSim.Step(
            grid, suit, cargoCell, BlockType.CargoContainer, isFiring: true, activeSlot: 3,
            hitDistance: 3.0f, grinderRange: 5.0f, grinderRate: 1.0f, deltaSeconds: 1.0f,
            ref grindProgress, ref lastGrind, cargoStore);

        Assert.Equal(GrinderStatus.ContainerNotEmpty, grindBlocked.Status);
        Assert.True(grid.HasBlock(cargoCell));
        Assert.True(cargoStore.HasContainer(cargoCell));

        // 8. Quicksave & Persistence Round-trip
        var pilotMachine = new PilotModeStateMachine();
        pilotMachine.Reset(PilotMode.FloatingInSuit);

        var shipKin = new KinematicsSaveData(Vector3.Zero, Quaternion.Identity, Vector3.Zero, Vector3.Zero, true);
        var suitKin = new KinematicsSaveData(new Vector3(5f, 0f, 5f), Quaternion.Identity, Vector3.Zero, Vector3.Zero, true);

        var snapshot = WorldStateMapper.CreateSnapshot(
            pilotMachine.Mode,
            shipKin,
            suitKin,
            activeSuitSlot: 1,
            suit,
            stationGrid: grid,
            refinery: refinery,
            oreDeposits: new[] { deposit },
            cargoStore: cargoStore);

        string json = WorldSaveSerializer.Serialize(snapshot);
        var loadResult = WorldSaveSerializer.Deserialize(json);
        Assert.True(loadResult.IsSuccess, loadResult.ErrorMessage);

        // Mutate current world state completely
        var freshGrid = new BlockGrid();
        var freshRefinery = new RefineryState();
        var freshCargoStore = new StationCargoStore(500);
        var freshSuit = new SuitInventory(50);
        var freshDeposits = new List<OreDepositState> { new(200, ItemId.IronOre, "asteroid-01-ore-seam") };

        // Apply snapshot to fresh state
        WorldStateMapper.Apply(
            loadResult.Data!,
            pilotMachine,
            freshSuit,
            stationGrid: freshGrid,
            refinery: freshRefinery,
            oreDeposits: freshDeposits,
            cargoStore: freshCargoStore);

        // Verify total conservation across the entire loop
        Assert.Equal(BlockType.CargoContainer, freshGrid.GetBlock(cargoCell));
        Assert.Equal(BlockType.SolarPanel, freshGrid.GetBlock(solarCell1));
        Assert.Equal(BlockType.SolarPanel, freshGrid.GetBlock(solarCell2));

        Assert.True(freshCargoStore.HasContainer(cargoCell));
        var restoredBox = freshCargoStore.GetContainer(cargoCell)!;
        Assert.Equal(80, restoredBox.GetCount(ItemId.IronOre));

        Assert.Equal(12, freshRefinery.InputInventory.GetCount(ItemId.IronOre));
        Assert.Equal(0, freshRefinery.OutputInventory.GetCount(ItemId.SteelBlock));
        Assert.Equal(100, freshDeposits[0].RemainingUnits);

        // Total IronOre accounted for:
        // 100 (in deposit) + 80 (in cargo container) + 12 (in refinery input) + 8 (consumed into 4 steel blocks) = 200 total!
        int totalAccountedOre = freshDeposits[0].RemainingUnits +
                                restoredBox.GetCount(ItemId.IronOre) +
                                freshRefinery.InputInventory.GetCount(ItemId.IronOre) +
                                (4 * (int)tuning.RefineryOrePerBlock);
        Assert.Equal(200, totalAccountedOre);
    }
}
