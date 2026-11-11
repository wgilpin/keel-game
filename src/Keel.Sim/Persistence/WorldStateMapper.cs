using System;
using System.Collections.Generic;
using Keel.Sim.Inventory;
using Keel.Sim.Pilot;

namespace Keel.Sim.Persistence;

public static class WorldStateMapper
{
    public const int CurrentVersion = 1;

    public static WorldSaveData CreateSnapshot(
        PilotMode mode,
        KinematicsSaveData shipKinematics,
        KinematicsSaveData suitKinematics,
        int activeSuitSlot,
        SuitInventory inventory)
    {
        var items = new List<InventoryItemSaveData>();
        foreach (ItemId id in Enum.GetValues<ItemId>())
        {
            if (id == ItemId.None) continue;
            int count = inventory.GetCount(id);
            if (count > 0)
            {
                items.Add(new InventoryItemSaveData(id, count));
            }
        }

        return new WorldSaveData(
            Version: CurrentVersion,
            PilotMode: mode,
            Ship: shipKinematics,
            Suit: new SuitSaveData(suitKinematics, activeSuitSlot),
            SuitInventory: items);
    }

    public static void Apply(
        WorldSaveData save,
        PilotModeStateMachine pilotMachine,
        SuitInventory inventory)
    {
        pilotMachine.Reset(save.PilotMode);

        inventory.Clear();
        if (save.SuitInventory != null)
        {
            foreach (var item in save.SuitInventory)
            {
                if (item.ItemId != ItemId.None && item.Quantity > 0)
                {
                    inventory.Add(item.ItemId, item.Quantity);
                }
            }
        }
    }
}
