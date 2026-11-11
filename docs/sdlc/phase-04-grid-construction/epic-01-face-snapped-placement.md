# Epic 01 — Face-Snapped Block Placement

**Phase:** 04 Station Grid Construction
**Goal:** Resolve station raycast face hits within `buildRange` to adjacent `Int3` cells and let discrete `LMB` clicks with `[2] Steel Block` consume `1` `SteelBlock` to spawn permanent station geometry and Jolt collision.
**Done criteria covered:** DC-1, DC-2, DC-7

## Behaviour

`config/tuning.json` adds `buildRange` (`7.5` m default), hot-reloaded alongside existing flight, mining, and refinery parameters. When the player floats in the suit with hotbar slot `[2] Steel Block` active and at least `1` `SteelBlock` in suit cargo, aiming at any `StationGridNode` face within `buildRange` resolves the struck `Int3` cell and snaps along the quantized 6-cardinal outward face normal (`±X`, `±Y`, `±Z`) to the adjacent `Int3` candidate cell. Clicking `LMB` once (edge-triggered while the mouse cursor is captured and `IsRefineryOpen` is `false`) atomically removes `1` `SteelBlock` from `SuitInventory`, inserts `BlockType.ArmorBlock` into `BlockGrid`, and spawns a permanent `#64748B` `2.5 m` block mesh with `#334155` seam lines and a `BoxShape3D` Jolt collider on `StationGridNode`—allowing the player to immediately aim at the new block's faces and chain further blocks outward without holding-`LMB` runaway placement.

## Acceptance criteria

- **AC-1** — Given `config/tuning.json` payloads containing `buildRange`, when `TuningConfigLoader.LoadOrFallback` executes, then positive finite `buildRange` values override the default (`7.5f`) while malformed JSON or zero, negative, or non-finite values retain the fallback `TuningConfig` with `IsSuccess == false`.
- **AC-2** — Given a seeded `BlockGrid`, `SuitInventory` with `>= 1` `SteelBlock`, hotbar slot `2`, and station-local raycast hits across all 6 signed cardinal block faces (`±X`, `±Y`, `±Z`, including slight normal tilt and exact `±1.25 m` seam jitter), when `BlockPlacementSim.Evaluate` computes the target, then hits at distance `<= buildRange` map to the struck `Int3` cell and adjacent `candidateCell = hitCell + faceOffset`, while degenerate near-zero normals, misses on unoccupied space, hits past `buildRange`, non-build slots (`1`, `3`–`5`), or `0` `SteelBlock` stock return non-placeable statuses (`NoTarget`, `OutOfRange`, `Inactive`, `NoBlocks`).
- **AC-3** — Given `BlockPlacementSim.TryPlace` across valid, occupied, out-of-range, wrong-slot, modal-locked, and zero-stock states, when discrete placement executes, then a valid call adds `BlockType.ArmorBlock` at `candidateCell` and deducts `1` `ItemId.SteelBlock` from `SuitInventory` (preserving `BlockGrid.IsConnected()` and allowing immediate chained placement onto any exposed face of `candidateCell`), whereas any invalid state leaves both `BlockGrid.Count` and `SuitInventory` completely unchanged.

## Tests

- `TuningConfigTests.LoadOrFallback_ValidatesBuildRangeWithSafeFallback` — proves `buildRange` parsing and non-positive/malformed fallback (`AC-1`).
- `BlockPlacementTests.Evaluate_QuantizesSignedFaceNormalsToAdjacentCandidateCellWithinBuildRange` — proves 6-axis face normal quantization, seam-jitter resilience, and range/slot/stock gating (`AC-2`).
- `BlockPlacementTests.TryPlace_AtomicallyConsumesOneSteelBlockAndChainsOntoNewlyPlacedCells` — proves 1-for-1 `SteelBlock` consumption, chained multi-block extension, and zero-mutation rollback on invalid states (`AC-3`).

## Files

- `config/tuning.json`
- `src/Keel.Sim/Config/TuningConfig.cs`
- `src/Keel.Sim/Config/TuningConfigLoader.cs`
- `src/Keel.Sim/Grid/BlockPlacementSim.cs`
- `src/Keel.Game/Input/ShipInputReader.cs`
- `src/Keel.Game/World/StationGridNode.cs`
- `src/Keel.Game/GameRoot.cs`
- `tests/Keel.Sim.Tests/Config/TuningConfigTests.cs`
- `tests/Keel.Sim.Tests/Grid/BlockPlacementTests.cs`

## Out of scope

- 3D translucent wireframe ghost preview mesh and suit/ship/refinery-port obstruction checks (`epic-02`)
- Visor construction prompts (`[LMB] Place Steel Block`, `INVALID CELL`) and `F3` candidate cell telemetry (`epic-03`)

## Notes

- Keep `BlockPlacementSim` in `Keel.Sim.Grid` 100% free of `GodotSharp` ([ADR 0001](../../adr/0001-engine-free-keel-sim-assembly.md)).
- Add an idempotency guard (`if (_blockVisuals.ContainsKey(cell)) return;`) to `StationGridNode.AddBlockVisualAndCollider` and expose a helper on `StationGridNode` that converts world hit position + world hit normal into station-local `System.Numerics.Vector3` pairs for `BlockPlacementSim`.
- In `ShipInputReader`, record discrete `_pendingPrimaryActionClick` on `InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true }` **only when `isMouseCaptured == true`**, and clear/ignore it in `GameRoot` when `_isRefineryOpen` is `true` or `PilotMode != PilotMode.FloatingInSuit`, so clicking to recapture the cursor after `Esc` or clicking inside the Refinery Terminal never places a block.

---

## Build note — 2026-11-11

**Result:** green

### Tasks done

- Added hot-reloadable `buildRange` (default `7.5f`) in `config/tuning.json`, `TuningConfig`, and `TuningConfigLoader` with safe non-positive/malformed fallback.
- Implemented `PlacementStatus`, `PlacementEvaluation`, and `BlockPlacementSim` in `Keel.Sim.Grid` with 6-cardinal face normal quantization, candidate `Int3` cell calculation, and atomic 1-for-1 `SteelBlock` deduction and `BlockGrid` placement.
- Added discrete primary action click recording and consumption in `ShipInputReader` only while the mouse cursor is captured.
- Added idempotency guard and local ray transformation in `StationGridNode`.
- Wired discrete `LMB` block placement and station mesh spawning in `GameRoot._PhysicsProcess` when slot `[2]` is active.

### Tests added

- `tests/Keel.Sim.Tests/Config/TuningConfigTests.cs`
  - `TuningConfigTests.LoadOrFallback_ValidatesBuildRangeWithSafeFallback` (proves AC-1)
- `tests/Keel.Sim.Tests/Grid/BlockPlacementTests.cs`
  - `BlockPlacementTests.Evaluate_QuantizesSignedFaceNormalsToAdjacentCandidateCellWithinBuildRange` (proves AC-2)
  - `BlockPlacementTests.TryPlace_AtomicallyConsumesOneSteelBlockAndChainsOntoNewlyPlacedCells` (proves AC-3)

### Suite result

- Command: `dotnet test Keel.sln`
- Summary: `Passed!  - Failed:     0, Passed:    32, Skipped:     0, Total:    32, Duration: 36 ms - Keel.Sim.Tests.dll (net8.0)`

### Deviations from the plan

None.

### Follow-ups

None.

---

## Review — 2026-11-11

**Verdict:** pass

### Acceptance criteria

| AC | Met | Proved by |
|---|---|---|
| AC-1 | yes | `TuningConfigTests.LoadOrFallback_ValidatesBuildRangeWithSafeFallback` |
| AC-2 | yes | `BlockPlacementTests.Evaluate_QuantizesSignedFaceNormalsToAdjacentCandidateCellWithinBuildRange` |
| AC-3 | yes | `BlockPlacementTests.TryPlace_AtomicallyConsumesOneSteelBlockAndChainsOntoNewlyPlacedCells` |

### Drift

None

### Findings

None

### Follow-ups

None


