using System.Globalization;
using System.Numerics;

namespace Keel.Sim.Telemetry;

public readonly record struct DebugTelemetrySnapshot(
    Vector3 Position,
    Vector3 LinearVelocity,
    Vector3 AngularVelocity,
    bool DampenersEnabled,
    bool IsReloadFlashActive,
    string? LastConfigError);

public static class DebugTelemetryFormatter
{
    public static string Format(in DebugTelemetrySnapshot snapshot)
    {
        var inv = CultureInfo.InvariantCulture;
        float speed = snapshot.LinearVelocity.Length();
        float originDist = snapshot.Position.Length();
        string dmp = snapshot.DampenersEnabled ? "ON" : "OFF";
        string cfg = !string.IsNullOrEmpty(snapshot.LastConfigError)
            ? $"ERR ({snapshot.LastConfigError})"
            : snapshot.IsReloadFlashActive
                ? "RELOADED"
                : "OK";

        return string.Create(inv,
            $"POS  {snapshot.Position.X,8:F1} {snapshot.Position.Y,8:F1} {snapshot.Position.Z,8:F1} m\n" +
            $"VEL  {snapshot.LinearVelocity.X,8:F1} {snapshot.LinearVelocity.Y,8:F1} {snapshot.LinearVelocity.Z,8:F1} m/s ({speed:F1})\n" +
            $"ANG  {snapshot.AngularVelocity.X,8:F2} {snapshot.AngularVelocity.Y,8:F2} {snapshot.AngularVelocity.Z,8:F2} rad/s\n" +
            $"DMP  {dmp,-8} ORG {originDist,8:F1} m\n" +
            $"CFG  {cfg}");
    }
}
