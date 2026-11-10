namespace Keel.Sim.Config;

public sealed class TuningSession
{
    public const float ReloadFlashDurationSeconds = 1.5f;

    private float _flashRemainingSeconds;

    public TuningSession(TuningConfig initialConfig)
    {
        Current = initialConfig;
    }

    public TuningConfig Current { get; private set; }

    public bool IsReloadFlashActive => _flashRemainingSeconds > 0.0f;

    public string? LastError { get; private set; }

    public bool ApplyInitial(string? json)
    {
        var result = TuningConfigLoader.LoadOrFallback(json, Current);
        Current = result.Config;
        LastError = result.ErrorMessage;
        _flashRemainingSeconds = 0.0f;
        return result.IsSuccess;
    }

    public bool ApplyReload(string? json)
    {
        var result = TuningConfigLoader.LoadOrFallback(json, Current);
        Current = result.Config;
        LastError = result.ErrorMessage;

        if (result.IsSuccess)
        {
            _flashRemainingSeconds = ReloadFlashDurationSeconds;
            return true;
        }

        _flashRemainingSeconds = 0.0f;
        return false;
    }

    public void Tick(float deltaSeconds)
    {
        if (deltaSeconds <= 0.0f || _flashRemainingSeconds <= 0.0f)
        {
            return;
        }

        _flashRemainingSeconds -= deltaSeconds;
        if (_flashRemainingSeconds < 0.0f)
        {
            _flashRemainingSeconds = 0.0f;
        }
    }
}
