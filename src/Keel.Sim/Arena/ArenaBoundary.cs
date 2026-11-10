using System.Numerics;

namespace Keel.Sim.Arena;

public readonly record struct BoundaryEvaluation(
    Vector3 Position,
    Vector3 Velocity,
    bool IsAtOrBeyondBoundary,
    bool IsInWarningZone);

public static class ArenaBoundary
{
    public static BoundaryEvaluation Evaluate(Vector3 position, Vector3 velocity)
    {
        float distance = position.Length();

        if (distance <= SpaceArenaLayout.SoftBoundaryRadius)
        {
            return new BoundaryEvaluation(position, velocity, IsAtOrBeyondBoundary: false, IsInWarningZone: false);
        }

        if (distance < SpaceArenaLayout.HardBoundaryRadius)
        {
            return new BoundaryEvaluation(position, velocity, IsAtOrBeyondBoundary: false, IsInWarningZone: true);
        }

        Vector3 normal = distance > 1e-4f ? position / distance : Vector3.UnitZ;
        Vector3 clampedPosition = normal * SpaceArenaLayout.HardBoundaryRadius;

        float radialSpeed = Vector3.Dot(velocity, normal);
        Vector3 clampedVelocity = radialSpeed > 0f
            ? velocity - (normal * radialSpeed)
            : velocity;

        return new BoundaryEvaluation(
            clampedPosition,
            clampedVelocity,
            IsAtOrBeyondBoundary: true,
            IsInWarningZone: true);
    }
}
