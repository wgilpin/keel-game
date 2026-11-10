using System;
using System.Numerics;
using Keel.Sim.Config;

namespace Keel.Sim.Flight;

public static class FlightKinematics
{
    private const float InputDeadzone = 1e-4f;

    public static FlightStepResult Step(
        in FlightState state,
        in FlightInput input,
        TuningConfig tuning,
        float deltaSeconds)
    {
        if (!float.IsFinite(deltaSeconds) || deltaSeconds <= 0f ||
            !float.IsFinite(tuning.ShipMass) || tuning.ShipMass <= 0f)
        {
            return new FlightStepResult(Vector3.Zero, Vector3.Zero, state);
        }

        float mass = tuning.ShipMass;
        float rotationalInertia = tuning.ShipMass;
        float maxLinearSpeed = MathF.Max(0f, tuning.MaxLinearSpeed);
        float maxAngularSpeed = MathF.Max(0f, tuning.MaxAngularSpeed);
        float linearThrust = MathF.Max(0f, tuning.LinearThrust);
        float angularTorque = MathF.Max(0f, tuning.AngularTorque);
        float dampenerStrength = MathF.Max(0f, tuning.DampenerStrength);

        Vector3 clampedTranslation = ClampAxesUnit(input.Translation);
        if (clampedTranslation.LengthSquared() > 1f)
        {
            clampedTranslation = Vector3.Normalize(clampedTranslation);
        }

        (Vector3 nextLinearVelocity, Vector3 appliedForce) = StepChannel(
            state.LinearVelocity,
            clampedTranslation,
            linearThrust,
            mass,
            maxLinearSpeed,
            dampenerStrength,
            state.DampenersEnabled,
            deltaSeconds);

        float pitchInput = Math.Clamp(-input.MouseDelta.Y * tuning.MouseSensitivity, -1f, 1f);
        float yawInput = Math.Clamp(-input.MouseDelta.X * tuning.MouseSensitivity, -1f, 1f);
        float rollInput = Math.Clamp(input.Roll, -1f, 1f);
        var rotationControl = new Vector3(pitchInput, yawInput, rollInput);

        (Vector3 nextAngularVelocity, Vector3 appliedTorque) = StepChannel(
            state.AngularVelocity,
            rotationControl,
            angularTorque,
            rotationalInertia,
            maxAngularSpeed,
            dampenerStrength,
            state.DampenersEnabled,
            deltaSeconds);

        var nextState = new FlightState(
            nextLinearVelocity,
            nextAngularVelocity,
            state.DampenersEnabled);

        return new FlightStepResult(appliedForce, appliedTorque, nextState);
    }

    private static Vector3 ClampAxesUnit(Vector3 v) =>
        new(
            Math.Clamp(v.X, -1f, 1f),
            Math.Clamp(v.Y, -1f, 1f),
            Math.Clamp(v.Z, -1f, 1f));

    private static (Vector3 NextVelocity, Vector3 AppliedEffort) StepChannel(
        Vector3 currentVelocity,
        Vector3 controlInput,
        float maxChannelEffort,
        float inertia,
        float maxSpeed,
        float dampenerStrength,
        bool dampenersEnabled,
        float deltaSeconds)
    {
        float currentSpeed = currentVelocity.Length();
        float effectiveCap = MathF.Max(maxSpeed, currentSpeed);

        Vector3 baseVelocity = currentVelocity;
        Vector3 dampenerEffort = Vector3.Zero;

        if (dampenersEnabled && dampenerStrength > 0f && maxChannelEffort > 0f)
        {
            bool xIdle = MathF.Abs(controlInput.X) <= InputDeadzone;
            bool yIdle = MathF.Abs(controlInput.Y) <= InputDeadzone;
            bool zIdle = MathF.Abs(controlInput.Z) <= InputDeadzone;

            var uncommanded = new Vector3(
                xIdle ? currentVelocity.X : 0f,
                yIdle ? currentVelocity.Y : 0f,
                zIdle ? currentVelocity.Z : 0f);

            float uncommandedSpeed = uncommanded.Length();
            if (uncommandedSpeed > 0f)
            {
                float maxDecelEffort = maxChannelEffort * dampenerStrength;
                float maxSpeedDrop = (maxDecelEffort / inertia) * deltaSeconds;

                if (uncommandedSpeed <= maxSpeedDrop)
                {
                    baseVelocity = new Vector3(
                        xIdle ? 0f : currentVelocity.X,
                        yIdle ? 0f : currentVelocity.Y,
                        zIdle ? 0f : currentVelocity.Z);
                    dampenerEffort = -(uncommanded / deltaSeconds) * inertia;
                }
                else
                {
                    float retainRatio = (uncommandedSpeed - maxSpeedDrop) / uncommandedSpeed;
                    baseVelocity = new Vector3(
                        xIdle ? currentVelocity.X * retainRatio : currentVelocity.X,
                        yIdle ? currentVelocity.Y * retainRatio : currentVelocity.Y,
                        zIdle ? currentVelocity.Z * retainRatio : currentVelocity.Z);
                    dampenerEffort = -(uncommanded / uncommandedSpeed) * maxDecelEffort;
                }
            }
        }

        Vector3 pilotEffort = controlInput * maxChannelEffort;
        Vector3 candidate = baseVelocity + ((pilotEffort / inertia) * deltaSeconds);
        float candidateSpeed = candidate.Length();

        if (candidateSpeed > effectiveCap && candidateSpeed > 1e-6f)
        {
            Vector3 clamped = (candidate / candidateSpeed) * effectiveCap;
            Vector3 clampedEffort = ((clamped - currentVelocity) / deltaSeconds) * inertia;
            return (clamped, clampedEffort);
        }

        return (candidate, dampenerEffort + pilotEffort);
    }
}
