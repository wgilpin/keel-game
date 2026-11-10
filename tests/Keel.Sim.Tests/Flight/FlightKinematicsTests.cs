using System.Numerics;
using Keel.Sim.Config;
using Keel.Sim.Flight;
using Xunit;

namespace Keel.Sim.Tests.Flight;

public sealed class FlightKinematicsTests
{
    [Fact]
    public void ThrustAndTorque_RespectsNormalizationAndSpeedCaps()
    {
        var tuning = TuningConfig.Default; // Mass 1200, Thrust 24000, Torque 4800, MaxLin 80, MaxAng 2.5, Sens 0.0025
        var atRest = new FlightState(Vector3.Zero, Vector3.Zero, DampenersEnabled: false);

        // 1. Diagonal 3-axis translation normalizes to unit length rather than sqrt(3) over-thrust.
        var diagInput = new FlightInput(new Vector3(1f, 1f, -1f), Vector2.Zero, Roll: 0f);
        FlightStepResult diagStep = FlightKinematics.Step(in atRest, in diagInput, tuning, deltaSeconds: 1f / 60f);

        Assert.Equal(tuning.LinearThrust, diagStep.Force.Length(), precision: 2);
        Vector3 expectedDir = Vector3.Normalize(new Vector3(1f, 1f, -1f));
        Assert.Equal(expectedDir.X, Vector3.Normalize(diagStep.Force).X, precision: 4);
        Assert.Equal(expectedDir.Y, Vector3.Normalize(diagStep.Force).Y, precision: 4);
        Assert.Equal(expectedDir.Z, Vector3.Normalize(diagStep.Force).Z, precision: 4);

        // Partial single-axis input preserves fractional authority.
        var halfStrafe = new FlightInput(new Vector3(0.5f, 0f, 0f), Vector2.Zero, Roll: 0f);
        FlightStepResult halfStep = FlightKinematics.Step(in atRest, in halfStrafe, tuning, deltaSeconds: 1f / 60f);
        Assert.Equal(0.5f * tuning.LinearThrust, halfStep.Force.X, precision: 2);

        // 2. Torque scales linearly with MouseSensitivity (pitch/yaw) and AngularTorque (pitch/yaw/roll).
        var rotInput = new FlightInput(Vector3.Zero, MouseDelta: new Vector2(100f, -50f), Roll: 0.5f);
        FlightStepResult rotStep = FlightKinematics.Step(in atRest, in rotInput, tuning, deltaSeconds: 1f / 60f);
        Assert.Equal(0.125f * tuning.AngularTorque, rotStep.Torque.X, precision: 2);  // Mouse up (-Y) -> +X pitch
        Assert.Equal(-0.25f * tuning.AngularTorque, rotStep.Torque.Y, precision: 2);  // Mouse right (+X) -> -Y yaw
        Assert.Equal(0.5f * tuning.AngularTorque, rotStep.Torque.Z, precision: 2);    // +Roll -> +Z roll

        var doubledTuning = tuning with { MouseSensitivity = tuning.MouseSensitivity * 2f, AngularTorque = tuning.AngularTorque * 2f };
        FlightStepResult doubledRotStep = FlightKinematics.Step(in atRest, in rotInput, doubledTuning, deltaSeconds: 1f / 60f);
        Assert.Equal(rotStep.Torque.X * 4f, doubledRotStep.Torque.X, precision: 1);
        Assert.Equal(rotStep.Torque.Y * 4f, doubledRotStep.Torque.Y, precision: 1);
        Assert.Equal(rotStep.Torque.Z * 2f, doubledRotStep.Torque.Z, precision: 1);

        // 3. Linear speed cap: clamps forward acceleration at MaxLinearSpeed, permits braking & lateral vectoring.
        var nearLinearCap = new FlightState(new Vector3(0f, 0f, -79.5f), Vector3.Zero, DampenersEnabled: false);
        var fullForward = new FlightInput(new Vector3(0f, 0f, -1f), Vector2.Zero, Roll: 0f);
        FlightStepResult capCrossStep = FlightKinematics.Step(in nearLinearCap, in fullForward, tuning, deltaSeconds: 0.1f);
        Assert.Equal(tuning.MaxLinearSpeed, capCrossStep.NextState.LinearVelocity.Length(), precision: 3);

        var atLinearCap = new FlightState(new Vector3(0f, 0f, -tuning.MaxLinearSpeed), Vector3.Zero, DampenersEnabled: false);
        FlightStepResult blockedForward = FlightKinematics.Step(in atLinearCap, in fullForward, tuning, deltaSeconds: 1f / 60f);
        Assert.Equal(0f, blockedForward.Force.Length(), precision: 3);
        Assert.Equal(tuning.MaxLinearSpeed, blockedForward.NextState.LinearVelocity.Length(), precision: 3);

        var fullBrake = new FlightInput(new Vector3(0f, 0f, 1f), Vector2.Zero, Roll: 0f);
        FlightStepResult brakeStep = FlightKinematics.Step(in atLinearCap, in fullBrake, tuning, deltaSeconds: 1f / 60f);
        Assert.Equal(tuning.LinearThrust, brakeStep.Force.Z, precision: 2);
        Assert.True(brakeStep.NextState.LinearVelocity.Length() < tuning.MaxLinearSpeed);

        var rightStrafe = new FlightInput(new Vector3(1f, 0f, 0f), Vector2.Zero, Roll: 0f);
        FlightStepResult lateralStep = FlightKinematics.Step(in atLinearCap, in rightStrafe, tuning, deltaSeconds: 1f / 60f);
        Assert.True(lateralStep.Force.X > 0f);
        Assert.True(lateralStep.NextState.LinearVelocity.X > 0f);
        Assert.Equal(tuning.MaxLinearSpeed, lateralStep.NextState.LinearVelocity.Length(), precision: 3);

        // 4. Angular speed cap: clamps forward angular acceleration at MaxAngularSpeed, permits braking & lateral vectoring.
        var atAngularCap = new FlightState(Vector3.Zero, new Vector3(tuning.MaxAngularSpeed, 0f, 0f), DampenersEnabled: false);
        var morePitchUp = new FlightInput(Vector3.Zero, MouseDelta: new Vector2(0f, -200f), Roll: 0f);
        FlightStepResult blockedPitch = FlightKinematics.Step(in atAngularCap, in morePitchUp, tuning, deltaSeconds: 1f / 60f);
        Assert.Equal(0f, blockedPitch.Torque.Length(), precision: 3);
        Assert.Equal(tuning.MaxAngularSpeed, blockedPitch.NextState.AngularVelocity.Length(), precision: 3);

        var counterPitch = new FlightInput(Vector3.Zero, MouseDelta: new Vector2(0f, 200f), Roll: 0f);
        FlightStepResult pitchBrake = FlightKinematics.Step(in atAngularCap, in counterPitch, tuning, deltaSeconds: 1f / 60f);
        Assert.True(pitchBrake.Torque.X < 0f);
        Assert.True(pitchBrake.NextState.AngularVelocity.Length() < tuning.MaxAngularSpeed);

        var lateralRoll = new FlightInput(Vector3.Zero, Vector2.Zero, Roll: 1f);
        FlightStepResult rollAtCap = FlightKinematics.Step(in atAngularCap, in lateralRoll, tuning, deltaSeconds: 1f / 60f);
        Assert.True(rollAtCap.NextState.AngularVelocity.Z > 0f);
        Assert.Equal(tuning.MaxAngularSpeed, rollAtCap.NextState.AngularVelocity.Length(), precision: 3);
    }

    [Fact]
    public void DampenersOff_MaintainsNewtonianDrift()
    {
        var tuning = TuningConfig.Default;
        var initialLin = new Vector3(35f, -12f, -60f);
        var initialAng = new Vector3(0.8f, -1.4f, 0.5f);
        var state = new FlightState(initialLin, initialAng, DampenersEnabled: false);

        for (int i = 0; i < 120; i++)
        {
            FlightStepResult step = FlightKinematics.Step(in state, FlightInput.Zero, tuning, deltaSeconds: 1f / 60f);
            Assert.Equal(Vector3.Zero, step.Force);
            Assert.Equal(Vector3.Zero, step.Torque);
            Assert.Equal(initialLin, step.NextState.LinearVelocity);
            Assert.Equal(initialAng, step.NextState.AngularVelocity);
            Assert.False(step.NextState.DampenersEnabled);
            state = step.NextState;
        }
    }

    [Fact]
    public void DampenersOn_StopsWithoutOvershoot()
    {
        var defaultTuning = TuningConfig.Default;
        var aggressiveTuning = defaultTuning with { DampenerStrength = 50f };

        AssertMonotonicZeroOvershootStop(defaultTuning, deltaSeconds: 1f / 60f, maxDurationSeconds: 3.0f);
        AssertMonotonicZeroOvershootStop(defaultTuning, deltaSeconds: 1f / 20f, maxDurationSeconds: 3.0f);
        AssertMonotonicZeroOvershootStop(aggressiveTuning, deltaSeconds: 1f / 20f, maxDurationSeconds: 1.0f);
    }

    private static void AssertMonotonicZeroOvershootStop(TuningConfig tuning, float deltaSeconds, float maxDurationSeconds)
    {
        var initialLin = new Vector3(48f, -24f, 36f);
        var initialAng = new Vector3(-1.8f, 1.2f, -0.9f);
        var state = new FlightState(initialLin, initialAng, DampenersEnabled: true);

        int maxSteps = (int)System.MathF.Ceiling(maxDurationSeconds / deltaSeconds);
        for (int i = 0; i < maxSteps; i++)
        {
            float prevLinSpeed = state.LinearVelocity.Length();
            float prevAngSpeed = state.AngularVelocity.Length();

            FlightStepResult step = FlightKinematics.Step(in state, FlightInput.Zero, tuning, deltaSeconds);
            FlightState next = step.NextState;

            float nextLinSpeed = next.LinearVelocity.Length();
            float nextAngSpeed = next.AngularVelocity.Length();

            if (prevLinSpeed > 0f)
            {
                Assert.True(nextLinSpeed < prevLinSpeed, $"Linear speed failed to decrease at dt={deltaSeconds}, step={i}: {prevLinSpeed} -> {nextLinSpeed}");
            }
            else
            {
                Assert.Equal(0f, nextLinSpeed);
                Assert.Equal(Vector3.Zero, step.Force);
            }

            if (prevAngSpeed > 0f)
            {
                Assert.True(nextAngSpeed < prevAngSpeed, $"Angular speed failed to decrease at dt={deltaSeconds}, step={i}: {prevAngSpeed} -> {nextAngSpeed}");
            }
            else
            {
                Assert.Equal(0f, nextAngSpeed);
                Assert.Equal(Vector3.Zero, step.Torque);
            }

            // Never reverse sign on any axis.
            Assert.True(initialLin.X * next.LinearVelocity.X >= 0f);
            Assert.True(initialLin.Y * next.LinearVelocity.Y >= 0f);
            Assert.True(initialLin.Z * next.LinearVelocity.Z >= 0f);
            Assert.True(initialAng.X * next.AngularVelocity.X >= 0f);
            Assert.True(initialAng.Y * next.AngularVelocity.Y >= 0f);
            Assert.True(initialAng.Z * next.AngularVelocity.Z >= 0f);

            state = next;
        }

        Assert.Equal(Vector3.Zero, state.LinearVelocity);
        Assert.Equal(Vector3.Zero, state.AngularVelocity);
    }
}
