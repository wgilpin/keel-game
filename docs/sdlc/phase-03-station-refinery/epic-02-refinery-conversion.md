# Epic 02 — Refinery Conversion

**Phase:** 03 Station Refinery
**Goal:** Add hot-reloadable refinery tuning, deterministic tick-based `IronOre` -> `SteelBlock` conversion with bounded suit transfers in `Keel.Sim`, and live station refining plus visor refinery prompts in `Keel.Game`.
**Done criteria covered:** DC-3, DC-4, DC-7

## Behaviour

`config/tuning.json` gains refinery parameters (`refineryCycleSeconds`: `2.0`, `refineryOrePerBlock`: `2`, `refineryInteractRange`: `5.0`, `refineryBufferCapacity`: `100`) hot-reloaded alongside flight and mining settings. `StationGridNode` owns a live `RefineryState` stepped every physics frame in `GameRoot._PhysicsProcess` via `RefinerySim.Step`, converting `IronOre` from its input buffer into `SteelBlock` items in its output buffer even while the player flies or jets elsewhere. Aiming at the station's `BlockType.Refinery` block in `FloatingInSuit` displays `[F] Open Refinery` above the hotbar when within `refineryInteractRange` (`5.0 m`), dims the reticle with `OUT OF RANGE` between `5.0 m` and `25.0 m`, and shows `NO STEEL BLOCKS — REFINE ORE AT STATION` when hotbar slot `[2]` is selected with `0` `SteelBlock` items in suit cargo.

## Acceptance criteria

- **AC-1** — Given `config/tuning.json` payloads containing Phase 03 refinery fields (`refineryCycleSeconds`, `refineryOrePerBlock`, `refineryInteractRange`, `refineryBufferCapacity`), when `TuningConfigLoader.LoadOrFallback` executes, then valid JSON overrides defaults (requiring finite values `> 0` and integer-convertible `refineryOrePerBlock >= 1` and `refineryBufferCapacity >= 1`) while malformed JSON or invalid values retain the fallback `TuningConfig` with `IsSuccess == false`.
- **AC-2** — Given a `RefineryState` and `TuningConfig`, when `RefinerySim.Step` runs across fractional and multi-cycle `deltaSeconds` ticks, then each `refineryCycleSeconds` interval consumes `refineryOrePerBlock` `IronOre` and adds `1` `SteelBlock` while preserving fractional timer rollover across cycles, and immediately pauses with `CycleProgress == 0` and zero lost `IronOre` whenever input drops below `refineryOrePerBlock` or output reaches `refineryBufferCapacity`—resuming automatically once ore is added or output is withdrawn.
- **AC-3** — Given `RefinerySim.DepositFromSuit`, `RefinerySim.WithdrawToSuit`, and `SuitVisorModel.Compute` across mixed-item (`IronOre` + `SteelBlock`) and capacity-constrained inventories, then depositing moves only `IronOre` up to refinery input space (leaving `SteelBlock` in suit cargo and retaining overflow `IronOre` in suit cargo), withdrawing moves `SteelBlock` up to `SuitInventory.RemainingCapacity` (leaving excess in refinery output), and visor state reports `[F] Open Refinery` within `refineryInteractRange`, `OUT OF RANGE` past range on a `Refinery` cell, and `NO STEEL BLOCKS — REFINE ORE AT STATION` when slot `[2]` has `0` blocks.

## Tests

- `TuningConfigTests.LoadOrFallback_ValidatesRefineryParametersWithSafeFallback` — proves Phase 03 refinery field loading and non-positive/malformed fallback (`AC-1`).
- `RefinerySimTests.Step_ConvertsOreAcrossFractionalAndMultiCycleTicksAndStallsWhenStarvedOrOutputFull` — proves fractional accumulation, multi-cycle rollover, ore conservation on starve/full stall, and clean resume (`AC-2`).
- `RefinerySimTests.DepositAndWithdraw_RespectItemFiltersAndBothInventoryCapacities` — proves `IronOre`-only deposits, `SteelBlock`-only withdrawals, and partial-capacity overflow preservation in both directions (`AC-3`).
- `SuitVisorHudTests.VisorModel_PrioritizesRefineryPromptsAndOutOfRangeStates` — proves `[F] Open Refinery` vs `[F] Board Ship` priority, `Refinery` `OUT OF RANGE` gating, and empty slot `[2]` prompt state (`AC-3`).

## Files

- `config/tuning.json`
- `src/Keel.Sim/Config/TuningConfig.cs`
- `src/Keel.Sim/Config/TuningConfigLoader.cs`
- `src/Keel.Sim/Refinery/RefineryState.cs`
- `src/Keel.Sim/Refinery/RefinerySim.cs`
- `src/Keel.Sim/Hud/SuitVisorModel.cs`
- `src/Keel.Game/World/StationGridNode.cs`
- `src/Keel.Game/UI/SuitVisorHudNode.cs`
- `src/Keel.Game/GameRoot.cs`
- `tests/Keel.Sim.Tests/Config/TuningConfigTests.cs`
- `tests/Keel.Sim.Tests/Refinery/RefinerySimTests.cs`
- `tests/Keel.Sim.Tests/Hud/SuitVisorHudTests.cs`

## Out of scope

- Opening the modal `RefineryTerminalNode` card with `F` and triggering `[E]`/`[R]` transfers from keyboard/UI (`epic-03`)
- Placing `SteelBlock` items onto the station `BlockGrid` (`Phase 04`)

## Notes

- Reuse `Keel.Sim.Inventory.Inventory` (`SuitInventory`) for `RefineryState.InputInventory` and `RefineryState.OutputInventory` so capacity invariants live in one place.
- Step `RefinerySim.Step(Station.Refinery, CurrentTuning, (float)delta)` unconditionally inside `GameRoot._PhysicsProcess` regardless of `PilotMode`, so ore deposited in `epic-03` continues refining if the player boards the ship or flies back toward the asteroid.
- When the player aims directly at `BlockType.Refinery` within `refineryInteractRange`, `[F] Open Refinery` takes priority over `[F] Board Ship` even if the ship is parked within `shipInteractRange` (`6.0 m`), matching the `F`-key priority `epic-03` will execute.

---

## Build note — 2026-11-10

**Result:** green

### Tasks done

- Added Phase 03 refinery tuning parameters (`refineryCycleSeconds`: 2.0, `refineryOrePerBlock`: 2.0, `refineryInteractRange`: 5.0, `refineryBufferCapacity`: 100.0) to `config/tuning.json`, `TuningConfig`, and `TuningConfigLoader`.
- Implemented `RefineryState` and `RefinerySim` in `Keel.Sim.Refinery` supporting fractional progress accumulation, multi-cycle conversion, ore conservation during starved or output-full stalls, and item-filtered deposits/withdrawals.
- Updated `SuitVisorModel` to prioritize `[F] Open Refinery` and `OUT OF RANGE` prompt states when aiming at refinery blocks, and surface `NO STEEL BLOCKS — REFINE ORE AT STATION` when hotbar slot `[2]` is empty.
- Attached `RefineryState` to `StationGridNode` and stepped `RefinerySim.Step` continuously in `GameRoot._PhysicsProcess`.
- Updated `SuitVisorHudNode` and `GameRoot` to stream targeted block type and distance to the visor HUD.

### Tests added

- `tests/Keel.Sim.Tests/Config/TuningConfigTests.cs`
  - `TuningConfigTests.LoadOrFallback_ValidatesRefineryParametersWithSafeFallback` (proves AC-1)
- `tests/Keel.Sim.Tests/Refinery/RefinerySimTests.cs`
  - `RefinerySimTests.Step_ConvertsOreAcrossFractionalAndMultiCycleTicksAndStallsWhenStarvedOrOutputFull` (proves AC-2)
  - `RefinerySimTests.DepositAndWithdraw_RespectItemFiltersAndBothInventoryCapacities` (proves AC-3)
- `tests/Keel.Sim.Tests/Hud/SuitVisorHudTests.cs`
  - `SuitVisorHudTests.VisorModel_PrioritizesRefineryPromptsAndOutOfRangeStates` (proves AC-3)

### Suite result

- Command: `dotnet test Keel.sln`
- Summary: `Passed! - Failed: 0, Passed: 26, Skipped: 0, Total: 26, Duration: 37 ms - Keel.Sim.Tests.dll (net8.0)` (with `Keel.Game` compiled cleanly)

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
| AC-1 | yes | `TuningConfigTests.LoadOrFallback_ValidatesRefineryParametersWithSafeFallback` |
| AC-2 | yes | `RefinerySimTests.Step_ConvertsOreAcrossFractionalAndMultiCycleTicksAndStallsWhenStarvedOrOutputFull` |
| AC-3 | yes | `RefinerySimTests.DepositAndWithdraw_RespectItemFiltersAndBothInventoryCapacities`, `SuitVisorHudTests.VisorModel_PrioritizesRefineryPromptsAndOutOfRangeStates` |

### Drift

None

### Findings

None

### Follow-ups

None


