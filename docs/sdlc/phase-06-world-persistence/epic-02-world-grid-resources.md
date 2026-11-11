# Epic 02 — Station Grid, Refinery & Ore Persistence

**Phase:** 06 World Persistence (Save & Load)
**Goal:** Extend world persistence to save, validate, and restore modified station `BlockGrid` cells and Jolt colliders, active `RefineryState` buffers/progress, and deterministic asteroid `OreDepositState` reserves and visuals.
**Done criteria covered:** DC-3, DC-4, DC-7

## Behaviour

Saving the world now captures every occupied cell in the station `BlockGrid` (preserving both player-placed `ArmorBlock` additions and deconstructed blueprint blocks), the station `RefineryState` (`IronOre` input buffer, `SteelBlock` output buffer, `CycleProgress`, and `RefineryStatus`), and all asteroid `OreDepositState` reserves keyed by deterministic `DepositId`. On load, `WorldSaveSerializer` verifies that the station grid still contains `BlockType.Core` at `(0, 0, 0)` and forms a single `6`-connected component (`BlockGrid.IsConnected()`), `StationGridNode.ReplaceGrid` dynamically rebuilds all `2.5 m` block meshes and Jolt `BoxShape3D` colliders, `RefineryState` resumes mid-cycle conversion seamlessly on the next physics tick, and each `OreDepositNode` restores its remaining yield and depleted visual appearance.

## Acceptance criteria

- **AC-1** — Given a `BlockGrid` modified from `StationBlueprint.CreateDefault()` by placing new `ArmorBlock` cells and removing non-essential blueprint cells, when round-tripped through `WorldSaveSerializer` and restored via `WorldStateMapper`, then the restored `BlockGrid` contains the exact saved `(Int3, BlockType)` set; and if a JSON payload omits `BlockType.Core` at `(0, 0, 0)`, contains `BlockType.None` or duplicate cell coordinates, or yields `IsConnected() == false`, then `WorldSaveSerializer.Deserialize` rejects the save with `IsSuccess == false`.
- **AC-2** — Given a `RefineryState` mid-cycle (with `IronOre` in `InputInventory`, `SteelBlock` in `OutputInventory`, and `0 < CycleProgress < 1`), when round-tripped through `WorldSaveSerializer`, restored, and stepped via `RefinerySim.Step` for the remaining cycle duration, then the in-progress batch completes on schedule without losing ore, duplicating output blocks, or overflowing buffer capacity.
- **AC-3** — Given asteroid `OreDepositState` instances with deterministic `DepositId` keys (`asteroid-01-seam-0` .. `asteroid-01-seam-3`), including partially mined and fully depleted (`RemainingUnits == 0`) seams, when round-tripped and restored onto world deposits, then each matching deposit updates its `RemainingUnits` (clamped to `[0, InitialUnits]`), `IsDepleted` reflects `RemainingUnits == 0`, and any world deposit absent from the save retains its initial yield.
- **AC-4** — Given `StationGridNode` and `OreDepositNode` in `Keel.Game`, when `GameRoot.ApplyWorldSnapshot` applies a loaded `WorldSaveData`, then `StationGridNode.ReplaceGrid` clears prior block visuals and `CollisionShape3D` nodes and builds visuals/colliders matching the loaded `BlockGrid`, and each `OreDepositNode.UpdateVisuals()` immediately renders depleted seams in their dark basalt depleted state.

## Tests

- `WorldPersistenceTests.RoundTrip_PreservesModifiedStationGridAndRejectsDisconnectedOrCorelessGrids` — proves exact restoration of placed and deconstructed station blocks and rejection of coreless, duplicate, or disconnected station topologies (`AC-1`).
- `WorldPersistenceTests.RoundTrip_PreservesMidCycleRefineryAndContinuesRefiningWithoutLoss` — proves `RefineryState` buffer and `CycleProgress` restoration and exact conversion completion on subsequent `RefinerySim.Step` ticks (`AC-2`).
- `WorldPersistenceTests.RoundTrip_RestoresOreDepositReservesByIdAndClampsBounds` — proves `DepositId` matching, depleted state restoration, `[0, InitialUnits]` clamping, and untouched defaults for unmatched seams (`AC-3`).

## Files

- `src/Keel.Sim/Grid/BlockGrid.cs`
- `src/Keel.Sim/Mining/OreDepositState.cs`
- `src/Keel.Sim/Persistence/WorldSaveData.cs`
- `src/Keel.Sim/Persistence/WorldSaveSerializer.cs`
- `src/Keel.Sim/Persistence/WorldStateMapper.cs`
- `src/Keel.Game/World/LandmarkBuilder.cs`
- `src/Keel.Game/World/OreDepositNode.cs`
- `src/Keel.Game/World/StationGridNode.cs`
- `src/Keel.Game/GameRoot.cs`
- `tests/Keel.Sim.Tests/Persistence/WorldPersistenceTests.cs`

## Out of scope

- `F5` quicksave / `F9` quickload hotkeys and HUD / `F3` persistence status indicators (`epic-03`)
- Saving multi-asteroid clusters beyond the initial landmark asteroid (Phase 09)

## Notes

- Because `BlockGrid.TryPlaceBlock` requires incremental adjacency during insertion, add a clean bulk load/replace helper on `BlockGrid` (e.g., `LoadCells(IEnumerable<KeyValuePair<Int3, BlockType>>)` that populates cells, verifies `GetBlock(Int3.Zero) == BlockType.Core`, and checks `IsConnected()`, rolling back and returning `false` on invalid topology) so valid saved grids load regardless of JSON array element ordering!
- Assign explicit IDs `"asteroid-01-seam-0"` through `"asteroid-01-seam-3"` to the four `OreDepositNode` children in `LandmarkBuilder.CreateAsteroid` and expose them to `GameRoot` so snapshot capture and restoration never rely on fragile scene-tree child indices.
- In `StationGridNode.ReplaceGrid(BlockGrid source)`, iterate a copy of `_blockVisuals.Keys` / `_blockColliders.Keys` calling `RemoveBlockVisualAndCollider`, synchronize `_grid` cells, and call `BuildAllBlockVisualsAndColliders()`.
