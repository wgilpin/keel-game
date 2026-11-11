using Keel.Sim.Inventory;

namespace Keel.Sim.Refinery;

public enum RefineryStatus
{
    Idle = 0,
    Refining = 1,
    Starved = 2,
    OutputFull = 3,
    NoPower = 4,
}

public class RefineryState
{
    public SuitInventory InputInventory { get; }
    public SuitInventory OutputInventory { get; }
    public float CycleProgress { get; set; }
    public RefineryStatus Status { get; set; } = RefineryStatus.Idle;

    public RefineryState(int bufferCapacity = 100)
    {
        InputInventory = new SuitInventory(bufferCapacity);
        OutputInventory = new SuitInventory(bufferCapacity);
    }
}
