using System;
using System.Globalization;
using Keel.Sim.Grid;
using Keel.Sim.Inventory;
using Keel.Sim.Power;

namespace Keel.Sim.Hud;

public readonly record struct CargoTerminalViewModel(
    bool IsOpen,
    Int3 Cell,
    int ContainerOreCount,
    int ContainerBlockCount,
    int ContainerTotalCount,
    int ContainerCapacity,
    float ContainerFillRatio,
    int SuitOreCount,
    int SuitBlockCount,
    int SuitTotalCount,
    int SuitCapacity,
    int SuitRemainingCapacity,
    bool CanDepositOre,
    bool CanWithdrawOre,
    bool CanDepositBlocks,
    bool CanWithdrawBlocks,
    bool CanDepositAll,
    bool CanWithdrawAll,
    string PowerMessage,
    string StatusMessage);

public static class CargoTerminalModel
{
    public static CargoTerminalViewModel Compute(
        Int3 cell,
        SuitInventory container,
        SuitInventory suit,
        StationPowerSnapshot? power,
        bool isOpen)
    {
        int containerOre = container.GetCount(ItemId.IronOre);
        int containerBlocks = container.GetCount(ItemId.SteelBlock);
        int containerTotal = container.TotalCount;
        int containerCap = container.Capacity;
        float fillRatio = containerCap > 0
            ? Math.Clamp((float)containerTotal / containerCap, 0.0f, 1.0f)
            : 0.0f;

        int suitOre = suit.GetCount(ItemId.IronOre);
        int suitBlocks = suit.GetCount(ItemId.SteelBlock);
        int suitTotal = suit.TotalCount;
        int suitCap = suit.Capacity;
        int suitRemaining = suit.RemainingCapacity;
        int containerRemaining = container.RemainingCapacity;

        bool canDepositOre = suitOre > 0 && containerRemaining > 0;
        bool canWithdrawOre = containerOre > 0 && suitRemaining > 0;
        bool canDepositBlocks = suitBlocks > 0 && containerRemaining > 0;
        bool canWithdrawBlocks = containerBlocks > 0 && suitRemaining > 0;
        bool canDepositAll = suitTotal > 0 && containerRemaining > 0;
        bool canWithdrawAll = containerTotal > 0 && suitRemaining > 0;

        string powerMsg = power.HasValue
            ? string.Create(CultureInfo.InvariantCulture, $"PWR  {power.Value.GeneratedKw:F0} / {power.Value.DemandKw:F0} kW ({power.Value.Status.ToString().ToUpperInvariant()})")
            : "PWR  --";

        string statusMsg;
        if (container.IsFull)
        {
            statusMsg = "CONTAINER FULL";
        }
        else if (containerTotal == 0)
        {
            statusMsg = "CONTAINER EMPTY";
        }
        else
        {
            statusMsg = string.Create(CultureInfo.InvariantCulture, $"STORED {containerTotal} / {containerCap}");
        }

        return new CargoTerminalViewModel(
            IsOpen: isOpen,
            Cell: cell,
            ContainerOreCount: containerOre,
            ContainerBlockCount: containerBlocks,
            ContainerTotalCount: containerTotal,
            ContainerCapacity: containerCap,
            ContainerFillRatio: fillRatio,
            SuitOreCount: suitOre,
            SuitBlockCount: suitBlocks,
            SuitTotalCount: suitTotal,
            SuitCapacity: suitCap,
            SuitRemainingCapacity: suitRemaining,
            CanDepositOre: canDepositOre,
            CanWithdrawOre: canWithdrawOre,
            CanDepositBlocks: canDepositBlocks,
            CanWithdrawBlocks: canWithdrawBlocks,
            CanDepositAll: canDepositAll,
            CanWithdrawAll: canWithdrawAll,
            PowerMessage: powerMsg,
            StatusMessage: statusMsg);
    }
}
