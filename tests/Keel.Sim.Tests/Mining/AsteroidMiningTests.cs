using System;
using System.Numerics;
using Keel.Sim.Config;
using Keel.Sim.Inventory;
using Keel.Sim.Mining;
using Keel.Sim.Pilot;
using Xunit;

namespace Keel.Sim.Tests.Mining;

public sealed class AsteroidMiningTests
{
    [Fact]
    public void Inventory_EnforcesCapacityAndPartialOverflowTransfers()
    {
        // AC-1: Given a capacity-bounded Inventory, when ItemId quantities are added and removed
        // across empty, partial, and near-capacity states, then counts stay within [0, Capacity],
        // near-full additions accept only the fitting delta while returning the unaccepted remainder,
        // and negative or zero transfer amounts leave state unchanged.

        const int capacity = 50;
        var inventory = new SuitInventory(capacity);

        // Initial invariants
        Assert.Equal(capacity, inventory.Capacity);
        Assert.Equal(0, inventory.TotalCount);
        Assert.Equal(capacity, inventory.RemainingCapacity);
        Assert.True(inventory.IsEmpty);
        Assert.False(inventory.IsFull);

        // Non-positive addition leaves state unchanged and returns 0 unaccepted remainder
        int remainderZero = inventory.Add(ItemId.IronOre, 0);
        Assert.Equal(0, remainderZero);
        Assert.Equal(0, inventory.TotalCount);

        int remainderNeg = inventory.Add(ItemId.IronOre, -10);
        Assert.Equal(0, remainderNeg);
        Assert.Equal(0, inventory.TotalCount);

        // Partial addition
        int rem1 = inventory.Add(ItemId.IronOre, 30);
        Assert.Equal(0, rem1);
        Assert.Equal(30, inventory.TotalCount);
        Assert.Equal(30, inventory.GetCount(ItemId.IronOre));
        Assert.Equal(20, inventory.RemainingCapacity);
        Assert.False(inventory.IsFull);
        Assert.False(inventory.IsEmpty);

        // Near-capacity addition accepting only the fitting delta and returning unaccepted remainder
        int rem2 = inventory.Add(ItemId.IronOre, 30); // only 20 fits, 10 remainder
        Assert.Equal(10, rem2);
        Assert.Equal(50, inventory.TotalCount);
        Assert.Equal(50, inventory.GetCount(ItemId.IronOre));
        Assert.Equal(0, inventory.RemainingCapacity);
        Assert.True(inventory.IsFull);

        // Addition when full returns all as remainder
        int rem3 = inventory.Add(ItemId.IronOre, 5);
        Assert.Equal(5, rem3);
        Assert.Equal(50, inventory.TotalCount);

        // Non-positive removal leaves state unchanged
        int removedZero = inventory.Remove(ItemId.IronOre, 0);
        Assert.Equal(0, removedZero);
        Assert.Equal(50, inventory.TotalCount);

        int removedNeg = inventory.Remove(ItemId.IronOre, -5);
        Assert.Equal(0, removedNeg);
        Assert.Equal(50, inventory.TotalCount);

        // Partial removal
        int remPartial = inventory.Remove(ItemId.IronOre, 20);
        Assert.Equal(20, remPartial);
        Assert.Equal(30, inventory.TotalCount);
        Assert.Equal(30, inventory.GetCount(ItemId.IronOre));

        // Excess removal removes only up to available and stays at 0
        int remExcess = inventory.Remove(ItemId.IronOre, 100);
        Assert.Equal(30, remExcess);
        Assert.Equal(0, inventory.TotalCount);
        Assert.Equal(0, inventory.GetCount(ItemId.IronOre));
        Assert.True(inventory.IsEmpty);
    }

    [Fact]
    public void DrillSim_ExtractsWholeOreUnitsAcrossFractionalTicksUntilDepleted()
    {
        // AC-2: Given an OreDepositState with positive RemainingUnits, a suit Inventory with free space,
        // [1] Drill selected, and a target hit distance <= drillRange, when DrillSim.Step advances across
        // fractional and multi-unit deltaSeconds * drillRate ticks with fire held, then fractional progress
        // wraps in [0, 1) and whole IronOre units transfer 1-for-1 from the deposit into Inventory until
        // the deposit reaches 0 (Depleted).

        const float drillRange = 5.0f;
        const float drillRate = 1.5f; // 1.5 units per second
        var deposit = new OreDepositState(initialUnits: 5, oreType: ItemId.IronOre);
        var inventory = new SuitInventory(capacity: 50);

        float progress = 0.0f;

        // 1. Fractional tick: 0.2s * 1.5 = 0.3 progress. Whole units = 0.
        var r1 = DrillSim.Step(
            deposit, inventory,
            isFiring: true, activeSlot: 1, hitDistance: 3.0f,
            drillRange, drillRate, deltaSeconds: 0.2f, ref progress);

        Assert.Equal(DrillStatus.Drilling, r1.Status);
        Assert.Equal(0, r1.UnitsExtracted);
        Assert.Equal(0.3f, progress, precision: 3);
        Assert.Equal(5, deposit.RemainingUnits);
        Assert.Equal(0, inventory.TotalCount);
        Assert.True(r1.BeamActive);

        // 2. Fractional accumulation crossing 1.0: 0.6s * 1.5 = 0.9.
        // Total progress = 0.3 + 0.9 = 1.2 -> 1 whole unit extracted, remainder progress = 0.2.
        var r2 = DrillSim.Step(
            deposit, inventory,
            isFiring: true, activeSlot: 1, hitDistance: 3.0f,
            drillRange, drillRate, deltaSeconds: 0.6f, ref progress);

        Assert.Equal(DrillStatus.Drilling, r2.Status);
        Assert.Equal(1, r2.UnitsExtracted);
        Assert.Equal(0.2f, progress, precision: 3);
        Assert.Equal(4, deposit.RemainingUnits);
        Assert.Equal(1, inventory.GetCount(ItemId.IronOre));

        // 3. Multi-unit rollover tick: 2.0s * 1.5 = 3.0.
        // Total progress = 0.2 + 3.0 = 3.2 -> 3 whole units extracted, remainder progress = 0.2.
        var r3 = DrillSim.Step(
            deposit, inventory,
            isFiring: true, activeSlot: 1, hitDistance: 4.5f,
            drillRange, drillRate, deltaSeconds: 2.0f, ref progress);

        Assert.Equal(DrillStatus.Drilling, r3.Status);
        Assert.Equal(3, r3.UnitsExtracted);
        Assert.Equal(0.2f, progress, precision: 3);
        Assert.Equal(1, deposit.RemainingUnits);
        Assert.Equal(4, inventory.GetCount(ItemId.IronOre));

        // 4. Final rollover depleting remaining 1 unit even when progress delta is >= 1 unit.
        // Tick: 1.0s * 1.5 = 1.5. Total = 0.2 + 1.5 = 1.7 -> 1 unit available in deposit.
        // Deposit drops to 0, status is Depleted.
        var r4 = DrillSim.Step(
            deposit, inventory,
            isFiring: true, activeSlot: 1, hitDistance: 2.0f,
            drillRange, drillRate, deltaSeconds: 1.0f, ref progress);

        Assert.Equal(DrillStatus.Depleted, r4.Status);
        Assert.Equal(1, r4.UnitsExtracted);
        Assert.Equal(0, deposit.RemainingUnits);
        Assert.True(deposit.IsDepleted);
        Assert.Equal(5, inventory.GetCount(ItemId.IronOre));
    }

    [Fact]
    public void DrillSim_HaltsOnOutOfRangeWrongSlotDepletedOrCargoFullAndPersistsAcrossBoarding()
    {
        // AC-3: Given DrillSim.Step when hit distance exceeds drillRange, hotbar slot [2]–[5] is selected,
        // deposit RemainingUnits == 0, or suit Inventory.IsFull becomes true mid-extraction, then zero excess
        // units are deducted from the deposit, Inventory never exceeds Capacity, DrillStatus reports the exact
        // state (OutOfRange, Idle, Depleted, CargoFull), and suit Inventory contents remain intact across
        // PilotModeStateMachine TryBoardShip and ExitCockpit transitions.

        const float drillRange = 5.0f;
        const float drillRate = 1.5f;

        // 1. Out of range (distance 5.1m > 5.0m)
        var dep1 = new OreDepositState(initialUnits: 10);
        var inv1 = new SuitInventory(capacity: 50);
        float p1 = 0f;
        var resOutOfRange = DrillSim.Step(
            dep1, inv1,
            isFiring: true, activeSlot: 1, hitDistance: 5.1f,
            drillRange, drillRate, deltaSeconds: 1.0f, ref p1);

        Assert.Equal(DrillStatus.OutOfRange, resOutOfRange.Status);
        Assert.Equal(0, resOutOfRange.UnitsExtracted);
        Assert.Equal(10, dep1.RemainingUnits);
        Assert.Equal(0, inv1.TotalCount);
        Assert.False(resOutOfRange.BeamActive);

        // 2. Wrong hotbar slot ([2] through [5] selected) -> Idle
        for (int slot = 2; slot <= 5; slot++)
        {
            float pSlot = 0f;
            var resWrongSlot = DrillSim.Step(
                dep1, inv1,
                isFiring: true, activeSlot: slot, hitDistance: 3.0f,
                drillRange, drillRate, deltaSeconds: 1.0f, ref pSlot);

            Assert.Equal(DrillStatus.Idle, resWrongSlot.Status);
            Assert.Equal(0, resWrongSlot.UnitsExtracted);
            Assert.Equal(10, dep1.RemainingUnits);
            Assert.Equal(0, inv1.TotalCount);
            Assert.False(resWrongSlot.BeamActive);
        }

        // 3. Deposit depleted (RemainingUnits == 0) -> Depleted
        var depEmpty = new OreDepositState(initialUnits: 0);
        float pDep = 0f;
        var resDepleted = DrillSim.Step(
            depEmpty, inv1,
            isFiring: true, activeSlot: 1, hitDistance: 3.0f,
            drillRange, drillRate, deltaSeconds: 1.0f, ref pDep);

        Assert.Equal(DrillStatus.Depleted, resDepleted.Status);
        Assert.Equal(0, resDepleted.UnitsExtracted);
        Assert.Equal(0, depEmpty.RemainingUnits);
        Assert.Equal(0, inv1.TotalCount);

        // 4. CargoFull mid-extraction: Inventory capacity = 2, deposit = 10, tick asks for 5 units
        var invSmall = new SuitInventory(capacity: 2);
        var depBig = new OreDepositState(initialUnits: 10);
        float pFull = 0f;
        var resFull = DrillSim.Step(
            depBig, invSmall,
            isFiring: true, activeSlot: 1, hitDistance: 3.0f,
            drillRange, drillRate, deltaSeconds: 5.0f, ref pFull); // would produce 7.5 units

        Assert.Equal(DrillStatus.CargoFull, resFull.Status);
        Assert.Equal(2, resFull.UnitsExtracted);
        Assert.Equal(2, invSmall.TotalCount);
        Assert.True(invSmall.IsFull);
        Assert.Equal(8, depBig.RemainingUnits); // Only 2 deducted, zero excess units deducted!

        // Further attempts on full inventory report CargoFull immediately
        var resFullAgain = DrillSim.Step(
            depBig, invSmall,
            isFiring: true, activeSlot: 1, hitDistance: 3.0f,
            drillRange, drillRate, deltaSeconds: 1.0f, ref pFull);

        Assert.Equal(DrillStatus.CargoFull, resFullAgain.Status);
        Assert.Equal(0, resFullAgain.UnitsExtracted);
        Assert.Equal(8, depBig.RemainingUnits);

        // 5. Suit Inventory persists across PilotModeStateMachine TryBoardShip and ExitCockpit transitions
        var suitInv = new SuitInventory(capacity: 50);
        suitInv.Add(ItemId.IronOre, 25);

        var pilotMachine = new PilotModeStateMachine(suitInv);
        var shipPos = new Vector3(0f, 0f, 0f);
        var suitPos = new Vector3(2f, 0f, 0f);

        // Transition 1: FloatingInSuit -> SeatedInCockpit (Board ship)
        pilotMachine.Reset(PilotMode.FloatingInSuit);
        bool boarded = pilotMachine.TryBoardShip(shipPos, suitPos, shipInteractRange: 5.0f);
        Assert.True(boarded);
        Assert.Equal(PilotMode.SeatedInCockpit, pilotMachine.CurrentMode);
        Assert.Equal(25, pilotMachine.SuitInventory.GetCount(ItemId.IronOre));
        Assert.Equal(25, suitInv.GetCount(ItemId.IronOre));

        // Transition 2: SeatedInCockpit -> FloatingInSuit (Exit cockpit)
        SuitSpawnState spawn = pilotMachine.ExitCockpit(shipPos, Quaternion.Identity, Vector3.Zero);
        Assert.Equal(PilotMode.FloatingInSuit, pilotMachine.CurrentMode);
        Assert.Equal(25, pilotMachine.SuitInventory.GetCount(ItemId.IronOre));
        Assert.Equal(25, suitInv.GetCount(ItemId.IronOre));
    }
}
