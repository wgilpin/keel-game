namespace Keel.Sim.Persistence;

public enum PersistenceStatus
{
    None = 0,
    Saved = 1,
    Loaded = 2,
    Error = 3,
}

public sealed class PersistenceSession
{
    public const float FlashDurationSeconds = 2.0f;

    private float _flashRemainingSeconds;

    public PersistenceStatus Status { get; private set; } = PersistenceStatus.None;
    public string? LastError { get; private set; }

    public bool IsFlashActive => _flashRemainingSeconds > 0.0f;

    public void RecordSaved()
    {
        Status = PersistenceStatus.Saved;
        LastError = null;
        _flashRemainingSeconds = FlashDurationSeconds;
    }

    public void RecordLoaded()
    {
        Status = PersistenceStatus.Loaded;
        LastError = null;
        _flashRemainingSeconds = FlashDurationSeconds;
    }

    public void RecordError(string error)
    {
        Status = PersistenceStatus.Error;
        LastError = error;
        _flashRemainingSeconds = FlashDurationSeconds;
    }

    public void Tick(float deltaSeconds)
    {
        if (deltaSeconds <= 0.0f || _flashRemainingSeconds <= 0.0f)
        {
            return;
        }

        _flashRemainingSeconds -= deltaSeconds;
        if (_flashRemainingSeconds <= 0.0f)
        {
            _flashRemainingSeconds = 0.0f;
            if (Status != PersistenceStatus.Error)
            {
                Status = PersistenceStatus.None;
            }
        }
    }
}
