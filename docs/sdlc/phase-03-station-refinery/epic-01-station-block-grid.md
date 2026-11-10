# Epic 01 — Station BlockGrid

**Phase:** 03 Station Refinery
**Goal:** Seed the prebuilt station on a `2.5 m` `BlockGrid`, render its block meshes, seam lines, and Jolt colliders in `StationGridNode`, and resolve suit raycast hits to `Int3` cells in `F3` telemetry.
**Done criteria covered:** DC-1, DC-2, DC-7

## Behaviour

At `SpaceArenaLayout.StationPosition` (`(0, 0, -500)`), the monolithic placeholder box from Phase 01 is replaced by `StationGridNode`, built directly from a `Keel.Sim.Grid.BlockGrid` (`2.5 m` cell pitch) seeded by `StationBlueprint.CreateDefault()`. The station features `#64748B` `ArmorBlock` cells framed by `#334155` `2.5 m` cell seam lines, an anchored `Core` cell at `Int3(0, 0, 0)`, symmetric radiator spars so its silhouette reads clearly from `500 m`, and an accessible `#475569` `Refinery` block with glowing `#F59E0B` port trim on the sunlit, origin-facing (`+Z`) side. When floating in the suit and aiming within `25 m` of the station, a forward raycast resolves the struck `Int3` cell and `BlockType` and displays it live in the top-left `F3` Debug Telemetry Overlay (`GRID (x, y, z) REFINERY` or `GRID --`).

## Acceptance criteria

- **AC-1** — Given an empty or populated `BlockGrid` (`CellSize = 2.5 m`), when `TryPlaceBlock` is called across candidate `Int3` coordinates, then the initial anchor block succeeds on an empty grid, subsequent blocks succeed if and only if the target `Int3` is unoccupied and shares at least one of the 6 cardinal faces (`±X`, `±Y`, `±Z`) with an existing occupied cell (rejecting diagonal/edge-only or disconnected cells), and `StationBlueprint.CreateDefault()` produces a 6-connected station containing `BlockType.Core`, `BlockType.ArmorBlock`, and an exposed `+Z`-face `BlockType.Refinery` cell.
- **AC-2** — Given station-local raycast hit positions and unit outward surface normals on `2.5 m` cell faces across positive, zero, and negative `Int3` coordinates (including small `±1e-5` floating-point jitter on exact `±1.25 m` half-cell seams), when `BlockGrid.ResolveHitCell` resolves the hit, then inward normal biasing maps every face hit to the exact struck occupied `Int3` cell and its `BlockType` rather than flipping into the adjacent empty neighbor cell, and returns `false` when the biased cell is unoccupied.

## Tests

- `BlockGridTests.TryPlaceBlock_EnforcesUnoccupiedAndSixFaceAdjacencyRules` — covers empty-grid anchor placement, 6-face adjacency acceptance, occupied-cell rejection, and diagonal/disconnected rejection (`AC-1`).
- `BlockGridTests.StationBlueprint_SeedsConnectedGridWithCoreArmorAndExposedRefinery` — proves the default station seed is 6-connected and exposes a `+Z`-accessible `BlockType.Refinery` cell with unoccupied approach space (`AC-1`).
- `BlockGridTests.ResolveHitCell_MapsExactFaceHitsAcrossSignedCoordinatesWithoutSeamFlip` — proves inward-normal cell mapping across negative, origin, and positive `Int3` cells under exact-face float jitter and unoccupied-cell misses (`AC-2`).

## Files

- `src/Keel.Sim/Grid/Int3.cs`
- `src/Keel.Sim/Grid/BlockType.cs`
- `src/Keel.Sim/Grid/BlockGrid.cs`
- `src/Keel.Sim/Grid/StationBlueprint.cs`
- `src/Keel.Sim/Telemetry/DebugTelemetryFormatter.cs`
- `src/Keel.Game/World/StationGridNode.cs`
- `src/Keel.Game/World/LandmarkBuilder.cs`
- `src/Keel.Game/GameRoot.cs`
- `tests/Keel.Sim.Tests/Grid/BlockGridTests.cs`

## Out of scope

- `RefinerySim` recipe ticks, `config/tuning.json` refinery fields, and `[F] Open Refinery` visor prompts (`epic-02`)
- Modal `RefineryTerminalNode` UI and `[E]`/`[R]` item transfers (`epic-03`)
- Placing new `SteelBlock` cells from the suit hotbar (`Phase 04`)

## Notes

- Keep `Keel.Sim.Grid` 100% free of `GodotSharp` ([ADR 0001](../../adr/0001-engine-free-keel-sim-assembly.md)); map `Int3` cell `(x, y, z)` to station-local center `(x * 2.5f, y * 2.5f, z * 2.5f)` so `Int3(0, 0, 0)` spans `[-1.25, +1.25]` on each axis.
- Keep `StationBlueprint.CreateDefault()` compact (~50–80 blocks: central spine/platform, port/starboard radiator wings along `±X` for `500 m` visual silhouette, and `BlockType.Refinery` on the front `+Z` end facing `(0, 0, 0)` with open space in front of its `+Z` face) rather than filling a solid 960-cell volume.
- Structure `StationGridNode` so adding a single cell mesh + `BoxShape3D` (`AddBlockVisualAndCollider(Int3, BlockType)`) is reusable directly in Phase 04 when the player places new `SteelBlock` cells at runtime.
- Do not write unit tests asserting colour constants or simple `DebugTelemetryFormatter` string templates (`CLAUDE.md`).

---

## Build note — 2026-11-10

**Result:** green

### Tasks done

- Implemented `Int3` and `BlockType` (`None`, `Core`, `ArmorBlock`, `Refinery`) in `Keel.Sim.Grid`.
- Implemented `BlockGrid` in `Keel.Sim.Grid` with `CellSize = 2.5m`, anchor placement, 6-cardinal face adjacency enforcement, inward-biased face hit resolution (`ResolveHitCell`), and 6-connected component verification (`IsConnected`).
- Implemented `StationBlueprint.CreateDefault()` in `Keel.Sim.Grid` seeding a 6-connected orbital station with `Core`, longitudinal spine, radiator spars, stabilizer tower, and an exposed front `+Z` `Refinery` cell.
- Updated `DebugTelemetryFormatter` and `DebugTelemetrySnapshot` in `Keel.Sim.Telemetry` to surface targeted `Int3` cells and block types in `F3` telemetry.
- Created `StationGridNode` in `Keel.Game.World` building per-cell visual meshes with `#334155` seam lines, `#64748B` armor material, `#F59E0B` amber port trim, individual `BoxShape3D` colliders, and local raycast hit resolution.
- Updated `LandmarkBuilder.CreateStation` to instantiate `StationGridNode` and wired `GameRoot` forward suit raycast to resolve station hits and stream `GRID (x, y, z) <TYPE>` to `F3` telemetry.

### Tests added

- `tests/Keel.Sim.Tests/Grid/BlockGridTests.cs`
  - `BlockGridTests.TryPlaceBlock_EnforcesUnoccupiedAndSixFaceAdjacencyRules` (proves AC-1)
  - `BlockGridTests.StationBlueprint_SeedsConnectedGridWithCoreArmorAndExposedRefinery` (proves AC-1)
  - `BlockGridTests.ResolveHitCell_MapsExactFaceHitsAcrossSignedCoordinatesWithoutSeamFlip` (proves AC-2)

### Suite result

- Command: `dotnet test Keel.sln`
- Summary: `Passed! - Failed: 0, Passed: 22, Skipped: 0, Total: 22, Duration: 32 ms - Keel.Sim.Tests.dll (net8.0)` (with `Keel.Game` compiled cleanly)

### Deviations from the plan

None.

### Follow-ups

None.

---

## Review — 2026-11-10

**Verdict:** pass

### Acceptance criteria

| AC | Met | Proved by |
|---|---|---|
| AC-1 | yes | `BlockGridTests.TryPlaceBlock_EnforcesUnoccupiedAndSixFaceAdjacencyRules`, `BlockGridTests.StationBlueprint_SeedsConnectedGridWithCoreArmorAndExposedRefinery` |
| AC-2 | yes | `BlockGridTests.ResolveHitCell_MapsExactFaceHitsAcrossSignedCoordinatesWithoutSeamFlip` |

### Drift

None

### Findings

None

### Follow-ups

None


