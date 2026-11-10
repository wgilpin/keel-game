using System;
using Keel.Sim.Inventory;

namespace Keel.Sim.Mining;

public sealed class OreDepositState
{
    public ItemId OreType { get; set; } = ItemId.IronOre;
    public int InitialUnits { get; set; }
    public int RemainingUnits { get; set; }
    public bool IsDepleted => RemainingUnits <= 0;

    public OreDepositState()
    {
    }

    public OreDepositState(int initialUnits, ItemId oreType = ItemId.IronOre)
    {
        InitialUnits = Math.Max(0, initialUnits);
        RemainingUnits = InitialUnits;
        OreType = oreType;
    }

    public int Extract(int units)
    {
        if (units <= 0 || IsDepleted)
        {
            return 0;
        }

        int toExtract = Math.Min(units, RemainingUnits);
        RemainingUnits -= toExtract;
        return toExtract;
    }
}
