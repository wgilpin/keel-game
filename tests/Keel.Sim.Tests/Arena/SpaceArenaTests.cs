using System.Numerics;
using Keel.Sim.Arena;
using Xunit;

namespace Keel.Sim.Tests.Arena;

public sealed class SpaceArenaTests
{
    [Fact]
    public void Boundary_ClampsOutwardMotionBeyond20Km()
    {
        // 1. Inside 19.5 km: untouched position and velocity, no warning, no clamp.
        var insidePos = new Vector3(1000f, -500f, 2000f);
        var insideVel = new Vector3(50f, 0f, -20f);
        BoundaryEvaluation insideEval = ArenaBoundary.Evaluate(insidePos, insideVel);

        Assert.Equal(insidePos, insideEval.Position);
        Assert.Equal(insideVel, insideEval.Velocity);
        Assert.False(insideEval.IsInWarningZone);
        Assert.False(insideEval.IsAtOrBeyondBoundary);

        // 2. Warning zone: between 19.5 km and 20 km -> untouched, but warning active.
        var warningPos = Vector3.Normalize(new Vector3(1f, 1f, 1f)) * 19600f;
        var warningVel = new Vector3(10f, 20f, 30f);
        BoundaryEvaluation warningEval = ArenaBoundary.Evaluate(warningPos, warningVel);

        Assert.Equal(warningPos, warningEval.Position);
        Assert.Equal(warningVel, warningEval.Velocity);
        Assert.True(warningEval.IsInWarningZone);
        Assert.False(warningEval.IsAtOrBeyondBoundary);

        // 3. Beyond 20 km with outward velocity:
        // Position clamps to the 20,000 m sphere.
        // Outward radial velocity is stripped, while tangential velocity is preserved.
        Vector3 outwardDir = Vector3.Normalize(new Vector3(3f, 4f, 0f)); // Normal is (0.6, 0.8, 0)
        Vector3 overshotPos = outwardDir * 20500f;

        Vector3 tangentialVel = new Vector3(-0.8f, 0.6f, 0f) * 40f; // Orthogonal to outwardDir
        Vector3 outwardRadialVel = outwardDir * 50f;
        Vector3 totalVel = tangentialVel + outwardRadialVel;

        BoundaryEvaluation clampedEval = ArenaBoundary.Evaluate(overshotPos, totalVel);

        Assert.True(clampedEval.IsAtOrBeyondBoundary);
        Assert.True(clampedEval.IsInWarningZone);
        Assert.Equal(SpaceArenaLayout.HardBoundaryRadius, clampedEval.Position.Length(), precision: 2);
        Assert.Equal(outwardDir.X, Vector3.Normalize(clampedEval.Position).X, precision: 4);
        Assert.Equal(outwardDir.Y, Vector3.Normalize(clampedEval.Position).Y, precision: 4);
        Assert.Equal(outwardDir.Z, Vector3.Normalize(clampedEval.Position).Z, precision: 4);

        // Outward component stripped (radial speed <= 0), tangential preserved
        float resultingRadial = Vector3.Dot(clampedEval.Velocity, outwardDir);
        Assert.True(resultingRadial <= 0.001f, $"Expected radial velocity <= 0 but was {resultingRadial}");
        Assert.Equal(tangentialVel.X, clampedEval.Velocity.X, precision: 2);
        Assert.Equal(tangentialVel.Y, clampedEval.Velocity.Y, precision: 2);
        Assert.Equal(tangentialVel.Z, clampedEval.Velocity.Z, precision: 2);

        // 4. Beyond 20 km but with INWARD velocity:
        // Position clamps to 20 km, but inward velocity is preserved.
        Vector3 inwardRadialVel = outwardDir * -30f;
        Vector3 inwardTotalVel = tangentialVel + inwardRadialVel;
        BoundaryEvaluation inwardEval = ArenaBoundary.Evaluate(overshotPos, inwardTotalVel);

        Assert.Equal(SpaceArenaLayout.HardBoundaryRadius, inwardEval.Position.Length(), precision: 2);
        Assert.Equal(inwardTotalVel.X, inwardEval.Velocity.X, precision: 2);
        Assert.Equal(inwardTotalVel.Y, inwardEval.Velocity.Y, precision: 2);
        Assert.Equal(inwardTotalVel.Z, inwardEval.Velocity.Z, precision: 2);
    }

    [Fact]
    public void DustField_EuclideanWrapsAcrossNegativeAndPositiveCoords()
    {
        const float boxSize = 20.0f;
        const float halfBox = boxSize * 0.5f;
        const int count = 64;

        var field = new SpaceDustField(particleCount: count, boxSize: boxSize, seed: 42);
        var buffer = new Vector3[count];

        Vector3[] testCameraPositions =
        [
            Vector3.Zero,
            new Vector3(15432.7f, 8912.3f, 19200.0f),
            new Vector3(-12345.6f, -7890.1f, -18500.0f),
            new Vector3(-0.001f, 0.001f, -10.0f),
            new Vector3(500.5f, -500.5f, 0.0f),
        ];

        foreach (Vector3 cameraPos in testCameraPositions)
        {
            field.WrapToCameraBox(cameraPos, buffer);

            for (int i = 0; i < count; i++)
            {
                Vector3 worldPos = buffer[i];
                Vector3 relative = worldPos - cameraPos;

                Assert.True(relative.X >= -halfBox - 1e-4f && relative.X <= halfBox + 1e-4f,
                    $"X coordinate {relative.X} outside [-{halfBox}, {halfBox}] for camera {cameraPos}");
                Assert.True(relative.Y >= -halfBox - 1e-4f && relative.Y <= halfBox + 1e-4f,
                    $"Y coordinate {relative.Y} outside [-{halfBox}, {halfBox}] for camera {cameraPos}");
                Assert.True(relative.Z >= -halfBox - 1e-4f && relative.Z <= halfBox + 1e-4f,
                    $"Z coordinate {relative.Z} outside [-{halfBox}, {halfBox}] for camera {cameraPos}");
            }
        }
    }
}
