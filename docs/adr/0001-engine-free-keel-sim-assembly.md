# 0001 — Engine-free Keel.Sim assembly

**Status:** accepted
**Date:** 2026-11-10

## Context

Keel is built primarily by a coding agent, with Will evaluating feel and playability in the Godot 4.7 (.NET) runtime. Game engines are difficult for coding agents to verify directly: code coupled to `Godot.Node`, `Godot.RigidBody3D`, or `.tscn` scene files cannot be unit-tested without launching the Godot binary, and runtime physics or scene-graph mistakes often fail visually without producing a compiler error or stack trace.

## Decision

Split the codebase into two assemblies:

1. **`Keel.Sim`**: A pure .NET class library with zero reference to `GodotSharp`. It owns 6DoF flight and inertial dampener kinematics, pilot/suit state transitions, discrete ore deposit mining, inventories, refinery recipe ticks, and the 3D integer `BlockGrid`. It uses `System.Numerics` and plain C# value types (`Int3`, `TuningConfig`).
2. **`Keel.Game`**: A thin Godot 4.7 .NET assembly that reads player input, passes current physics state into `Keel.Sim`, applies the resulting force and torque vectors to Jolt `RigidBody3D` nodes, performs raycasts, builds scene nodes from code, hot-reloads `config/tuning.json`, and draws the debug telemetry overlay.

All simulation and flight-math behaviours are proved first in `Keel.Sim.Tests` via `dotnet test Keel.sln`.

## Consequences

- **Easy:** Every simulation rule—including 6DoF thruster allocation, counter-thrust dampeners, speed clamping, grid adjacency, mining yields, and refinery conversion—runs in millisecond CLI unit tests without launching Godot.
- **Easy:** Static C# types and pure functions make agent-authored logic deterministic and easy to review.
- **Hard:** Requires thin adapter code in `Keel.Game` to convert between `System.Numerics.Vector3` (or `Keel.Sim` types) and `Godot.Vector3` / `Godot.Transform3D`, and to synchronize `BlockGrid` state with Godot visual/collision nodes.

## Alternatives rejected

- **Simulation logic directly in Godot C# Node scripts:** Simpler upfront (no type conversion at the boundary), but requires running the Godot executable to test any game logic and mixes physics integration with control math.
- **GDScript across the whole project:** Fast hot-reload, but weaker static typing, slower loop performance on 3D grids, and less reliable for agent-driven red-green-refactor testing from the command line.
