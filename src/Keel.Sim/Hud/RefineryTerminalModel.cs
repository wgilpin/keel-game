using System;
using System.Globalization;
using Keel.Sim.Inventory;
using Keel.Sim.Refinery;

namespace Keel.Sim.Hud;

public readonly record struct RefineryTerminalViewModel(
    bool IsOpen,
    int InputOreCount,
    int InputOreCapacity,
    int OutputBlockCount,
    int OutputBlockCapacity,
    int SuitOreCount,
    int SuitBlockCount,
    int SuitRemainingCapacity,
    RefineryStatus Status,
    float CycleProgress,
    string StatusMessage,
    bool CanDeposit,
    bool CanWithdraw);

public static class RefineryTerminalModel
{
    public static RefineryTerminalViewModel Compute(
        RefineryState refinery,
        SuitInventory suit,
        bool isOpen,
        float cycleDurationSeconds)
    {
        int inputOre = refinery.InputInventory.GetCount(ItemId.IronOre);
        int inputCapacity = refinery.InputInventory.Capacity;
        int outputBlocks = refinery.OutputInventory.GetCount(ItemId.SteelBlock);
        int outputCapacity = refinery.OutputInventory.Capacity;

        int suitOre = suit.GetCount(ItemId.IronOre);
        int suitBlocks = suit.GetCount(ItemId.SteelBlock);
        int suitRemaining = suit.RemainingCapacity;

        bool canDeposit = suitOre > 0 && refinery.InputInventory.RemainingCapacity > 0;
        bool canWithdraw = outputBlocks > 0 && suitRemaining > 0;

        float progress = Math.Clamp(refinery.CycleProgress, 0.0f, 1.0f);
        float elapsedSeconds = progress * cycleDurationSeconds;

        string message;
        switch (refinery.Status)
        {
            case RefineryStatus.Refining:
                message = string.Create(CultureInfo.InvariantCulture, $"REFINING {elapsedSeconds:F1}s / {cycleDurationSeconds:F1}s");
                break;
            case RefineryStatus.OutputFull:
                message = "OUTPUT FULL — [R] WITHDRAW";
                break;
            case RefineryStatus.Starved:
            case RefineryStatus.Idle:
            default:
                message = canDeposit ? "IDLE — [E] DEPOSIT IRON ORE" : "IDLE — NO IRON ORE AVAILABLE";
                break;
        }

        return new RefineryTerminalViewModel(
            IsOpen: isOpen,
            InputOreCount: inputOre,
            InputOreCapacity: inputCapacity,
            OutputBlockCount: outputBlocks,
            OutputBlockCapacity: outputCapacity,
            SuitOreCount: suitOre,
            SuitBlockCount: suitBlocks,
            SuitRemainingCapacity: suitRemaining,
            Status: refinery.Status,
            CycleProgress: progress,
            StatusMessage: message,
            CanDeposit: canDeposit,
            CanWithdraw: canWithdraw);
    }
}
