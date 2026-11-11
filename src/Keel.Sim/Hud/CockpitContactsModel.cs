using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using Keel.Sim.Arena;

namespace Keel.Sim.Hud;

public readonly record struct LandmarkContact(
    string Name,
    Vector3 WorldPosition,
    float DistanceMeters,
    string FormattedDistance);

public static class CockpitContactsModel
{
    public static IReadOnlyList<LandmarkContact> Compute(Vector3 shipPosition)
    {
        var stationPos = SpaceArenaLayout.StationPosition;
        var asteroidPos = SpaceArenaLayout.AsteroidPosition;

        float stationDist = (stationPos - shipPosition).Length();
        float asteroidDist = (asteroidPos - shipPosition).Length();

        var station = new LandmarkContact("STATION", stationPos, stationDist, FormatDistance(stationDist));
        var asteroid = new LandmarkContact("ASTEROID", asteroidPos, asteroidDist, FormatDistance(asteroidDist));

        return stationDist <= asteroidDist
            ? [station, asteroid]
            : [asteroid, station];
    }

    public static readonly BearingLandmark[] CockpitLandmarks =
    [
        new BearingLandmark("STATION", SpaceArenaLayout.StationPosition),
        new BearingLandmark("ASTEROID", SpaceArenaLayout.AsteroidPosition),
    ];

    public static IReadOnlyList<BearingMarkerResult> ComputeBearings(
        Vector3 shipPosition,
        Vector3 cameraForward,
        Vector3 cameraRight,
        float spanDegrees = VisorBearingTapeModel.DefaultSpanDegrees)
    {
        return VisorBearingTapeModel.Compute(shipPosition, cameraForward, cameraRight, CockpitLandmarks, spanDegrees);
    }

    public static string FormatDistance(float distanceMeters)
    {
        if (distanceMeters < 1000.0f)
        {
            int meters = (int)MathF.Round(distanceMeters);
            return string.Create(CultureInfo.InvariantCulture, $"{meters} m");
        }

        float km = distanceMeters / 1000.0f;
        return string.Create(CultureInfo.InvariantCulture, $"{km:F1} km");
    }
}
