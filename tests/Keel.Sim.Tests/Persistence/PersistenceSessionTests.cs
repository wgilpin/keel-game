using System.Numerics;
using Keel.Sim.Persistence;
using Keel.Sim.Telemetry;
using Xunit;

namespace Keel.Sim.Tests.Persistence;

public class PersistenceSessionTests
{
    [Fact]
    public void Session_TracksSaveLoadFlashTimerAndSurfacesErrorsInTelemetry()
    {
        var session = new PersistenceSession();
        Assert.Equal(PersistenceStatus.None, session.Status);
        Assert.False(session.IsFlashActive);

        // Test RecordSaved
        session.RecordSaved();
        Assert.Equal(PersistenceStatus.Saved, session.Status);
        Assert.True(session.IsFlashActive);

        var snapSaved = new DebugTelemetrySnapshot(
            Vector3.Zero, Vector3.Zero, Vector3.Zero, true, false, null,
            PersistenceStatus: session.Status,
            IsPersistenceFlashActive: session.IsFlashActive);
        string textSaved = DebugTelemetryFormatter.Format(snapSaved);
        Assert.Contains("SAV  SAVED", textSaved);

        // Tick decay
        session.Tick(1.0f);
        Assert.True(session.IsFlashActive);
        session.Tick(1.5f);
        Assert.False(session.IsFlashActive);
        Assert.Equal(PersistenceStatus.None, session.Status);

        var snapNone = new DebugTelemetrySnapshot(
            Vector3.Zero, Vector3.Zero, Vector3.Zero, true, false, null,
            PersistenceStatus: session.Status,
            IsPersistenceFlashActive: session.IsFlashActive);
        string textNone = DebugTelemetryFormatter.Format(snapNone);
        Assert.Contains("SAV  NONE", textNone);

        // Test RecordLoaded
        session.RecordLoaded();
        Assert.Equal(PersistenceStatus.Loaded, session.Status);
        Assert.True(session.IsFlashActive);

        var snapLoaded = new DebugTelemetrySnapshot(
            Vector3.Zero, Vector3.Zero, Vector3.Zero, true, false, null,
            PersistenceStatus: session.Status,
            IsPersistenceFlashActive: session.IsFlashActive);
        string textLoaded = DebugTelemetryFormatter.Format(snapLoaded);
        Assert.Contains("SAV  LOADED", textLoaded);

        // Test RecordError
        session.RecordError("DiskFull");
        Assert.Equal(PersistenceStatus.Error, session.Status);
        Assert.Equal("DiskFull", session.LastError);

        var snapErr = new DebugTelemetrySnapshot(
            Vector3.Zero, Vector3.Zero, Vector3.Zero, true, false, null,
            PersistenceStatus: session.Status,
            LastPersistenceError: session.LastError,
            IsPersistenceFlashActive: session.IsFlashActive);
        string textErr = DebugTelemetryFormatter.Format(snapErr);
        Assert.Contains("SAV  ERR (DiskFull)", textErr);
    }
}
