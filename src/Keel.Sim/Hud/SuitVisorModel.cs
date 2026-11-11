using System;
using System.Collections.Generic;
using Keel.Sim.Grid;

namespace Keel.Sim.Hud;

public enum PromptTone
{
    None = 0,
    Primary = 1,
    Warning = 2,
    Danger = 3,
    Muted = 4,
}

public readonly record struct HotbarSlot(
    int Index,
    string Name,
    int Count,
    bool IsSelected,
    bool IsDimmed);

public readonly record struct SuitVisorState(
    int ActiveSlot,
    int CargoCount,
    int CargoCapacity,
    float CargoFillRatio,
    bool IsCargoFull,
    bool CanBoardShip,
    string? BoardingPrompt,
    float SpeedMetersPerSecond,
    bool DampenersEnabled,
    IReadOnlyList<HotbarSlot> Slots,
    bool CanOpenRefinery = false,
    string? PromptText = null,
    PromptTone PromptTone = PromptTone.None,
    bool IsTargetOutOfRange = false);

public static class SuitVisorModel
{
    public const int DefaultSlot = 1;
    public const int MinSlot = 1;
    public const int MaxSlot = 5;

    public static SuitVisorState Compute(
        int currentSlot,
        int? slotInput,
        int cargoCount,
        int cargoCapacity,
        float suitToShipDistance,
        float shipInteractRange,
        float currentSpeed,
        bool dampenersEnabled,
        int steelBlockCount = 0,
        BlockType targetedBlockType = BlockType.None,
        float targetedBlockDistance = float.MaxValue,
        float refineryInteractRange = 5.0f,
        PlacementStatus placementStatus = PlacementStatus.NoTarget,
        Mining.DrillStatus drillStatus = Mining.DrillStatus.Idle,
        bool isAimingAtDeposit = false,
        GrinderStatus grinderStatus = GrinderStatus.Idle,
        float grinderRange = 5.0f,
        Persistence.PersistenceStatus persistenceStatus = Persistence.PersistenceStatus.None,
        bool isPersistenceFlashActive = false)
    {
        // 1. Hotbar slot clamping / preservation
        int activeSlot = currentSlot;
        if (activeSlot < MinSlot || activeSlot > MaxSlot)
        {
            activeSlot = DefaultSlot;
        }

        if (slotInput.HasValue && slotInput.Value >= MinSlot && slotInput.Value <= MaxSlot)
        {
            activeSlot = slotInput.Value;
        }

        // 2. Cargo fill ratio clamping and full flag
        float fillRatio;
        if (cargoCapacity <= 0)
        {
            fillRatio = 0.0f;
        }
        else
        {
            fillRatio = Math.Clamp((float)cargoCount / cargoCapacity, 0.0f, 1.0f);
        }

        bool isCargoFull = cargoCapacity > 0 && cargoCount >= cargoCapacity;

        // 3. Prompt prioritization
        bool canBoard = suitToShipDistance <= shipInteractRange;
        bool canOpenRefinery = false;
        string? prompt = null;
        PromptTone promptTone = PromptTone.None;
        bool isTargetOutOfRange = false;

        if (isPersistenceFlashActive && persistenceStatus != Persistence.PersistenceStatus.None)
        {
            switch (persistenceStatus)
            {
                case Persistence.PersistenceStatus.Saved:
                    prompt = "WORLD SAVED";
                    promptTone = PromptTone.Primary;
                    break;
                case Persistence.PersistenceStatus.Loaded:
                    prompt = "WORLD LOADED";
                    promptTone = PromptTone.Primary;
                    break;
                case Persistence.PersistenceStatus.Error:
                    prompt = "SAVE LOAD FAILED";
                    promptTone = PromptTone.Danger;
                    break;
            }
        }
        else if (targetedBlockType == BlockType.Refinery)
        {
            if (targetedBlockDistance <= refineryInteractRange)
            {
                canOpenRefinery = true;
                prompt = "[F] Open Refinery";
                promptTone = PromptTone.Warning;
            }
            else if (targetedBlockDistance <= 25.0f)
            {
                prompt = "OUT OF RANGE";
                promptTone = PromptTone.Warning;
                isTargetOutOfRange = true;
            }
        }
        else if (activeSlot == 2)
        {
            if (steelBlockCount > 0)
            {
                if (placementStatus == PlacementStatus.Valid)
                {
                    prompt = "[LMB] Place Steel Block";
                    promptTone = PromptTone.Primary;
                }
                else if (placementStatus == PlacementStatus.InvalidCell)
                {
                    prompt = "INVALID CELL";
                    promptTone = PromptTone.Danger;
                }
                else if (placementStatus == PlacementStatus.OutOfRange)
                {
                    prompt = "OUT OF RANGE";
                    promptTone = PromptTone.Warning;
                    isTargetOutOfRange = true;
                }
                else if (canBoard)
                {
                    prompt = "[F] Board Ship";
                    promptTone = PromptTone.Primary;
                }
            }
            else
            {
                if (canBoard)
                {
                    prompt = "[F] Board Ship";
                    promptTone = PromptTone.Primary;
                }
                else
                {
                    prompt = "NO STEEL BLOCKS — REFINE ORE AT STATION";
                    promptTone = PromptTone.Warning;
                }
            }
        }
        else if (activeSlot == 1)
        {
            if (isAimingAtDeposit)
            {
                if (drillStatus == Mining.DrillStatus.CargoFull || isCargoFull)
                {
                    prompt = "CARGO FULL";
                    promptTone = PromptTone.Danger;
                }
                else if (drillStatus == Mining.DrillStatus.Depleted)
                {
                    prompt = "DEPLETED";
                    promptTone = PromptTone.Muted;
                }
                else if (drillStatus == Mining.DrillStatus.OutOfRange)
                {
                    prompt = "OUT OF RANGE";
                    promptTone = PromptTone.Warning;
                    isTargetOutOfRange = true;
                }
                else
                {
                    prompt = "[LMB] Drill Iron Ore";
                    promptTone = PromptTone.Warning;
                }
            }
            else if (canBoard)
            {
                prompt = "[F] Board Ship";
                promptTone = PromptTone.Primary;
            }
        }
        else if (activeSlot == 3)
        {
            if (targetedBlockType != BlockType.None)
            {
                if (targetedBlockDistance > grinderRange || grinderStatus == GrinderStatus.OutOfRange)
                {
                    prompt = "OUT OF RANGE";
                    promptTone = PromptTone.Warning;
                    isTargetOutOfRange = true;
                }
                else if (targetedBlockType == BlockType.Core || grinderStatus == GrinderStatus.CannotDeconstructCore)
                {
                    prompt = "CANNOT DECONSTRUCT CORE";
                    promptTone = PromptTone.Danger;
                }
                else if (isCargoFull || grinderStatus == GrinderStatus.CargoFull)
                {
                    prompt = "CARGO FULL";
                    promptTone = PromptTone.Danger;
                }
                else if (grinderStatus == GrinderStatus.WouldSplitGrid)
                {
                    prompt = "CANNOT SPLIT GRID";
                    promptTone = PromptTone.Danger;
                }
                else
                {
                    prompt = "[LMB] Deconstruct Block";
                    promptTone = PromptTone.Primary;
                }
            }
            else if (canBoard)
            {
                prompt = "[F] Board Ship";
                promptTone = PromptTone.Primary;
            }
        }
        else
        {
            if (canBoard)
            {
                prompt = "[F] Board Ship";
                promptTone = PromptTone.Primary;
            }
        }

        // 4. Hotbar slot items: [1] Drill, [2] Steel Block, [3] Grinder, [4]–[5] Empty
        var slots = new[]
        {
            new HotbarSlot(1, "Drill", 1, activeSlot == 1, IsDimmed: false),
            new HotbarSlot(2, "Steel Block", steelBlockCount, activeSlot == 2, IsDimmed: steelBlockCount == 0),
            new HotbarSlot(3, "Grinder", 1, activeSlot == 3, IsDimmed: false),
            new HotbarSlot(4, "Empty", 0, activeSlot == 4, IsDimmed: true),
            new HotbarSlot(5, "Empty", 0, activeSlot == 5, IsDimmed: true),
        };

        return new SuitVisorState(
            ActiveSlot: activeSlot,
            CargoCount: cargoCount,
            CargoCapacity: cargoCapacity,
            CargoFillRatio: fillRatio,
            IsCargoFull: isCargoFull,
            CanBoardShip: canBoard,
            BoardingPrompt: prompt,
            SpeedMetersPerSecond: currentSpeed,
            DampenersEnabled: dampenersEnabled,
            Slots: slots,
            CanOpenRefinery: canOpenRefinery,
            PromptText: prompt,
            PromptTone: promptTone,
            IsTargetOutOfRange: isTargetOutOfRange);
    }
}
