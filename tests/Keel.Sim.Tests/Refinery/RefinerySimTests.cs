using Keel.Sim.Config;
using Keel.Sim.Inventory;
using Keel.Sim.Refinery;
using Xunit;

namespace Keel.Sim.Tests.Refinery;

public class RefinerySimTests
{
    [Fact]
    public void Step_ConvertsOreAcrossFractionalAndMultiCycleTicksAndStallsWhenStarvedOrOutputFull()
    {
        var config = TuningConfig.Default with
        {
            RefineryCycleSeconds = 2.0f,
            RefineryOrePerBlock = 2.0f,
            RefineryBufferCapacity = 20.0f,
        };

        var state = new RefineryState((int)config.RefineryBufferCapacity);

        // 1. Initial empty state is Starved/Idle, zero progress
        RefinerySim.Step(state, config, 1.0f);
        Assert.Equal(0f, state.CycleProgress);
        Assert.Equal(RefineryStatus.Idle, state.Status);
        Assert.Equal(0, state.OutputInventory.GetCount(ItemId.SteelBlock));

        // 2. Deposit 5 IronOre (enough for 2 cycles, 1 remainder)
        state.InputInventory.Add(ItemId.IronOre, 5);

        // Advance 1.0s (half cycle) -> progress 0.5f, no conversion yet
        RefinerySim.Step(state, config, 1.0f);
        Assert.Equal(0.5f, state.CycleProgress, precision: 3);
        Assert.Equal(RefineryStatus.Refining, state.Status);
        Assert.Equal(5, state.InputInventory.GetCount(ItemId.IronOre));
        Assert.Equal(0, state.OutputInventory.GetCount(ItemId.SteelBlock));

        // Advance another 1.0s (cycle 1 complete) -> 1 SteelBlock, 3 IronOre left, progress 0.0f
        RefinerySim.Step(state, config, 1.0f);
        Assert.Equal(0.0f, state.CycleProgress, precision: 3);
        Assert.Equal(3, state.InputInventory.GetCount(ItemId.IronOre));
        Assert.Equal(1, state.OutputInventory.GetCount(ItemId.SteelBlock));

        // Advance 3.0s (multi-cycle tick: 1.5 cycles) -> cycle 2 completes (1 IronOre left < 2, starved)
        RefinerySim.Step(state, config, 3.0f);
        Assert.Equal(1, state.InputInventory.GetCount(ItemId.IronOre));
        Assert.Equal(2, state.OutputInventory.GetCount(ItemId.SteelBlock));
        Assert.Equal(0.0f, state.CycleProgress);
        Assert.Equal(RefineryStatus.Idle, state.Status);

        // 3. Fill output buffer to capacity (20 blocks)
        state.OutputInventory.Add(ItemId.SteelBlock, 18);
        Assert.True(state.OutputInventory.IsFull);
        Assert.Equal(20, state.OutputInventory.GetCount(ItemId.SteelBlock));

        // Add plenty of ore (10 units -> 1 + 10 = 11, well within capacity 20)
        state.InputInventory.Add(ItemId.IronOre, 10);
        RefinerySim.Step(state, config, 2.0f);

        // Output full stalls refining without consuming ore or advancing timer
        Assert.Equal(RefineryStatus.OutputFull, state.Status);
        Assert.Equal(0.0f, state.CycleProgress);
        Assert.Equal(11, state.InputInventory.GetCount(ItemId.IronOre));
        Assert.Equal(20, state.OutputInventory.GetCount(ItemId.SteelBlock));

        // Withdraw 1 block from output -> should resume immediately
        state.OutputInventory.Remove(ItemId.SteelBlock, 1);
        RefinerySim.Step(state, config, 2.0f);
        Assert.Equal(9, state.InputInventory.GetCount(ItemId.IronOre));
        Assert.Equal(20, state.OutputInventory.GetCount(ItemId.SteelBlock));
    }

    [Fact]
    public void DepositAndWithdraw_RespectItemFiltersAndBothInventoryCapacities()
    {
        var refinery = new RefineryState(bufferCapacity: 20);
        var suit = new SuitInventory(capacity: 10);

        // Suit has 8 IronOre and 2 SteelBlocks (total 10, full)
        suit.Add(ItemId.IronOre, 8);
        suit.Add(ItemId.SteelBlock, 2);
        Assert.Equal(10, suit.TotalCount);

        // Deposit moves only IronOre to refinery, leaving SteelBlock in suit
        int deposited = RefinerySim.DepositFromSuit(refinery, suit);
        Assert.Equal(8, deposited);
        Assert.Equal(8, refinery.InputInventory.GetCount(ItemId.IronOre));
        Assert.Equal(0, refinery.InputInventory.GetCount(ItemId.SteelBlock));
        Assert.Equal(0, suit.GetCount(ItemId.IronOre));
        Assert.Equal(2, suit.GetCount(ItemId.SteelBlock)); // untouched

        // Refinery output has 12 SteelBlocks
        refinery.OutputInventory.Add(ItemId.SteelBlock, 12);

        // Suit has capacity 10 - 2 = 8 available
        int withdrawn = RefinerySim.WithdrawToSuit(refinery, suit);
        Assert.Equal(8, withdrawn);
        Assert.Equal(10, suit.GetCount(ItemId.SteelBlock));
        Assert.True(suit.IsFull);
        Assert.Equal(4, refinery.OutputInventory.GetCount(ItemId.SteelBlock)); // 12 - 8 = 4 remains
    }
}
