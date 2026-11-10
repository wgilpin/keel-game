using System;
using Keel.Sim.Config;
using Keel.Sim.Inventory;

namespace Keel.Sim.Mining;

public enum DrillStatus
{
    Idle,
    Drilling,
    OutOfRange,
    Depleted,
    CargoFull,
}

public readonly record struct DrillStepResult(
    DrillStatus Status,
    float Progress,
    int UnitsExtracted,
    bool BeamActive);

public static class DrillSim
{
    public static DrillStepResult Step(
        OreDepositState? deposit,
        SuitInventory inventory,
        bool isFiring,
        int activeSlot,
        float hitDistance,
        float drillRange,
        float drillRate,
        float deltaSeconds,
        ref float progress)
    {
        var result = Step(deposit, inventory, isFiring, activeSlot, hitDistance, drillRange, drillRate, deltaSeconds, progress);
        progress = result.Progress;
        return result;
    }

    public static DrillStepResult Step(
        OreDepositState? deposit,
        SuitInventory inventory,
        bool isFiring,
        int activeSlot,
        float hitDistance,
        TuningConfig tuning,
        float deltaSeconds,
        ref float progress)
    {
        var result = Step(deposit, inventory, isFiring, activeSlot, hitDistance, tuning.DrillRange, tuning.DrillRate, deltaSeconds, progress);
        progress = result.Progress;
        return result;
    }

    public static DrillStepResult Step(
        OreDepositState? deposit,
        SuitInventory inventory,
        bool isFiring,
        int activeSlot,
        float hitDistance,
        float drillRange,
        float drillRate,
        float deltaSeconds,
        float currentProgress = 0f)
    {
        // 1. Slot check: only [1] Drill can extract ore
        if (activeSlot != 1)
        {
            return new DrillStepResult(DrillStatus.Idle, Progress: 0f, UnitsExtracted: 0, BeamActive: false);
        }

        // 2. Deposit check
        if (deposit == null)
        {
            return new DrillStepResult(DrillStatus.Idle, Progress: 0f, UnitsExtracted: 0, BeamActive: false);
        }

        // 3. Range check
        if (hitDistance > drillRange)
        {
            return new DrillStepResult(DrillStatus.OutOfRange, Progress: 0f, UnitsExtracted: 0, BeamActive: false);
        }

        // 4. Depleted check
        if (deposit.RemainingUnits <= 0)
        {
            return new DrillStepResult(DrillStatus.Depleted, Progress: 0f, UnitsExtracted: 0, BeamActive: false);
        }

        // 5. CargoFull check
        if (inventory.IsFull)
        {
            return new DrillStepResult(DrillStatus.CargoFull, Progress: 0f, UnitsExtracted: 0, BeamActive: false);
        }

        // 6. Firing check
        if (!isFiring || deltaSeconds <= 0f || drillRate <= 0f)
        {
            return new DrillStepResult(DrillStatus.Idle, Progress: 0f, UnitsExtracted: 0, BeamActive: false);
        }

        // Active extraction
        float totalProgress = currentProgress + (deltaSeconds * drillRate);
        int wholeUnits = (int)MathF.Floor(totalProgress);
        float newProgress = totalProgress - wholeUnits;

        int unitsExtracted = 0;
        if (wholeUnits > 0)
        {
            int canExtract = Math.Min(wholeUnits, Math.Min(deposit.RemainingUnits, inventory.RemainingCapacity));
            if (canExtract > 0)
            {
                int extracted = deposit.Extract(canExtract);
                int unaccepted = inventory.Add(deposit.OreType, extracted);
                unitsExtracted = extracted - unaccepted;
            }
        }

        if (deposit.RemainingUnits <= 0)
        {
            return new DrillStepResult(DrillStatus.Depleted, Progress: 0f, UnitsExtracted: unitsExtracted, BeamActive: false);
        }

        if (inventory.IsFull)
        {
            return new DrillStepResult(DrillStatus.CargoFull, Progress: 0f, UnitsExtracted: unitsExtracted, BeamActive: false);
        }

        return new DrillStepResult(DrillStatus.Drilling, Progress: newProgress, UnitsExtracted: unitsExtracted, BeamActive: true);
    }
}
