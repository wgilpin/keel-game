using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Keel.Sim.Grid;
using Keel.Sim.Inventory;
using Keel.Sim.Mining;
using Keel.Sim.Pilot;
using Keel.Sim.Refinery;

namespace Keel.Sim.Persistence;

public sealed record WorldSaveResult(
    WorldSaveData? Data,
    bool IsSuccess,
    string? ErrorMessage);

public static class WorldSaveSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        IncludeFields = true,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string Serialize(WorldSaveData data)
    {
        return JsonSerializer.Serialize(data, JsonOptions);
    }

    public static WorldSaveResult Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new WorldSaveResult(null, false, "Save JSON is empty or missing.");
        }

        WorldSaveData? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<WorldSaveData>(json, JsonOptions);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            return new WorldSaveResult(null, false, $"Malformed save JSON: {ex.Message}");
        }

        if (parsed is null)
        {
            return new WorldSaveResult(null, false, "Save JSON deserialized to null.");
        }

        if (parsed.Version < 1)
        {
            return new WorldSaveResult(null, false, $"Unsupported save version: {parsed.Version}.");
        }

        if (!Enum.IsDefined(typeof(PilotMode), parsed.PilotMode))
        {
            return new WorldSaveResult(null, false, $"Invalid pilot mode: {parsed.PilotMode}.");
        }

        if (parsed.Ship is null)
        {
            return new WorldSaveResult(null, false, "Missing ship kinematics in save payload.");
        }

        var shipValidation = ValidateKinematics(parsed.Ship, "Ship");
        if (!shipValidation.IsValid)
        {
            return new WorldSaveResult(null, false, shipValidation.Error);
        }

        if (parsed.Suit is null || parsed.Suit.Kinematics is null)
        {
            return new WorldSaveResult(null, false, "Missing suit kinematics in save payload.");
        }

        var suitValidation = ValidateKinematics(parsed.Suit.Kinematics, "Suit");
        if (!suitValidation.IsValid)
        {
            return new WorldSaveResult(null, false, suitValidation.Error);
        }

        int clampedSlot = Math.Clamp(parsed.Suit.ActiveHotbarSlot, 1, 5);

        // Validate suit inventory
        var suitInvValidation = ValidateItemList(parsed.SuitInventory, 50, "Suit inventory");
        if (!suitInvValidation.IsValid)
        {
            return new WorldSaveResult(null, false, suitInvValidation.Error);
        }

        // Validate station blocks
        List<GridBlockSaveData>? validatedBlocks = null;
        if (parsed.StationBlocks != null)
        {
            var tempGrid = new Dictionary<Int3, BlockType>();
            bool hasCoreAtZero = false;
            validatedBlocks = new List<GridBlockSaveData>();

            foreach (var b in parsed.StationBlocks)
            {
                if (b == null || b.BlockType == BlockType.None || !Enum.IsDefined(typeof(BlockType), b.BlockType))
                {
                    return new WorldSaveResult(null, false, "Invalid or None block type in station blocks.");
                }

                if (tempGrid.ContainsKey(b.Cell))
                {
                    return new WorldSaveResult(null, false, $"Duplicate station block cell: {b.Cell}.");
                }

                tempGrid[b.Cell] = b.BlockType;
                if (b.Cell == Int3.Zero && b.BlockType == BlockType.Core)
                {
                    hasCoreAtZero = true;
                }

                validatedBlocks.Add(b);
            }

            if (!hasCoreAtZero)
            {
                return new WorldSaveResult(null, false, "Station blocks must contain BlockType.Core at (0, 0, 0).");
            }

            var dummyGrid = new BlockGrid();
            if (!dummyGrid.TryLoadCells(tempGrid, out string? gridErr))
            {
                return new WorldSaveResult(null, false, $"Invalid station grid: {gridErr}");
            }
        }

        // Validate refinery
        RefinerySaveData? validatedRefinery = null;
        if (parsed.Refinery != null)
        {
            var inItems = ValidateItemList(parsed.Refinery.InputInventory, 100, "Refinery input");
            if (!inItems.IsValid) return new WorldSaveResult(null, false, inItems.Error);

            var outItems = ValidateItemList(parsed.Refinery.OutputInventory, 100, "Refinery output");
            if (!outItems.IsValid) return new WorldSaveResult(null, false, outItems.Error);

            if (!float.IsFinite(parsed.Refinery.CycleProgress) || parsed.Refinery.CycleProgress < 0f)
            {
                return new WorldSaveResult(null, false, "Refinery cycle progress must be a non-negative finite number.");
            }

            float clampedProgress = Math.Clamp(parsed.Refinery.CycleProgress, 0f, 0.9999f);
            validatedRefinery = new RefinerySaveData(
                inItems.Items!,
                outItems.Items!,
                clampedProgress,
                Enum.IsDefined(typeof(RefineryStatus), parsed.Refinery.Status) ? parsed.Refinery.Status : RefineryStatus.Idle);
        }

        // Validate ore deposits
        List<OreDepositSaveData>? validatedDeposits = null;
        if (parsed.OreDeposits != null)
        {
            validatedDeposits = new List<OreDepositSaveData>();
            foreach (var d in parsed.OreDeposits)
            {
                if (d == null || string.IsNullOrWhiteSpace(d.DepositId))
                {
                    return new WorldSaveResult(null, false, "Ore deposit must have a non-empty depositId.");
                }

                int initUnits = Math.Max(0, d.InitialUnits);
                int remUnits = Math.Clamp(d.RemainingUnits, 0, initUnits);
                validatedDeposits.Add(new OreDepositSaveData(
                    d.DepositId,
                    Enum.IsDefined(typeof(ItemId), d.OreType) ? d.OreType : ItemId.IronOre,
                    initUnits,
                    remUnits));
            }
        }

        var normalizedData = new WorldSaveData(
            Version: parsed.Version,
            PilotMode: parsed.PilotMode,
            Ship: shipValidation.NormalizedKinematics!,
            Suit: new SuitSaveData(suitValidation.NormalizedKinematics!, clampedSlot),
            SuitInventory: suitInvValidation.Items!,
            StationBlocks: validatedBlocks,
            Refinery: validatedRefinery,
            OreDeposits: validatedDeposits);

        return new WorldSaveResult(normalizedData, true, null);
    }

    private static (bool IsValid, string? Error, List<InventoryItemSaveData>? Items) ValidateItemList(
        IReadOnlyList<InventoryItemSaveData>? items,
        int maxCapacity,
        string label)
    {
        var validated = new List<InventoryItemSaveData>();
        int total = 0;
        if (items != null)
        {
            foreach (var item in items)
            {
                if (item == null) continue;
                if (!Enum.IsDefined(typeof(ItemId), item.ItemId) || item.ItemId == ItemId.None)
                {
                    return (false, $"Invalid item ID in {label}: {item.ItemId}.", null);
                }

                if (item.Quantity < 0)
                {
                    return (false, $"Negative item quantity in {label}: {item.Quantity}.", null);
                }

                if (item.Quantity > 0)
                {
                    total += item.Quantity;
                    validated.Add(item);
                }
            }
        }

        if (total > maxCapacity)
        {
            return (false, $"{label} total items ({total}) exceeds capacity ({maxCapacity}).", null);
        }

        return (true, null, validated);
    }

    private static (bool IsValid, string? Error, KinematicsSaveData? NormalizedKinematics) ValidateKinematics(KinematicsSaveData k, string label)
    {
        if (!IsFinite(k.Position))
        {
            return (false, $"{label} position contains non-finite numbers.", null);
        }

        if (!IsFinite(k.LinearVelocity))
        {
            return (false, $"{label} linear velocity contains non-finite numbers.", null);
        }

        if (!IsFinite(k.AngularVelocity))
        {
            return (false, $"{label} angular velocity contains non-finite numbers.", null);
        }

        if (!IsFinite(k.Orientation))
        {
            return (false, $"{label} orientation contains non-finite numbers.", null);
        }

        Quaternion q = k.Orientation;
        float lenSq = q.LengthSquared();
        Quaternion normalizedQ = lenSq > 1e-8f ? Quaternion.Normalize(q) : Quaternion.Identity;

        var normalized = new KinematicsSaveData(
            Position: k.Position,
            Orientation: normalizedQ,
            LinearVelocity: k.LinearVelocity,
            AngularVelocity: k.AngularVelocity,
            DampenersEnabled: k.DampenersEnabled);

        return (true, null, normalized);
    }

    private static bool IsFinite(Vector3 v) =>
        float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);

    private static bool IsFinite(Quaternion q) =>
        float.IsFinite(q.X) && float.IsFinite(q.Y) && float.IsFinite(q.Z) && float.IsFinite(q.W);
}
