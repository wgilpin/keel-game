# Epic 03 — Modal Cargo Container Terminal & Full Power-Storage Loop

**Phase:** 07 Station Power & Cargo Storage
**Goal:** Build `CargoTerminalModel` and the modal slate-cyan `CargoTerminalNode` UI with single-keystroke and button transfers, modal input/tether handling, and end-to-end power-storage-persistence loop verification.
**Done criteria covered:** DC-5, DC-6, DC-7

## Behaviour

Pressing `F` in `FloatingInSuit` while aiming at any `BlockType.CargoContainer` within `cargoInteractRange` (`5.0 m`) opens the slate-cyan **Station Cargo Container Terminal** bound to that specific `Int3` container cell. While open, suit thrusters, rotation, tools, and hotbar slot switches are locked and the mouse cursor becomes visible so the player can transfer reserves either by clicking buttons or pressing single keys: `[1]` deposit `IronOre`, `[2]` withdraw `IronOre`, `[3]` deposit `SteelBlock`, `[4]` withdraw `SteelBlock`, `[E]` deposit all suit items, and `[R]` withdraw all container items up to remaining suit capacity. The terminal displays the container's `Int3` coordinates, `0 / 500` storage progress bar, live station power readout (`NOMINAL` / `BROWNOUT` / `UNPOWERED`), and suit cargo summary, and closes cleanly via `F`, `Esc`, drifting past `cargoInteractRange + 1.5 m`, boarding the ship, or pressing `F9` to quickload.

## Acceptance criteria

- **AC-1** — Given `CargoTerminalModel.Compute(cell, containerInventory, suitInventory, powerSnapshot, isOpen)` in `Keel.Sim.Hud`, when evaluated across empty, partial, and full suit and container states, then it computes accurate per-item counts (`IronOre`, `SteelBlock`), container fill ratio, station power status text, and action enablement flags (`CanDepositOre`, `CanWithdrawOre`, `CanDepositBlocks`, `CanWithdrawBlocks`, `CanDepositAll`, `CanWithdrawAll`) that are `true` if and only if source stock `> 0` and destination `RemainingCapacity > 0`.
- **AC-2** — Given `GameRoot` and `CargoTerminalNode` in `Keel.Game`, when `F` is pressed while aiming at a `CargoContainer` cell within `cargoInteractRange`, then `CargoTerminalNode` opens bound to that `Int3` cell with visible mouse cursor, locks suit flight (`FlightInput.Zero` with dampeners preserved) and tool firing, routes keys `1`–`4`, `E`, and `R` to `CargoTransferSim` without changing `_suitVisorHud.CurrentActiveSlot`, and closes cleanly on `F`, `Esc`, drifting beyond `cargoInteractRange + 1.5 m` from that cell's world center, mode change, or `F9` `QuickLoad`.
- **AC-3** — Given a complete headless session across `DrillSim`, `StationCargoStore`, `CargoTransferSim`, `StationPowerSim`, `RefinerySim`, `BlockPlacementSim`, `GrinderSim`, and `WorldSaveSerializer`, when mining multiple `50`-unit suit loads into a `500`-unit `CargoContainer`, refining `SteelBlock` items, placing `SolarPanel` blocks to reach `100%` power efficiency, withdrawing stored ore/blocks via terminal transfers, saving to JSON, mutating world state, and reloading, then every `IronOre` and `SteelBlock` unit, power ratio, grid cell, and container stack is conserved with zero drift.

## Tests

- `CargoTerminalTests.ViewModel_ComputesActionAvailabilityPowerStateAndCellSummary` — proves `CargoTerminalModel` enablement flags, fill clamping, cell header formatting, and power status display across empty/partial/full suit and container states (`AC-1`).
- `CargoTerminalTests.FullLoop_MineStorePowerRefineBuildAndPersistConservesAllItems` — proves the end-to-end Phase 07 loop (multi-trip mining into `500`-unit `CargoContainer`, brownout-to-nominal `SolarPanel` upgrade, bulk transfers, deconstruction guard, and JSON save/load rollback) conserves all items and invariants (`AC-3`).

## Files

- `src/Keel.Sim/Hud/CargoTerminalModel.cs`
- `src/Keel.Game/UI/CargoTerminalNode.cs`
- `src/Keel.Game/GameRoot.cs`
- `tests/Keel.Sim.Tests/Storage/CargoTerminalTests.cs`

## Out of scope

- Drag-and-drop split-stack sliders or cross-container conveyor routing
- Dynamic ship cargo containers (Phase 08)

## Notes

- Match `RefineryTerminalNode` visual tokens from `docs/sdlc/design.md` (`CanvasLayer` `Layer = 20`, `#101A26` card background, `#16222F` row surfaces, `#2B4257` crisp `1px`/`2px` borders, `#38BDF8` primary cyan headers, `#F59E0B` amber brownout badge, `#EF4444` danger unpowered/full badge).
- Ensure `OpenCargoTerminal(Int3 cell)` closes `_isRefineryOpen` if somehow active (and vice versa) and stores `Int3? _openCargoCell` so tether distance is measured against `_stationNode.GlobalPosition + ToGodot(_stationNode.Grid.GetCellCenter(_openCargoCell.Value))` rather than a hardcoded coordinate.
- In `GameRoot._Input`, when `_isCargoOpen` is `true`, consume `_inputReader.ConsumeSlotSelection()` and map `1` -> `Deposit(IronOre)`, `2` -> `Withdraw(IronOre)`, `3` -> `Deposit(SteelBlock)`, `4` -> `Withdraw(SteelBlock)` before `_Process` runs so hotbar selection never changes while operating the terminal.
- In `GameRoot.QuickLoad` and `ApplyWorldSnapshot`, close `CargoTerminalNode` (`CloseCargoTerminal()`) so reloading into `SeatedInCockpit` or onto a grid where that container didn't exist yet never leaves a dangling modal or unlocked cursor.

## Build note

- **Tasks done**:
  - Implemented `CargoTerminalModel.Compute` evaluating container per-item and bulk counts, progress fill ratios, live power snapshots, and action enablement flags (`CanDepositOre`, `CanWithdrawOre`, `CanDepositBlocks`, `CanWithdrawBlocks`, `CanDepositAll`, `CanWithdrawAll`).
  - Implemented `CargoTerminalNode` UI on `CanvasLayer` 20 adhering to design tokens (`#101A26` card, `#16222F` rows, `#2B4257` borders, `#38BDF8` cyan trim) with interactive single-item and bulk transfer buttons and keyboard shortcuts (`[1]`–`[4]`, `[E]`, `[R]`, `[F]`/`[Esc]`).
  - Integrated `CargoTerminalNode` into `GameRoot` with modal mouse unlocking, zero-flight input dampening, tether auto-close at `cargoInteractRange + 1.5 m` from target cell center, slot selection interception, and mutual exclusion with `RefineryTerminalNode`.
  - Added clean modal shutdown on `QuickLoad`, `ApplyWorldSnapshot`, and cockpit boarding.
  - Verified full end-to-end loop in `CargoTerminalTests`: multi-trip mining into `500`-unit cargo container, brownout-to-nominal solar panel upgrade, bulk transfers, grinder protection, and lossless JSON save/load round-trip.
- **Tests added**:
  - `CargoTerminalTests.ViewModel_ComputesActionAvailabilityPowerStateAndCellSummary`
  - `CargoTerminalTests.FullLoop_MineStorePowerRefineBuildAndPersistConservesAllItems`
- **Suite result**: 75 tests passing via `dotnet test Keel.sln` (duration ~93ms).
- **Deviation from plan**: None.

## Review

### Pass 1: Acceptance criteria conformance
- **AC-1**: Verified. `CargoTerminalModel.Compute` evaluates per-item counts, container fill ratios, power telemetry, and action enablement flags (`CanDepositOre`, `CanWithdrawOre`, `CanDepositBlocks`, `CanWithdrawBlocks`, `CanDepositAll`, `CanWithdrawAll`) strictly gated on source stock and destination capacity across empty, partial, and full states. Tested by `CargoTerminalTests.ViewModel_ComputesActionAvailabilityPowerStateAndCellSummary`.
- **AC-2**: Verified. `GameRoot` and `CargoTerminalNode` open bound to target container cell, manage modal cursor state and zero-thrust flight input, route single-keystroke transfers (`[1]`–`[4]`, `[E]`, `[R]`), enforce cell-relative tether breaking (`5.0 m + 1.5 m`), and close cleanly on `F`, `Esc`, boarding, or `QuickLoad`.
- **AC-3**: Verified. Full headless multi-system loop (mining 100 ore into 500-unit container, brownout refining, building 2 solar panels to reach 125 kW nominal power, bulk transfers, grinder protection, and JSON world snapshot rollback) maintains complete item conservation (200 ore total) with zero drift. Tested by `CargoTerminalTests.FullLoop_MineStorePowerRefineBuildAndPersistConservesAllItems`.

### Pass 2: Architecture & drift check
- All mathematical evaluation and transfer logic remains purely in `Keel.Sim.Hud` and `Keel.Sim.Storage` with zero engine dependencies.
- `CargoTerminalNode` adheres to design tokens (`#101A26` card, `#16222F` row surfaces, `#2B4257` borders, `#38BDF8` cyan trim) on `CanvasLayer` 20.
- Clean mutual exclusion between refinery and cargo terminals prevents overlapping modals or stuck input capture.

### Pass 3: Correctness, security & test suite
- All numerical clamping, nullability, and boundary validations are rigorously tested.
- Full test suite: 75 tests pass in 93ms via `dotnet test Keel.sln`.

**Verdict**: pass

