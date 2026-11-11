using System;
using Keel.Sim.Config;
using Keel.Sim.Inventory;

namespace Keel.Sim.Grid;

public enum GrinderStatus
{
    Idle,
    Deconstructing,
    OutOfRange,
    CannotDeconstructCore,
    CargoFull,
    WouldSplitGrid,
    NoTarget,
}

public readonly record struct GrinderStepResult(
    GrinderStatus Status,
    float Progress,
    bool BlockDeconstructed,
    Int3? DeconstructedCell,
    ItemId? RefundedItem,
    int UnitsRefunded,
    bool BeamActive);

public static class GrinderSim
{
    public const int GrinderSlotIndex = 3;

    public static GrinderStepResult Step(
        BlockGrid grid,
        SuitInventory inventory,
        Int3? targetCell,
        BlockType targetType,
        bool isFiring,
        int activeSlot,
        float hitDistance,
        float grinderRange,
        float grinderRate,
        float deltaSeconds,
        ref float progress,
        ref Int3? lastTargetCell)
    {
        var result = Step(
            grid,
            inventory,
            targetCell,
            targetType,
            isFiring,
            activeSlot,
            hitDistance,
            grinderRange,
            grinderRate,
            deltaSeconds,
            progress,
            lastTargetCell);

        progress = result.Progress;
        lastTargetCell = targetCell;
        return result;
    }

    public static GrinderStepResult Step(
        BlockGrid grid,
        SuitInventory inventory,
        Int3? targetCell,
        BlockType targetType,
        bool isFiring,
        int activeSlot,
        float hitDistance,
        TuningConfig tuning,
        float deltaSeconds,
        ref float progress,
        ref Int3? lastTargetCell)
    {
        return Step(
            grid,
            inventory,
            targetCell,
            targetType,
            isFiring,
            activeSlot,
            hitDistance,
            tuning.GrinderRange,
            tuning.GrinderRate,
            deltaSeconds,
            ref progress,
            ref lastTargetCell);
    }

    public static GrinderStepResult Step(
        BlockGrid grid,
        SuitInventory inventory,
        Int3? targetCell,
        BlockType targetType,
        bool isFiring,
        int activeSlot,
        float hitDistance,
        float grinderRange,
        float grinderRate,
        float deltaSeconds,
        float currentProgress = 0f,
        Int3? lastTargetCell = null)
    {
        // 1. Slot check: only [3] Grinder can deconstruct blocks
        if (activeSlot != GrinderSlotIndex)
        {
            return new GrinderStepResult(GrinderStatus.Idle, Progress: 0f, BlockDeconstructed: false, DeconstructedCell: null, RefundedItem: null, UnitsRefunded: 0, BeamActive: false);
        }

        // 2. Target check
        if (!targetCell.HasValue || targetType == BlockType.None || !grid.HasBlock(targetCell.Value))
        {
            return new GrinderStepResult(GrinderStatus.NoTarget, Progress: 0f, BlockDeconstructed: false, DeconstructedCell: null, RefundedItem: null, UnitsRefunded: 0, BeamActive: false);
        }

        // Reset progress if target cell changed
        float activeProgress = currentProgress;
        if (lastTargetCell.HasValue && lastTargetCell.Value != targetCell.Value)
        {
            activeProgress = 0f;
        }

        // 3. Core block protection
        if (targetType == BlockType.Core)
        {
            return new GrinderStepResult(GrinderStatus.CannotDeconstructCore, Progress: 0f, BlockDeconstructed: false, DeconstructedCell: null, RefundedItem: null, UnitsRefunded: 0, BeamActive: false);
        }

        // 4. Range check
        if (hitDistance > grinderRange)
        {
            return new GrinderStepResult(GrinderStatus.OutOfRange, Progress: 0f, BlockDeconstructed: false, DeconstructedCell: null, RefundedItem: null, UnitsRefunded: 0, BeamActive: false);
        }

        int refundCount = BlockCosts.GetSteelBlockCost(targetType);
        if (refundCount <= 0)
        {
            refundCount = 1;
        }

        // 5. CargoFull check
        if (inventory.RemainingCapacity < refundCount)
        {
            return new GrinderStepResult(GrinderStatus.CargoFull, Progress: 0f, BlockDeconstructed: false, DeconstructedCell: null, RefundedItem: null, UnitsRefunded: 0, BeamActive: false);
        }

        // 6. Grid split / disconnection check
        if (grid.WouldDisconnect(targetCell.Value))
        {
            return new GrinderStepResult(GrinderStatus.WouldSplitGrid, Progress: 0f, BlockDeconstructed: false, DeconstructedCell: null, RefundedItem: null, UnitsRefunded: 0, BeamActive: false);
        }

        // 7. Firing check
        if (!isFiring || deltaSeconds <= 0f || grinderRate <= 0f)
        {
            return new GrinderStepResult(GrinderStatus.Idle, Progress: activeProgress, BlockDeconstructed: false, DeconstructedCell: null, RefundedItem: null, UnitsRefunded: 0, BeamActive: false);
        }

        // 8. Progress accumulation
        float newProgress = activeProgress + (deltaSeconds * grinderRate);
        if (newProgress >= 1.0f)
        {
            grid.RemoveBlock(targetCell.Value, out _);
            inventory.Add(ItemId.SteelBlock, refundCount);

            return new GrinderStepResult(
                Status: GrinderStatus.Idle,
                Progress: 0f,
                BlockDeconstructed: true,
                DeconstructedCell: targetCell.Value,
                RefundedItem: ItemId.SteelBlock,
                UnitsRefunded: refundCount,
                BeamActive: false);
        }

        return new GrinderStepResult(
            Status: GrinderStatus.Deconstructing,
            Progress: newProgress,
            BlockDeconstructed: false,
            DeconstructedCell: null,
            RefundedItem: null,
            UnitsRefunded: 0,
            BeamActive: true);
    }
}
