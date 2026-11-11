# Epic 01 — Grinder Simulation & Block Recycling

**Phase:** 05 Tool Deconstruction & Block Recycling
**Goal:** Implement core deconstruction simulation in `Keel.Sim.Grid` with `grinderRange`/`grinderRate` tuning, progress accumulation, atomic `BlockGrid` removal, `SuitInventory` `SteelBlock` refund, Core protection, cargo capacity check, and grid connectivity guard.
**Done criteria covered:** DC-1, DC-2, DC-3, DC-7

## Behaviour

`config/tuning.json` adds `grinderRange` (`5.0` m default) and `grinderRate` (`1.0` progress/s default), hot-reloaded alongside existing parameters. When the player floats in the suit with hotbar slot `[3] Grinder` active, aiming at an occupied station block within `grinderRange` evaluates deconstruction status. Holding `LMB` accumulates deconstruction progress at `grinderRate` per second; reaching `1.0` progress atomically removes the block from `BlockGrid`, awards `1` `SteelBlock` to `SuitInventory`, and resets progress. Attempting to deconstruct `BlockType.Core`, deconstructing while `SuitInventory` is full, or deconstructing a structural block whose removal would disconnect the station grid is strictly prevented with zero mutations to the grid or inventory.

## Acceptance criteria

- **AC-1** — Given `config/tuning.json` payloads containing `grinderRange` and `grinderRate`, when `TuningConfigLoader.LoadOrFallback` executes, then positive finite values override the defaults (`grinderRange: 5.0f`, `grinderRate: 1.0f`) while malformed JSON or non-positive/non-finite values retain the fallback `TuningConfig` with `IsSuccess == false`.
- **AC-2** — Given a seeded `BlockGrid` with an `ArmorBlock` at `(0, 0, 1)`, a `SuitInventory` with available capacity, hotbar slot `3`, and hit distance `<= grinderRange`, when `GrinderSim.Step` is called with `isFiring == true`, then deconstruction progress advances by `deltaSeconds * grinderRate`, and upon reaching `>= 1.0f`, `BlockGrid.RemoveBlock` removes the block, `1` `ItemId.SteelBlock` is added to `SuitInventory`, and `result.BlockDeconstructed == true` with `Progress == 0f`.
- **AC-3** — Given an active deconstruction in progress, when `isFiring` is `false` or the target cell changes, then progress halts or resets, and no block is removed until a full continuous cycle on a single target is completed.
- **AC-4** — Given a targeted block with `BlockType.Core`, or `SuitInventory.IsFull == true`, or an `ArmorBlock` whose removal would partition the station into multiple disconnected components (`IsConnected() == false`), when `GrinderSim.Step` evaluates, then it returns `CannotDeconstructCore`, `CargoFull`, or `WouldSplitGrid` with `Progress == 0f`, `BlockDeconstructed == false`, and both `BlockGrid` and `SuitInventory` completely unmodified.

## Tests

- `TuningConfigTests.LoadOrFallback_ValidatesGrinderParametersWithSafeFallback` — proves `grinderRange` and `grinderRate` parsing, defaults, and non-positive/malformed fallback (`AC-1`).
- `GrinderSimTests.Evaluate_ValidatesHitDistanceAndSlotGating` — proves hit distance gating against `grinderRange` and slot 3 isolation (`AC-2`).
- `GrinderSimTests.Step_AccumulatesProgressAndAtomicallyRefundsSteelBlockOnCompletion` — proves continuous progress accumulation and 1-for-1 `SteelBlock` refund upon completion (`AC-2`).
- `GrinderSimTests.Step_ResetsProgressOnTargetChangeOrFireRelease` — proves progress reset on target cell change and progress preservation/halt on fire release (`AC-3`).
- `GrinderSimTests.Step_RejectsCoreCargoFullAndGridDisconnectionWithoutMutatingState` — proves Core protection, cargo capacity enforcement, and `IsConnected()` grid split prevention (`AC-4`).

## Files

- `config/tuning.json`
- `src/Keel.Sim/Config/TuningConfig.cs`
- `src/Keel.Sim/Config/TuningConfigLoader.cs`
- `src/Keel.Sim/Grid/GrinderSim.cs`
- `tests/Keel.Sim.Tests/Config/TuningConfigTests.cs`
- `tests/Keel.Sim.Tests/Grid/GrinderSimTests.cs`

## Out of scope

- Visual node and Jolt collider removal on `StationGridNode` (`epic-02`)
- Tool cutting beam rendering on `SuitRigNode` (`epic-02`)
- Visor contextual HUD prompts and `F3` telemetry formatting (`epic-03`)

## Notes

- Keep `GrinderSim` in `Keel.Sim.Grid` 100% free of `GodotSharp` references ([ADR 0001](../../adr/0001-engine-free-keel-sim-assembly.md)).
- Mirror the step pattern from `DrillSim.Step` so integration in `GameRoot` feels natural and consistent.
- For grid disconnection check, temporarily remove the block, call `grid.IsConnected()`, and if `false`, immediately restore the block via `grid.TryPlaceBlock(cell, type, out _)` and return `GrinderStatus.WouldSplitGrid`.

---

## Build note — 2026-11-11

**Result:** green

### Tasks done

- Added `grinderRange: 5.0` and `grinderRate: 1.0` to `config/tuning.json`, `TuningConfig`, and `TuningConfigLoader` with positive finite validation and safe fallbacks.
- Added `WouldDisconnect(Int3 cell)` to `BlockGrid` to evaluate topological grid connectivity before block removal without mutating state on failure.
- Implemented `GrinderSim` with `GrinderStatus`, `GrinderStepResult`, slot 3 verification, hit distance checks, Core protection, cargo capacity validation, grid disconnection checks, and continuous progress accumulation into atomic `BlockGrid.RemoveBlock` and `SuitInventory.Add(ItemId.SteelBlock, 1)`.
- Added unit tests in `TuningConfigTests` and `GrinderSimTests` covering all acceptance criteria.

### Tests added

- `tests/Keel.Sim.Tests/Config/TuningConfigTests.cs`:
  - `LoadOrFallback_ValidatesGrinderParametersWithSafeFallback`
- `tests/Keel.Sim.Tests/Grid/GrinderSimTests.cs`:
  - `Evaluate_ValidatesHitDistanceAndSlotGating`
  - `Step_AccumulatesProgressAndAtomicallyRefundsSteelBlockOnCompletion`
  - `Step_ResetsProgressOnTargetChangeOrFireRelease`
  - `Step_RejectsCoreCargoFullAndGridDisconnectionWithoutMutatingState`

### Suite result

- Command: `dotnet test Keel.sln`
- Output: `Passed!  - Failed:     0, Passed:    43, Skipped:     0, Total:    43, Duration: 34 ms - Keel.Sim.Tests.dll (net8.0)`

### Deviations from the plan

- Implemented `WouldDisconnect` directly on `BlockGrid` using direct dictionary removal/restoration to ensure atomic, side-effect-free connectivity verification.

### Follow-ups

- None.

---

## Review — 2026-11-11

**Verdict:** pass

### Acceptance criteria

| AC | Met | Proved by |
|---|---|---|
| AC-1 | yes | `TuningConfigTests.LoadOrFallback_ValidatesGrinderParametersWithSafeFallback` |
| AC-2 | yes | `GrinderSimTests.Evaluate_ValidatesHitDistanceAndSlotGating`, `GrinderSimTests.Step_AccumulatesProgressAndAtomicallyRefundsSteelBlockOnCompletion` |
| AC-3 | yes | `GrinderSimTests.Step_ResetsProgressOnTargetChangeOrFireRelease` |
| AC-4 | yes | `GrinderSimTests.Step_RejectsCoreCargoFullAndGridDisconnectionWithoutMutatingState` |

### Drift

None. `GrinderSim` is pure C# in `Keel.Sim.Grid` with zero engine dependencies, upholding ADR 0001.

### Findings

None.

### Follow-ups

None.


