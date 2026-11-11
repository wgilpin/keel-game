using System;
using System.Collections.Generic;
using Keel.Sim.Grid;
using Keel.Sim.Inventory;
using Keel.Sim.Mining;
using Keel.Sim.Pilot;
using Keel.Sim.Refinery;
using Keel.Sim.Storage;

namespace Keel.Sim.Persistence;

public static class WorldStateMapper
{
    public const int CurrentVersion = 1;

    public static WorldSaveData CreateSnapshot(
        PilotMode mode,
        KinematicsSaveData shipKinematics,
        KinematicsSaveData suitKinematics,
        int activeSuitSlot,
        SuitInventory inventory,
        BlockGrid? stationGrid = null,
        RefineryState? refinery = null,
        IReadOnlyList<OreDepositState>? oreDeposits = null,
        StationCargoStore? cargoStore = null)
    {
        var items = MapInventory(inventory);

        List<GridBlockSaveData>? blocks = null;
        if (stationGrid != null)
        {
            blocks = new List<GridBlockSaveData>();
            foreach (var kvp in stationGrid.Blocks)
            {
                blocks.Add(new GridBlockSaveData(kvp.Key, kvp.Value));
            }
        }

        RefinerySaveData? refineryData = null;
        if (refinery != null)
        {
            refineryData = new RefinerySaveData(
                MapInventory(refinery.InputInventory),
                MapInventory(refinery.OutputInventory),
                refinery.CycleProgress,
                refinery.Status);
        }

        List<OreDepositSaveData>? deposits = null;
        if (oreDeposits != null)
        {
            deposits = new List<OreDepositSaveData>();
            foreach (var d in oreDeposits)
            {
                deposits.Add(new OreDepositSaveData(
                    d.DepositId,
                    d.OreType,
                    d.InitialUnits,
                    d.RemainingUnits));
            }
        }

        List<CargoContainerSaveData>? cargoData = null;
        if (cargoStore != null && cargoStore.ContainerCount > 0)
        {
            cargoData = new List<CargoContainerSaveData>();
            foreach (var kvp in cargoStore.Containers)
            {
                var containerItems = MapInventory(kvp.Value);
                cargoData.Add(new CargoContainerSaveData(kvp.Key, containerItems));
            }
        }

        return new WorldSaveData(
            Version: CurrentVersion,
            PilotMode: mode,
            Ship: shipKinematics,
            Suit: new SuitSaveData(suitKinematics, activeSuitSlot),
            SuitInventory: items,
            StationBlocks: blocks,
            Refinery: refineryData,
            OreDeposits: deposits,
            StationCargoContainers: cargoData);
    }

    private static List<InventoryItemSaveData> MapInventory(SuitInventory inventory)
    {
        var items = new List<InventoryItemSaveData>();
        foreach (ItemId id in Enum.GetValues<ItemId>())
        {
            if (id == ItemId.None) continue;
            int count = inventory.GetCount(id);
            if (count > 0)
            {
                items.Add(new InventoryItemSaveData(id, count));
            }
        }
        return items;
    }

    public static void Apply(
        WorldSaveData save,
        PilotModeStateMachine pilotMachine,
        SuitInventory inventory,
        BlockGrid? stationGrid = null,
        RefineryState? refinery = null,
        IEnumerable<OreDepositState>? oreDeposits = null,
        StationCargoStore? cargoStore = null)
    {
        pilotMachine.Reset(save.PilotMode);

        inventory.Clear();
        if (save.SuitInventory != null)
        {
            foreach (var item in save.SuitInventory)
            {
                if (item.ItemId != ItemId.None && item.Quantity > 0)
                {
                    inventory.Add(item.ItemId, item.Quantity);
                }
            }
        }

        if (stationGrid != null)
        {
            ApplyStationBlocks(save, stationGrid);
        }

        if (refinery != null)
        {
            ApplyRefinery(save, refinery);
        }

        if (oreDeposits != null)
        {
            ApplyOreDeposits(save, oreDeposits);
        }

        if (cargoStore != null)
        {
            ApplyStationCargo(save, cargoStore);
        }
    }

    public static void ApplyStationCargo(WorldSaveData save, StationCargoStore cargoStore)
    {
        cargoStore.Clear();

        if (save.StationBlocks != null)
        {
            foreach (var b in save.StationBlocks)
            {
                if (b.BlockType == BlockType.CargoContainer)
                {
                    cargoStore.RegisterContainer(b.Cell, 500);
                }
            }
        }

        if (save.StationCargoContainers != null)
        {
            foreach (var c in save.StationCargoContainers)
            {
                var container = cargoStore.RegisterContainer(c.Cell, 500);
                container.Clear();
                if (c.Items != null)
                {
                    foreach (var item in c.Items)
                    {
                        if (item.ItemId != ItemId.None && item.Quantity > 0)
                        {
                            container.Add(item.ItemId, item.Quantity);
                        }
                    }
                }
            }
        }
    }

    public static bool ApplyStationBlocks(WorldSaveData save, BlockGrid grid)
    {
        if (save.StationBlocks == null) return false;
        var cells = new Dictionary<Int3, BlockType>();
        foreach (var b in save.StationBlocks)
        {
            cells[b.Cell] = b.BlockType;
        }
        return grid.TryLoadCells(cells, out _);
    }

    public static void ApplyRefinery(WorldSaveData save, RefineryState refinery)
    {
        if (save.Refinery == null) return;

        refinery.InputInventory.Clear();
        foreach (var item in save.Refinery.InputInventory)
        {
            if (item.ItemId != ItemId.None && item.Quantity > 0)
            {
                refinery.InputInventory.Add(item.ItemId, item.Quantity);
            }
        }

        refinery.OutputInventory.Clear();
        foreach (var item in save.Refinery.OutputInventory)
        {
            if (item.ItemId != ItemId.None && item.Quantity > 0)
            {
                refinery.OutputInventory.Add(item.ItemId, item.Quantity);
            }
        }

        refinery.CycleProgress = save.Refinery.CycleProgress;
        refinery.Status = save.Refinery.Status;
    }

    public static void ApplyOreDeposits(WorldSaveData save, IEnumerable<OreDepositState> deposits)
    {
        if (save.OreDeposits == null) return;

        var saveLookup = new Dictionary<string, OreDepositSaveData>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in save.OreDeposits)
        {
            if (!string.IsNullOrEmpty(d.DepositId))
            {
                saveLookup[d.DepositId] = d;
            }
        }

        foreach (var dep in deposits)
        {
            if (saveLookup.TryGetValue(dep.DepositId, out var saved))
            {
                dep.InitialUnits = saved.InitialUnits;
                dep.RemainingUnits = Math.Clamp(saved.RemainingUnits, 0, dep.InitialUnits);
                dep.OreType = saved.OreType;
            }
        }
    }
}
