using System.Numerics;

namespace Keel.Sim.Flight;

/// <summary>
/// Ship-local linear velocity (m/s), ship-local angular velocity (rad/s), and inertial dampener mode.
/// </summary>
public readonly record struct FlightState(
    Vector3 LinearVelocity,
    Vector3 AngularVelocity,
    bool DampenersEnabled)
{
    public static FlightState Initial => new(Vector3.Zero, Vector3.Zero, DampenersEnabled: true);
}

public readonly record struct FlightStepResult(
    Vector3 Force,
    Vector3 Torque,
    FlightState NextState);
