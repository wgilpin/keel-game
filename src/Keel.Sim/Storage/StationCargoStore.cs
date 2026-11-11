using System;
using System.Collections.Generic;
using Keel.Sim.Grid;
using Keel.Sim.Inventory;

namespace Keel.Sim.Storage;

public sealed class StationCargoStore
{
    private readonly Dictionary<Int3, SuitInventory> _containers = new();

    public int DefaultCapacity { get; }

    public StationCargoStore(int defaultCapacity = 500)
    {
        DefaultCapacity = Math.Max(0, defaultCapacity);
    }

    public int ContainerCount => _containers.Count;

    public int TotalItemsStored
    {
        get
        {
            int sum = 0;
            foreach (var inv in _containers.Values)
            {
                sum += inv.TotalCount;
            }
            return sum;
        }
    }

    public int TotalCapacity
    {
        get
        {
            int sum = 0;
            foreach (var inv in _containers.Values)
            {
                sum += inv.Capacity;
            }
            return sum;
        }
    }

    public IReadOnlyDictionary<Int3, SuitInventory> Containers => _containers;

    public bool HasContainer(Int3 cell) => _containers.ContainsKey(cell);

    public SuitInventory RegisterContainer(Int3 cell, int? capacity = null)
    {
        if (_containers.TryGetValue(cell, out var existing))
        {
            return existing;
        }

        var inv = new SuitInventory(capacity ?? DefaultCapacity);
        _containers[cell] = inv;
        return inv;
    }

    public bool TryGetContainer(Int3 cell, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out SuitInventory? container)
    {
        return _containers.TryGetValue(cell, out container);
    }

    public SuitInventory? GetContainer(Int3 cell)
    {
        return _containers.TryGetValue(cell, out var container) ? container : null;
    }

    public bool UnregisterContainer(Int3 cell, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out SuitInventory? removed)
    {
        return _containers.Remove(cell, out removed);
    }

    public void Clear()
    {
        _containers.Clear();
    }
}
