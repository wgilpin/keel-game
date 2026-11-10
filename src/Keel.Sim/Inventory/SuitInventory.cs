using System;
using System.Collections.Generic;

namespace Keel.Sim.Inventory;

public class SuitInventory
{
    private readonly Dictionary<ItemId, int> _items = new();

    public int Capacity { get; }

    public SuitInventory(int capacity = 50)
    {
        Capacity = Math.Max(0, capacity);
    }

    public int TotalCount
    {
        get
        {
            int sum = 0;
            foreach (var kvp in _items)
            {
                sum += kvp.Value;
            }
            return sum;
        }
    }

    public int RemainingCapacity => Math.Max(0, Capacity - TotalCount);

    public bool IsFull => TotalCount >= Capacity;

    public bool IsEmpty => TotalCount == 0;

    public int GetCount(ItemId itemId)
    {
        return _items.TryGetValue(itemId, out int count) ? count : 0;
    }

    public int this[ItemId itemId] => GetCount(itemId);

    /// <summary>
    /// Adds quantity of itemId up to remaining capacity.
    /// Returns the unaccepted remainder (0 if all accepted).
    /// Negative or zero quantities leave state unchanged and return 0.
    /// </summary>
    public int Add(ItemId itemId, int quantity)
    {
        if (quantity <= 0 || itemId == ItemId.None)
        {
            return 0;
        }

        int available = RemainingCapacity;
        int toAdd = Math.Min(quantity, available);
        int remainder = quantity - toAdd;

        if (toAdd > 0)
        {
            _items[itemId] = GetCount(itemId) + toAdd;
        }

        return remainder;
    }

    /// <summary>
    /// Removes up to quantity of itemId.
    /// Returns the amount actually removed.
    /// Negative or zero quantities leave state unchanged and return 0.
    /// </summary>
    public int Remove(ItemId itemId, int quantity)
    {
        if (quantity <= 0 || itemId == ItemId.None)
        {
            return 0;
        }

        int current = GetCount(itemId);
        int toRemove = Math.Min(quantity, current);

        if (toRemove > 0)
        {
            int updated = current - toRemove;
            if (updated <= 0)
            {
                _items.Remove(itemId);
            }
            else
            {
                _items[itemId] = updated;
            }
        }

        return toRemove;
    }

    public void Clear()
    {
        _items.Clear();
    }
}

public sealed class Inventory : SuitInventory
{
    public Inventory(int capacity = 50) : base(capacity) { }
}
