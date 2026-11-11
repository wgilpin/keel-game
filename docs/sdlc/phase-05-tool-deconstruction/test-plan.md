# Test plan — Phase 05 Tool Deconstruction & Block Recycling

## Coverage

| Done criterion | Proved by | Kind |
|---|---|---|
| DC-1 | `TuningConfigTests.LoadOrFallback_ValidatesGrinderParametersWithSafeFallback`, `GrinderSimTests.Evaluate_ValidatesHitDistanceAndSlotGating` | automated |
| DC-2 | `GrinderSimTests.Step_AccumulatesProgressAndAtomicallyRefundsSteelBlockOnCompletion`, `GrinderSimTests.Step_ResetsProgressOnTargetChangeOrFireRelease` | automated |
| DC-3 | `GrinderSimTests.Step_RejectsCoreCargoFullAndGridDisconnectionWithoutMutatingState` | automated |
| DC-4 | `GrinderSimTests.StationGridNode_RemovesVisualAndColliderSafelyAndUpdatesHitTesting` | automated |
| DC-5 | `SuitVisorHudTests.VisorModel_SurfacesGrinderSlotAndContextualPrompts`, `SuitVisorHudTests.VisorModel_HandlesCoreCargoFullAndOutOfRangeForGrinder` | automated |
| DC-6 | `GrinderSimTests.ClosedLoop_PlaceAndDeconstructConservesSteelBlocksAndGridConnectivity`, `DebugTelemetryFormatterTests.Format_SurfacesGrinderProgress` | automated |
| DC-7 | Launch `src/Keel.Game/project.godot` in Godot 4.7 (.NET) and press `F5`: (1) fly the ship from `(0, 0, 0)` to the station at `(0, 0, -500)`, exit the cockpit into the zero-g suit (`F`), and ensure suit cargo has at least 1 `SteelBlock` (or mine and refine ore via slots `[1]` and `[2]`); (2) press `2` and place an `ArmorBlock` onto the station collar; (3) press `3` to select `Grinder` on the hotbar and aim at the placed block from `> 5.0 m` (`OUT OF RANGE` prompt + dimmed center reticle); (4) jet within `3.0 m`, hold `LMB` to observe the cutting beam line and `[LMB] Deconstruct Block` prompt; (5) hold `LMB` until progress reaches 100%, verifying the block visual and Jolt collision disappear while suit cargo gains `1` `SteelBlock`; (6) aim at the station `Core` block at `(0, 0, 0)` within range, verifying `CANNOT DECONSTRUCT CORE` is displayed and holding `LMB` does not damage or remove the Core block. | manual |

Every done criterion has a row. A criterion with no row is a gap.

## Gaps

None.

## How to run

`dotnet test Keel.sln`

## Test data

- Default `config/tuning.json` checked into the repository plus in-memory JSON test payloads.
- Synthetic `BlockGrid`, `StationBlueprint`, `SuitInventory`, and `OreDepositState` fixtures in `Keel.Sim.Tests`.
