using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Keel.Sim.Inventory;
using Keel.Sim.Pilot;

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

        // Validate inventory
        var validatedInventory = new List<InventoryItemSaveData>();
        int totalItems = 0;
        if (parsed.SuitInventory != null)
        {
            foreach (var item in parsed.SuitInventory)
            {
                if (item is null)
                {
                    continue;
                }

                if (!Enum.IsDefined(typeof(ItemId), item.ItemId) || item.ItemId == ItemId.None)
                {
                    return new WorldSaveResult(null, false, $"Invalid item ID in save inventory: {item.ItemId}.");
                }

                if (item.Quantity < 0)
                {
                    return new WorldSaveResult(null, false, $"Negative inventory item quantity for {item.ItemId}: {item.Quantity}.");
                }

                if (item.Quantity > 0)
                {
                    totalItems += item.Quantity;
                    validatedInventory.Add(item);
                }
            }
        }

        const int maxCapacity = 50; // standard SuitInventory capacity
        if (totalItems > maxCapacity)
        {
            return new WorldSaveResult(null, false, $"Suit inventory total items ({totalItems}) exceeds max capacity ({maxCapacity}).");
        }

        var normalizedData = new WorldSaveData(
            Version: parsed.Version,
            PilotMode: parsed.PilotMode,
            Ship: shipValidation.NormalizedKinematics!,
            Suit: new SuitSaveData(suitValidation.NormalizedKinematics!, clampedSlot),
            SuitInventory: validatedInventory);

        return new WorldSaveResult(normalizedData, true, null);
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
