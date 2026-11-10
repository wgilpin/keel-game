# Test plan — Phase 03 Station Refinery

## Coverage

| Done criterion | Proved by | Kind |
|---|---|---|
| DC-1 | `BlockGridTests` (`TryPlaceBlock_EnforcesUnoccupiedAndSixFaceAdjacencyRules`, `StationBlueprint_SeedsConnectedGridWithCoreArmorAndExposedRefinery`) | automated |
| DC-2 | `BlockGridTests.ResolveHitCell_MapsExactFaceHitsAcrossSignedCoordinatesWithoutSeamFlip` | automated |
| DC-3 | `TuningConfigTests.LoadOrFallback_ValidatesRefineryParametersWithSafeFallback`, `RefinerySimTests.Step_ConvertsOreAcrossFractionalAndMultiCycleTicksAndStallsWhenStarvedOrOutputFull` | automated |
| DC-4 | `RefinerySimTests.DepositAndWithdraw_RespectItemFiltersAndBothInventoryCapacities`, `SuitVisorHudTests.VisorModel_PrioritizesRefineryPromptsAndOutOfRangeStates` | automated |
| DC-5 | `RefineryTerminalTests.InteractAndCancel_PrioritizeRefineryOpenCloseOverShipBoardingAndAutoCloseOutOfTether` | automated |
| DC-6 | `RefineryTerminalTests.ModalSession_MasksFlightAndDrillWhileAllowingDepositWithdrawAndBackgroundRefining` | automated |
| DC-7 | Open `src/Keel.Game/project.godot` in Godot 4.7 (.NET) and press `F5`: (1) fly from spawn toward the station at `(0, 0, -500)` and confirm it renders from `2.5 m` `#64748B` armor blocks with `#334155` seam lines and an `#F59E0B` amber-trimmed `#475569` refinery block on the origin-facing side; (2) mine `IronOre` at the asteroid, return to the station, press `F` to exit into the suit, aim at station blocks within `25 m`, and confirm `F3` telemetry updates the target `Int3` cell and block type; (3) aim at the refinery block past `5 m` (`OUT OF RANGE` + dimmed reticle) and then within `5 m` (`[F] Open Refinery`), press `F`, and verify the slate-cyan Refinery Terminal opens, dims the visor, unlocks the mouse cursor, and locks `W`/`A`/`S`/`D`/`E`/`LMB` suit thrust and drill fire while suit dampeners keep holding steady; (4) press `E` to deposit `IronOre`, watch the amber `REFINING` bar tick over `2.0 s` cycles (including after closing with `Esc` and reopening with `F`), press `R` to withdraw `SteelBlock` items onto hotbar slot `[2]`, and board/exit the ship to confirm `SteelBlock` stack count persists. | manual |

Every done criterion has a row. A criterion with no row is a gap.

## Gaps

None.

## How to run

`dotnet test Keel.sln`

## Test data

- Default `config/tuning.json` checked into the repository plus in-memory JSON payloads and synthetic `BlockGrid`, `RefineryState`, and `SuitInventory` fixtures in `Keel.Sim.Tests`.
