using System;
using System.Collections.Generic;
using System.Numerics;

namespace Keel.Sim.Grid;

public class BlockGrid
{
    public const float CellSize = 2.5f;

    private readonly Dictionary<Int3, BlockType> _blocks = new();

    public IReadOnlyDictionary<Int3, BlockType> Blocks => _blocks;

    public int Count => _blocks.Count;

    public bool HasBlock(Int3 cell) => _blocks.ContainsKey(cell);

    public BlockType GetBlock(Int3 cell) => _blocks.TryGetValue(cell, out var type) ? type : BlockType.None;

    public bool TryPlaceBlock(Int3 cell, BlockType type, out string? reason)
    {
        if (type == BlockType.None)
        {
            reason = "Cannot place BlockType.None.";
            return false;
        }

        if (_blocks.ContainsKey(cell))
        {
            reason = $"Cell {cell} is already occupied by {_blocks[cell]}.";
            return false;
        }

        // Empty grid accepts initial anchor block
        if (_blocks.Count == 0)
        {
            _blocks[cell] = type;
            reason = null;
            return true;
        }

        // Must be adjacent to at least one 6-cardinal face of an existing block
        bool hasAdjacent = false;
        foreach (var offset in Int3.CardinalNeighbors)
        {
            if (_blocks.ContainsKey(cell + offset))
            {
                hasAdjacent = true;
                break;
            }
        }

        if (!hasAdjacent)
        {
            reason = $"Cell {cell} is not adjacent to any existing block face.";
            return false;
        }

        _blocks[cell] = type;
        reason = null;
        return true;
    }

    public bool RemoveBlock(Int3 cell, out BlockType removedType)
    {
        if (_blocks.Remove(cell, out removedType))
        {
            return true;
        }

        removedType = BlockType.None;
        return false;
    }

    public Vector3 GetCellCenter(Int3 cell) => new(
        cell.X * CellSize,
        cell.Y * CellSize,
        cell.Z * CellSize);

    /// <summary>
    /// Maps a station-local raycast hit position and outward face normal to the struck occupied cell.
    /// Inward normal biasing prevents boundary seam float flipping across 2.5m half-cell boundaries.
    /// </summary>
    public bool ResolveHitCell(
        Vector3 localHitPosition,
        Vector3 outwardHitNormal,
        out Int3 cell,
        out BlockType type)
    {
        // Bias inward by 2% of cell size along opposite of hit normal
        Vector3 inwardPoint = localHitPosition - (outwardHitNormal * (CellSize * 0.02f));

        int x = (int)MathF.Round(inwardPoint.X / CellSize);
        int y = (int)MathF.Round(inwardPoint.Y / CellSize);
        int z = (int)MathF.Round(inwardPoint.Z / CellSize);

        cell = new Int3(x, y, z);
        return _blocks.TryGetValue(cell, out type);
    }

    /// <summary>
    /// Verifies that all occupied blocks form a single 6-connected component.
    /// </summary>
    public bool IsConnected()
    {
        if (_blocks.Count <= 1)
        {
            return true;
        }

        var visited = new HashSet<Int3>();
        var queue = new Queue<Int3>();

        using var enumerator = _blocks.Keys.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            return true;
        }

        Int3 start = enumerator.Current;
        visited.Add(start);
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            Int3 current = queue.Dequeue();
            foreach (var offset in Int3.CardinalNeighbors)
            {
                Int3 neighbor = current + offset;
                if (_blocks.ContainsKey(neighbor) && visited.Add(neighbor))
                {
                    queue.Enqueue(neighbor);
                }
            }
        }

        return visited.Count == _blocks.Count;
    }

    /// <summary>
    /// Checks whether removing the specified cell would partition the remaining blocks into disconnected components.
    /// Returns false if the cell is not present or if the grid has 2 or fewer blocks.
    /// </summary>
    public bool WouldDisconnect(Int3 cell)
    {
        if (!_blocks.TryGetValue(cell, out var type))
        {
            return false;
        }

        if (_blocks.Count <= 2)
        {
            return false;
        }

        _blocks.Remove(cell);
        bool connected = IsConnected();
        _blocks[cell] = type;
        return !connected;
    }

    /// <summary>
    /// Replaces the grid's contents with the provided cells if they form a valid connected station containing Core at (0, 0, 0).
    /// </summary>
    public bool TryLoadCells(IEnumerable<KeyValuePair<Int3, BlockType>> cells, out string? error)
    {
        var temp = new Dictionary<Int3, BlockType>();
        bool hasCoreAtZero = false;

        foreach (var kvp in cells)
        {
            if (kvp.Value == BlockType.None)
            {
                error = $"Cannot load BlockType.None at cell {kvp.Key}.";
                return false;
            }

            if (temp.ContainsKey(kvp.Key))
            {
                error = $"Duplicate cell coordinate {kvp.Key}.";
                return false;
            }

            temp[kvp.Key] = kvp.Value;
            if (kvp.Key == Int3.Zero && kvp.Value == BlockType.Core)
            {
                hasCoreAtZero = true;
            }
        }

        if (!hasCoreAtZero)
        {
            error = "Grid must contain BlockType.Core at (0, 0, 0).";
            return false;
        }

        if (!IsDictionaryConnected(temp))
        {
            error = "Loaded grid blocks do not form a single connected component.";
            return false;
        }

        _blocks.Clear();
        foreach (var kvp in temp)
        {
            _blocks[kvp.Key] = kvp.Value;
        }

        error = null;
        return true;
    }

    private static bool IsDictionaryConnected(IReadOnlyDictionary<Int3, BlockType> blocks)
    {
        if (blocks.Count <= 1)
        {
            return true;
        }

        var visited = new HashSet<Int3>();
        var queue = new Queue<Int3>();

        using var enumerator = blocks.Keys.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            return true;
        }

        Int3 start = enumerator.Current;
        visited.Add(start);
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            Int3 current = queue.Dequeue();
            foreach (var offset in Int3.CardinalNeighbors)
            {
                Int3 neighbor = current + offset;
                if (blocks.ContainsKey(neighbor) && visited.Add(neighbor))
                {
                    queue.Enqueue(neighbor);
                }
            }
        }

        return visited.Count == blocks.Count;
    }
}
