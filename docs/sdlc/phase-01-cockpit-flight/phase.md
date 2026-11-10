# Phase 01 — Cockpit Flight Tracer

**Goal:** Prove the `Keel.Sim` and `Keel.Game` stack end-to-end with a playable Newtonian 6DoF ship, hot-reloaded tuning config, amber Cockpit HUD, and `F3` telemetry overlay.

## Scope

Creates the three-project solution (`Keel.sln`: `src/Keel.Sim`, `src/Keel.Game`, `tests/Keel.Sim.Tests`), `config/tuning.json` with live reload, `FlightKinematics` (6DoF thrust, torque, speed clamping, inertial dampeners), `SpaceArenaLayout` (20 km origin boundary, landmark coordinates, `20m` space-dust drift cube), and two surfaces from `design.md`: the **Debug Telemetry Overlay** (`F3`) and the **Ship Cockpit HUD** (framed by 3D canopy struts).

## Done criteria

1. **DC-1** — `dotnet test Keel.sln` builds `Keel.Sim` (with zero `GodotSharp` dependency), `Keel.Game`, and `Keel.Sim.Tests` cleanly from the command line. (Covered by `epic-01-tracer-skeleton.md`)
2. **DC-2** — `config/tuning.json` loads ship flight and camera tuning values at startup, retains the fallback config on malformed JSON without crashing, and updates active values with a timed reload indicator when modified on disk. (Covered by `epic-01-tracer-skeleton.md`)
3. **DC-3** — 6DoF translation (`W`/`S`, `A`/`D`, `Space`/`Ctrl`) and rotation (mouse pitch/yaw, `Q`/`E` roll) inputs compute normalized local force and torque vectors scaled by `TuningConfig` and clamp ship speed at `MaxLinearSpeed` and `MaxAngularSpeed`. (Covered by `epic-02-newtonian-flight.md`)
4. **DC-4** — With inertial dampeners `OFF` (`Z`), zero input leaves linear and angular velocity unchanged; with dampeners `ON`, counter-thrust decelerates linear and angular velocity monotonically to zero without overshoot oscillation. (Covered by `epic-02-newtonian-flight.md`)
5. **DC-5** — The 3D space arena enforces the `20,000 m` origin bubble by clamping outward radial velocity and wraps local `20 m` space-dust points Euclidean-cleanly around positive and negative camera coordinates. (Covered by `epic-03-space-arena.md`)
6. **DC-6** — The Cockpit HUD model projects prograde and retrograde velocity pips (hidden below `0.5 m/s`, clamped to the HUD ring when off-axis or behind the camera), formats `STATION` and `ASTEROID` ranges in `m`/`km`, and caps the `INFO` log at the 3 newest entries. (Covered by `epic-04-cockpit-hud.md`)
7. **DC-7** — Launching `Keel.Game` in Godot 4.7 (.NET) spawns the player inside the Jolt `RigidBody3D` cockpit framed by dark 3D canopy struts, flying through lit space-dust streaks around the grey-box station and asteroid with the amber 3-panel Cockpit HUD and `F3`-toggleable Debug Telemetry Overlay legible at `1920x1080`+. (Covered by `epic-01-tracer-skeleton.md`, `epic-03-space-arena.md`, `epic-04-cockpit-hud.md`)

## Not in this phase

- Exiting the cockpit (`PilotModeStateMachine` suit transitions) and zero-g suit movement
- Cyan Suit Visor HUD (bearing tape, bottom-left cargo stack, 5-slot hotbar)
- Discrete ore deposits, raycast drilling, and inventories
- `BlockGrid` station data model, Refinery Terminal, and block placement

## Epics

Epics run sequentially (`01` -> `02` -> `03` -> `04`) because each builds onto the shared `GameRoot` tracer scene; none are independent.

1. `epic-01-tracer-skeleton.md` — Stand up `Keel.sln`, hot-reloaded `config/tuning.json`, and the `F3` Debug Telemetry Overlay (`DC-1`, `DC-2`, `DC-7`).
2. `epic-02-newtonian-flight.md` — Drive a Jolt `RigidBody3D` ship from the cockpit using `Keel.Sim` 6DoF thrust, torque, speed caps, and toggleable inertial dampeners (`DC-3`, `DC-4`).
3. `epic-03-space-arena.md` — Populate the 20 km space arena with lit grey-box station and asteroid landmarks, 3D cockpit canopy struts, space-dust drift cues, and boundary clamping (`DC-5`, `DC-7`).
4. `epic-04-cockpit-hud.md` — Render the warm amber 3-panel Ship Cockpit HUD with prograde/retrograde vector pips, landmark contacts, system checkboxes, and `INFO` feed (`DC-6`, `DC-7`).

## Risks

- **Toolchain availability:** `.NET 10 SDK` and `Godot 4.7 (.NET)` must be installed before building `epic-01`, and `Godot.NET.Sdk` NuGet packages must restore cleanly for `dotnet test Keel.sln`.
- **Dampener oscillation at variable frame deltas:** Naive proportional damping can overshoot zero velocity on large `dt` spikes; `FlightKinematics` must clamp counter-impulse per step so velocity never flips sign.
- **Disorientation in empty space:** Addressed in `epic-03` via close-range `20m` space-dust drift particles, directional + `#1E293B` ambient fill lighting, and distinct landmark meshes.
