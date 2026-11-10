using System;
using System.Numerics;
using Keel.Sim.Pilot;
using Xunit;

namespace Keel.Sim.Tests.Pilot;

public sealed class PilotModeStateMachineTests
{
    [Fact]
    public void ExitCockpit_OffsetsInRotatedShipSpaceAndInheritsLinearVelocity()
    {
        var machine = new PilotModeStateMachine();
        Assert.Equal(PilotMode.SeatedInCockpit, machine.CurrentMode);

        var shipPos = new Vector3(100f, -50f, 250f);
        var shipOrientation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2f); // 90 deg around Y
        var shipLinVel = new Vector3(18f, -4f, 32f);

        SuitSpawnState spawn = machine.ExitCockpit(shipPos, shipOrientation, shipLinVel);

        Assert.Equal(PilotMode.FloatingInSuit, machine.CurrentMode);
        Assert.Equal(shipLinVel, spawn.LinearVelocity);
        Assert.Equal(Vector3.Zero, spawn.AngularVelocity);
        Assert.Equal(shipOrientation, spawn.Orientation);

        Vector3 expectedOffset = Vector3.Transform(PilotModeStateMachine.DefaultExitOffset, shipOrientation);
        Vector3 expectedPos = shipPos + expectedOffset;

        Assert.Equal(expectedPos.X, spawn.Position.X, precision: 3);
        Assert.Equal(expectedPos.Y, spawn.Position.Y, precision: 3);
        Assert.Equal(expectedPos.Z, spawn.Position.Z, precision: 3);

        // Verify spawn position is clear of the ship hull box (hull half-extents: 1.5m, 0.9m, 2.0m)
        float distFromShip = Vector3.Distance(shipPos, spawn.Position);
        Assert.True(distFromShip > 2.0f, $"Spawn distance {distFromShip} must be clear of hull bounds.");

        // Re-entering or exiting while already in suit throws
        Assert.Throws<InvalidOperationException>(() => machine.ExitCockpit(shipPos, shipOrientation, shipLinVel));
    }

    [Fact]
    public void TryBoardShip_EnforcesInteractRangeThreshold()
    {
        var machine = new PilotModeStateMachine();
        var shipPos = new Vector3(10f, 20f, 30f);
        machine.ExitCockpit(shipPos, Quaternion.Identity, Vector3.Zero);
        Assert.Equal(PilotMode.FloatingInSuit, machine.CurrentMode);

        const float interactRange = 6.0f;

        // 1. Beyond interact range -> rejected, stays FloatingInSuit
        var farSuitPos = shipPos + new Vector3(interactRange + 1.5f, 0f, 0f);
        bool rejected = machine.TryBoardShip(shipPos, farSuitPos, interactRange);
        Assert.False(rejected);
        Assert.Equal(PilotMode.FloatingInSuit, machine.CurrentMode);

        // 2. Exactly on interact range threshold -> accepted, transitions to SeatedInCockpit
        var thresholdSuitPos = shipPos + new Vector3(interactRange, 0f, 0f);
        bool acceptedThreshold = machine.TryBoardShip(shipPos, thresholdSuitPos, interactRange);
        Assert.True(acceptedThreshold);
        Assert.Equal(PilotMode.SeatedInCockpit, machine.CurrentMode);

        // 3. Already seated in cockpit -> TryBoardShip returns false
        bool alreadySeated = machine.TryBoardShip(shipPos, shipPos, interactRange);
        Assert.False(alreadySeated);
        Assert.Equal(PilotMode.SeatedInCockpit, machine.CurrentMode);

        // 4. Within interact range (< range) -> accepted
        machine.ExitCockpit(shipPos, Quaternion.Identity, Vector3.Zero);
        var closeSuitPos = shipPos + new Vector3(2.0f, 1.0f, -1.0f);
        bool acceptedClose = machine.TryBoardShip(shipPos, closeSuitPos, interactRange);
        Assert.True(acceptedClose);
        Assert.Equal(PilotMode.SeatedInCockpit, machine.CurrentMode);
    }
}
