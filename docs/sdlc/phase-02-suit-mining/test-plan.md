# Test plan — Phase 02 Zero-G Suit and Mining

## Coverage

| Done criterion | Proved by | Kind |
|---|---|---|
| DC-1 | `PilotModeStateMachineTests` (`ExitCockpit_OffsetsInRotatedShipSpaceAndInheritsLinearVelocity`, `TryBoardShip_EnforcesInteractRangeThreshold`) | automated |
| DC-2 | `TuningConfigTests.LoadOrFallback_ValidatesSuitAndMiningParametersWithSafeFallback`, `SuitFlightTests.StepSuit_UsesSuitProfileIndependentlyOfShipCapsAndStopsWithoutOvershoot` | automated |
| DC-3 | `SuitVisorHudTests` (`BearingTape_ProjectsAzimuthsAndClampsOffSpanAndRearLandmarks`, `BearingTape_WrapsCompassTicksCleanlyAcross180Seam`) | automated |
| DC-4 | `SuitVisorHudTests.VisorModel_ClampsHotbarSelectionCargoFillAndBoardingPrompt` | automated |
| DC-5 | `AsteroidMiningTests` (`Inventory_EnforcesCapacityAndPartialOverflowTransfers`, `DrillSim_ExtractsWholeOreUnitsAcrossFractionalTicksUntilDepleted`) | automated |
| DC-6 | `AsteroidMiningTests.DrillSim_HaltsOnOutOfRangeWrongSlotDepletedOrCargoFullAndPersistsAcrossBoarding` | automated |
| DC-7 | Open `src/Keel.Game/project.godot` in Godot 4.7 (.NET) and press `F5`: (1) fly toward the `#27272A` asteroid and verify metallic `#D97706` ore seams are visible on its sunlit face; (2) press `F` to exit into the zero-g suit and confirm the `#94A3B8` ship hull stays floating in space, the amber Cockpit HUD switches to the slate-cyan Suit Visor HUD with right-hand 3D grey-box tool mesh, and `F3` reports `SUIT` mode; (3) rotate in 6DoF and verify `SHIP` and `STATION` diamond markers track and clamp cleanly on the top-center `480px` bearing tape while space-dust particles follow the suit camera; (4) jet within `5m` of an ore seam with `[1] Drill` active, hold `LMB`, and confirm the drill beam, amber reticle extraction ring, and `CARGO` bar (`0 / 50` -> `IronOre`) advance—then step past `5m` or switch to `[2]` to verify drilling stops; (5) jet back within `shipInteractRange` of the ship, confirm `[F] Board Ship` appears, press `F` to re-enter the amber Cockpit HUD, and press `F` once more to confirm mined `IronOre` is still in suit `CARGO`. | manual |

Every done criterion has a row. A criterion with no row is a gap.

## Gaps

None.

## How to run

`dotnet test Keel.sln`

## Test data

- Default `config/tuning.json` checked into the repository plus in-memory JSON payloads and synthetic `OreDepositState` / `Inventory` fixtures in `Keel.Sim.Tests`.
