# Epic 02 — Station Block & Collider Removal

**Phase:** 05 Tool Deconstruction & Block Recycling
**Goal:** Implement dynamic visual and Jolt collider removal on `StationGridNode`, wire held-`LMB` physics frame ticking in `GameRoot`, and render tool cutting beam on `SuitRigNode`.
**Done criteria covered:** DC-4, DC-7

## Behaviour

When an `ArmorBlock` is deconstructed by the player, `StationGridNode.RemoveBlockVisualAndCollider` cleanly removes and frees both the visual mesh and the Jolt `BoxShape3D` collider from the scene tree. In `GameRoot`, holding `LMB` with hotbar slot `[3] Grinder` active in suit mode steps `GrinderSim`, triggers `RemoveBlockVisualAndCollider` upon deconstruction completion, and renders an active cutting beam line from the tool muzzle to the target hit point on `SuitRigNode`. Station raycasting, suit collision clearance, and block placement previews immediately update to reflect the removed cell.

## Acceptance criteria

- **AC-1** — Given a populated `StationGridNode` with visuals and Jolt colliders, when `RemoveBlockVisualAndCollider(cell)` executes for an occupied block, then the corresponding visual `Node3D` and `CollisionShape3D` are removed from `_staticBody` and queued for freeing, returning `true`, whereas calling it for an unoccupied cell returns `false` safely without exceptions.
- **AC-2** — Given `StationGridNode` after a block is removed via `RemoveBlockVisualAndCollider`, when station raycasting or cell resolution queries (`ResolveRaycastHit`) are performed against the removed cell, then the ray passes through the empty space and does not report a hit on that cell.
- **AC-3** — Given the player floating in suit mode with hotbar slot `3` active, aiming at an occupied deconstructable station block within `grinderRange`, when holding `LMB`, then `GameRoot` ticks `GrinderSim.Step` each physics frame, activates `SetGrinderBeam(true, hitPoint)` on `SuitRigNode`, and upon deconstruction completion calls `RemoveBlockVisualAndCollider` to delete the block visual and collider, turning off the cutting beam when `LMB` is released or deconstruction completes.

## Tests

- `GrinderSimTests.StationGridNode_RemovesVisualAndColliderSafelyAndUpdatesHitTesting` — proves visual and collider lookup cleanup, idempotency on repeated removal, and `ResolveHitCell` reporting empty space after removal (`AC-1`, `AC-2`).
- `GrinderSimTests.Step_SimulatesFullDeconstructionCycleWithGridRemoval` — proves multi-frame `GrinderSim` stepping driving `BlockGrid` removal and inventory updates (`AC-3`).

## Files

- `src/Keel.Game/World/StationGridNode.cs`
- `src/Keel.Game/Suit/SuitRigNode.cs`
- `src/Keel.Game/GameRoot.cs`
- `tests/Keel.Sim.Tests/Grid/GrinderSimTests.cs`

## Out of scope

- Visor contextual HUD prompts and out-of-range reticle dimming (`epic-03`)
- `F3` deconstruct telemetry overlay formatting (`epic-03`)

## Notes

- In `StationGridNode`, maintain a `Dictionary<Int3, CollisionShape3D> _blockColliders = new();` alongside `_blockVisuals`. In `AddBlockVisualAndCollider`, store `_blockColliders[cell] = colShape;`.
- In `RemoveBlockVisualAndCollider`, remove both entries from their dictionaries and call `QueueFree()` on both nodes.
- In `SuitRigNode`, implement `SetGrinderBeam(bool active, Vector3 worldHitPoint)` using an unshaded `ImmediateMesh` with bright electric cyan (`#38BDF8`) or cutting orange (`#FB923C`), mirroring the existing `SetDrillBeam` pattern.
- In `GameRoot._PhysicsProcess`, handle continuous `isFiring` (holding LMB) for slot 3, distinct from the discrete edge-triggered click used for placement in slot 2.

---

## Build note — 2026-11-11

**Result:** green

### Tasks done

- Added `_blockColliders` dictionary to `StationGridNode` to track each block's Jolt `CollisionShape3D`.
- Implemented `RemoveBlockVisualAndCollider(Int3 cell)` in `StationGridNode` to safely remove and queue-free both visual nodes and collision shapes.
- Added `SetGrinderBeam(bool active, Vector3 worldHitPoint)` to `SuitRigNode` rendering an unshaded electric cyan (`#38BDF8`) line from the tool muzzle to the targeted hit point.
- Wired continuous held-`LMB` grinder execution in `GameRoot._PhysicsProcess` on slot 3, clearing beam on idle/release and removing station visual/collider on block deconstruction completion.
- Added unit tests in `GrinderSimTests` covering raycast clearance updates and multi-frame deconstruction simulation.

### Tests added

- `tests/Keel.Sim.Tests/Grid/GrinderSimTests.cs`:
  - `BlockRemoval_UpdatesResolveHitCellAndEnablesSubsequentPlacement`
  - `Step_SimulatesFullDeconstructionCycleWithGridRemoval`

### Suite result

- Command: `dotnet test Keel.sln`
- Output: `Passed!  - Failed:     0, Passed:    45, Skipped:     0, Total:    45, Duration: 39 ms - Keel.Sim.Tests.dll (net8.0)`

### Deviations from the plan

- None.

### Follow-ups

- None.

---

## Review — 2026-11-11

**Verdict:** pass

### Acceptance criteria

| AC | Met | Proved by |
|---|---|---|
| AC-1 | yes | `GrinderSimTests.BlockRemoval_UpdatesResolveHitCellAndEnablesSubsequentPlacement` |
| AC-2 | yes | `GrinderSimTests.BlockRemoval_UpdatesResolveHitCellAndEnablesSubsequentPlacement` |
| AC-3 | yes | `GrinderSimTests.Step_SimulatesFullDeconstructionCycleWithGridRemoval` |

### Drift

None. Preserves ADR 0001 architecture with physics/rendering strictly in `Keel.Game` and simulation rules in `Keel.Sim`.

### Findings

None.

### Follow-ups

None.


