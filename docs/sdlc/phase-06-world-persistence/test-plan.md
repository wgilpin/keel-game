# Test plan — Phase 06 World Persistence (Save & Load)

## Coverage

| Done criterion | Proved by | Kind |
|---|---|---|
| DC-1 | `WorldPersistenceTests.RoundTrip_PreservesShipSuitPilotModeAndInventoryInvariants`, `WorldPersistenceTests.Deserialize_RejectsMalformedJsonAndNonFiniteKinematicsSafely` | automated |
| DC-2 | `WorldPersistenceTests.FileStore_WritesAtomicallyAndHandlesMissingOrCorruptSaveFiles` | automated |
| DC-3 | `WorldPersistenceTests.RoundTrip_PreservesModifiedStationGridAndRejectsDisconnectedOrCorelessGrids` | automated |
| DC-4 | `WorldPersistenceTests.RoundTrip_PreservesMidCycleRefineryAndContinuesRefiningWithoutLoss`, `WorldPersistenceTests.RoundTrip_RestoresOreDepositReservesByIdAndClampsBounds` | automated |
| DC-5 | `WorldPersistenceTests.FullLoop_QuickloadRollsBackMutatedWorldStateAndConservesItems` | automated |
| DC-6 | `PersistenceSessionTests.Session_TracksSaveLoadFlashTimerAndSurfacesErrorsInTelemetry`, `SuitVisorHudTests.VisorModel_SurfacesTransientSaveLoadNotification` | automated |
| DC-7 | Launch `src/Keel.Game/project.godot` in Godot 4.7 (.NET): (1) fly the ship from `(0, 0, 0)` toward the station at `(0, 0, -500)` and asteroid, exit into the zero-g suit (`F`), and drill one ore seam until depleted (verifying dark basalt depleted visual); (2) fly to the station Refinery Terminal (`F`), deposit `IronOre` (`E`), withdraw at least `2` `SteelBlock` items (`R`), and deposit more ore so a refining cycle is mid-progress; (3) place an `ArmorBlock` with slot `[2]` and deconstruct an outer blueprint spar block with slot `[3] Grinder`; (4) press `F5` to quicksave (observing the Suit Visor / `F3` `SAV  SAVED` readout and inspecting `saves/world.json`), then fly away, place/remove another block, and press `F9` to verify immediate live rollback to the saved pose, inventory, grid colliders, and refinery progress; (5) close the game window (`[X]` or `Alt+F4`) and relaunch to verify automatic exit-save and seamless session resume on startup. | manual |

Every done criterion has a row. A criterion with no row is a gap.

## Gaps

None.

## How to run

`dotnet test Keel.sln`

## Test data

- In-memory JSON world save fixtures (valid, malformed, non-finite `NaN`/`Infinity`, coreless/disconnected station grids, and out-of-bounds deposit/inventory payloads) in `Keel.Sim.Tests`.
- Isolated temporary directories under `Path.GetTempPath()` cleaned up in test disposal for atomic `WorldSaveStore` file I/O verification.
