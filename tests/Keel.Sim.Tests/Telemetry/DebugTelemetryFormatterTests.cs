using System.Numerics;
using Keel.Sim.Grid;
using Keel.Sim.Pilot;
using Keel.Sim.Telemetry;
using Xunit;

namespace Keel.Sim.Tests.Telemetry;

public sealed class DebugTelemetryFormatterTests
{
    [Fact]
    public void Format_SurfacesGrinderProgress()
    {
        var snapshot = new DebugTelemetrySnapshot(
            Position: new Vector3(0f, 0f, -500f),
            LinearVelocity: Vector3.Zero,
            AngularVelocity: Vector3.Zero,
            DampenersEnabled: true,
            IsReloadFlashActive: false,
            LastConfigError: null,
            Mode: PilotMode.FloatingInSuit,
            TargetGridCell: new Int3(1, 0, 2),
            TargetBlockType: BlockType.ArmorBlock,
            GrinderProgress: 0.65f);

        string output = DebugTelemetryFormatter.Format(in snapshot);

        Assert.Contains("MODE SUIT", output);
        Assert.Contains("GRID (1, 0, 2) ARMORBLOCK", output);
        Assert.Contains("-> DECONSTRUCT (65%)", output);
    }
}
