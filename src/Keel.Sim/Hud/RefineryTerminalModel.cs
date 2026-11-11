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
    bool CanWithdraw,
    string PowerMessage = "");

public static class RefineryTerminalModel
{
    public static RefineryTerminalViewModel Compute(
        RefineryState refinery,
        SuitInventory suit,
        bool isOpen,
        float cycleDurationSeconds,
        Power.StationPowerSnapshot? power = null)
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
                if (power.HasValue && power.Value.Status == Power.PowerStatus.Brownout)
                {
                    int effPct = (int)MathF.Round(power.Value.PowerEfficiency * 100f);
                    message = string.Create(CultureInfo.InvariantCulture, $"REFINING {elapsedSeconds:F1}s / {cycleDurationSeconds:F1}s (BROWNOUT {effPct}%)");
                }
                else
                {
                    message = string.Create(CultureInfo.InvariantCulture, $"REFINING {elapsedSeconds:F1}s / {cycleDurationSeconds:F1}s");
                }
                break;
            case RefineryStatus.NoPower:
                message = "NO POWER — ADD SOLAR PANELS TO STATION";
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

        string powerMsg = power.HasValue
            ? string.Create(CultureInfo.InvariantCulture, $"PWR  {power.Value.GeneratedKw:F0} / {power.Value.DemandKw:F0} kW ({power.Value.Status.ToString().ToUpperInvariant()})")
            : "PWR  --";

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
            CanWithdraw: canWithdraw,
            PowerMessage: powerMsg);
    }
}
