# Epic 01 — Solar Panel Construction & Power-Scaled Refining

**Phase:** 07 Station Power & Cargo Storage
**Goal:** Add `[4] Solar Panel` block placement and deconstruction (`2` `SteelBlock` cost/refund), passive station power bus simulation (`StationPowerSim`), power-scaled refining (`RefinerySim`), 3D photovoltaic block visuals, and live power telemetry.
**Done criteria covered:** DC-1, DC-2, DC-7

## Behaviour

Hotbar slot `[4]` becomes `Solar Panel` (displaying how many panels can be built at `2` `SteelBlock` per panel). Aiming at an unobstructed station block face within `buildRange` projects the `2.5 m` placement ghost and clicking `LMB` consumes `2` `SteelBlock` items to snap a photovoltaic `SolarPanel` block onto the station `BlockGrid` (while `[3] Grinder` deconstructs it and refunds all `2` `SteelBlock` items if suit cargo has room). `StationPowerSim` continuously evaluates station generation (`corePowerKw` from `Core` plus `solarPanelPowerKw` per `SolarPanel`) against active `Refinery` power demand (`refineryPowerDemandKw` while refining): on a fresh station with `0` Solar Panels, `Core` emergency output (`25 kW` vs `100 kW` demand) runs refining at `25%` brownout speed without soft-locking, placing `2` Solar Panels (`125 kW`) brings refining to `100%` nominal speed, and `0 kW` generation pauses refining cleanly (`RefineryStatus.NoPower`) with zero ore or progress loss. Live generation, demand, and efficiency appear in both the Refinery Terminal and `F3` telemetry overlay.

## Acceptance criteria

- **AC-1** — Given `config/tuning.json` and `TuningConfigLoader.LoadOrFallback`, when loading Phase 07 fields (`corePowerKw`, `solarPanelPowerKw`, `refineryPowerDemandKw`, `cargoContainerPowerDemandKw`, `cargoContainerCapacity`, `cargoInteractRange`), then valid overrides apply cleanly (allowing `0` for `corePowerKw` and `cargoContainerPowerDemandKw`), missing fields fall back to existing `TuningConfig` values, and negative, non-finite, or sub-minimum values (`solarPanelPowerKw <= 0`, `refineryPowerDemandKw <= 0`, `cargoContainerCapacity < 1`, `cargoInteractRange <= 0`) return `IsSuccess == false` without mutating active tuning.
- **AC-2** — Given hotbar slot `[4]` (`BlockType.SolarPanel`, cost `2` `SteelBlock`), when `SuitInventory` holds fewer than `2` `SteelBlock` items then `BlockPlacementSim.Evaluate` returns `PlacementStatus.NoBlocks` (`ShowGhost == false`) and `SuitVisorModel` dims slot `[4]` with count `0`; when `SuitInventory` holds `>= 2` `SteelBlock` items and a valid station face is targeted within `buildRange`, `BlockPlacementSim.TryPlace` atomically places `BlockType.SolarPanel` and removes `2` `SteelBlock` items; and when `[3] Grinder` targets a `SolarPanel`, `GrinderSim.Step` rejects deconstruction with `GrinderStatus.CargoFull` if `SuitInventory.RemainingCapacity < 2` and otherwise removes the block and refunds `2` `SteelBlock` items on completion.
- **AC-3** — Given `StationPowerSim.Evaluate(grid, refinery, tuning)`, when `RefineryState` is `Idle` or `OutputFull` then refinery power demand is `0 kW`, and when `RefineryState` has sufficient `IronOre` (`>= refineryOrePerBlock`) and free output capacity then each `BlockType.Refinery` on `grid` demands `refineryPowerDemandKw`, producing `PowerStatus.Nominal` (`PowerEfficiency == 1.0`) when `GeneratedKw >= DemandKw`, `PowerStatus.Brownout` (`PowerEfficiency == GeneratedKw / DemandKw`) when `0 < GeneratedKw < DemandKw`, and `PowerStatus.Unpowered` (`PowerEfficiency == 0.0`) when `GeneratedKw == 0`.
- **AC-4** — Given `RefinerySim.Step(state, config, deltaSeconds, powerEfficiency)`, when `0 < powerEfficiency < 1` then `CycleProgress` advances at `powerEfficiency` times nominal speed and converts `IronOre` to `SteelBlock` without loss across multi-tick cycles; when `powerEfficiency == 0` with refineable `IronOre` present, `state.Status` becomes `RefineryStatus.NoPower` while preserving partial `CycleProgress` and `InputInventory` intact until `powerEfficiency > 0` resumes refining; and `RefineryTerminalModel` and `DebugTelemetryFormatter` format live `PWR` generation, demand, and status (`NOMINAL`, `BROWNOUT`, `UNPOWERED`).
- **AC-5** — Given `StationGridNode`, `RefineryTerminalNode`, and `GameRoot` in `Keel.Game`, when placing `BlockType.SolarPanel` on slot `[4]`, then `StationGridNode` spawns a `2.5 m` photovoltaic panel visual and `BoxShape3D` collider, `GameRoot._PhysicsProcess` steps `StationPowerSim` into `RefinerySim.Step`, and the Refinery Terminal displays a live station power status line.

## Tests

- `TuningConfigTests.LoadOrFallback_ValidatesPhase07PowerAndCargoFieldsWithSafeFallback` — proves Phase 07 tuning parsing, `0 kW` auxiliary/standby allowance, and rejection of negative/non-finite/zero capacity or range values (`AC-1`).
- `BlockPlacementTests.MultiSlotPlacement_PlacesSolarAndCargoBlocksAndEnforcesMultiBlockSteelCost` — proves slot `[4]` (`SolarPanel`) `2`-block stock gating, atomic `SteelBlock` deduction, and Refinery `+Z` port protection (`AC-2`).
- `GrinderSimTests.Step_RefundsFullMultiBlockCostAndBlocksWhenRemainingSuitCapacityInsufficient` — proves `GrinderSim` refunds `2` `SteelBlock` items for `SolarPanel` and returns `CargoFull` when only `1` cargo slot is free (`AC-2`).
- `StationPowerTests.Evaluate_ComputesGenerationDemandBrownoutAndUnpoweredEfficiency` — proves `Core` + `SolarPanel` generation, idle vs. active `Refinery` demand, and `Nominal` / `Brownout` / `Unpowered` efficiency ratios (`AC-3`).
- `StationPowerTests.RefineryStep_ScalesProgressWithPowerEfficiencyAndPreservesStateWhenUnpowered` — proves proportional brownout refining duration, zero-loss `NoPower` pause preserving partial `CycleProgress`, and seamless resumption when solar power is added (`AC-4`).
- `DebugTelemetryFormatterTests.Format_RendersStationPowerAndStorageReadouts` — proves `PWR` line formatting across `Nominal`, `Brownout`, and `Unpowered` states (`AC-4`).

## Files

- `config/tuning.json`
- `src/Keel.Sim/Config/TuningConfig.cs`
- `src/Keel.Sim/Config/TuningConfigLoader.cs`
- `src/Keel.Sim/Grid/BlockType.cs`
- `src/Keel.Sim/Grid/BlockPlacementSim.cs`
- `src/Keel.Sim/Grid/GrinderSim.cs`
- `src/Keel.Sim/Power/StationPowerSim.cs`
- `src/Keel.Sim/Refinery/RefineryState.cs`
- `src/Keel.Sim/Refinery/RefinerySim.cs`
- `src/Keel.Sim/Hud/SuitVisorModel.cs`
- `src/Keel.Sim/Hud/RefineryTerminalModel.cs`
- `src/Keel.Sim/Telemetry/DebugTelemetryFormatter.cs`
- `src/Keel.Game/World/StationGridNode.cs`
- `src/Keel.Game/UI/SuitVisorHudNode.cs`
- `src/Keel.Game/UI/RefineryTerminalNode.cs`
- `src/Keel.Game/GameRoot.cs`
- `tests/Keel.Sim.Tests/Config/TuningConfigTests.cs`
- `tests/Keel.Sim.Tests/Grid/BlockPlacementTests.cs`
- `tests/Keel.Sim.Tests/Grid/GrinderSimTests.cs`
- `tests/Keel.Sim.Tests/Power/StationPowerTests.cs`
- `tests/Keel.Sim.Tests/Telemetry/DebugTelemetryFormatterTests.cs`

## Out of scope

- `BlockType.CargoContainer` placement on slot `[5]`, per-cell bulk cargo storage, and cargo persistence (`epic-02`)
- Modal Cargo Container Terminal UI (`epic-03`)

## Notes

- Keep `StationPowerSim` in `Keel.Sim.Power` 100% free of `GodotSharp` ([ADR 0001](../../adr/0001-engine-free-keel-sim-assembly.md)).
- Keep `RefinerySim.Step(RefineryState state, TuningConfig config, float deltaSeconds, float powerEfficiency = 1.0f)` backward-compatible via default `powerEfficiency = 1.0f` so all existing Phase 03–06 unit tests compile and pass unchanged.
- Define deterministic `SteelBlock` build/refund costs by `BlockType` (`ArmorBlock = 1`, `SolarPanel = 2`, `CargoContainer = 2`) in a shared `Keel.Sim.Grid` helper rather than `tuning.json` so hot-reloading config can never create an item duplication mismatch between placement cost and grinder refund.
- In `SuitVisorHudNode.UpdateHud`, render stack counts on all buildable block slots (`slot.Index is 2 or 4 or 5`) rather than only `slot.Index == 2`.

---

## Build note — 2026-11-11

**Result:** green

### Tasks done

- Added Phase 07 tuning fields (`CorePowerKw`, `SolarPanelPowerKw`, `RefineryPowerDemandKw`, `CargoContainerPowerDemandKw`, `CargoContainerCapacity`, `CargoInteractRange`) across `TuningConfig`, `TuningConfigLoader`, and `config/tuning.json`.
- Added `BlockType.SolarPanel` and `BlockType.CargoContainer` to `BlockType.cs` and added `BlockCosts` helper with deterministic `SteelBlock` costs/refunds.
- Updated `BlockPlacementSim` to support multi-slot placement (slots 2, 4, 5) with multi-block cost gating, and updated `GrinderSim` to enforce multi-block suit cargo capacity before deconstruction and refund full `SteelBlock` costs.
- Implemented `StationPowerSim` in `Keel.Sim.Power` calculating station bus generation, active demand, and `PowerEfficiency` (`Nominal`, `Brownout`, `Unpowered`).
- Updated `RefinerySim.Step` to accept `powerEfficiency`, scaling progress under brownout and cleanly pausing with `RefineryStatus.NoPower` at `0 kW` without ore or progress loss.
- Updated `SuitVisorModel` for slot 4/5 buildable stack counts and placement prompts, updated `RefineryTerminalModel` with power status lines, and updated `DebugTelemetryFormatter` to surface `PWR` generation and demand.
- Integrated `BlockType.SolarPanel` photovoltaic rendering in `StationGridNode`, continuous power evaluation in `GameRoot`, power-scaled refining, and live Refinery Terminal and `F3` telemetry readouts.

### Tests added

- `tests/Keel.Sim.Tests/Config/TuningConfigTests.cs`:
  - `LoadOrFallback_ValidatesPhase07PowerAndCargoFieldsWithSafeFallback`
- `tests/Keel.Sim.Tests/Grid/BlockPlacementTests.cs`:
  - `MultiSlotPlacement_PlacesSolarAndCargoBlocksAndEnforcesMultiBlockSteelCost`
- `tests/Keel.Sim.Tests/Grid/GrinderSimTests.cs`:
  - `Step_RefundsFullMultiBlockCostAndBlocksWhenRemainingSuitCapacityInsufficient`
- `tests/Keel.Sim.Tests/Power/StationPowerTests.cs`:
  - `Evaluate_ComputesGenerationDemandBrownoutAndUnpoweredEfficiency`
  - `RefineryStep_ScalesProgressWithPowerEfficiencyAndPreservesStateWhenUnpowered`
- `tests/Keel.Sim.Tests/Telemetry/DebugTelemetryFormatterTests.cs`:
  - `Format_RendersStationPowerAndStorageReadouts`

### Suite result

- Command: `dotnet test Keel.sln`
- Output: `Passed!  - Failed:     0, Passed:    68, Skipped:     0, Total:    68, Duration: 88 ms - Keel.Sim.Tests.dll (net8.0)`

### Deviations from the plan

- None.

### Follow-ups

- None.

---

## Review — 2026-11-11

**Verdict:** pass

### Acceptance criteria

| AC | Met | Proved by |
|---|---|---|
| AC-1 | yes | `TuningConfigTests.LoadOrFallback_ValidatesPhase07PowerAndCargoFieldsWithSafeFallback` |
| AC-2 | yes | `BlockPlacementTests.MultiSlotPlacement_PlacesSolarAndCargoBlocksAndEnforcesMultiBlockSteelCost`, `GrinderSimTests.Step_RefundsFullMultiBlockCostAndBlocksWhenRemainingSuitCapacityInsufficient` |
| AC-3 | yes | `StationPowerTests.Evaluate_ComputesGenerationDemandBrownoutAndUnpoweredEfficiency` |
| AC-4 | yes | `StationPowerTests.RefineryStep_ScalesProgressWithPowerEfficiencyAndPreservesStateWhenUnpowered`, `DebugTelemetryFormatterTests.Format_RendersStationPowerAndStorageReadouts` |
| AC-5 | yes | `StationGridNode`, `RefineryTerminalNode`, `GameRoot` |

### Drift

None.

### Findings

None.

### Follow-ups

None.


