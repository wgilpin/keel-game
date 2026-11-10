# Epic 01 — Suit EVA

**Phase:** 02 Zero-G Suit and Mining
**Goal:** Let the player press `F` to exit the ship cockpit into a zero-g Jolt `RigidBody3D` suit with independent 6DoF tuning and dampeners, and press `F` near the ship to board again.
**Done criteria covered:** DC-1, DC-2, DC-7

## Behaviour

Pressing `F` while seated in the cockpit spawns the player beside the `#94A3B8` ship hull in a zero-g EVA suit (`FloatingInSuit`), inheriting the ship's linear velocity and facing direction while the unmanned ship continues obeying its own Jolt physics and dampener setting. In the suit, `W`/`A`/`S`/`D`/`Space`/`Ctrl`, mouse pitch/yaw, `Q`/`E` roll, and `Z` dampeners drive `SuitRigNode` using suit-specific `config/tuning.json` values (`suitMass`, `suitLinearThrust`, `suitAngularTorque`, `suitMaxLinearSpeed`), with a right-hand 3D grey-box tool mesh framing the camera, space dust tracking the active camera, and `F3` telemetry reporting `MODE COCKPIT` or `MODE SUIT`. Jetting back within `shipInteractRange` of the ship and pressing `F` returns control and camera to `SeatedInCockpit`.

## Acceptance criteria

- **AC-1** — Given `SeatedInCockpit` mode and an arbitrary ship world position, `Quaternion` orientation, and linear velocity, when `PilotModeStateMachine.ExitCockpit` runs, then mode transitions to `FloatingInSuit`, suit spawn world position equals `shipPosition + Rotate(localExitOffset, shipOrientation)` clear of the ship hull box, and initial suit linear velocity equals the ship's linear velocity with zero initial suit spin.
- **AC-2** — Given `FloatingInSuit` mode and ship/suit world coordinates, when `PilotModeStateMachine.TryBoardShip` runs, then boarding returns `false` and stays `FloatingInSuit` when distance exceeds `shipInteractRange`, and returns `true` transitioning back to `SeatedInCockpit` when distance is `<= shipInteractRange`.
- **AC-3** — Given `config/tuning.json` with Phase 02 suit and mining fields (`suitMass`, `suitLinearThrust`, `suitAngularTorque`, `suitMaxLinearSpeed`, `shipInteractRange`, `drillRange`, `drillRate`, `suitCargoCapacity`), when `TuningConfigLoader.LoadOrFallback` and `FlightKinematics.StepSuit` execute, then valid JSON overrides defaults, malformed or non-positive values fall back safely without throwing, and suit 6DoF thrust, `suitMaxLinearSpeed` cap, and zero-overshoot dampeners operate independently of ship mass and speed caps.

## Tests

- `PilotModeStateMachineTests.ExitCockpit_OffsetsInRotatedShipSpaceAndInheritsLinearVelocity` — proves rotated local exit offset placement, velocity inheritance, and spin reset on EVA exit (`AC-1`).
- `PilotModeStateMachineTests.TryBoardShip_EnforcesInteractRangeThreshold` — proves boarding rejection beyond `shipInteractRange` and transition back to `SeatedInCockpit` within range (`AC-2`).
- `TuningConfigTests.LoadOrFallback_ValidatesSuitAndMiningParametersWithSafeFallback` — proves JSON parsing and invalid-value fallback for Phase 02 suit/mining fields (`AC-3`).
- `SuitFlightTests.StepSuit_UsesSuitProfileIndependentlyOfShipCapsAndStopsWithoutOvershoot` — proves suit kinematics use suit mass/thrust/cap and converge monotonically to zero with dampeners on (`AC-3`).

## Files

- `config/tuning.json`
- `src/Keel.Sim/Config/TuningConfig.cs`
- `src/Keel.Sim/Config/TuningConfigLoader.cs`
- `src/Keel.Sim/Flight/FlightKinematics.cs`
- `src/Keel.Sim/Pilot/PilotMode.cs`
- `src/Keel.Sim/Pilot/PilotModeStateMachine.cs`
- `src/Keel.Sim/Telemetry/DebugTelemetryFormatter.cs`
- `src/Keel.Game/Flight/ShipRigNode.cs`
- `src/Keel.Game/Flight/ShipHullBuilder.cs`
- `src/Keel.Game/Suit/SuitRigNode.cs`
- `src/Keel.Game/Suit/SuitToolMeshBuilder.cs`
- `src/Keel.Game/Input/ShipInputReader.cs`
- `src/Keel.Game/UI/CockpitHudNode.cs`
- `src/Keel.Game/GameRoot.cs`
- `tests/Keel.Sim.Tests/Pilot/PilotModeStateMachineTests.cs`
- `tests/Keel.Sim.Tests/Flight/SuitFlightTests.cs`
- `tests/Keel.Sim.Tests/Config/TuningConfigTests.cs`

## Out of scope

- Slate-cyan 2D Suit Visor HUD panels, bearing tape, and 5-slot hotbar (`epic-02`)
- Asteroid ore deposits, raycast drilling, and suit `Inventory` (`epic-03`)

## Notes

- Keep `Keel.Sim` 100% free of `GodotSharp` ([ADR 0001](../../adr/0001-engine-free-keel-sim-assembly.md)); use `System.Numerics.Vector3` and `System.Numerics.Quaternion` in `PilotModeStateMachine`.
- Add all Phase 02 tuning parameters (`suitMass`: `95.0`, `suitLinearThrust`: `1400.0`, `suitAngularTorque`: `220.0`, `suitMaxLinearSpeed`: `25.0`, `shipInteractRange`: `6.0`, `drillRange`: `5.0`, `drillRate`: `1.5`, `suitCargoCapacity`: `50`) to `TuningConfig` and `config/tuning.json` now so `epic-02` and `epic-03` do not churn config schemas.
- Give `ShipRigNode` an exterior `#94A3B8` grey-box hull mesh (`ShipHullBuilder`) hidden while `SeatedInCockpit` and visible while `FloatingInSuit` so cockpit sightlines stay unobstructed while the parked ship is clearly visible in EVA.
- While in `FloatingInSuit`, keep stepping `ShipRigNode.ApplyFlightStep(FlightInput.Zero, ...)` in `_PhysicsProcess` so unmanned ship dampeners and `ArenaBoundary` stay active, and feed `SuitRigNode.SuitCamera.GlobalPosition` to `SpaceDustNode.UpdateDust`.

## Build note — 2026-11-10

### Tasks done

- Added Phase 02 tuning parameters (`suitMass`: 95.0, `suitLinearThrust`: 1400.0, `suitAngularTorque`: 220.0, `suitMaxLinearSpeed`: 25.0, `shipInteractRange`: 6.0, `drillRange`: 5.0, `drillRate`: 1.5, `suitCargoCapacity`: 50.0) to `TuningConfig`, `TuningConfigLoader`, and `config/tuning.json`.
- Implemented `PilotMode` enum and `PilotModeStateMachine` in `Keel.Sim.Pilot` handling `ExitCockpit` spawn positioning/velocity inheritance clear of hull boundaries and `TryBoardShip` range gating (`shipInteractRange`).
- Refactored `FlightKinematics` with shared `StepCore` engine and implemented `StepSuit` supporting independent suit mass/thrust/caps and monotonic zero-overshoot dampener deceleration.
- Updated `DebugTelemetryFormatter` and `DebugTelemetrySnapshot` with pilot mode awareness (`MODE COCKPIT` vs `MODE SUIT`).
- Created `ShipHullBuilder` generating an exterior `#94A3B8` grey-box hull mesh for `ShipRigNode`, hidden while seated and visible during EVA.
- Created `SuitToolMeshBuilder` generating a right-hand grey-box 3D tool mesh framing the camera and `SuitRigNode` with Jolt `RigidBody3D` 6DoF suit kinematics.
- Updated `ShipInputReader` to capture `F` interact key and wired `GameRoot` for EVA exit/entry, camera switching, HUD visibility, unmanned ship dampening, and active camera space dust tracking.

### Tests added

- `tests/Keel.Sim.Tests/Pilot/PilotModeStateMachineTests.cs`
  - `PilotModeStateMachineTests.ExitCockpit_OffsetsInRotatedShipSpaceAndInheritsLinearVelocity`
  - `PilotModeStateMachineTests.TryBoardShip_EnforcesInteractRangeThreshold`
- `tests/Keel.Sim.Tests/Config/TuningConfigTests.cs`
  - `TuningConfigTests.LoadOrFallback_ValidatesSuitAndMiningParametersWithSafeFallback`
- `tests/Keel.Sim.Tests/Flight/SuitFlightTests.cs`
  - `SuitFlightTests.StepSuit_UsesSuitProfileIndependentlyOfShipCapsAndStopsWithoutOvershoot`

### Suite result

- Command: `dotnet test Keel.sln`
- Summary: `Passed! - Failed: 0, Passed: 13, Skipped: 0, Total: 13, Duration: 93 ms - Keel.Sim.Tests.dll (net8.0)` (with `Keel.Game` compiled cleanly)

### Deviations from the plan

None.

### Follow-ups

- Launch Godot (`F5`) to verify suit jetting feel, tool mesh camera framing, and exterior ship hull sightlines.

---

## Review — 2026-11-10

**Verdict:** pass

### Acceptance criteria

| AC | Met | Proved by |
|---|---|---|
| AC-1 | yes | `PilotModeStateMachineTests.ExitCockpit_OffsetsInRotatedShipSpaceAndInheritsLinearVelocity` |
| AC-2 | yes | `PilotModeStateMachineTests.TryBoardShip_EnforcesInteractRangeThreshold` |
| AC-3 | yes | `TuningConfigTests.LoadOrFallback_ValidatesSuitAndMiningParametersWithSafeFallback`, `SuitFlightTests.StepSuit_UsesSuitProfileIndependentlyOfShipCapsAndStopsWithoutOvershoot` |

### Drift

None

### Findings

None

### Follow-ups

None

