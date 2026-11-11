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
using Keel.Sim.Hud;
using Keel.Sim.Inventory;
using Keel.Sim.Mining;
using Keel.Sim.Pilot;
using Keel.Sim.Persistence;
using Keel.Sim.Refinery;
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
    private RefineryTerminalNode _refineryTerminal = null!;
    private readonly ShipInputReader _inputReader = new();
    private readonly TuningSession _tuningSession = new(TuningConfig.Default);
    private readonly PilotModeStateMachine _pilotModeMachine = new();

    private string _configPath = string.Empty;
    private string _savePath = string.Empty;
    private bool _hasSavedOnClose;
    private DateTime _lastObservedWriteUtc;
    private double _pollAccumulator;
    private bool _f3WasPressed;
    private float _drillProgress;
    private DrillStepResult _lastDrillResult;
    private bool _isAimingAtDeposit;
    private Int3? _targetedGridCell;
    private BlockType _targetedBlockType = BlockType.None;
    private float _targetedStationDistance = float.MaxValue;
    private bool _isRefineryOpen;
    private PlacementEvaluation _lastPlacementEval;
    private float _grinderProgress;
    private Int3? _lastGrinderTargetCell;
    private GrinderStepResult _lastGrinderResult;

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
        _savePath = ResolveSavePath();

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

        _refineryTerminal = new RefineryTerminalNode();
        _refineryTerminal.DepositRequested += HandleRefineryDeposit;
        _refineryTerminal.WithdrawRequested += HandleRefineryWithdraw;
        _refineryTerminal.CloseRequested += CloseRefineryTerminal;
        AddChild(_refineryTerminal);

        _spaceDust = new SpaceDustNode();
        AddChild(_spaceDust);

        LoadWorldStateOnStartup();

        Godot.Input.MouseMode = Godot.Input.MouseModeEnum.Captured;
    }

    public override void _Input(InputEvent @event)
    {
        bool isCaptured = Godot.Input.MouseMode == Godot.Input.MouseModeEnum.Captured;
        _inputReader.HandleInputEvent(@event, isCaptured);

        if (_isRefineryOpen)
        {
            _inputReader.ConsumePrimaryActionClick();
            if (_inputReader.ConsumeInteractToggle() || _inputReader.ConsumeMouseCaptureToggle())
            {
                CloseRefineryTerminal();
                return;
            }

            if (_inputReader.ConsumeDepositToggle())
            {
                HandleRefineryDeposit();
            }
            else if (_inputReader.ConsumeWithdrawToggle())
            {
                HandleRefineryWithdraw();
            }

            _inputReader.ConsumeMouseRecaptureClick();
            return;
        }

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
            else if (_isRefineryOpen)
            {
                CloseRefineryTerminal();
            }
            else if (_targetedBlockType == BlockType.Refinery && _targetedStationDistance <= CurrentTuning.RefineryInteractRange)
            {
                OpenRefineryTerminal();
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

        // Auto-close refinery terminal if drifted past tether limit
        if (_isRefineryOpen && _stationNode != null)
        {
            Vector3 refineryWorldPos = _stationNode.GlobalPosition + new Vector3(0f, 0f, 3f * 2.5f);
            float distToRefinery = _suitRig.GlobalPosition.DistanceTo(refineryWorldPos);
            if (distToRefinery > CurrentTuning.RefineryInteractRange + 1.5f)
            {
                CloseRefineryTerminal();
            }
        }

        // Step station refinery continuously in physics process
        if (_stationNode != null)
        {
            RefinerySim.Step(_stationNode.Refinery, CurrentTuning, (float)delta);
        }

        // Apply physics flight step
        if (_pilotModeMachine.Mode == PilotMode.SeatedInCockpit)
        {
            _lastPlacementEval = default;
            _inputReader.ConsumePrimaryActionClick();
            _drillProgress = 0.0f;
            _lastDrillResult = default;
            _isAimingAtDeposit = false;
            _suitRig.SetDrillBeam(false, Vector3.Zero);
            _stationNode?.GhostNode.HideGhost();

            FlightInput flightInput = _inputReader.ConsumeFlightInput(isCaptured);
            _shipRig.ApplyFlightStep(in flightInput, CurrentTuning, (float)delta);
        }
        else
        {
            // Unmanned ship continues dampening and boundary enforcement
            _shipRig.ApplyFlightStep(FlightInput.Zero, CurrentTuning, (float)delta);

            FlightInput suitInput = _inputReader.ConsumeFlightInput(isCaptured, isModalActive: _isRefineryOpen);
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
            _targetedStationDistance = float.MaxValue;
            bool hasStationHit = false;
            SimVector3 simLocalHitPos = SimVector3.Zero;
            SimVector3 simLocalHitNormal = SimVector3.Zero;

            var hit = spaceState.IntersectRay(query);
            if (hit.Count > 0)
            {
                targetHitPos = (Vector3)hit["position"];
                Vector3 hitNormal = hit.ContainsKey("normal") ? (Vector3)hit["normal"] : Vector3.Up;
                targetedDistance = camPos.DistanceTo(targetHitPos);
                var colliderObj = (GodotObject)hit["collider"];
                targetedDeposit = colliderObj as OreDepositNode ?? (colliderObj as Node)?.GetParent<OreDepositNode>();

                if (_stationNode != null && _stationNode.ResolveRaycastHit(targetHitPos, hitNormal, out Int3 cell, out BlockType bType))
                {
                    _targetedGridCell = cell;
                    _targetedBlockType = bType;
                    _targetedStationDistance = targetedDistance;
                    hasStationHit = _stationNode.TryGetStationLocalRay(targetHitPos, hitNormal, out simLocalHitPos, out simLocalHitNormal);
                }
            }

            _isAimingAtDeposit = targetedDeposit != null;
            bool isFiring = !_isRefineryOpen && _inputReader.IsFireHeld(isCaptured);
            int activeSlot = _suitVisorHud.CurrentActiveSlot;
            bool primaryClicked = _inputReader.ConsumePrimaryActionClick();

            if (_stationNode != null)
            {
                var obstacles = ComputeStationLocalObstacles();
                _lastPlacementEval = BlockPlacementSim.Evaluate(
                    grid: _stationNode.Grid,
                    isFloatingInSuit: true,
                    activeSlot: activeSlot,
                    steelBlockCount: _pilotModeMachine.SuitInventory.GetCount(ItemId.SteelBlock),
                    hasStationHit: hasStationHit,
                    localHitPosition: simLocalHitPos,
                    localHitNormal: simLocalHitNormal,
                    hitDistance: targetedDistance,
                    buildRange: CurrentTuning.BuildRange,
                    isModalOpen: _isRefineryOpen,
                    dynamicObstacles: obstacles);

                if (_lastPlacementEval.ShowGhost && _lastPlacementEval.CandidateCell.HasValue)
                {
                    Vector3 localCenter = new(
                        _lastPlacementEval.CandidateCell.Value.X * BlockGrid.CellSize,
                        _lastPlacementEval.CandidateCell.Value.Y * BlockGrid.CellSize,
                        _lastPlacementEval.CandidateCell.Value.Z * BlockGrid.CellSize);
                    _stationNode.GhostNode.ShowGhost(localCenter, _lastPlacementEval.IsValid);
                }
                else
                {
                    _stationNode.GhostNode.HideGhost();
                }

                if (primaryClicked && activeSlot == BlockPlacementSim.BuildSlotIndex && !_isRefineryOpen)
                {
                    if (BlockPlacementSim.TryPlace(_stationNode.Grid, _pilotModeMachine.SuitInventory, in _lastPlacementEval, out _))
                    {
                        _stationNode.AddBlockVisualAndCollider(_lastPlacementEval.CandidateCell!.Value, BlockType.ArmorBlock);
                    }
                }
            }
            else
            {
                _lastPlacementEval = default;
            }

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

            if (activeSlot == GrinderSim.GrinderSlotIndex && !_isRefineryOpen)
            {
                _lastGrinderResult = GrinderSim.Step(
                    _stationNode!.Grid,
                    _pilotModeMachine.SuitInventory!,
                    _targetedGridCell,
                    _targetedBlockType,
                    isFiring,
                    activeSlot,
                    _targetedStationDistance,
                    CurrentTuning,
                    (float)delta,
                    ref _grinderProgress,
                    ref _lastGrinderTargetCell);

                if (_lastGrinderResult.BlockDeconstructed && _lastGrinderResult.DeconstructedCell.HasValue)
                {
                    _stationNode.RemoveBlockVisualAndCollider(_lastGrinderResult.DeconstructedCell.Value);
                    _targetedGridCell = null;
                    _targetedBlockType = BlockType.None;
                }

                _suitRig.SetGrinderBeam(_lastGrinderResult.BeamActive, targetHitPos);
            }
            else
            {
                _lastGrinderResult = default;
                _grinderProgress = 0f;
                _lastGrinderTargetCell = null;
                _suitRig.SetGrinderBeam(false, Vector3.Zero);
            }
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

        Int3? placementCandidate = isSuit && _lastPlacementEval.ShowGhost
            ? _lastPlacementEval.CandidateCell
            : null;
        bool? isPlacementValid = isSuit && _lastPlacementEval.ShowGhost
            ? _lastPlacementEval.IsValid
            : null;

        float? grinderTelemetry = isSuit && _lastGrinderResult.Status == GrinderStatus.Deconstructing
            ? _grinderProgress
            : null;

        var snapshot = new DebugTelemetrySnapshot(
            Position: new SimVector3(pos.X, pos.Y, pos.Z),
            LinearVelocity: new SimVector3(linVel.X, linVel.Y, linVel.Z),
            AngularVelocity: new SimVector3(angVel.X, angVel.Y, angVel.Z),
            DampenersEnabled: dampeners,
            IsReloadFlashActive: _tuningSession.IsReloadFlashActive,
            LastConfigError: _tuningSession.LastError,
            Mode: _pilotModeMachine.Mode,
            TargetGridCell: _targetedGridCell,
            TargetBlockType: _targetedBlockType,
            PlacementCandidateCell: placementCandidate,
            IsPlacementValid: isPlacementValid,
            GrinderProgress: grinderTelemetry);

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
                isAimingAtDeposit: _isAimingAtDeposit,
                targetedBlockType: _targetedBlockType,
                targetedBlockDistance: _targetedStationDistance,
                placementStatus: _lastPlacementEval.Status,
                grinderStatus: _lastGrinderResult.Status,
                grinderProgress: _grinderProgress);

            if (_isRefineryOpen)
            {
                var vm = RefineryTerminalModel.Compute(
                    _stationNode.Refinery,
                    _pilotModeMachine.SuitInventory,
                    _isRefineryOpen,
                    CurrentTuning.RefineryCycleSeconds);
                _refineryTerminal.UpdateTerminal(in vm);
            }
        }
    }

    private void OpenRefineryTerminal()
    {
        _isRefineryOpen = true;
        _refineryTerminal.Visible = true;
        Godot.Input.MouseMode = Godot.Input.MouseModeEnum.Visible;
    }

    private void CloseRefineryTerminal()
    {
        _isRefineryOpen = false;
        _refineryTerminal.Visible = false;
        Godot.Input.MouseMode = Godot.Input.MouseModeEnum.Captured;
    }

    private void HandleRefineryDeposit()
    {
        RefinerySim.DepositFromSuit(_stationNode.Refinery, _pilotModeMachine.SuitInventory);
    }

    private void HandleRefineryWithdraw()
    {
        RefinerySim.WithdrawToSuit(_stationNode.Refinery, _pilotModeMachine.SuitInventory);
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

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            if (!_hasSavedOnClose)
            {
                _hasSavedOnClose = true;
                SaveWorldState();
            }
        }
    }

    private void SaveWorldState()
    {
        if (_shipRig == null || _suitRig == null)
        {
            return;
        }

        Godot.Quaternion shipRotGodot = _shipRig.GlobalBasis.GetRotationQuaternion();
        var shipKin = new KinematicsSaveData(
            ToSim(_shipRig.GlobalPosition),
            ToSim(shipRotGodot),
            ToSim(_shipRig.LinearVelocity),
            ToSim(_shipRig.AngularVelocity),
            _shipRig.DampenersEnabled);

        Godot.Quaternion suitRotGodot = _suitRig.GlobalBasis.GetRotationQuaternion();
        var suitKin = new KinematicsSaveData(
            ToSim(_suitRig.GlobalPosition),
            ToSim(suitRotGodot),
            ToSim(_suitRig.LinearVelocity),
            ToSim(_suitRig.AngularVelocity),
            _suitRig.DampenersEnabled);

        int activeSlot = _suitVisorHud?.CurrentActiveSlot ?? SuitVisorModel.DefaultSlot;

        WorldSaveData snapshot = WorldStateMapper.CreateSnapshot(
            _pilotModeMachine.Mode,
            shipKin,
            suitKin,
            activeSlot,
            _pilotModeMachine.SuitInventory);

        WorldSaveStore.SaveToFile(_savePath, snapshot);
    }

    private void LoadWorldStateOnStartup()
    {
        var loadResult = WorldSaveStore.LoadFromFile(_savePath);
        if (!loadResult.IsSuccess || loadResult.Data == null)
        {
            return;
        }

        WorldSaveData data = loadResult.Data;
        WorldStateMapper.Apply(data, _pilotModeMachine, _pilotModeMachine.SuitInventory);

        _shipRig.GlobalPosition = ToGodot(data.Ship.Position);
        _shipRig.GlobalBasis = new Basis(ToGodot(data.Ship.Orientation));
        _shipRig.LinearVelocity = ToGodot(data.Ship.LinearVelocity);
        _shipRig.AngularVelocity = ToGodot(data.Ship.AngularVelocity);
        _shipRig.DampenersEnabled = data.Ship.DampenersEnabled;

        _suitRig.Spawn(
            ToGodot(data.Suit.Kinematics.Position),
            ToGodot(data.Suit.Kinematics.Orientation),
            ToGodot(data.Suit.Kinematics.LinearVelocity),
            ToGodot(data.Suit.Kinematics.AngularVelocity));
        _suitRig.DampenersEnabled = data.Suit.Kinematics.DampenersEnabled;
        _suitVisorHud.SetActiveSlot(data.Suit.ActiveHotbarSlot);

        if (data.PilotMode == PilotMode.FloatingInSuit)
        {
            _shipRig.SetHullVisible(true);
            _suitRig.SetActive(true);
            _shipRig.CockpitCamera.Current = false;
            _suitRig.SuitCamera.Current = true;
            _cockpitHud.Visible = false;
            _suitVisorHud.Visible = true;
        }
        else
        {
            _suitRig.SetActive(false);
            _shipRig.SetHullVisible(false);
            _suitRig.SuitCamera.Current = false;
            _shipRig.CockpitCamera.Current = true;
            _suitVisorHud.Visible = false;
            _cockpitHud.Visible = true;
        }
    }

    private static string ResolveSavePath()
    {
        string projectDir = ProjectSettings.GlobalizePath("res://");
        string repoCandidate = Path.GetFullPath(Path.Combine(projectDir, "..", "..", "saves", "world.json"));
        string configCandidate = Path.GetFullPath(Path.Combine(projectDir, "..", "..", "config", "tuning.json"));
        return File.Exists(configCandidate)
            ? repoCandidate
            : Path.Combine(projectDir, "saves", "world.json");
    }

    private static string ResolveConfigPath()
    {
        string projectDir = ProjectSettings.GlobalizePath("res://");
        string repoCandidate = Path.GetFullPath(Path.Combine(projectDir, "..", "..", "config", "tuning.json"));
        return File.Exists(repoCandidate)
            ? repoCandidate
            : Path.Combine(projectDir, "tuning.json");
    }

    private (SimVector3 Min, SimVector3 Max)[] ComputeStationLocalObstacles()
    {
        if (_stationNode == null)
        {
            return Array.Empty<(SimVector3, SimVector3)>();
        }

        Transform3D invStation = _stationNode.GlobalTransform.AffineInverse();

        // 1. Suit AABB (capsule radius 0.4m, half-height 0.9m)
        Vector3 suitLocal = invStation * _suitRig.GlobalPosition;
        Vector3 suitMin = suitLocal - new Vector3(0.4f, 0.9f, 0.4f);
        Vector3 suitMax = suitLocal + new Vector3(0.4f, 0.9f, 0.4f);

        // 2. Ship AABB (hull box 3.0m x 1.8m x 4.0m -> half extents 1.5, 0.9, 2.0)
        Vector3 h = new(1.5f, 0.9f, 2.0f);
        Vector3[] corners =
        {
            new(-h.X, -h.Y, -h.Z),
            new( h.X, -h.Y, -h.Z),
            new( h.X,  h.Y, -h.Z),
            new(-h.X,  h.Y, -h.Z),
            new(-h.X, -h.Y,  h.Z),
            new( h.X, -h.Y,  h.Z),
            new( h.X,  h.Y,  h.Z),
            new(-h.X,  h.Y,  h.Z),
        };

        Vector3 shipMin = new(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 shipMax = new(float.MinValue, float.MinValue, float.MinValue);
        for (int i = 0; i < 8; i++)
        {
            Vector3 worldCorner = _shipRig.GlobalTransform * corners[i];
            Vector3 stationCorner = invStation * worldCorner;
            shipMin = new Vector3(
                MathF.Min(shipMin.X, stationCorner.X),
                MathF.Min(shipMin.Y, stationCorner.Y),
                MathF.Min(shipMin.Z, stationCorner.Z));
            shipMax = new Vector3(
                MathF.Max(shipMax.X, stationCorner.X),
                MathF.Max(shipMax.Y, stationCorner.Y),
                MathF.Max(shipMax.Z, stationCorner.Z));
        }

        return new[]
        {
            (ToSim(suitMin), ToSim(suitMax)),
            (ToSim(shipMin), ToSim(shipMax)),
        };
    }

    private static SimVector3 ToSim(Vector3 v) => new(v.X, v.Y, v.Z);
    private static Vector3 ToGodot(SimVector3 v) => new(v.X, v.Y, v.Z);
    private static SimQuaternion ToSim(Godot.Quaternion q) => new(q.X, q.Y, q.Z, q.W);
    private static Godot.Quaternion ToGodot(SimQuaternion q) => new(q.X, q.Y, q.Z, q.W);
}
