# Epic 01 — Tracer Skeleton

**Phase:** 01 Cockpit Flight Tracer
**Goal:** Stand up `Keel.sln` (`Keel.Sim`, `Keel.Game`, `Keel.Sim.Tests`) end-to-end with hot-reloaded `config/tuning.json` and the `F3` Debug Telemetry Overlay.
**Done criteria covered:** DC-1, DC-2, DC-7

## Behaviour

Running `dotnet test Keel.sln` builds the pure C# `Keel.Sim` library, the Godot 4.7 `Keel.Game` project, and the xUnit test suite cleanly. Launching `Keel.Game` opens a `1920x1080` dark-space 3D viewport with a cockpit camera, loads `config/tuning.json`, watches it for live edits on disk, and renders the top-left `F3`-toggleable Debug Telemetry Overlay (position, velocity, dampeners, origin distance, and live reload flash).

## Acceptance criteria

- **AC-1** — Given JSON input to `TuningConfigLoader.LoadOrFallback`, when the JSON is valid it parses all configured ship flight/camera fields, and when the JSON is missing or malformed it returns the supplied fallback `TuningConfig` with a non-empty error message instead of throwing, while `dotnet test Keel.sln` compiles all three projects cleanly.
- **AC-2** — Given an active `TuningSession`, when a new valid `TuningConfig` is applied on file reload and ticks advance, then `Current` updates immediately and `IsReloadFlashActive` stays `true` until the `1.5 s` flash timer expires.

## Tests

- `TuningConfigTests.LoadOrFallback_ParsesValidJsonAndRecoversFromInvalidJson` — proves valid JSON parsing and non-throwing fallback recovery on malformed JSON (`AC-1`).
- `TuningConfigTests.Reload_UpdatesActiveConfigAndExpiresFlashTimer` — proves config swap and `1.5 s` reload-flash countdown (`AC-2`).

## Files

- `.gitignore`
- `Keel.sln`
- `config/tuning.json`
- `src/Keel.Sim/Keel.Sim.csproj`
- `src/Keel.Sim/Config/TuningConfig.cs`
- `src/Keel.Sim/Config/TuningConfigLoader.cs`
- `src/Keel.Sim/Config/TuningSession.cs`
- `src/Keel.Sim/Telemetry/DebugTelemetryFormatter.cs`
- `src/Keel.Game/Keel.Game.csproj`
- `src/Keel.Game/project.godot`
- `src/Keel.Game/Main.tscn`
- `src/Keel.Game/GameRoot.cs`
- `src/Keel.Game/UI/DebugOverlayNode.cs`
- `tests/Keel.Sim.Tests/Keel.Sim.Tests.csproj`
- `tests/Keel.Sim.Tests/Config/TuningConfigTests.cs`

## Out of scope

- Ship `RigidBody3D` movement, 6DoF thrust, and inertial dampeners (`epic-02`)
- 3D landmarks, canopy struts, and space-dust particles (`epic-03`)
- Amber 3-panel Ship Cockpit HUD (`epic-04`)

## Notes

- Follow [ADR 0001](../../adr/0001-engine-free-keel-sim-assembly.md) and [CLAUDE.md](../../../../CLAUDE.md): keep `Keel.Sim` free of `GodotSharp`, and do not add boilerplate tests for `F3` boolean toggles or formatting strings.
- Keep `Main.tscn` minimal (a single root `Node3D` attaching `GameRoot.cs`) and construct camera, environment, and `DebugOverlayNode` in C# using `design.md` tokens (`Consolas` `12px`, `#E6EDF3` text, `#101A26` 82% alpha backing, top-left anchor).
