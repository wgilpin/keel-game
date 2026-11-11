# Epic 02 — Placement Ghost and Clearance

**Phase:** 04 Station Grid Construction
**Goal:** Preview a collision-free `2.5 m` translucent wireframe ghost cube (`#38BDF8` valid / `#EF4444` invalid) on `StationGridNode` and block placements that overlap the suit/ship or seal the Refinery's `+Z` port.
**Done criteria covered:** DC-3, DC-4, DC-7

## Behaviour

When hotbar slot `[2] Steel Block` is selected with `>= 1` `SteelBlock` in suit cargo and the forward raycast hits a station block within `buildRange`, `BlockGhostNode` displays a `2.5 m` translucent (`35%` alpha) cube framed by 12 crisp unshaded wireframe edges at the snapped candidate `Int3` cell. When the candidate cell is clear and buildable, the ghost renders in `#38BDF8` cyan (`Valid`). If the candidate `2.5 m` cell is already occupied, sits directly in front of the `BlockType.Refinery` `+Z` terminal port, or intersects the player's suit collider or parked ship hull in station-local space, `BlockPlacementSim` marks the preview `InvalidCell`: the ghost turns `#EF4444` danger red and clicking `LMB` refuses placement without consuming a `SteelBlock`. Switching away from slot `[2]`, exhausting the last `SteelBlock`, aiming beyond `buildRange` or into open space, or opening the Refinery Terminal immediately hides the ghost cube.

## Acceptance criteria

- **AC-1** — Given candidate `Int3` cells (`[center - 1.25 m, center + 1.25 m]`) evaluated against station-local dynamic body AABBs (suit collider and ship hull bounds), when `BlockPlacementSim.Evaluate` and `TryPlace` run, then any overlap inside the candidate cell (beyond a small skin epsilon so flush contact with neighboring faces does not false-trigger) returns `PlacementStatus.InvalidCell` with `ShowGhost == true` and `IsValid == false` and rejects `TryPlace` with `0` `SteelBlock` items consumed, while positions strictly outside the candidate cell evaluate `Valid`.
- **AC-2** — Given a `BlockGrid` containing `BlockType.Refinery` (such as `StationBlueprint.CreateDefault()`), when `BlockPlacementSim.Evaluate` and `TryPlace` target a cell that is already occupied or lies immediately on the `+Z` terminal port face of any `BlockType.Refinery` (`grid.GetBlock(candidateCell + new Int3(0, 0, -1)) == BlockType.Refinery`), then evaluation returns `PlacementStatus.InvalidCell` (`ShowGhost == true`, `IsValid == false`) and blocks `TryPlace`, while exposed non-port faces of the `Refinery` (`±X`, `±Y`, `-Z`) remain `Valid`.
- **AC-3** — Given `BlockPlacementSim.Evaluate` across slot switches (`[1]`–`[5]`), `SteelBlock` depletion (`1 -> 0` after `TryPlace`), range boundaries (`<= buildRange` vs. `> buildRange`), off-station misses, and `isModalOpen == true`, then `ShowGhost` is `true` if and only if `activeSlot == 2`, `steelBlockCount > 0`, `!isModalOpen`, and a station block is hit within `buildRange` (`Valid` or `InvalidCell`), and drops to `false` in all other states.

## Tests

- `BlockPlacementTests.EvaluateAndTryPlace_RejectCellsOverlappingSuitOrShipBoundsWithoutFlushNeighborFalsePositives` — proves 3D AABB clearance rejection for suit/ship overlap and boundary tolerance (`AC-1`).
- `BlockPlacementTests.EvaluateAndTryPlace_ProtectRefineryFrontPortWhileAllowingOtherRefineryFaces` — proves `+Z` refinery port protection, occupied-cell `InvalidCell` reporting, and valid side-face construction (`AC-2`).
- `BlockPlacementTests.Evaluate_ShowsGhostOnlyWhenSlotTwoHasStockWithinRangeAndModalClosed` — proves `ShowGhost` visibility transitions across slot changes, last-block consumption, range limits, and modal lock (`AC-3`).

## Files

- `src/Keel.Sim/Grid/BlockPlacementSim.cs`
- `src/Keel.Game/World/BlockGhostNode.cs`
- `src/Keel.Game/World/StationGridNode.cs`
- `src/Keel.Game/GameRoot.cs`
- `tests/Keel.Sim.Tests/Grid/BlockPlacementTests.cs`

## Out of scope

- Visor HUD construction context prompts (`[LMB] Place Steel Block`, `INVALID CELL`), reticle range dimming on slot `[2]`, and `F3` candidate cell readout (`epic-03`)

## Notes

- Use exact placement ghost tokens from [design.md](../../design.md): valid `#38BDF8` (`35%` alpha translucent fill + crisp unshaded wireframe lines) and invalid `#EF4444` (`35%` alpha translucent fill + crisp unshaded wireframe lines). Size the ghost slightly proud of `2.50 m` (e.g. `2.505 m` fill box and `2.52 m` line cage) so shared faces with adjacent `#64748B` blocks never Z-fight. Do not write unit tests asserting colour constants (`CLAUDE.md`).
- Attach `BlockGhostNode` with **zero `CollisionShape3D` children** under `StationGridNode` (using local `cell * 2.5f` coordinates) so forward physics raycasts pass straight through the ghost cube without needing RID exclusions, and hide it whenever `PilotMode == PilotMode.SeatedInCockpit`.
- Compute conservative station-local AABBs for `SuitRigNode` (capsule radius `0.4 m`, half-height `0.9 m`) and `ShipRigNode` (hull box `3.0 m x 1.8 m x 4.0 m`, transformed across the 8 local box corners so arbitrary 6DoF ship orientation is respected) before passing them into `BlockPlacementSim`.
