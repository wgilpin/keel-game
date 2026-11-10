# Test plan — Phase 01 Cockpit Flight Tracer

## Coverage

| Done criterion | Proved by | Kind |
|---|---|---|
| DC-1 | `dotnet test Keel.sln` clean build of `Keel.Sim`, `Keel.Game`, and `Keel.Sim.Tests` | automated |
| DC-2 | `TuningConfigTests` (`LoadOrFallback_ParsesValidJsonAndRecoversFromInvalidJson`, `Reload_UpdatesActiveConfigAndExpiresFlashTimer`) | automated |
| DC-3 | `FlightKinematicsTests.ThrustAndTorque_RespectsNormalizationAndSpeedCaps` | automated |
| DC-4 | `FlightKinematicsTests` (`DampenersOff_MaintainsNewtonianDrift`, `DampenersOn_StopsWithoutOvershoot`) | automated |
| DC-5 | `SpaceArenaTests` (`Boundary_ClampsOutwardMotionBeyond20Km`, `DustField_EuclideanWrapsAcrossNegativeAndPositiveCoords`) | automated |
| DC-6 | `CockpitHudTests` (`ReticleProjector_HandlesDeadzoneProjectionAndRearClamping`, `ContactsAndInfoLog_FormatRangesAndCapHistoryAtThree`) | automated |
| DC-7 | Open `src/Keel.Game/project.godot` in Godot 4.7 (.NET) and press `F5`: (1) confirm dark `#0F172A` 3D canopy struts frame the view and lit station (`#64748B`) + asteroid (`#27272A`) + space-dust streaks are visible; (2) hold `W`/`A`/`S`/`D`/`Space`/`Ctrl`, move mouse, and roll with `Q`/`E` to verify 6DoF motion and drifting prograde/retrograde pips; (3) press `Z` to verify Newtonian drift vs. smooth stop and amber `[□] DAMPENERS` indicator; (4) press `F3` twice to verify top-left telemetry hides and reappears cleanly; (5) edit `MaxLinearSpeed` in `config/tuning.json`, save, and confirm the reload indicator flashes and new cap applies live. | manual |

Every done criterion has a row. A criterion with no row is a gap.

## Gaps

None.

## How to run

`dotnet test Keel.sln`

## Test data

- Default `config/tuning.json` checked into the repository plus in-memory JSON strings for `TuningConfigTests` (no database or network fixtures).
