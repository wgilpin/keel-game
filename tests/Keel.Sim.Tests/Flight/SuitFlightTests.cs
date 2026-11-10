using System.Numerics;
using Keel.Sim.Config;
using Keel.Sim.Flight;
using Xunit;

namespace Keel.Sim.Tests.Flight;

public sealed class SuitFlightTests
{
    [Fact]
    public void StepSuit_UsesSuitProfileIndependentlyOfShipCapsAndStopsWithoutOvershoot()
    {
        var tuning = TuningConfig.Default; // SuitMass 95, SuitLinearThrust 1400, SuitTorque 220, SuitMaxLinearSpeed 25
        Assert.Equal(95.0f, tuning.SuitMass);
        Assert.Equal(1400.0f, tuning.SuitLinearThrust);
        Assert.Equal(220.0f, tuning.SuitAngularTorque);
        Assert.Equal(25.0f, tuning.SuitMaxLinearSpeed);

        var atRest = new FlightState(Vector3.Zero, Vector3.Zero, DampenersEnabled: false);

        // 1. Thrust scales with SuitLinearThrust (1400 N), not ShipLinearThrust (24000 N)
        var forwardInput = new FlightInput(new Vector3(0f, 0f, -1f), Vector2.Zero, Roll: 0f);
        FlightStepResult thrustStep = FlightKinematics.StepSuit(in atRest, in forwardInput, tuning, deltaSeconds: 1f / 60f);

        Assert.Equal(tuning.SuitLinearThrust, thrustStep.Force.Length(), precision: 2);
        Assert.True(thrustStep.Force.Length() < tuning.LinearThrust);

        // 2. Speed cap clamps at SuitMaxLinearSpeed (25 m/s), NOT ShipMaxLinearSpeed (80 m/s)
        var nearCapState = new FlightState(new Vector3(0f, 0f, -24.8f), Vector3.Zero, DampenersEnabled: false);
        FlightStepResult capStep = FlightKinematics.StepSuit(in nearCapState, in forwardInput, tuning, deltaSeconds: 0.1f);

        Assert.Equal(tuning.SuitMaxLinearSpeed, capStep.NextState.LinearVelocity.Length(), precision: 3);

        var atCapState = new FlightState(new Vector3(0f, 0f, -tuning.SuitMaxLinearSpeed), Vector3.Zero, DampenersEnabled: false);
        FlightStepResult blockedForward = FlightKinematics.StepSuit(in atCapState, in forwardInput, tuning, deltaSeconds: 1f / 60f);
        Assert.Equal(0f, blockedForward.Force.Length(), precision: 3);
        Assert.Equal(tuning.SuitMaxLinearSpeed, blockedForward.NextState.LinearVelocity.Length(), precision: 3);

        // 3. Zero-overshoot dampeners: starts with arbitrary linear/angular motion and monotonically converges to zero
        var movingState = new FlightState(
            new Vector3(15f, -10f, 8f),
            new Vector3(1.2f, -0.8f, 0.5f),
            DampenersEnabled: true);

        FlightState state = movingState;
        float prevLinSpeed = state.LinearVelocity.Length();
        float prevAngSpeed = state.AngularVelocity.Length();

        for (int i = 0; i < 180; i++)
        {
            FlightStepResult step = FlightKinematics.StepSuit(in state, FlightInput.Zero, tuning, deltaSeconds: 1f / 60f);
            float curLinSpeed = step.NextState.LinearVelocity.Length();
            float curAngSpeed = step.NextState.AngularVelocity.Length();

            Assert.True(curLinSpeed <= prevLinSpeed + 1e-5f, $"Linear speed must decrease monotonically: {curLinSpeed} vs {prevLinSpeed}");
            Assert.True(curAngSpeed <= prevAngSpeed + 1e-5f, $"Angular speed must decrease monotonically: {curAngSpeed} vs {prevAngSpeed}");

            prevLinSpeed = curLinSpeed;
            prevAngSpeed = curAngSpeed;
            state = step.NextState;
        }

        // Must come to a complete standstill without overshoot
        Assert.Equal(Vector3.Zero, state.LinearVelocity);
        Assert.Equal(Vector3.Zero, state.AngularVelocity);
    }
}
