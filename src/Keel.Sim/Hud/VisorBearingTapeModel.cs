using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using Keel.Sim.Arena;

namespace Keel.Sim.Hud;

public readonly record struct CompassTick(
    float NormalizedX,
    int HeadingDegrees,
    string Label,
    bool IsMajor = false);

public readonly record struct BearingMarkerResult(
    string Name,
    Vector3 WorldPosition,
    float DistanceMeters,
    string FormattedDistance,
    float AzimuthDegrees,
    float NormalizedX,
    bool IsClamped,
    bool IsRear);

public readonly record struct BearingLandmark(
    string Name,
    Vector3 WorldPosition);

public static class VisorBearingTapeModel
{
    public const float DefaultSpanDegrees = 90.0f;
    public const float DefaultTickIntervalDegrees = 15.0f;

    public static BearingMarkerResult ProjectTarget(
        Vector3 cameraPosition,
        Vector3 cameraForward,
        Vector3 cameraRight,
        string name,
        Vector3 targetPosition,
        float spanDegrees = DefaultSpanDegrees)
    {
        Vector3 delta = targetPosition - cameraPosition;
        float distance = delta.Length();
        string formattedDistance = CockpitContactsModel.FormatDistance(distance);

        if (distance < 1e-5f)
        {
            return new BearingMarkerResult(
                name,
                targetPosition,
                0.0f,
                formattedDistance,
                0.0f,
                0.0f,
                IsClamped: false,
                IsRear: false);
        }

        Vector3 normRight = cameraRight.LengthSquared() > 1e-6f ? Vector3.Normalize(cameraRight) : Vector3.UnitX;
        Vector3 normForward = cameraForward.LengthSquared() > 1e-6f ? Vector3.Normalize(cameraForward) : -Vector3.UnitZ;

        float lateral = Vector3.Dot(delta, normRight);
        float forward = Vector3.Dot(delta, normForward);

        float azimuthRad = MathF.Atan2(lateral, forward);
        float azimuthDeg = azimuthRad * (180.0f / MathF.PI);

        bool isRear = forward < 0.0f;
        float halfSpan = MathF.Max(spanDegrees * 0.5f, 1e-3f);
        float unclampedX = azimuthDeg / halfSpan;

        float normalizedX;
        bool isClamped;

        if (isRear)
        {
            // Rear targets clamp to +/-1 according to lateral side without flipping across 180 seam
            normalizedX = lateral >= 0.0f ? 1.0f : -1.0f;
            isClamped = true;
        }
        else if (unclampedX > 1.0f)
        {
            normalizedX = 1.0f;
            isClamped = true;
        }
        else if (unclampedX < -1.0f)
        {
            normalizedX = -1.0f;
            isClamped = true;
        }
        else
        {
            normalizedX = unclampedX;
            isClamped = false;
        }

        return new BearingMarkerResult(
            name,
            targetPosition,
            distance,
            formattedDistance,
            azimuthDeg,
            normalizedX,
            isClamped,
            isRear);
    }

    public static IReadOnlyList<BearingMarkerResult> Compute(
        Vector3 cameraPosition,
        Vector3 cameraForward,
        Vector3 cameraRight,
        IEnumerable<BearingLandmark> landmarks,
        float spanDegrees = DefaultSpanDegrees)
    {
        var results = new List<BearingMarkerResult>();
        foreach (BearingLandmark landmark in landmarks)
        {
            results.Add(ProjectTarget(
                cameraPosition,
                cameraForward,
                cameraRight,
                landmark.Name,
                landmark.WorldPosition,
                spanDegrees));
        }
        return results;
    }

    public static IReadOnlyList<BearingMarkerResult> Compute(
        Vector3 cameraPosition,
        Vector3 cameraForward,
        Vector3 cameraRight,
        Vector3 shipPosition,
        float spanDegrees = DefaultSpanDegrees)
    {
        var landmarks = new[]
        {
            new BearingLandmark("SHIP", shipPosition),
            new BearingLandmark("STATION", SpaceArenaLayout.StationPosition),
            new BearingLandmark("ASTEROID", SpaceArenaLayout.AsteroidPosition),
        };

        return Compute(cameraPosition, cameraForward, cameraRight, landmarks, spanDegrees);
    }

    public static IReadOnlyList<CompassTick> ComputeTicks(
        float cameraHeadingDegrees,
        float spanDegrees = DefaultSpanDegrees,
        float tickIntervalDegrees = DefaultTickIntervalDegrees)
    {
        float halfSpan = MathF.Max(spanDegrees * 0.5f, 1e-3f);
        float interval = MathF.Max(tickIntervalDegrees, 1.0f);

        float minAngle = cameraHeadingDegrees - halfSpan;
        float maxAngle = cameraHeadingDegrees + halfSpan;

        float start = MathF.Ceiling(minAngle / interval) * interval;
        float end = MathF.Floor(maxAngle / interval) * interval;

        var ticks = new List<CompassTick>();

        for (float angle = start; angle <= end + 1e-4f; angle += interval)
        {
            float offset = angle - cameraHeadingDegrees;
            float normX = offset / halfSpan;
            if (normX < -1.0f) normX = -1.0f;
            if (normX > 1.0f) normX = 1.0f;

            float wrapped = angle % 360.0f;
            if (wrapped < 0.0f) wrapped += 360.0f;
            if (wrapped >= 360.0f) wrapped -= 360.0f;

            int heading = (int)MathF.Round(wrapped);
            if (heading >= 360) heading = 0;

            bool isMajor = (heading % 30) == 0;
            string label = heading.ToString(CultureInfo.InvariantCulture);

            ticks.Add(new CompassTick(normX, heading, label, isMajor));
        }

        return ticks;
    }

    public static float ComputeHeadingDegrees(Vector3 cameraForward)
    {
        if (MathF.Abs(cameraForward.X) < 1e-5f && MathF.Abs(cameraForward.Z) < 1e-5f)
        {
            return 0.0f;
        }

        // Godot forward is -Z: yaw of (0, 0, -1) is 0°, (1, 0, 0) is 90°
        float rad = MathF.Atan2(cameraForward.X, -cameraForward.Z);
        float deg = rad * (180.0f / MathF.PI);
        if (deg < 0.0f) deg += 360.0f;
        if (deg >= 360.0f) deg -= 360.0f;
        return deg;
    }
}
