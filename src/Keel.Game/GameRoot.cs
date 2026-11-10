using System;
using System.IO;
using Godot;
using Keel.Game.Flight;
using Keel.Game.Input;
using Keel.Game.UI;
using Keel.Sim.Config;
using Keel.Sim.Flight;
using Keel.Sim.Telemetry;
using SimVector3 = System.Numerics.Vector3;

namespace Keel.Game;

public partial class GameRoot : Node3D
{
    private const double ConfigPollIntervalSeconds = 0.25;

    private ShipRigNode _shipRig = null!;
    private DebugOverlayNode _debugOverlay = null!;
    private readonly ShipInputReader _inputReader = new();
    private readonly TuningSession _tuningSession = new(TuningConfig.Default);

    private string _configPath = string.Empty;
    private DateTime _lastObservedWriteUtc;
    private double _pollAccumulator;
    private bool _f3WasPressed;

    private static readonly Color SpaceBackdrop = new(0x05 / 255f, 0x07 / 255f, 0x0B / 255f, 1.0f);

    public TuningConfig CurrentTuning => _tuningSession.Current;

    public override void _Ready()
    {
        var env = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Color,
            BackgroundColor = SpaceBackdrop,
        };
        AddChild(new WorldEnvironment { Environment = env });

        _configPath = ResolveConfigPath();
        LoadInitialConfig();

        _shipRig = new ShipRigNode();
        AddChild(_shipRig);

        _debugOverlay = new DebugOverlayNode();
        AddChild(_debugOverlay);

        Godot.Input.MouseMode = Godot.Input.MouseModeEnum.Captured;
    }

    public override void _Input(InputEvent @event)
    {
        bool isCaptured = Godot.Input.MouseMode == Godot.Input.MouseModeEnum.Captured;
        _inputReader.HandleInputEvent(@event, isCaptured);

        if (_inputReader.ConsumeMouseCaptureToggle())
        {
            Godot.Input.MouseMode = isCaptured
                ? Godot.Input.MouseModeEnum.Visible
                : Godot.Input.MouseModeEnum.Captured;
        }
        else if (_inputReader.ConsumeMouseRecaptureClick())
        {
            Godot.Input.MouseMode = Godot.Input.MouseModeEnum.Captured;
        }

        if (_inputReader.ConsumeDampenerToggle())
        {
            _shipRig.DampenersEnabled = !_shipRig.DampenersEnabled;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        bool isCaptured = Godot.Input.MouseMode == Godot.Input.MouseModeEnum.Captured;
        FlightInput flightInput = _inputReader.ConsumeFlightInput(isCaptured);
        _shipRig.ApplyFlightStep(in flightInput, CurrentTuning, (float)delta);
    }

    public override void _Process(double delta)
    {
        bool f3Pressed = Godot.Input.IsKeyPressed(Key.F3);
        if (f3Pressed && !_f3WasPressed)
        {
            _debugOverlay.ToggleVisibility();
        }
        _f3WasPressed = f3Pressed;

        _tuningSession.Tick((float)delta);
        PollConfigReload(delta);

        Vector3 pos = _shipRig.GlobalPosition;
        Vector3 linVel = _shipRig.LinearVelocity;
        Vector3 angVel = _shipRig.AngularVelocity;

        var snapshot = new DebugTelemetrySnapshot(
            Position: new SimVector3(pos.X, pos.Y, pos.Z),
            LinearVelocity: new SimVector3(linVel.X, linVel.Y, linVel.Z),
            AngularVelocity: new SimVector3(angVel.X, angVel.Y, angVel.Z),
            DampenersEnabled: _shipRig.DampenersEnabled,
            IsReloadFlashActive: _tuningSession.IsReloadFlashActive,
            LastConfigError: _tuningSession.LastError);

        _debugOverlay.Render(in snapshot);
    }

    private void LoadInitialConfig()
    {
        if (TryReadFile(_configPath, out string? json, out DateTime writeUtc))
        {
            _lastObservedWriteUtc = writeUtc;
            _tuningSession.ApplyInitial(json);
        }
        else
        {
            _tuningSession.ApplyInitial(null);
        }
    }

    private void PollConfigReload(double delta)
    {
        _pollAccumulator += delta;
        if (_pollAccumulator < ConfigPollIntervalSeconds)
        {
            return;
        }
        _pollAccumulator = 0.0;

        if (!File.Exists(_configPath))
        {
            return;
        }

        DateTime currentWriteUtc;
        try
        {
            currentWriteUtc = File.GetLastWriteTimeUtc(_configPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return;
        }

        if (currentWriteUtc <= _lastObservedWriteUtc)
        {
            return;
        }

        if (TryReadFile(_configPath, out string? json, out DateTime confirmedWriteUtc))
        {
            _lastObservedWriteUtc = confirmedWriteUtc;
            _tuningSession.ApplyReload(json);
        }
    }

    private static bool TryReadFile(string path, out string? content, out DateTime writeUtc)
    {
        content = null;
        writeUtc = DateTime.MinValue;

        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            writeUtc = File.GetLastWriteTimeUtc(path);
            content = File.ReadAllText(path);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Editor may hold a transient lock or atomic-replace handle mid-save; retry on next poll.
            return false;
        }
    }

    private static string ResolveConfigPath()
    {
        string projectDir = ProjectSettings.GlobalizePath("res://");
        string repoCandidate = Path.GetFullPath(Path.Combine(projectDir, "..", "..", "config", "tuning.json"));
        return File.Exists(repoCandidate)
            ? repoCandidate
            : Path.Combine(projectDir, "tuning.json");
    }
}
