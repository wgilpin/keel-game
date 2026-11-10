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

    [Fact]
    public void LoadOrFallback_ValidatesSuitAndMiningParametersWithSafeFallback()
    {
        const string validJson = """
        {
          "shipMass": 1200.0,
          "linearThrust": 24000.0,
          "angularTorque": 4800.0,
          "dampenerStrength": 4.0,
          "maxLinearSpeed": 80.0,
          "maxAngularSpeed": 2.5,
          "mouseSensitivity": 0.0025,
          "suitMass": 95.0,
          "suitLinearThrust": 1400.0,
          "suitAngularTorque": 220.0,
          "suitMaxLinearSpeed": 25.0,
          "shipInteractRange": 6.0,
          "drillRange": 5.0,
          "drillRate": 1.5,
          "suitCargoCapacity": 50.0
        }
        """;

        var loaded = TuningConfigLoader.LoadOrFallback(validJson, TuningConfig.Default);
        Assert.True(loaded.IsSuccess);
        Assert.Null(loaded.ErrorMessage);
        Assert.Equal(95.0f, loaded.Config.SuitMass);
        Assert.Equal(1400.0f, loaded.Config.SuitLinearThrust);
        Assert.Equal(220.0f, loaded.Config.SuitAngularTorque);
        Assert.Equal(25.0f, loaded.Config.SuitMaxLinearSpeed);
        Assert.Equal(6.0f, loaded.Config.ShipInteractRange);
        Assert.Equal(5.0f, loaded.Config.DrillRange);
        Assert.Equal(1.5f, loaded.Config.DrillRate);
        Assert.Equal(50.0f, loaded.Config.SuitCargoCapacity);

        // Invalid negative or non-positive suit parameters trigger fallback
        const string invalidSuitMassJson = """
        {
          "suitMass": -10.0
        }
        """;

        var fallback = loaded.Config;
        var invalidMassResult = TuningConfigLoader.LoadOrFallback(invalidSuitMassJson, fallback);
        Assert.False(invalidMassResult.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(invalidMassResult.ErrorMessage));
        Assert.Equal(fallback, invalidMassResult.Config);

        const string invalidSpeedJson = """
        {
          "suitMaxLinearSpeed": 0.0
        }
        """;
        var invalidSpeedResult = TuningConfigLoader.LoadOrFallback(invalidSpeedJson, fallback);
        Assert.False(invalidSpeedResult.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(invalidSpeedResult.ErrorMessage));
        Assert.Equal(fallback, invalidSpeedResult.Config);
    }

    [Fact]
    public void LoadOrFallback_ValidatesRefineryParametersWithSafeFallback()
    {
        const string validJson = """
        {
          "refineryCycleSeconds": 2.5,
          "refineryOrePerBlock": 3.0,
          "refineryInteractRange": 6.5,
          "refineryBufferCapacity": 120.0
        }
        """;

        var loaded = TuningConfigLoader.LoadOrFallback(validJson, TuningConfig.Default);
        Assert.True(loaded.IsSuccess);
        Assert.Null(loaded.ErrorMessage);
        Assert.Equal(2.5f, loaded.Config.RefineryCycleSeconds);
        Assert.Equal(3.0f, loaded.Config.RefineryOrePerBlock);
        Assert.Equal(6.5f, loaded.Config.RefineryInteractRange);
        Assert.Equal(120.0f, loaded.Config.RefineryBufferCapacity);

        // Non-positive or invalid refinery values trigger safe fallback
        const string invalidCycleJson = """
        {
          "refineryCycleSeconds": -1.0
        }
        """;

        var fallback = loaded.Config;
        var invalidCycle = TuningConfigLoader.LoadOrFallback(invalidCycleJson, fallback);
        Assert.False(invalidCycle.IsSuccess);
        Assert.Equal(fallback, invalidCycle.Config);

        const string zeroCapacityJson = """
        {
          "refineryBufferCapacity": 0.0
        }
        """;
        var invalidCap = TuningConfigLoader.LoadOrFallback(zeroCapacityJson, fallback);
        Assert.False(invalidCap.IsSuccess);
        Assert.Equal(fallback, invalidCap.Config);
    }
}
