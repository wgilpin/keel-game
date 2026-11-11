using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json.Serialization;
using Keel.Sim.Inventory;
using Keel.Sim.Pilot;

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

public sealed record WorldSaveData(
    int Version,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] PilotMode PilotMode,
    KinematicsSaveData Ship,
    SuitSaveData Suit,
    IReadOnlyList<InventoryItemSaveData> SuitInventory);
