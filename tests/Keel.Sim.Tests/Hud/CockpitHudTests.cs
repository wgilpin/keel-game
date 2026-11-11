using System;
using System.Numerics;
using Keel.Sim.Hud;
using Xunit;

namespace Keel.Sim.Tests.Hud;

public sealed class CockpitHudTests
{
    [Fact]
    public void ReticleProjector_HandlesDeadzoneProjectionAndRearClamping()
    {
        var viewport = new Vector2(1920f, 1080f);
        var center = viewport * 0.5f;
        const float ringRadius = 180.0f;
        const float fov = 75.0f;

        // 1. Below 0.5 m/s speed threshold: both markers hidden
        ReticleProjectionResult slow = CockpitReticleProjector.Project(
            shipLocalVelocity: new Vector3(0.1f, 0.2f, -0.3f), // Length ~0.37 < 0.5
            viewportSize: viewport,
            verticalFovDegrees: fov,
            ringRadiusPx: ringRadius);

        Assert.False(slow.Prograde.IsVisible);
        Assert.False(slow.Retrograde.IsVisible);

        // 2. Pure forward flight at 40 m/s:
        // Prograde directly at screen center, unclamped.
        // Retrograde is directly behind, clamped to ring edge.
        ReticleProjectionResult forward = CockpitReticleProjector.Project(
            shipLocalVelocity: new Vector3(0f, 0f, -40f),
            viewportSize: viewport,
            verticalFovDegrees: fov,
            ringRadiusPx: ringRadius);

        Assert.True(forward.Prograde.IsVisible);
        Assert.False(forward.Prograde.IsClampedToRing);
        Assert.Equal(center.X, forward.Prograde.ScreenPosition.X, precision: 2);
        Assert.Equal(center.Y, forward.Prograde.ScreenPosition.Y, precision: 2);

        Assert.True(forward.Retrograde.IsVisible);
        Assert.True(forward.Retrograde.IsClampedToRing);
        float retroDist = (forward.Retrograde.ScreenPosition - center).Length();
        Assert.Equal(ringRadius, retroDist, precision: 2);

        // 3. Diagonal forward-right flight within HUD ring (on-axis)
        // 5 degrees right of bore: tan(5 deg) * focalLength < ringRadius
        float smallRight = MathF.Tan(5f * MathF.PI / 180f);
        ReticleProjectionResult slightDrift = CockpitReticleProjector.Project(
            shipLocalVelocity: new Vector3(smallRight * 30f, 0f, -30f),
            viewportSize: viewport,
            verticalFovDegrees: fov,
            ringRadiusPx: ringRadius);

        Assert.True(slightDrift.Prograde.IsVisible);
        Assert.False(slightDrift.Prograde.IsClampedToRing);
        Assert.True(slightDrift.Prograde.ScreenPosition.X > center.X);
        Assert.Equal(center.Y, slightDrift.Prograde.ScreenPosition.Y, precision: 2);
        float slightDist = (slightDrift.Prograde.ScreenPosition - center).Length();
        Assert.True(slightDist < ringRadius);

        // 4. Off-axis strafe (45 degrees forward-right): projected offset exceeds ringRadius -> clamped to ring
        ReticleProjectionResult wideStrafe = CockpitReticleProjector.Project(
            shipLocalVelocity: new Vector3(30f, 0f, -30f),
            viewportSize: viewport,
            verticalFovDegrees: fov,
            ringRadiusPx: ringRadius);

        Assert.True(wideStrafe.Prograde.IsVisible);
        Assert.True(wideStrafe.Prograde.IsClampedToRing);
        float wideDist = (wideStrafe.Prograde.ScreenPosition - center).Length();
        Assert.Equal(ringRadius, wideDist, precision: 2);
        Assert.True(wideStrafe.Prograde.ScreenPosition.X > center.X);

        // 5. Pure rear flight (moving backward at 20 m/s):
        // Prograde is behind -> clamped to ring
        // Retrograde is forward -> at center, unclamped
        ReticleProjectionResult backward = CockpitReticleProjector.Project(
            shipLocalVelocity: new Vector3(0f, 0f, 20f),
            viewportSize: viewport,
            verticalFovDegrees: fov,
            ringRadiusPx: ringRadius);

        Assert.True(backward.Prograde.IsVisible);
        Assert.True(backward.Prograde.IsClampedToRing);
        Assert.Equal(ringRadius, (backward.Prograde.ScreenPosition - center).Length(), precision: 2);

        Assert.True(backward.Retrograde.IsVisible);
        Assert.False(backward.Retrograde.IsClampedToRing);
        Assert.Equal(center.X, backward.Retrograde.ScreenPosition.X, precision: 2);
        Assert.Equal(center.Y, backward.Retrograde.ScreenPosition.Y, precision: 2);
    }

    [Fact]
    public void ContactsAndInfoLog_FormatRangesAndCapHistoryAtThree()
    {
        // 1. Distance formatting: < 1000m as integer 'm', >= 1000m as 1-decimal 'km'
        Assert.Equal("500 m", CockpitContactsModel.FormatDistance(500.4f));
        Assert.Equal("999 m", CockpitContactsModel.FormatDistance(999.2f));
        Assert.Equal("1.0 km", CockpitContactsModel.FormatDistance(1000.0f));
        Assert.Equal("1.2 km", CockpitContactsModel.FormatDistance(1234.5f));
        Assert.Equal("20.0 km", CockpitContactsModel.FormatDistance(20000.0f));

        // 2. Contacts model: sorts nearest-first
        // At origin (0, 0, 0): Station is at (0, 0, -500) -> 500m; Asteroid is at (600, 150, -1200) -> ~1350m
        var contactsAtOrigin = CockpitContactsModel.Compute(Vector3.Zero);
        Assert.Equal(2, contactsAtOrigin.Count);
        Assert.Equal("STATION", contactsAtOrigin[0].Name);
        Assert.Equal("500 m", contactsAtOrigin[0].FormattedDistance);
        Assert.Equal("ASTEROID", contactsAtOrigin[1].Name);
        Assert.Equal("1.4 km", contactsAtOrigin[1].FormattedDistance);

        // Near asteroid at (600, 150, -1150) -> Asteroid is 50m away, Station is ~900m away
        var contactsNearAsteroid = CockpitContactsModel.Compute(new Vector3(600f, 150f, -1150f));
        Assert.Equal("ASTEROID", contactsNearAsteroid[0].Name);
        Assert.Equal("50 m", contactsNearAsteroid[0].FormattedDistance);
        Assert.Equal("STATION", contactsNearAsteroid[1].Name);

        // 3. HudInfoLog ring buffer: retains at most 3 entries in chronological order
        var log = new HudInfoLog();
        Assert.Empty(log.Entries);

        log.Push("SYS: FLIGHT COMPUTER ONLINE");
        log.Push("DMP: INERTIAL DAMPENERS ON");
        Assert.Equal(2, log.Entries.Count);
        Assert.Equal("SYS: FLIGHT COMPUTER ONLINE", log.Entries[0]);
        Assert.Equal("DMP: INERTIAL DAMPENERS ON", log.Entries[1]);

        log.Push("CFG: TUNING RELOADED");
        Assert.Equal(3, log.Entries.Count);
        Assert.Equal("SYS: FLIGHT COMPUTER ONLINE", log.Entries[0]);
        Assert.Equal("DMP: INERTIAL DAMPENERS ON", log.Entries[1]);
        Assert.Equal("CFG: TUNING RELOADED", log.Entries[2]);

        // 4th entry evicts the oldest ("SYS: FLIGHT COMPUTER ONLINE")
        log.Push("NAV: APPROACHING STATION");
        Assert.Equal(3, log.Entries.Count);
        Assert.Equal("DMP: INERTIAL DAMPENERS ON", log.Entries[0]);
        Assert.Equal("CFG: TUNING RELOADED", log.Entries[1]);
        Assert.Equal("NAV: APPROACHING STATION", log.Entries[2]);
    }

    [Fact]
    public void ContactsModel_ComputesStationAndAsteroidBearingsForCockpit()
    {
        // AC: CockpitContactsModel computes landmark bearing results for STATION and ASTEROID
        // relative to cockpit position and camera basis, allowing the pilot to orient toward the station.
        var shipPos = Vector3.Zero;
        var camForward = -Vector3.UnitZ;
        var camRight = Vector3.UnitX;

        IReadOnlyList<BearingMarkerResult> bearings = CockpitContactsModel.ComputeBearings(
            shipPos,
            camForward,
            camRight);

        Assert.Equal(2, bearings.Count);

        // Station at (0, 0, -500): directly ahead along -Z -> Azimuth 0, unclamped
        BearingMarkerResult station = bearings[0];
        Assert.Equal("STATION", station.Name);
        Assert.Equal(500.0f, station.DistanceMeters, precision: 1);
        Assert.Equal("500 m", station.FormattedDistance);
        Assert.Equal(0.0f, station.AzimuthDegrees, precision: 2);
        Assert.Equal(0.0f, station.NormalizedX, precision: 2);
        Assert.False(station.IsClamped);
        Assert.False(station.IsRear);

        // Asteroid at (600, 150, -1200): forward-right -> lateral > 0, forward > 0
        BearingMarkerResult asteroid = bearings[1];
        Assert.Equal("ASTEROID", asteroid.Name);
        Assert.True(asteroid.DistanceMeters > 1300f);
        Assert.True(asteroid.AzimuthDegrees > 0f);

        // When ship camera turns 180 degrees (looking along +Z), station is directly behind -> clamped to edge and IsRear
        var rearBearings = CockpitContactsModel.ComputeBearings(
            shipPos,
            cameraForward: Vector3.UnitZ,
            cameraRight: -Vector3.UnitX);

        BearingMarkerResult rearStation = rearBearings[0];
        Assert.Equal("STATION", rearStation.Name);
        Assert.True(rearStation.IsClamped);
        Assert.True(rearStation.IsRear);
    }
}
