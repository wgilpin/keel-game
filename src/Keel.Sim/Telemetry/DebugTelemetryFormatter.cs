using System.Globalization;
using System.Numerics;
using Keel.Sim.Grid;
using Keel.Sim.Pilot;

namespace Keel.Sim.Telemetry;

public readonly record struct DebugTelemetrySnapshot(
    Vector3 Position,
    Vector3 LinearVelocity,
    Vector3 AngularVelocity,
    bool DampenersEnabled,
    bool IsReloadFlashActive,
    string? LastConfigError,
    PilotMode Mode = PilotMode.SeatedInCockpit,
    Int3? TargetGridCell = null,
    BlockType TargetBlockType = BlockType.None,
    Int3? PlacementCandidateCell = null,
    bool? IsPlacementValid = null,
    float? GrinderProgress = null,
    Persistence.PersistenceStatus PersistenceStatus = Persistence.PersistenceStatus.None,
    string? LastPersistenceError = null,
    bool IsPersistenceFlashActive = false,
    Power.StationPowerSnapshot? StationPower = null,
    int TotalCargoUnits = 0,
    int TotalCargoCapacity = 0,
    int CargoContainerCount = 0);

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

        string sav;
        if (!string.IsNullOrEmpty(snapshot.LastPersistenceError))
        {
            sav = $"SAV  ERR ({snapshot.LastPersistenceError})";
        }
        else if (snapshot.IsPersistenceFlashActive)
        {
            sav = snapshot.PersistenceStatus switch
            {
                Persistence.PersistenceStatus.Saved => "SAV  SAVED",
                Persistence.PersistenceStatus.Loaded => "SAV  LOADED",
                _ => "SAV  OK",
            };
        }
        else
        {
            sav = "SAV  NONE";
        }

        string mode = snapshot.Mode == PilotMode.FloatingInSuit ? "MODE SUIT" : "MODE COCKPIT";

        string gridText = snapshot.TargetGridCell.HasValue && snapshot.TargetBlockType != BlockType.None
            ? $"GRID {snapshot.TargetGridCell.Value} {snapshot.TargetBlockType.ToString().ToUpperInvariant()}"
            : "GRID --";

        if (snapshot.PlacementCandidateCell.HasValue && snapshot.IsPlacementValid.HasValue)
        {
            string validStr = snapshot.IsPlacementValid.Value ? "VALID" : "INVALID";
            gridText += $" -> PLACE {snapshot.PlacementCandidateCell.Value} {validStr}";
        }
        else if (snapshot.GrinderProgress.HasValue)
        {
            int percent = (int)MathF.Round(snapshot.GrinderProgress.Value * 100f);
            gridText += $" -> DECONSTRUCT ({percent}%)";
        }

        string pwr = snapshot.StationPower.HasValue
            ? $"PWR  {snapshot.StationPower.Value.GeneratedKw:F0} / {snapshot.StationPower.Value.DemandKw:F0} kW ({snapshot.StationPower.Value.Status.ToString().ToUpperInvariant()})"
            : "PWR  --";

        return string.Create(inv,
            $"{mode} | {gridText}\n" +
            $"POS  {snapshot.Position.X,8:F1} {snapshot.Position.Y,8:F1} {snapshot.Position.Z,8:F1} m\n" +
            $"VEL  {snapshot.LinearVelocity.X,8:F1} {snapshot.LinearVelocity.Y,8:F1} {snapshot.LinearVelocity.Z,8:F1} m/s ({speed:F1})\n" +
            $"ANG  {snapshot.AngularVelocity.X,8:F2} {snapshot.AngularVelocity.Y,8:F2} {snapshot.AngularVelocity.Z,8:F2} rad/s\n" +
            $"DMP  {dmp,-8} ORG {originDist,8:F1} m\n" +
            $"CFG  {cfg,-15} {sav}\n" +
            $"{pwr}");
    }
}
