using System;
using System.Collections.Generic;

namespace Keel.Sim.Hud;

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
    IReadOnlyList<HotbarSlot> Slots);

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
        int steelBlockCount = 0)
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

        // 3. Boarding prompt gating
        bool canBoard = suitToShipDistance <= shipInteractRange;
        string? prompt = canBoard ? "[F] Board Ship" : null;

        // 4. Hotbar slot items: [1] Drill, [2] Steel Block, [3]–[5] Empty
        var slots = new[]
        {
            new HotbarSlot(1, "Drill", 1, activeSlot == 1, IsDimmed: false),
            new HotbarSlot(2, "Steel Block", steelBlockCount, activeSlot == 2, IsDimmed: steelBlockCount == 0),
            new HotbarSlot(3, "Empty", 0, activeSlot == 3, IsDimmed: true),
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
            Slots: slots);
    }
}
