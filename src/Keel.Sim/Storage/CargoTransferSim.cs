using System;
using Keel.Sim.Inventory;

namespace Keel.Sim.Storage;

public static class CargoTransferSim
{
    public static int Transfer(SuitInventory source, SuitInventory destination, ItemId itemId, int requestedQuantity)
    {
        if (itemId == ItemId.None || requestedQuantity <= 0)
        {
            return 0;
        }

        int available = source.GetCount(itemId);
        if (available <= 0)
        {
            return 0;
        }

        int capacity = destination.RemainingCapacity;
        int toTransfer = Math.Min(requestedQuantity, Math.Min(available, capacity));
        if (toTransfer <= 0)
        {
            return 0;
        }

        int removed = source.Remove(itemId, toTransfer);
        int remainder = destination.Add(itemId, removed);
        if (remainder > 0)
        {
            source.Add(itemId, remainder);
            return removed - remainder;
        }

        return removed;
    }

    public static int TransferAll(SuitInventory source, SuitInventory destination)
    {
        int totalTransferred = 0;
        foreach (ItemId id in Enum.GetValues<ItemId>())
        {
            if (id == ItemId.None) continue;
            if (destination.IsFull) break;

            int count = source.GetCount(id);
            if (count > 0)
            {
                int transferred = Transfer(source, destination, id, count);
                totalTransferred += transferred;
            }
        }
        return totalTransferred;
    }

    public static int Deposit(SuitInventory suit, SuitInventory container, ItemId itemId, int count) =>
        Transfer(suit, container, itemId, count);

    public static int Withdraw(SuitInventory suit, SuitInventory container, ItemId itemId, int count) =>
        Transfer(container, suit, itemId, count);

    public static int DepositAll(SuitInventory suit, SuitInventory container) =>
        TransferAll(suit, container);

    public static int WithdrawAll(SuitInventory suit, SuitInventory container) =>
        TransferAll(container, suit);
}
