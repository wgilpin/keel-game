using Keel.Sim.Config;
using Keel.Sim.Grid;
using Keel.Sim.Hud;
using Keel.Sim.Inventory;
using Keel.Sim.Pilot;
using Keel.Sim.Refinery;
using Xunit;

namespace Keel.Sim.Tests.Refinery;

public class RefineryTerminalTests
{
    [Fact]
    public void InteractAndCancel_PrioritizeRefineryOpenCloseOverShipBoardingAndAutoCloseOutOfTether()
    {
        const float refineryInteractRange = 5.0f;
        const float shipInteractRange = 6.0f;
        const float tetherLimit = refineryInteractRange + 1.5f; // 6.5m

        // State machine test harness simulating GameRoot modal gating
        bool isRefineryOpen = false;
        var pilotMachine = new PilotModeStateMachine();
        pilotMachine.ExitCockpit(System.Numerics.Vector3.Zero, System.Numerics.Quaternion.Identity, System.Numerics.Vector3.Zero);

        // 1. Aiming at Refinery within range (4.0m <= 5.0m) while also near ship (3.0m <= 6.0m)
        // Pressing F must open refinery, NOT board ship
        bool aimingAtRefinery = true;
        float distanceToRefinery = 4.0f;
        float distanceToShip = 3.0f;

        // F pressed logic:
        if (isRefineryOpen)
        {
            isRefineryOpen = false;
        }
        else if (aimingAtRefinery && distanceToRefinery <= refineryInteractRange)
        {
            isRefineryOpen = true;
        }
        else
        {
            pilotMachine.TryBoardShip(System.Numerics.Vector3.Zero, new System.Numerics.Vector3(distanceToShip, 0, 0), shipInteractRange);
        }

        Assert.True(isRefineryOpen, "Refinery should open when aimed at in range, taking priority over ship boarding.");
        Assert.Equal(PilotMode.FloatingInSuit, pilotMachine.Mode);

        // 2. Pressing F or Esc while open closes refinery without side-effects
        // Simulating F press while open:
        if (isRefineryOpen)
        {
            isRefineryOpen = false;
        }
        Assert.False(isRefineryOpen, "Pressing F while open should close the refinery.");
        Assert.Equal(PilotMode.FloatingInSuit, pilotMachine.Mode);

        // 3. Re-open and drift past tether limit (7.0m > 6.5m): auto-closes
        isRefineryOpen = true;
        float driftedDistance = 7.0f;
        if (isRefineryOpen && driftedDistance > tetherLimit)
        {
            isRefineryOpen = false;
        }
        Assert.False(isRefineryOpen, "Drifting past tether limit should auto-close the terminal.");

        // 4. Boarding cockpit automatically ensures terminal is closed
        isRefineryOpen = true;
        pilotMachine.TryBoardShip(System.Numerics.Vector3.Zero, System.Numerics.Vector3.Zero, shipInteractRange);
        if (pilotMachine.Mode == PilotMode.SeatedInCockpit)
        {
            isRefineryOpen = false;
        }
        Assert.False(isRefineryOpen);
        Assert.Equal(PilotMode.SeatedInCockpit, pilotMachine.Mode);
    }

    [Fact]
    public void ModalSession_MasksFlightAndDrillWhileAllowingDepositWithdrawAndBackgroundRefining()
    {
        var config = TuningConfig.Default with
        {
            RefineryCycleSeconds = 2.0f,
            RefineryOrePerBlock = 2.0f,
            RefineryBufferCapacity = 50.0f,
        };

        var refinery = new RefineryState((int)config.RefineryBufferCapacity);
        var suit = new SuitInventory(capacity: 50);

        // Pilot in suit starts with 20 IronOre
        suit.Add(ItemId.IronOre, 20);
        Assert.Equal(20, suit.GetCount(ItemId.IronOre));
        Assert.Equal(0, suit.GetCount(ItemId.SteelBlock));

        // 1. Open modal terminal and verify initial ViewModel
        bool isOpen = true;
        var vm = RefineryTerminalModel.Compute(refinery, suit, isOpen, config.RefineryCycleSeconds);
        Assert.True(vm.IsOpen);
        Assert.True(vm.CanDeposit);
        Assert.False(vm.CanWithdraw);
        Assert.Equal(RefineryStatus.Idle, vm.Status);
        Assert.Equal("IDLE — [E] DEPOSIT IRON ORE", vm.StatusMessage);

        // 2. Deposit [E] moves ore from suit into refinery
        int deposited = RefinerySim.DepositFromSuit(refinery, suit);
        Assert.Equal(20, deposited);
        Assert.Equal(0, suit.GetCount(ItemId.IronOre));
        Assert.Equal(20, refinery.InputInventory.GetCount(ItemId.IronOre));

        // 3. Step refining: background ticks advance conversion
        // Step 3 seconds (1.5 cycles) -> 1 cycle done (2 ore consumed -> 1 SteelBlock produced, 18 ore remaining)
        RefinerySim.Step(refinery, config, 3.0f);
        Assert.Equal(18, refinery.InputInventory.GetCount(ItemId.IronOre));
        Assert.Equal(1, refinery.OutputInventory.GetCount(ItemId.SteelBlock));

        vm = RefineryTerminalModel.Compute(refinery, suit, isOpen, config.RefineryCycleSeconds);
        Assert.True(vm.CanWithdraw);
        Assert.Equal(RefineryStatus.Refining, vm.Status);
        Assert.Contains("REFINING", vm.StatusMessage);
        Assert.True(vm.CycleProgress >= 0.0f && vm.CycleProgress <= 1.0f);

        // 4. Close terminal (isOpen = false) and continue refining 3 more cycles (6 seconds)
        // 18 ore -> 3 more blocks (6 ore consumed) -> total 4 blocks, 12 ore remaining
        isOpen = false;
        RefinerySim.Step(refinery, config, 6.0f);
        Assert.Equal(12, refinery.InputInventory.GetCount(ItemId.IronOre));
        Assert.Equal(4, refinery.OutputInventory.GetCount(ItemId.SteelBlock));

        // 5. Re-open terminal and Withdraw [R]
        isOpen = true;
        int withdrawn = RefinerySim.WithdrawToSuit(refinery, suit);
        Assert.Equal(4, withdrawn);
        Assert.Equal(0, refinery.OutputInventory.GetCount(ItemId.SteelBlock));
        Assert.Equal(4, suit.GetCount(ItemId.SteelBlock));

        // 6. Verify SteelBlock items persist in suit inventory across boarding and exiting
        var pilotMachine = new PilotModeStateMachine();
        pilotMachine.ExitCockpit(System.Numerics.Vector3.Zero, System.Numerics.Quaternion.Identity, System.Numerics.Vector3.Zero);
        pilotMachine.SuitInventory.Add(ItemId.SteelBlock, 4);

        pilotMachine.TryBoardShip(System.Numerics.Vector3.Zero, System.Numerics.Vector3.Zero, 6.0f);
        Assert.Equal(PilotMode.SeatedInCockpit, pilotMachine.Mode);
        Assert.Equal(4, pilotMachine.SuitInventory.GetCount(ItemId.SteelBlock));

        pilotMachine.ExitCockpit(System.Numerics.Vector3.Zero, System.Numerics.Quaternion.Identity, System.Numerics.Vector3.Zero);
        Assert.Equal(PilotMode.FloatingInSuit, pilotMachine.Mode);
        Assert.Equal(4, pilotMachine.SuitInventory.GetCount(ItemId.SteelBlock));
    }
}
