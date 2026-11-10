using System;
using System.IO;
using Godot;
using Keel.Game.Flight;
using Keel.Game.Input;
using Keel.Game.Suit;
using Keel.Game.UI;
using Keel.Game.World;
using Keel.Sim.Config;
using Keel.Sim.Flight;
using Keel.Sim.Grid;
using Keel.Sim.Inventory;
using Keel.Sim.Mining;
using Keel.Sim.Pilot;
using Keel.Sim.Telemetry;
using SimQuaternion = System.Numerics.Quaternion;
using SimVector3 = System.Numerics.Vector3;

namespace Keel.Game;

public partial class GameRoot : Node3D
{
    private const double ConfigPollIntervalSeconds = 0.25;

    private ShipRigNode _shipRig = null!;
    private SuitRigNode _suitRig = null!;
    private DebugOverlayNode _debugOverlay = null!;
    private SpaceDustNode _spaceDust = null!;
    private CockpitHudNode _cockpitHud = null!;
    private SuitVisorHudNode _suitVisorHud = null!;
    private StationGridNode _stationNode = null!;
    private readonly ShipInputReader _inputReader = new();
    private readonly TuningSession _tuningSession = new(TuningConfig.Default);
    private readonly PilotModeStateMachine _pilotModeMachine = new();

    private string _configPath = string.Empty;
    private DateTime _lastObservedWriteUtc;
    private double _pollAccumulator;
    private bool _f3WasPressed;
    private float _drillProgress;
    private DrillStepResult _lastDrillResult;
    private bool _isAimingAtDeposit;
    private Int3? _targetedGridCell;
    private BlockType _targetedBlockType = BlockType.None;

    public TuningConfig CurrentTuning => _tuningSession.Current;
    public PilotMode CurrentPilotMode => _pilotModeMachine.CurrentMode;

    public override void _Ready()
    {
        AddChild(SpaceEnvironmentBuilder.CreateEnvironment());
        AddChild(SpaceEnvironmentBuilder.CreateSunlight());
        _stationNode = (StationGridNode)LandmarkBuilder.CreateStation();
        AddChild(_stationNode);
        AddChild(LandmarkBuilder.CreateAsteroid());

        _configPath = ResolveConfigPath();
        LoadInitialConfig();

        _shipRig = new ShipRigNode();
        AddChild(_shipRig);

        _suitRig = new SuitRigNode();
        AddChild(_suitRig);

        _debugOverlay = new DebugOverlayNode();
        AddChild(_debugOverlay);

        _cockpitHud = new CockpitHudNode();
        AddChild(_cockpitHud);

        _suitVisorHud = new SuitVisorHudNode();
        AddChild(_suitVisorHud);
        _suitVisorHud.Visible = false;

        _spaceDust = new SpaceDustNode();
        AddChild(_spaceDust);

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
            if (_pilotModeMachine.Mode == PilotMode.SeatedInCockpit)
            {
                _shipRig.DampenersEnabled = !_shipRig.DampenersEnabled;
                _cockpitHud.InfoLog.Push(_shipRig.DampenersEnabled
                    ? "DMP: INERTIAL DAMPENERS ON"
                    : "DMP: INERTIAL DAMPENERS OFF");
            }
            else
            {
                _suitRig.DampenersEnabled = !_suitRig.DampenersEnabled;
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        bool isCaptured = Godot.Input.MouseMode == Godot.Input.MouseModeEnum.Captured;

        // Handle EVA exit / board ship toggle (F key)
        if (_inputReader.ConsumeInteractToggle())
        {
            if (_pilotModeMachine.Mode == PilotMode.SeatedInCockpit)
            {
                // Exit cockpit into EVA suit
                Godot.Quaternion shipRotGodot = _shipRig.GlobalBasis.GetRotationQuaternion();
                SuitSpawnState spawn = _pilotModeMachine.ExitCockpit(
                    ToSim(_shipRig.GlobalPosition),
                    ToSim(shipRotGodot),
                    ToSim(_shipRig.LinearVelocity));

                _shipRig.SetHullVisible(true);
                _suitRig.Spawn(
                    ToGodot(spawn.Position),
                    ToGodot(spawn.Orientation),
                    ToGodot(spawn.LinearVelocity),
                    ToGodot(spawn.AngularVelocity));
                _suitRig.SetActive(true);
                _shipRig.CockpitCamera.Current = false;
                _suitRig.SuitCamera.Current = true;
                _cockpitHud.Visible = false;
                _suitVisorHud.Visible = true;
            }
            else if (_pilotModeMachine.Mode == PilotMode.FloatingInSuit)
            {
                // Try to board ship if within interact range
                bool boarded = _pilotModeMachine.TryBoardShip(
                    ToSim(_shipRig.GlobalPosition),
                    ToSim(_suitRig.GlobalPosition),
                    CurrentTuning.ShipInteractRange);

                if (boarded)
                {
                    _suitRig.SetActive(false);
                    _shipRig.SetHullVisible(false);
                    _suitRig.SuitCamera.Current = false;
                    _shipRig.CockpitCamera.Current = true;
                    _suitVisorHud.Visible = false;
                    _cockpitHud.Visible = true;
                    _cockpitHud.InfoLog.Push("PILOT: BOARDED SHIP");
                }
            }
        }

        // Apply physics flight step
        if (_pilotModeMachine.Mode == PilotMode.SeatedInCockpit)
        {
            _drillProgress = 0.0f;
            _lastDrillResult = default;
            _isAimingAtDeposit = false;
            _suitRig.SetDrillBeam(false, Vector3.Zero);

            FlightInput flightInput = _inputReader.ConsumeFlightInput(isCaptured);
            _shipRig.ApplyFlightStep(in flightInput, CurrentTuning, (float)delta);
        }
        else
        {
            // Unmanned ship continues dampening and boundary enforcement
            _shipRig.ApplyFlightStep(FlightInput.Zero, CurrentTuning, (float)delta);

            FlightInput suitInput = _inputReader.ConsumeFlightInput(isCaptured);
            _suitRig.ApplyFlightStep(in suitInput, CurrentTuning, (float)delta);

            // Forward raycast from SuitCamera out to 25m, excluding suit collider
            Vector3 camPos = _suitRig.SuitCamera.GlobalPosition;
            Vector3 rayDir = -_suitRig.SuitCamera.GlobalBasis.Z;
            Vector3 rayEnd = camPos + (rayDir * 25.0f);

            var spaceState = _suitRig.GetWorld3D().DirectSpaceState;
            var query = PhysicsRayQueryParameters3D.Create(camPos, rayEnd);
            query.Exclude = new Godot.Collections.Array<Rid> { _suitRig.GetRid() };

            OreDepositNode? targetedDeposit = null;
            float targetedDistance = float.MaxValue;
            Vector3 targetHitPos = Vector3.Zero;
            _targetedGridCell = null;
            _targetedBlockType = BlockType.None;

            var hit = spaceState.IntersectRay(query);
            if (hit.Count > 0)
            {
                targetHitPos = (Vector3)hit["position"];
                Vector3 hitNormal = hit.ContainsKey("normal") ? (Vector3)hit["normal"] : Vector3.Up;
                targetedDistance = camPos.DistanceTo(targetHitPos);
                var colliderObj = (GodotObject)hit["collider"];
                targetedDeposit = colliderObj as OreDepositNode ?? (colliderObj as Node)?.GetParent<OreDepositNode>();

                if (_stationNode.ResolveRaycastHit(targetHitPos, hitNormal, out Int3 cell, out BlockType bType))
                {
                    _targetedGridCell = cell;
                    _targetedBlockType = bType;
                }
            }

            _isAimingAtDeposit = targetedDeposit != null;
            bool isFiring = _inputReader.IsFireHeld(isCaptured);
            int activeSlot = _suitVisorHud.CurrentActiveSlot;

            _lastDrillResult = DrillSim.Step(
                targetedDeposit?.State,
                _pilotModeMachine.SuitInventory,
                isFiring,
                activeSlot,
                targetedDistance,
                CurrentTuning,
                (float)delta,
                ref _drillProgress);

            if (_lastDrillResult.UnitsExtracted > 0 && targetedDeposit != null)
            {
                targetedDeposit.UpdateVisuals();
            }

            _suitRig.SetDrillBeam(_lastDrillResult.BeamActive, targetHitPos);
        }
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

        bool isSuit = _pilotModeMachine.Mode == PilotMode.FloatingInSuit;
        Vector3 activeCamPos = isSuit ? _suitRig.SuitCamera.GlobalPosition : _shipRig.CockpitCamera.GlobalPosition;
        _spaceDust.UpdateDust(activeCamPos);

        Vector3 pos = isSuit ? _suitRig.GlobalPosition : _shipRig.GlobalPosition;
        Vector3 linVel = isSuit ? _suitRig.LinearVelocity : _shipRig.LinearVelocity;
        Vector3 angVel = isSuit ? _suitRig.AngularVelocity : _shipRig.AngularVelocity;
        bool dampeners = isSuit ? _suitRig.DampenersEnabled : _shipRig.DampenersEnabled;

        var snapshot = new DebugTelemetrySnapshot(
            Position: new SimVector3(pos.X, pos.Y, pos.Z),
            LinearVelocity: new SimVector3(linVel.X, linVel.Y, linVel.Z),
            AngularVelocity: new SimVector3(angVel.X, angVel.Y, angVel.Z),
            DampenersEnabled: dampeners,
            IsReloadFlashActive: _tuningSession.IsReloadFlashActive,
            LastConfigError: _tuningSession.LastError,
            Mode: _pilotModeMachine.Mode,
            TargetGridCell: _targetedGridCell,
            TargetBlockType: _targetedBlockType);

        _debugOverlay.Render(in snapshot);

        if (!isSuit)
        {
            Basis worldToLocal = _shipRig.GlobalBasis.Transposed();
            Vector3 localLin = worldToLocal * linVel;
            bool isAtBoundary = pos.Length() >= Keel.Sim.Arena.SpaceArenaLayout.SoftBoundaryRadius;

            _cockpitHud.UpdateHud(
                new SimVector3(localLin.X, localLin.Y, localLin.Z),
                new SimVector3(pos.X, pos.Y, pos.Z),
                linVel.Length(),
                CurrentTuning,
                _shipRig.DampenersEnabled,
                isAtBoundary,
                _shipRig.CockpitCamera.Fov);
        }
        else
        {
            Basis worldToLocal = _suitRig.GlobalBasis.Transposed();
            Vector3 localLin = worldToLocal * linVel;
            Basis camBasis = _suitRig.SuitCamera.GlobalBasis;
            var camRight = new SimVector3(camBasis.X.X, camBasis.X.Y, camBasis.X.Z);
            var camForward = new SimVector3(-camBasis.Z.X, -camBasis.Z.Y, -camBasis.Z.Z);
            int? slotInput = _inputReader.ConsumeSlotSelection();

            _suitVisorHud.UpdateHud(
                new SimVector3(localLin.X, localLin.Y, localLin.Z),
                new SimVector3(pos.X, pos.Y, pos.Z),
                camForward,
                camRight,
                ToSim(_shipRig.GlobalPosition),
                linVel.Length(),
                _suitRig.DampenersEnabled,
                _suitRig.SuitCamera.Fov,
                CurrentTuning,
                slotInput,
                cargoCount: _pilotModeMachine.SuitInventory.TotalCount,
                steelBlockCount: _pilotModeMachine.SuitInventory.GetCount(ItemId.SteelBlock),
                drillStatus: _lastDrillResult.Status,
                drillProgress: _drillProgress,
                isAimingAtDeposit: _isAimingAtDeposit);
        }
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
            _cockpitHud.InfoLog.Push("CFG: TUNING RELOADED");
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

    private static SimVector3 ToSim(Vector3 v) => new(v.X, v.Y, v.Z);
    private static Vector3 ToGodot(SimVector3 v) => new(v.X, v.Y, v.Z);
    private static SimQuaternion ToSim(Godot.Quaternion q) => new(q.X, q.Y, q.Z, q.W);
    private static Godot.Quaternion ToGodot(SimQuaternion q) => new(q.X, q.Y, q.Z, q.W);
}
