using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json.Serialization;
using Keel.Sim.Grid;
using Keel.Sim.Inventory;
using Keel.Sim.Mining;
using Keel.Sim.Pilot;
using Keel.Sim.Refinery;

namespace Keel.Sim.Persistence;

public sealed record KinematicsSaveData(
    Vector3 Position,
    Quaternion Orientation,
    Vector3 LinearVelocity,
    Vector3 AngularVelocity,
    bool DampenersEnabled);

public sealed record SuitSaveData(
    KinematicsSaveData Kinematics,
    int ActiveHotbarSlot);

public sealed record InventoryItemSaveData(
    [property: JsonConverter(typeof(JsonStringEnumConverter))] ItemId ItemId,
    int Quantity);

public sealed record GridBlockSaveData(
    Int3 Cell,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] BlockType BlockType);

public sealed record RefinerySaveData(
    IReadOnlyList<InventoryItemSaveData> InputInventory,
    IReadOnlyList<InventoryItemSaveData> OutputInventory,
    float CycleProgress,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] RefineryStatus Status);

public sealed record OreDepositSaveData(
    string DepositId,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] ItemId OreType,
    int InitialUnits,
    int RemainingUnits);

public sealed record WorldSaveData(
    int Version,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] PilotMode PilotMode,
    KinematicsSaveData Ship,
    SuitSaveData Suit,
    IReadOnlyList<InventoryItemSaveData> SuitInventory,
    IReadOnlyList<GridBlockSaveData>? StationBlocks = null,
    RefinerySaveData? Refinery = null,
    IReadOnlyList<OreDepositSaveData>? OreDeposits = null);
