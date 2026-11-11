using System;
using Keel.Sim.Config;
using Keel.Sim.Grid;
using Keel.Sim.Inventory;
using Keel.Sim.Power;
using Keel.Sim.Storage;
using Xunit;

namespace Keel.Sim.Tests.Storage;

public sealed class StationCargoTests
{
    [Fact]
    public void CargoStore_IsolatesPerCellContainersAndContributesStandbyPowerDemand()
    {
        // AC-1: Given hotbar slot [5] (BlockType.CargoContainer, cost 2 SteelBlock), when placed
        // and registered in StationCargoStore, then each CargoContainer Int3 cell owns an independent
        // SuitInventory of capacity cargoContainerCapacity (500) without sharing items across cells,
        // and StationPowerSim.Evaluate includes cargoCount * cargoContainerPowerDemandKw in station power demand.

        var tuning = TuningConfig.Default with
        {
            CorePowerKw = 50.0f,
            SolarPanelPowerKw = 50.0f,
            RefineryPowerDemandKw = 100.0f,
            CargoContainerPowerDemandKw = 10.0f,
            CargoContainerCapacity = 500.0f,
        };

        var store = new StationCargoStore((int)MathF.Round(tuning.CargoContainerCapacity));
        var cellA = new Int3(5, 0, 0);
        var cellB = new Int3(-5, 0, 0);

        var containerA = store.RegisterContainer(cellA);
        var containerB = store.RegisterContainer(cellB);

        Assert.NotNull(containerA);
        Assert.NotNull(containerB);
        Assert.NotSame(containerA, containerB);
        Assert.Equal(500, containerA.Capacity);
        Assert.Equal(500, containerB.Capacity);
        Assert.Equal(2, store.ContainerCount);
        Assert.Equal(1000, store.TotalCapacity);

        // Populate container A
        containerA.Add(ItemId.IronOre, 100);
        Assert.Equal(100, containerA.GetCount(ItemId.IronOre));
        Assert.Equal(0, containerB.GetCount(ItemId.IronOre)); // Container B is isolated
        Assert.Equal(100, store.TotalItemsStored);

        // Populate container B
        containerB.Add(ItemId.SteelBlock, 50);
        Assert.Equal(100, containerA.GetCount(ItemId.IronOre));
        Assert.Equal(0, containerA.GetCount(ItemId.SteelBlock)); // Container A is isolated
        Assert.Equal(50, containerB.GetCount(ItemId.SteelBlock));
        Assert.Equal(150, store.TotalItemsStored);

        // Power demand evaluation with 2 cargo containers
        var grid = StationBlueprint.CreateDefault();
        grid.TryPlaceBlock(cellA, BlockType.CargoContainer, out _);
        grid.TryPlaceBlock(cellB, BlockType.CargoContainer, out _);

        // Station has Core (50 kW) + 2 CargoContainers (10 kW each) + 0 Refinery demand
        var power = StationPowerSim.Evaluate(grid, refinery: null, tuning);
        Assert.Equal(50.0f, power.GeneratedKw);
        Assert.Equal(20.0f, power.DemandKw); // 2 * 10 kW
        Assert.Equal(2, power.CargoContainerCount);
        Assert.Equal(PowerStatus.Nominal, power.Status);
        Assert.Equal(1.0f, power.PowerEfficiency);

        // Unregister container A
        bool removed = store.UnregisterContainer(cellA, out var removedInv);
        Assert.True(removed);
        Assert.NotNull(removedInv);
        Assert.Equal(100, removedInv.GetCount(ItemId.IronOre));
        Assert.False(store.HasContainer(cellA));
        Assert.True(store.HasContainer(cellB));
        Assert.Equal(1, store.ContainerCount);
        Assert.Equal(500, store.TotalCapacity);
        Assert.Equal(50, store.TotalItemsStored);
    }

    [Fact]
    public void Transfers_DepositAndWithdrawIronOreSteelBlocksAndAllItemsWithinBounds()
    {
        // AC-2: Given CargoTransferSim operating between SuitInventory (50 capacity) and a CargoContainer (500 capacity),
        // when depositing or withdrawing a specific ItemId (IronOre or SteelBlock) or all items (DepositAll / WithdrawAll),
        // then transferred quantities clamp strictly to available source stock and remaining destination capacity,
        // leaving non-targeted items untouched and conserving total item counts across both inventories.

        var suit = new SuitInventory(50);
        var container = new SuitInventory(500);

        suit.Add(ItemId.IronOre, 40);
        suit.Add(ItemId.SteelBlock, 10);
        Assert.True(suit.IsFull);
        Assert.Equal(50, suit.TotalCount);

        // 1. Partial deposit of IronOre
        int depositedOre = CargoTransferSim.Deposit(suit, container, ItemId.IronOre, 25);
        Assert.Equal(25, depositedOre);
        Assert.Equal(15, suit.GetCount(ItemId.IronOre));
        Assert.Equal(10, suit.GetCount(ItemId.SteelBlock)); // SteelBlock untouched
        Assert.Equal(25, container.GetCount(ItemId.IronOre));
        Assert.Equal(0, container.GetCount(ItemId.SteelBlock));
        Assert.Equal(50, suit.TotalCount + container.TotalCount); // Conserved

        // 2. Partial withdraw of IronOre
        int withdrawnOre = CargoTransferSim.Withdraw(suit, container, ItemId.IronOre, 10);
        Assert.Equal(10, withdrawnOre);
        Assert.Equal(25, suit.GetCount(ItemId.IronOre));
        Assert.Equal(10, suit.GetCount(ItemId.SteelBlock));
        Assert.Equal(15, container.GetCount(ItemId.IronOre));
        Assert.Equal(50, suit.TotalCount + container.TotalCount); // Conserved

        // 3. DepositAll transfers all items from suit to container
        int totalDeposited = CargoTransferSim.DepositAll(suit, container);
        Assert.Equal(35, totalDeposited);
        Assert.Equal(0, suit.TotalCount);
        Assert.True(suit.IsEmpty);
        Assert.Equal(40, container.GetCount(ItemId.IronOre)); // 15 + 25 from suit = 40
        Assert.Equal(10, container.GetCount(ItemId.SteelBlock));
        Assert.Equal(50, container.TotalCount);

        // 4. Fill container to near capacity (495 / 500)
        container.Add(ItemId.IronOre, 445);
        Assert.Equal(495, container.TotalCount);
        Assert.Equal(5, container.RemainingCapacity);

        // Suit has 20 SteelBlocks. Attempt to deposit all -> only 5 should transfer due to container capacity
        suit.Add(ItemId.SteelBlock, 20);
        int clampedDeposit = CargoTransferSim.DepositAll(suit, container);
        Assert.Equal(5, clampedDeposit);
        Assert.Equal(15, suit.GetCount(ItemId.SteelBlock));
        Assert.Equal(500, container.TotalCount);
        Assert.True(container.IsFull);

        // Attempt deposit into full container -> 0
        int zeroDeposit = CargoTransferSim.Deposit(suit, container, ItemId.SteelBlock, 5);
        Assert.Equal(0, zeroDeposit);
        Assert.Equal(15, suit.GetCount(ItemId.SteelBlock));

        // 5. WithdrawAll when suit has space for only 35 (suit has 15, capacity 50 -> 35 free)
        int clampedWithdraw = CargoTransferSim.WithdrawAll(suit, container);
        Assert.Equal(35, clampedWithdraw);
        Assert.True(suit.IsFull);
        Assert.Equal(50, suit.TotalCount);
        Assert.Equal(465, container.TotalCount);

        // 6. Zero or negative transfer requests do nothing
        int negTransferred = CargoTransferSim.Transfer(suit, container, ItemId.SteelBlock, -5);
        Assert.Equal(0, negTransferred);
        int noneTransferred = CargoTransferSim.Transfer(suit, container, ItemId.None, 10);
        Assert.Equal(0, noneTransferred);
    }
}
