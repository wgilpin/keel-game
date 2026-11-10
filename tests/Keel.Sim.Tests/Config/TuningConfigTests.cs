using Keel.Sim.Config;
using Xunit;

namespace Keel.Sim.Tests.Config;

public sealed class TuningConfigTests
{
    [Fact]
    public void LoadOrFallback_ParsesValidJsonAndRecoversFromInvalidJson()
    {
        const string validJson = """
        {
          "shipMass": 1500.0,
          "linearThrust": 30000.0,
          "angularTorque": 6000.0,
          "dampenerStrength": 5.5,
          "maxLinearSpeed": 110.0,
          "maxAngularSpeed": 3.2,
          "mouseSensitivity": 0.004
        }
        """;

        var loaded = TuningConfigLoader.LoadOrFallback(validJson, TuningConfig.Default);

        Assert.True(loaded.IsSuccess);
        Assert.Null(loaded.ErrorMessage);
        Assert.Equal(1500.0f, loaded.Config.ShipMass);
        Assert.Equal(30000.0f, loaded.Config.LinearThrust);
        Assert.Equal(6000.0f, loaded.Config.AngularTorque);
        Assert.Equal(5.5f, loaded.Config.DampenerStrength);
        Assert.Equal(110.0f, loaded.Config.MaxLinearSpeed);
        Assert.Equal(3.2f, loaded.Config.MaxAngularSpeed);
        Assert.Equal(0.004f, loaded.Config.MouseSensitivity);

        var customFallback = loaded.Config;

        var malformed = TuningConfigLoader.LoadOrFallback("{ not-valid-json", customFallback);
        Assert.False(malformed.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(malformed.ErrorMessage));
        Assert.Equal(customFallback, malformed.Config);

        var missing = TuningConfigLoader.LoadOrFallback(null, customFallback);
        Assert.False(missing.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(missing.ErrorMessage));
        Assert.Equal(customFallback, missing.Config);
    }

    [Fact]
    public void Reload_UpdatesActiveConfigAndExpiresFlashTimer()
    {
        var session = new TuningSession(TuningConfig.Default);
        Assert.False(session.IsReloadFlashActive);
        Assert.Null(session.LastError);

        const string updatedJson = """
        {
          "shipMass": 1200.0,
          "linearThrust": 36000.0,
          "angularTorque": 4800.0,
          "dampenerStrength": 4.0,
          "maxLinearSpeed": 140.0,
          "maxAngularSpeed": 2.5,
          "mouseSensitivity": 0.0025
        }
        """;

        bool reloaded = session.ApplyReload(updatedJson);

        Assert.True(reloaded);
        Assert.Equal(140.0f, session.Current.MaxLinearSpeed);
        Assert.Equal(36000.0f, session.Current.LinearThrust);
        Assert.True(session.IsReloadFlashActive);
        Assert.Null(session.LastError);

        session.Tick(1.0f);
        Assert.True(session.IsReloadFlashActive);

        session.Tick(0.5f);
        Assert.False(session.IsReloadFlashActive);

        // A subsequent broken save retains the active reloaded config and surfaces the error.
        bool brokenReload = session.ApplyReload("{ broken");
        Assert.False(brokenReload);
        Assert.Equal(140.0f, session.Current.MaxLinearSpeed);
        Assert.False(session.IsReloadFlashActive);
        Assert.False(string.IsNullOrWhiteSpace(session.LastError));
    }
}
