using System;
using Keel.Sim.Config;
using Keel.Sim.Inventory;

namespace Keel.Sim.Refinery;

public static class RefinerySim
{
    public static void Step(RefineryState state, TuningConfig config, float deltaSeconds, float powerEfficiency = 1.0f)
    {
        if (deltaSeconds <= 0f)
        {
            return;
        }

        int oreRequired = (int)MathF.Round(config.RefineryOrePerBlock);
        if (oreRequired < 1)
        {
            oreRequired = 1;
        }

        int availableOre = state.InputInventory.GetCount(ItemId.IronOre);

        // Check if output is already full
        if (state.OutputInventory.IsFull)
        {
            state.Status = RefineryStatus.OutputFull;
            state.CycleProgress = 0f;
            return;
        }

        // Check if starved for ore
        if (availableOre < oreRequired)
        {
            state.Status = RefineryStatus.Idle;
            state.CycleProgress = 0f;
            return;
        }

        // Check if unpowered (preserve partial progress and inventory)
        if (powerEfficiency <= 0f)
        {
            state.Status = RefineryStatus.NoPower;
            return;
        }

        // Active refining scaled by power efficiency
        state.Status = RefineryStatus.Refining;
        float cycleDuration = Math.Max(0.01f, config.RefineryCycleSeconds);
        state.CycleProgress += (deltaSeconds * powerEfficiency) / cycleDuration;

        while (state.CycleProgress >= 1.0f)
        {
            if (state.OutputInventory.RemainingCapacity <= 0)
            {
                state.Status = RefineryStatus.OutputFull;
                state.CycleProgress = 0f;
                return;
            }

            if (state.InputInventory.GetCount(ItemId.IronOre) < oreRequired)
            {
                state.Status = RefineryStatus.Idle;
                state.CycleProgress = 0f;
                return;
            }

            // Convert ore to steel block
            state.InputInventory.Remove(ItemId.IronOre, oreRequired);
            state.OutputInventory.Add(ItemId.SteelBlock, 1);
            state.CycleProgress -= 1.0f;
        }

        // If after cycle completions we no longer have enough ore or output buffer is full, reset progress to 0
        if (state.OutputInventory.RemainingCapacity <= 0)
        {
            state.Status = RefineryStatus.OutputFull;
            state.CycleProgress = 0f;
        }
        else if (state.InputInventory.GetCount(ItemId.IronOre) < oreRequired)
        {
            state.Status = RefineryStatus.Idle;
            state.CycleProgress = 0f;
        }
    }

    /// <summary>
    /// Transfers all available IronOre from suit cargo into refinery input buffer.
    /// Returns the number of units transferred. Leaves other items in suit untouched.
    /// </summary>
    public static int DepositFromSuit(RefineryState refinery, SuitInventory suit)
    {
        int availableOre = suit.GetCount(ItemId.IronOre);
        if (availableOre <= 0)
        {
            return 0;
        }

        int space = refinery.InputInventory.RemainingCapacity;
        int toTransfer = Math.Min(availableOre, space);
        if (toTransfer > 0)
        {
            suit.Remove(ItemId.IronOre, toTransfer);
            refinery.InputInventory.Add(ItemId.IronOre, toTransfer);
        }

        return toTransfer;
    }

    /// <summary>
    /// Transfers available SteelBlock items from refinery output buffer into suit cargo.
    /// Returns the number of units transferred.
    /// </summary>
    public static int WithdrawToSuit(RefineryState refinery, SuitInventory suit)
    {
        int availableBlocks = refinery.OutputInventory.GetCount(ItemId.SteelBlock);
        if (availableBlocks <= 0)
        {
            return 0;
        }

        int space = suit.RemainingCapacity;
        int toTransfer = Math.Min(availableBlocks, space);
        if (toTransfer > 0)
        {
            refinery.OutputInventory.Remove(ItemId.SteelBlock, toTransfer);
            suit.Add(ItemId.SteelBlock, toTransfer);
        }

        return toTransfer;
    }
}
