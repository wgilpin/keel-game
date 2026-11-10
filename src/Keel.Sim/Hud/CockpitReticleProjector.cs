using System;
using System.Numerics;

namespace Keel.Sim.Hud;

public readonly record struct MarkerProjection(
    Vector2 ScreenPosition,
    bool IsVisible,
    bool IsClampedToRing);

public readonly record struct ReticleProjectionResult(
    MarkerProjection Prograde,
    MarkerProjection Retrograde);

public static class CockpitReticleProjector
{
    public const float SpeedThreshold = 0.5f;

    public static ReticleProjectionResult Project(
        Vector3 shipLocalVelocity,
        Vector2 viewportSize,
        float verticalFovDegrees = 75.0f,
        float ringRadiusPx = 180.0f)
    {
        Vector2 center = viewportSize * 0.5f;
        float speed = shipLocalVelocity.Length();

        if (speed < SpeedThreshold || viewportSize.X <= 0f || viewportSize.Y <= 0f)
        {
            return new ReticleProjectionResult(
                new MarkerProjection(center, IsVisible: false, IsClampedToRing: false),
                new MarkerProjection(center, IsVisible: false, IsClampedToRing: false));
        }

        Vector3 progradeDir = shipLocalVelocity / speed;
        Vector3 retrogradeDir = -progradeDir;

        float fovRad = verticalFovDegrees * (MathF.PI / 180f);
        float focalLength = (viewportSize.Y * 0.5f) / MathF.Tan(fovRad * 0.5f);

        MarkerProjection projPrograde = ProjectDirection(progradeDir, center, focalLength, ringRadiusPx);
        MarkerProjection projRetrograde = ProjectDirection(retrogradeDir, center, focalLength, ringRadiusPx);

        return new ReticleProjectionResult(projPrograde, projRetrograde);
    }

    private static MarkerProjection ProjectDirection(
        Vector3 localDir,
        Vector2 screenCenter,
        float focalLength,
        float ringRadiusPx)
    {
        // Local 3D forward is -Z.
        if (localDir.Z < -1e-4f)
        {
            float depth = -localDir.Z;
            float pxX = (localDir.X / depth) * focalLength;
            float pxY = -(localDir.Y / depth) * focalLength; // screen Y is down
            var offset = new Vector2(pxX, pxY);

            if (offset.Length() <= ringRadiusPx)
            {
                return new MarkerProjection(screenCenter + offset, IsVisible: true, IsClampedToRing: false);
            }

            Vector2 clamped = Vector2.Normalize(offset) * ringRadiusPx;
            return new MarkerProjection(screenCenter + clamped, IsVisible: true, IsClampedToRing: true);
        }

        // On or behind camera plane (localDir.Z >= -1e-4f): clamp to perimeter in 2D direction
        var dir2D = new Vector2(localDir.X, -localDir.Y);
        if (dir2D.LengthSquared() < 1e-6f)
        {
            dir2D = Vector2.UnitY;
        }
        else
        {
            dir2D = Vector2.Normalize(dir2D);
        }

        return new MarkerProjection(screenCenter + (dir2D * ringRadiusPx), IsVisible: true, IsClampedToRing: true);
    }
}
