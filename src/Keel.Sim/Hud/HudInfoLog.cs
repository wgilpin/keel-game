using System.Collections.Generic;

namespace Keel.Sim.Hud;

public sealed class HudInfoLog
{
    public const int MaxEntries = 3;
    private readonly List<string> _entries = new(MaxEntries);

    public IReadOnlyList<string> Entries => _entries;

    public void Push(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (_entries.Count >= MaxEntries)
        {
            _entries.RemoveAt(0);
        }

        _entries.Add(message.Trim());
    }

    public void Clear() => _entries.Clear();
}
