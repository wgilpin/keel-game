# Test plan — Phase 04 Station Grid Construction

## Coverage

| Done criterion | Proved by | Kind |
|---|---|---|
| DC-1 | `TuningConfigTests.LoadOrFallback_ValidatesBuildRangeWithSafeFallback`, `BlockPlacementTests.Evaluate_QuantizesSignedFaceNormalsToAdjacentCandidateCellWithinBuildRange` | automated |
| DC-2 | `BlockPlacementTests.TryPlace_AtomicallyConsumesOneSteelBlockAndChainsOntoNewlyPlacedCells` | automated |
| DC-3 | `BlockPlacementTests.EvaluateAndTryPlace_RejectCellsOverlappingSuitOrShipBoundsWithoutFlushNeighborFalsePositives`, `BlockPlacementTests.EvaluateAndTryPlace_ProtectRefineryFrontPortWhileAllowingOtherRefineryFaces` | automated |
| DC-4 | `BlockPlacementTests.Evaluate_ShowsGhostOnlyWhenSlotTwoHasStockWithinRangeAndModalClosed` | automated |
| DC-5 | `SuitVisorHudTests.VisorModel_SurfacesConstructionPromptsAndRangeDimmingWhilePreservingInteractFlags`, `SuitVisorHudTests.VisorModel_IsolatesDrillAndBuildSlotStatesAndZeroStockFallbacks` | automated |
| DC-6 | `BlockPlacementTests.ClosedLoop_MineRefineWithdrawPlaceAndBoardConservesItemsAndGridConnectivity` | automated |
| DC-7 | Open `src/Keel.Game/project.godot` in Godot 4.7 (.NET) and press `F5`: (1) fly the ship from `(0, 0, 0)` to the asteroid, press `F` to exit into the zero-g suit, mine `IronOre` with `[1] Drill`, fly/jet to the station at `(0, 0, -500)`, open the Refinery Terminal (`F`), deposit ore (`E`), and withdraw refined `SteelBlock` items (`R`) onto hotbar slot `[2]`; (2) press `2` to select `Steel Block` and aim at a station `ArmorBlock` past `7.5 m` (`OUT OF RANGE` + dimmed reticle, no ghost cube) and then within `3.5 m`–`7.5 m` (`#38BDF8` cyan `2.5 m` wireframe ghost cube snapped to the face normal, `[LMB] Place Steel Block` prompt, and `F3` telemetry showing struck and candidate `Int3` cells); (3) jet within `1.5 m` of the face (or aim directly at the Refinery's `+Z` amber port) and verify the ghost cube and prompt turn `#EF4444` red (`INVALID CELL`) and clicking `LMB` consumes `0` `SteelBlock` items; (4) back clear to a valid face, click and hold `LMB`, and confirm exactly `1` `SteelBlock` is consumed per click, spawning a permanent `#64748B` block with `#334155` seam lines that collides physically with the suit and supports chaining a second `SteelBlock` onto its face until slot `[2]` reaches `0` (`NO STEEL BLOCKS — REFINE ORE AT STATION`, ghost hidden). | manual |

Every done criterion has a row. A criterion with no row is a gap.

## Gaps

None.

## How to run

`dotnet test Keel.sln`

## Test data

- Default `config/tuning.json` checked into the repository plus in-memory JSON payloads and synthetic `BlockGrid`, `StationBlueprint`, `SuitInventory`, `OreDepositState`, and `RefineryState` fixtures in `Keel.Sim.Tests`.
