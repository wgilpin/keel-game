using System.Numerics;

namespace Keel.Sim.Flight;

/// <summary>
/// Ship-local 6DoF pilot control inputs.
/// Local axes match Godot 3D: +X right, +Y up, -Z forward.
/// </summary>
public readonly record struct FlightInput(
    Vector3 Translation,
    Vector2 MouseDelta,
    float Roll)
{
    public static FlightInput Zero => default;
}
