# Test plan — Phase 07 Station Power & Cargo Storage

## Coverage

| Done criterion | Proved by | Kind |
|---|---|---|
| DC-1 | `BlockPlacementTests.MultiSlotPlacement_PlacesSolarAndCargoBlocksAndEnforcesMultiBlockSteelCost`, `GrinderSimTests.Step_RefundsFullMultiBlockCostAndBlocksWhenRemainingSuitCapacityInsufficient` | automated |
| DC-2 | `TuningConfigTests.LoadOrFallback_ValidatesPhase07PowerAndCargoFieldsWithSafeFallback`, `StationPowerTests.Evaluate_ComputesGenerationDemandBrownoutAndUnpoweredEfficiency`, `StationPowerTests.RefineryStep_ScalesProgressWithPowerEfficiencyAndPreservesStateWhenUnpowered`, `DebugTelemetryFormatterTests.Format_RendersStationPowerAndStorageReadouts` | automated |
| DC-3 | `StationCargoTests.CargoStore_IsolatesPerCellContainersAndContributesStandbyPowerDemand`, `GrinderSimTests.Step_BlocksNonEmptyCargoContainerAndAllowsDeconstructionOnceEmptied` | automated |
| DC-4 | `StationCargoTests.Transfers_DepositAndWithdrawIronOreSteelBlocksAndAllItemsWithinBounds`, `SuitVisorHudTests.VisorModel_PrioritizesCargoPromptHotbarCountsAndContainerGrinderWarnings` | automated |
| DC-5 | `CargoTerminalTests.ViewModel_ComputesActionAvailabilityPowerStateAndCellSummary`, `CargoTerminalTests.FullLoop_MineStorePowerRefineBuildAndPersistConservesAllItems` | automated |
| DC-6 | `WorldPersistenceTests.RoundTrip_PreservesSolarAndCargoContainersAndRejectsInvalidOrOrphanedCargo` | automated |
| DC-7 | Launch `src/Keel.Game/project.godot` in Godot 4.7 (.NET): (1) jet to the station, mine `IronOre`, open the Refinery Terminal (`F`), deposit ore (`E`), and verify `25 / 100 kW` (`25%` brownout) refining progress and `F3` `PWR` telemetry; (2) withdraw `4` `SteelBlock` items (`R`), select `[4] Solar Panel`, place `2` photovoltaic `SolarPanel` blocks onto the station spars, and verify station power rises to `125 / 100 kW` (`100%` nominal) with full `2.0s` refining cycles; (3) withdraw `2` more `SteelBlock` items, select `[5] Cargo Box`, place a cyan-trimmed `CargoContainer` block, aim at it within `5.0 m`, and press `F` to open the slate-cyan Cargo Container Terminal; (4) test `[1]`/`[2]` (`IronOre`), `[3]`/`[4]` (`SteelBlock`), and `[E]`/`[R]` bulk transfers to store reserves exceeding the `50`-unit suit limit; (5) select `[3] Grinder` and verify aiming at a non-empty `CargoContainer` displays `CONTAINER NOT EMPTY` and refuses to grind, whereas emptying the container allows grinding and refunds `2` `SteelBlock` items; (6) rebuild and stock a `CargoContainer`, press `F5` to quicksave, mutate state, and press `F9` (and restart the window) to verify `SolarPanel`s, `CargoContainer`s, power readouts, and stored cargo restore cleanly. | manual |

Every done criterion has a row. A criterion with no row is a gap.

## Gaps

None.

## How to run

`dotnet test Keel.sln`

## Test data

- Seeded `BlockGrid` topologies (`StationBlueprint.CreateDefault()` plus placed `SolarPanel` and `CargoContainer` cells) and `TuningConfig` overrides (including `corePowerKw = 0` for `Unpowered` zero-generation edge cases) in `Keel.Sim.Tests`.
- Multi-container `StationCargoStore` fixtures and JSON save payloads (valid, overflowing container capacity, negative item counts, duplicate container cells, and orphaned `CargoContainerSaveData` cells not matching `BlockType.CargoContainer` in `StationBlocks`).
