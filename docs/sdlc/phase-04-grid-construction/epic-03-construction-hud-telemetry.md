# Epic 03 — Construction HUD and Telemetry

**Phase:** 04 Station Grid Construction
**Goal:** Wire Suit Visor construction prompts, out-of-range reticle dimming, and `F3` candidate cell telemetry across the full mine-refine-place gameplay loop.
**Done criteria covered:** DC-5, DC-6, DC-7

## Behaviour

Selecting `[2] Steel Block` updates the Suit Visor HUD and `F3` Debug Telemetry Overlay in lockstep with `BlockPlacementSim`. Aiming at a valid station face within `buildRange` shows `[LMB] Place Steel Block` in `#38BDF8` cyan above the hotbar; aiming at an obstructed, occupied, or `+Z` refinery-port cell shows `INVALID CELL` in `#EF4444` danger red; aiming at a station block between `buildRange` and `25 m` dims the center reticle and shows `OUT OF RANGE` in `#F59E0B` amber; and running out of `SteelBlock` items dims slot `[2]` with count `0` and shows `NO STEEL BLOCKS — REFINE ORE AT STATION` (unless aiming at an in-range `Refinery` or standing by the ship, where `[F] Open Refinery` and `[F] Board Ship` remain prioritized). Meanwhile, `F3` telemetry appends the snapped candidate `Int3` cell and validity (`-> PLACE (x, y, z) VALID` / `INVALID`) whenever the placement ghost is shown, closing the grey-box slice from raw asteroid ore to permanent station expansion.

## Acceptance criteria

- **AC-1** — Given `SuitVisorModel.Compute` with `activeSlot == 2` and `steelBlockCount > 0` across `PlacementStatus` values (`Valid`, `InvalidCell`, `OutOfRange`, `NoTarget`) and varying ship/refinery distances, when visor state is evaluated, then `Valid` outputs `"[LMB] Place Steel Block"` (`PromptTone.Primary`), `InvalidCell` outputs `"INVALID CELL"` (`PromptTone.Danger`), and `OutOfRange` outputs `"OUT OF RANGE"` (`PromptTone.Warning`, `IsTargetOutOfRange == true`)—taking prompt display precedence over `"[F] Board Ship"` while keeping `CanBoardShip` and `CanOpenRefinery` (`F`-key eligibility) intact, and falling back to `"[F] Board Ship"` when `PlacementStatus == NoTarget` within `shipInteractRange`.
- **AC-2** — Given `SuitVisorModel.Compute` across hotbar slot switches (`[1]` Drill, `[2]` Steel Block, `[3]`–`[5]` Empty) and zero-stock transitions (`steelBlockCount == 0`), when visor state is evaluated, then drill status prompts (`[LMB] Drill Iron Ore`, `CARGO FULL`, `DEPLETED`, deposit `OUT OF RANGE`) activate only in slot `[1]`, construction prompts/range-dimming activate only in slot `[2]`, and with `steelBlockCount == 0` in slot `[2]` the model prioritizes `"[F] Open Refinery"` (on `Refinery <= refineryInteractRange`), then `"[F] Board Ship"` (within `shipInteractRange`), and otherwise `"NO STEEL BLOCKS — REFINE ORE AT STATION"` with slot `[2]` marked `IsDimmed == true`.
- **AC-3** — Given an end-to-end simulation session starting from `StationBlueprint.CreateDefault()`, an `OreDepositState`, `RefineryState`, `SuitInventory`, and `PilotModeStateMachine`, when the player mines `IronOre` via `DrillSim.Step`, deposits and refines via `RefinerySim`, withdraws `SteelBlock` items, places blocks via `BlockPlacementSim.TryPlace` until `SteelBlock` count reaches `0`, and boards/re-exits the ship, then every `refineryOrePerBlock` mined `IronOre` yields `1` placed `BlockType.ArmorBlock` on a `6`-connected `BlockGrid`, exhausting stock immediately transitions `ShowGhost` to `false` and visor prompt to `"NO STEEL BLOCKS — REFINE ORE AT STATION"`, and both the expanded `BlockGrid` and any unplaced `SuitInventory` items persist across `TryBoardShip` and `ExitCockpit`.

## Tests

- `SuitVisorHudTests.VisorModel_SurfacesConstructionPromptsAndRangeDimmingWhilePreservingInteractFlags` — proves `[LMB] Place Steel Block` / `INVALID CELL` / `OUT OF RANGE` priority, `PromptTone`, `IsTargetOutOfRange`, and `F`-action flag preservation (`AC-1`).
- `SuitVisorHudTests.VisorModel_IsolatesDrillAndBuildSlotStatesAndZeroStockFallbacks` — proves slot `[1]` vs. `[2]` vs. `[3]`–`[5]` prompt/reticle isolation and `steelBlockCount == 0` fallback priority (`AC-2`).
- `BlockPlacementTests.ClosedLoop_MineRefineWithdrawPlaceAndBoardConservesItemsAndGridConnectivity` — proves end-to-end `IronOre` -> `SteelBlock` -> `BlockGrid` conservation, zero-stock ghost/HUD transition, and ship boarding persistence (`AC-3`).

## Files

- `src/Keel.Sim/Hud/SuitVisorModel.cs`
- `src/Keel.Sim/Telemetry/DebugTelemetryFormatter.cs`
- `src/Keel.Game/UI/SuitVisorHudNode.cs`
- `src/Keel.Game/GameRoot.cs`
- `tests/Keel.Sim.Tests/Hud/SuitVisorHudTests.cs`
- `tests/Keel.Sim.Tests/Grid/BlockPlacementTests.cs`

## Out of scope

- Removing/grinding placed blocks, dynamic ship grids, conveyors, power, or save/load persistence ([phases.md](../../phases.md))

## Notes

- Consolidate contextual prompt text, `PromptTone` (`Primary` `#38BDF8`, `Warning` `#F59E0B`, `Danger` `#EF4444`, `Muted` `#8B9EB0`), and `IsTargetOutOfRange` into `SuitVisorModel.Compute` (keeping default parameter values backwards-compatible for existing callers) so `SuitVisorHudNode` renders `visorState` directly without duplicate UI branching.
- Extend `DebugTelemetrySnapshot` with optional `Int3? PlacementCandidateCell = null` and `bool? IsPlacementValid = null` so `DebugTelemetryFormatter` appends `-> PLACE (x, y, z) VALID` / `INVALID` on the `GRID` line when `ShowGhost == true`. Do not write unit tests asserting UI colour hex values or `DebugTelemetryFormatter` string templates (`CLAUDE.md`).
