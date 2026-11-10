# Epic 03 — Asteroid Mining

**Phase:** 02 Zero-G Suit and Mining
**Goal:** Attach discrete metallic `#D97706` `IronOre` deposits to the asteroid and let the player raycast-drill ore into capacity-bounded suit cargo.
**Done criteria covered:** DC-5, DC-6, DC-7

## Behaviour

Metallic rust-gold (`#D97706`) `IronOre` seam colliders sit on the `#27272A` asteroid surface. Floating in the suit with `[1] Drill` active, aiming at a seam within `drillRange` (`5.0 m`) shows `[LMB] Drill Iron Ore` above the hotbar (or dims the reticle with `OUT OF RANGE` past `5.0 m`). Holding `LMB` renders an amber drill beam to the hit point, sweeps an amber extraction progress arc around the center reticle, and transfers discrete `IronOre` units from the deposit into the suit's `Inventory` (`CARGO` filling toward `50 / 50`). Extraction stops cleanly with `CARGO FULL` (`#EF4444`) if suit capacity fills or `DEPLETED` (darkening the seam mesh) when remaining yield hits `0`, and mined `IronOre` stays in suit cargo across boarding and exiting the ship.

## Acceptance criteria

- **AC-1** — Given a capacity-bounded `Inventory`, when `ItemId` quantities are added and removed across empty, partial, and near-capacity states, then counts stay within `[0, Capacity]`, near-full additions accept only the fitting delta while returning the unaccepted remainder, and negative or zero transfer amounts leave state unchanged.
- **AC-2** — Given an `OreDepositState` with positive `RemainingUnits`, a suit `Inventory` with free space, `[1] Drill` selected, and a target hit distance `<= drillRange`, when `DrillSim.Step` advances across fractional and multi-unit `deltaSeconds * drillRate` ticks with fire held, then fractional progress wraps in `[0, 1)` and whole `IronOre` units transfer 1-for-1 from the deposit into `Inventory` until the deposit reaches `0` (`Depleted`).
- **AC-3** — Given `DrillSim.Step` when hit distance exceeds `drillRange`, hotbar slot `[2]`–`[5]` is selected, deposit `RemainingUnits == 0`, or suit `Inventory.IsFull` becomes true mid-extraction, then zero excess units are deducted from the deposit, `Inventory` never exceeds `Capacity`, `DrillStatus` reports the exact state (`OutOfRange`, `Idle`, `Depleted`, `CargoFull`), and suit `Inventory` contents remain intact across `PilotModeStateMachine` `TryBoardShip` and `ExitCockpit` transitions.

## Tests

- `AsteroidMiningTests.Inventory_EnforcesCapacityAndPartialOverflowTransfers` — covers `[0, Capacity]` invariants, partial overflow remainder calculation, and non-positive input guards (`AC-1`).
- `AsteroidMiningTests.DrillSim_ExtractsWholeOreUnitsAcrossFractionalTicksUntilDepleted` — covers fractional progress accumulation, multi-unit tick rollover, and 1-for-1 depletion to zero (`AC-2`).
- `AsteroidMiningTests.DrillSim_HaltsOnOutOfRangeWrongSlotDepletedOrCargoFullAndPersistsAcrossBoarding` — covers range/slot/depletion/`CargoFull` conservation invariants and cargo persistence across cockpit boarding/exit (`AC-3`).

## Files

- `src/Keel.Sim/Inventory/ItemId.cs`
- `src/Keel.Sim/Inventory/SuitInventory.cs`
- `src/Keel.Sim/Mining/OreDepositState.cs`
- `src/Keel.Sim/Mining/DrillSim.cs`
- `src/Keel.Game/World/LandmarkBuilder.cs`
- `src/Keel.Game/World/OreDepositNode.cs`
- `src/Keel.Game/Suit/SuitRigNode.cs`
- `src/Keel.Game/UI/ReticleCanvasControl.cs`
- `src/Keel.Game/UI/SuitVisorHudNode.cs`
- `src/Keel.Game/GameRoot.cs`
- `tests/Keel.Sim.Tests/Mining/AsteroidMiningTests.cs`

## Out of scope

- Depositing `IronOre` into a station refinery or producing `SteelBlock` items (Phase 03)
- Voxel terrain carving on the asteroid mesh (the deposit mesh darkens/shrinks slightly when `Depleted`)

## Notes

- Exclude `SuitRigNode.GetRid()` from `PhysicsRayQueryParameters3D.Exclude` when casting forward from `SuitCamera` (out to e.g. `25m` so aiming at a deposit between `5m` and `25m` can detect `OutOfRange` and dim the reticle per [design.md](../../design.md), while only allowing extraction at `<= drillRange`).
- Place 3–4 `#D97706` metallic ore deposit nodes (`25`–`35` `IronOre` units each, so two deposits can fill the `50`-unit suit cargo and prove both `Depleted` and `CargoFull` in one run) on the station/origin-facing side of the asteroid (`SpaceArenaLayout.AsteroidPosition`) where directional sunlight hits directly.
