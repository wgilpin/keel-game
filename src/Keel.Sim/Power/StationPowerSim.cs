using System;
using Keel.Sim.Config;
using Keel.Sim.Grid;
using Keel.Sim.Inventory;
using Keel.Sim.Refinery;

namespace Keel.Sim.Power;

public enum PowerStatus
{
    Nominal = 0,
    Brownout = 1,
    Unpowered = 2,
}

public readonly record struct StationPowerSnapshot(
    float GeneratedKw,
    float DemandKw,
    float PowerEfficiency,
    PowerStatus Status,
    int SolarPanelCount,
    int CargoContainerCount);

public static class StationPowerSim
{
    public static StationPowerSnapshot Evaluate(
        BlockGrid grid,
        RefineryState? refinery,
        TuningConfig tuning)
    {
        float generatedKw = 0f;
        int solarCount = 0;
        int cargoCount = 0;
        int refineryCount = 0;
        bool hasCore = false;

        foreach (var kvp in grid.Blocks)
        {
            switch (kvp.Value)
            {
                case BlockType.Core:
                    hasCore = true;
                    break;
                case BlockType.SolarPanel:
                    solarCount++;
                    break;
                case BlockType.CargoContainer:
                    cargoCount++;
                    break;
                case BlockType.Refinery:
                    refineryCount++;
                    break;
            }
        }

        if (hasCore)
        {
            generatedKw += tuning.CorePowerKw;
        }
        generatedKw += solarCount * tuning.SolarPanelPowerKw;

        float demandKw = 0f;
        demandKw += cargoCount * tuning.CargoContainerPowerDemandKw;

        if (refinery != null && refineryCount > 0)
        {
            int oreRequired = (int)MathF.Round(tuning.RefineryOrePerBlock);
            if (oreRequired < 1) oreRequired = 1;

            bool canRefine = !refinery.OutputInventory.IsFull &&
                             refinery.InputInventory.GetCount(ItemId.IronOre) >= oreRequired;

            if (canRefine)
            {
                demandKw += refineryCount * tuning.RefineryPowerDemandKw;
            }
        }

        float efficiency;
        PowerStatus status;

        if (demandKw <= 0.0001f)
        {
            efficiency = 1.0f;
            status = PowerStatus.Nominal;
        }
        else if (generatedKw >= demandKw)
        {
            efficiency = 1.0f;
            status = PowerStatus.Nominal;
        }
        else if (generatedKw > 0f)
        {
            efficiency = Math.Clamp(generatedKw / demandKw, 0f, 1f);
            status = PowerStatus.Brownout;
        }
        else
        {
            efficiency = 0f;
            status = PowerStatus.Unpowered;
        }

        return new StationPowerSnapshot(
            GeneratedKw: generatedKw,
            DemandKw: demandKw,
            PowerEfficiency: efficiency,
            Status: status,
            SolarPanelCount: solarCount,
            CargoContainerCount: cargoCount);
    }
}
