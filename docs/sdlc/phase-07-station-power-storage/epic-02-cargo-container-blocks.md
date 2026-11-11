# Epic 02 — Cargo Container Placement, Bulk Storage & Persistence

**Phase:** 07 Station Power & Cargo Storage
**Goal:** Add `[5] Cargo Box` block placement, isolated `500`-unit per-cell `StationCargoStore` and `CargoTransferSim`, non-empty grinder protection, 3D cyan-trimmed container visuals, and JSON world persistence.
**Done criteria covered:** DC-3, DC-4, DC-6, DC-7

## Behaviour

Hotbar slot `[5]` becomes `Cargo Box` (cost `2` `SteelBlock`). Placing a `CargoContainer` onto the station `BlockGrid` creates an isolated per-cell `500`-unit inventory in `StationCargoStore` capable of holding `IronOre` and `SteelBlock` reserves far beyond the `50`-unit suit limit, while adding `cargoContainerPowerDemandKw` (`10 kW`) continuous load to `StationPowerSim`. Aiming at a `CargoContainer` within `cargoInteractRange` (`5.0 m`) displays `[F] Open Cargo Container` on the Suit Visor HUD. Attempting to grind a `CargoContainer` that still holds items is blocked (`GrinderStatus.ContainerNotEmpty` -> `CONTAINER NOT EMPTY`), whereas an empty container deconstructs cleanly, unregisters its cell from `StationCargoStore`, and refunds `2` `SteelBlock` items. Quicksaving (`F5` / exit) and quickloading (`F9` / startup) persists all `SolarPanel` and `CargoContainer` cells alongside per-cell `StationCargoContainers` inventories in `saves/world.json`.

## Acceptance criteria

- **AC-1** — Given hotbar slot `[5]` (`BlockType.CargoContainer`, cost `2` `SteelBlock`), when placed via `BlockPlacementSim.TryPlace` and registered in `StationCargoStore`, then each `CargoContainer` `Int3` cell owns an independent `SuitInventory` of capacity `cargoContainerCapacity` (`500`) without sharing items across cells, and `StationPowerSim.Evaluate` includes `cargoCount * cargoContainerPowerDemandKw` in station power demand whether the `Refinery` is active or idle.
- **AC-2** — Given `CargoTransferSim` operating between `SuitInventory` (`50` capacity) and a `CargoContainer` (`500` capacity), when depositing or withdrawing a specific `ItemId` (`IronOre` or `SteelBlock`) or all items (`DepositAll` / `WithdrawAll`), then transferred quantities clamp strictly to available source stock and remaining destination capacity, leaving non-targeted items untouched and conserving total item counts across both inventories.
- **AC-3** — Given `[3] Grinder` targeting a `BlockType.CargoContainer` cell on the station grid, when the container's inventory in `StationCargoStore` has `TotalCount > 0`, then `GrinderSim.Step` returns `GrinderStatus.ContainerNotEmpty` without advancing progress or removing the block, and `SuitVisorModel` surfaces `"CONTAINER NOT EMPTY"` (`PromptTone.Danger`); once the container is emptied (`TotalCount == 0`), deconstruction completes, removes the cell from both `BlockGrid` and `StationCargoStore`, and refunds `2` `SteelBlock` items.
- **AC-4** — Given `SuitVisorModel.Compute` in `FloatingInSuit`, when aiming at `BlockType.CargoContainer` within `cargoInteractRange`, then `CanOpenCargo` is `true` and `PromptText` is `"[F] Open Cargo Container"` (`PromptTone.Primary`, taking priority over tool/placement prompts), and aiming between `cargoInteractRange` and `25.0 m` surfaces `"OUT OF RANGE"` (`IsTargetOutOfRange == true`).
- **AC-5** — Given a world with placed `SolarPanel` and `CargoContainer` blocks containing stored `IronOre` and `SteelBlock` stacks, when serialized and deserialized via `WorldSaveSerializer` and applied via `WorldStateMapper`, then all block types and per-cell container inventories round-trip losslessly (while Phase 06 saves omitting `stationCargoContainers` still load cleanly), and payloads with negative quantities, overflowing container totals, duplicate container cells, or `CargoContainerSaveData` cells not matching `BlockType.CargoContainer` in `StationBlocks` fail validation safely with `IsSuccess == false`.

## Tests

- `StationCargoTests.CargoStore_IsolatesPerCellContainersAndContributesStandbyPowerDemand` — proves per-cell `500`-unit container isolation, lifecycle sync with placed/removed cells, and `cargoContainerPowerDemandKw` bus load in `StationPowerSim` (`AC-1`).
- `StationCargoTests.Transfers_DepositAndWithdrawIronOreSteelBlocksAndAllItemsWithinBounds` — proves per-item and bulk deposit/withdraw clamping and total item conservation between `50`-unit suit and `500`-unit container (`AC-2`).
- `GrinderSimTests.Step_BlocksNonEmptyCargoContainerAndAllowsDeconstructionOnceEmptied` — proves `ContainerNotEmpty` grinder protection and clean removal + `2`-block refund once emptied (`AC-3`).
- `SuitVisorHudTests.VisorModel_PrioritizesCargoPromptHotbarCountsAndContainerGrinderWarnings` — proves `[F] Open Cargo Container`, `OUT OF RANGE`, slot `[4]`/`[5]` buildable counts, and `CONTAINER NOT EMPTY` prompts (`AC-3`, `AC-4`).
- `WorldPersistenceTests.RoundTrip_PreservesSolarAndCargoContainersAndRejectsInvalidOrOrphanedCargo` — proves JSON round-trip of `SolarPanel`, `CargoContainer`, and per-cell inventories plus rejection of overflowing, duplicate, or orphaned cargo entries (`AC-5`).

## Files

- `src/Keel.Sim/Grid/BlockPlacementSim.cs`
- `src/Keel.Sim/Grid/GrinderSim.cs`
- `src/Keel.Sim/Power/StationPowerSim.cs`
- `src/Keel.Sim/Storage/StationCargoStore.cs`
- `src/Keel.Sim/Storage/CargoTransferSim.cs`
- `src/Keel.Sim/Hud/SuitVisorModel.cs`
- `src/Keel.Sim/Persistence/WorldSaveData.cs`
- `src/Keel.Sim/Persistence/WorldSaveSerializer.cs`
- `src/Keel.Sim/Persistence/WorldStateMapper.cs`
- `src/Keel.Sim/Telemetry/DebugTelemetryFormatter.cs`
- `src/Keel.Game/World/StationGridNode.cs`
- `src/Keel.Game/UI/SuitVisorHudNode.cs`
- `src/Keel.Game/GameRoot.cs`
- `tests/Keel.Sim.Tests/Grid/BlockPlacementTests.cs`
- `tests/Keel.Sim.Tests/Grid/GrinderSimTests.cs`
- `tests/Keel.Sim.Tests/Storage/StationCargoTests.cs`
- `tests/Keel.Sim.Tests/Hud/SuitVisorHudTests.cs`
- `tests/Keel.Sim.Tests/Persistence/WorldPersistenceTests.cs`

## Out of scope

- Interactive modal `CargoTerminalNode` UI and single-keystroke terminal session (`epic-03`)

## Notes

- Host `StationCargoStore` on `StationGridNode` alongside `BlockGrid` and `RefineryState` so `StationGridNode.ReplaceGrid` and block placement/removal hooks keep `StationCargoStore` synchronized with `BlockType.CargoContainer` cells.
- Style `BlockType.CargoContainer` in `StationGridNode.AddBlockVisualAndCollider` using slate `#475569` / `#334155` framing with recessed `#38BDF8` cyan hatch trim on exposed faces so Cargo Containers are visually distinct from `#F59E0B` amber-trimmed Refinery blocks from any approach vector.
- Add `STG  <used> / <capacity> (<count> BOX)` to `DebugTelemetryFormatter` alongside `PWR` so container state is observable in `F3` telemetry even before `CargoTerminalNode` lands in `epic-03`.
