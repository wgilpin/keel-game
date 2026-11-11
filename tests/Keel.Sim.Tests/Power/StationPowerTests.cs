using Keel.Sim.Config;
using Keel.Sim.Grid;
using Keel.Sim.Inventory;
using Keel.Sim.Power;
using Keel.Sim.Refinery;
using Xunit;

namespace Keel.Sim.Tests.Power;

public sealed class StationPowerTests
{
    [Fact]
    public void Evaluate_ComputesGenerationDemandBrownoutAndUnpoweredEfficiency()
    {
        var tuning = TuningConfig.Default with
        {
            CorePowerKw = 25.0f,
            SolarPanelPowerKw = 50.0f,
            RefineryPowerDemandKw = 100.0f,
            CargoContainerPowerDemandKw = 10.0f,
        };

        var grid = StationBlueprint.CreateDefault(); // Has Core (0, 0, 0) and Refinery (0, 0, 3)
        var refinery = new RefineryState();

        // 1. Refinery Idle -> Demand is 0, Generation is 25kW (Core), Nominal 100%
        var powerIdle = StationPowerSim.Evaluate(grid, refinery, tuning);
        Assert.Equal(25.0f, powerIdle.GeneratedKw);
        Assert.Equal(0.0f, powerIdle.DemandKw);
        Assert.Equal(1.0f, powerIdle.PowerEfficiency);
        Assert.Equal(PowerStatus.Nominal, powerIdle.Status);

        // 2. Deposit IronOre into refinery -> Demand becomes 100kW. Generation is 25kW -> Brownout 25% (0.25)
        refinery.InputInventory.Add(ItemId.IronOre, 10);
        var powerBrownout = StationPowerSim.Evaluate(grid, refinery, tuning);
        Assert.Equal(25.0f, powerBrownout.GeneratedKw);
        Assert.Equal(100.0f, powerBrownout.DemandKw);
        Assert.Equal(0.25f, powerBrownout.PowerEfficiency, precision: 3);
        Assert.Equal(PowerStatus.Brownout, powerBrownout.Status);

        // 3. Add 1 Solar Panel (+50kW) -> Generation 75kW / Demand 100kW -> Brownout 75%
        bool placed1 = grid.TryPlaceBlock(new Int3(5, 0, 0), BlockType.SolarPanel, out string? reason1);
        Assert.True(placed1, reason1);
        var powerOneSolar = StationPowerSim.Evaluate(grid, refinery, tuning);
        Assert.Equal(75.0f, powerOneSolar.GeneratedKw);
        Assert.Equal(100.0f, powerOneSolar.DemandKw);
        Assert.Equal(0.75f, powerOneSolar.PowerEfficiency, precision: 3);
        Assert.Equal(PowerStatus.Brownout, powerOneSolar.Status);

        // 4. Add 2nd Solar Panel (+50kW) -> Generation 125kW >= 100kW -> Nominal 100%
        bool placed2 = grid.TryPlaceBlock(new Int3(-5, 0, 0), BlockType.SolarPanel, out string? reason2);
        Assert.True(placed2, reason2);
        var powerTwoSolar = StationPowerSim.Evaluate(grid, refinery, tuning);
        Assert.Equal(125.0f, powerTwoSolar.GeneratedKw);
        Assert.Equal(100.0f, powerTwoSolar.DemandKw);
        Assert.Equal(1.0f, powerTwoSolar.PowerEfficiency);
        Assert.Equal(PowerStatus.Nominal, powerTwoSolar.Status);

        // 5. Zero generation (no solar panels, corePowerKw = 0) -> Unpowered (0%)
        var unpoweredGrid = new BlockGrid();
        unpoweredGrid.TryPlaceBlock(new Int3(0, 0, 0), BlockType.Core, out _);
        unpoweredGrid.TryPlaceBlock(new Int3(0, 0, 1), BlockType.Refinery, out _);
        var zeroCoreTuning = tuning with { CorePowerKw = 0.0f };

        var powerUnpowered = StationPowerSim.Evaluate(unpoweredGrid, refinery, zeroCoreTuning);
        Assert.Equal(0.0f, powerUnpowered.GeneratedKw);
        Assert.Equal(100.0f, powerUnpowered.DemandKw);
        Assert.Equal(0.0f, powerUnpowered.PowerEfficiency);
        Assert.Equal(PowerStatus.Unpowered, powerUnpowered.Status);
    }

    [Fact]
    public void RefineryStep_ScalesProgressWithPowerEfficiencyAndPreservesStateWhenUnpowered()
    {
        var config = TuningConfig.Default with
        {
            RefineryCycleSeconds = 2.0f,
            RefineryOrePerBlock = 2.0f,
        };

        var state = new RefineryState();
        state.InputInventory.Add(ItemId.IronOre, 10);

        // 1. Brownout: powerEfficiency = 0.5f. Nominal cycle is 2.0s.
        // At 0.5 efficiency, advancing 1.0s adds (1.0 * 0.5) / 2.0 = 0.25 progress
        RefinerySim.Step(state, config, deltaSeconds: 1.0f, powerEfficiency: 0.5f);
        Assert.Equal(0.25f, state.CycleProgress, precision: 3);
        Assert.Equal(RefineryStatus.Refining, state.Status);
        Assert.Equal(10, state.InputInventory.GetCount(ItemId.IronOre));
        Assert.Equal(0, state.OutputInventory.GetCount(ItemId.SteelBlock));

        // Advance 3.0s @ 0.5 efficiency -> + (3.0 * 0.5) / 2.0 = +0.75 -> 0.25 + 0.75 = 1.0 (1 batch completes!)
        RefinerySim.Step(state, config, deltaSeconds: 3.0f, powerEfficiency: 0.5f);
        Assert.Equal(0.0f, state.CycleProgress, precision: 3);
        Assert.Equal(8, state.InputInventory.GetCount(ItemId.IronOre));
        Assert.Equal(1, state.OutputInventory.GetCount(ItemId.SteelBlock));

        // 2. Unpowered: powerEfficiency = 0.0f
        // Partial progress: advance 0.5s @ 1.0 efficiency -> 0.25 progress
        RefinerySim.Step(state, config, deltaSeconds: 0.5f, powerEfficiency: 1.0f);
        Assert.Equal(0.25f, state.CycleProgress, precision: 3);

        // Now power drops to 0.0f
        RefinerySim.Step(state, config, deltaSeconds: 2.0f, powerEfficiency: 0.0f);
        Assert.Equal(RefineryStatus.NoPower, state.Status);
        Assert.Equal(0.25f, state.CycleProgress, precision: 3); // Progress preserved!
        Assert.Equal(8, state.InputInventory.GetCount(ItemId.IronOre)); // No ore lost!
        Assert.Equal(1, state.OutputInventory.GetCount(ItemId.SteelBlock));

        // Power restored to 1.0f -> resumes from 0.25
        RefinerySim.Step(state, config, deltaSeconds: 1.5f, powerEfficiency: 1.0f);
        Assert.Equal(RefineryStatus.Refining, state.Status);
        Assert.Equal(0.0f, state.CycleProgress, precision: 3);
        Assert.Equal(6, state.InputInventory.GetCount(ItemId.IronOre));
        Assert.Equal(2, state.OutputInventory.GetCount(ItemId.SteelBlock));
    }
}
