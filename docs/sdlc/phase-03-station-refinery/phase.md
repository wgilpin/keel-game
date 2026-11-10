# Phase 03 — Station Refinery

**Goal:** Seed the prebuilt station on `BlockGrid` and convert mined `IronOre` into `SteelBlock` items through the Refinery Terminal.

## Scope

Replaces the monolithic placeholder station in `LandmarkBuilder` with a sparse `2.5 m` `BlockGrid`-backed station and adds the station refinery loop across `Keel.Sim` and `Keel.Game`. Introduces `Int3`, `BlockType` (`Core`, `ArmorBlock`, `Refinery`), `BlockGrid`, and `StationBlueprint` in `Keel.Sim.Grid`; adds Phase 03 refinery tuning fields in `config/tuning.json`; adds `RefineryState` and `RefinerySim` in `Keel.Sim.Refinery`; extends `PilotModeStateMachine` and `SuitVisorModel` for refinery prompt priority and modal terminal locking; and adds `StationGridNode` (`#64748B` armor cells with `#334155` `2.5 m` seam lines and `#475569` / `#F59E0B` amber-trimmed `Refinery` block), `F3` target cell telemetry, and the slate-cyan **Refinery Terminal Panel** (`RefineryTerminalNode`) in `Keel.Game`.

## Done criteria

1. **DC-1** — `BlockGrid` (`2.5 m` cell pitch) enforces unoccupied-cell and 6-cardinal-face (`±X`, `±Y`, `±Z`) adjacency rules once seeded (rejecting diagonal-only or disconnected cells), and `StationBlueprint.CreateDefault()` seeds a contiguous 6-connected station containing `Core`, `ArmorBlock`, and an exposed `+Z` `Refinery` cell. (Covered by `epic-01-station-block-grid.md`)
2. **DC-2** — Raycast hit resolution (`BlockGrid.ResolveHitCell`) maps station-local hit coordinates and outward face normals across positive, zero, and negative `2.5 m` boundaries to the exact struck occupied `Int3` cell and `BlockType` without boundary-seam flip, and `DebugTelemetryFormatter` surfaces the targeted `Int3` cell in `F3` telemetry. (Covered by `epic-01-station-block-grid.md`)
3. **DC-3** — `config/tuning.json` loads Phase 03 refinery parameters (`refineryCycleSeconds`, `refineryOrePerBlock`, `refineryInteractRange`, `refineryBufferCapacity`) with safe fallback on malformed or non-positive values, and `RefinerySim.Step` converts `refineryOrePerBlock` `IronOre` into `1` `SteelBlock` per `refineryCycleSeconds` across fractional and multi-cycle ticks while stalling without ore loss when input is insufficient or output is full. (Covered by `epic-02-refinery-conversion.md`)
4. **DC-4** — `RefinerySim.DepositFromSuit` transfers only `IronOre` from suit cargo up to refinery input capacity (leaving suit `SteelBlock` items untouched and keeping overflow `IronOre` in suit cargo), `RefinerySim.WithdrawToSuit` transfers `SteelBlock` items up to remaining suit cargo capacity (retaining excess in refinery output), and aiming at a `Refinery` block in `FloatingInSuit` surfaces `[F] Open Refinery` within `refineryInteractRange` or `OUT OF RANGE` beyond it. (Covered by `epic-02-refinery-conversion.md`)
5. **DC-5** — Pressing `F` in `FloatingInSuit` while aiming at `BlockType.Refinery` within `refineryInteractRange` opens the modal Refinery Terminal (taking precedence over `TryBoardShip`), pressing `F` or `Esc` while open closes the terminal cleanly without boarding the ship, and drifting past tether range or switching to `SeatedInCockpit` auto-closes the terminal. (Covered by `epic-03-refinery-terminal.md`)
6. **DC-6** — While the Refinery Terminal is open, suit thrust/rotation and drill firing are locked (`FlightInput.Zero` with suit dampeners preserved) while `[E]` deposits `IronOre`, `[R]` withdraws `SteelBlock` items onto hotbar slot `[2]` (persisting across boarding and exiting the ship), and `RefinerySim.Step` continues advancing whether the terminal is open or closed. (Covered by `epic-03-refinery-terminal.md`)
7. **DC-7** — Launching `Keel.Game` in Godot 4.7 (.NET) renders the station at `SpaceArenaLayout.StationPosition` from `BlockGrid` cells (`#64748B` armor blocks with `#334155` `2.5 m` seam lines and `#475569` refinery block with `#F59E0B` port trim), lets Will jet in the suit to the refinery port (`F3` showing the targeted `Int3` cell), press `F` to open the slate-cyan Refinery Terminal with visible cursor and dimmed visor, press `E` to deposit mined `IronOre`, watch the amber `REFINING` progress bar produce `SteelBlock` items, press `R` to withdraw them onto hotbar slot `[2]`, and press `F` or `Esc` to resume zero-g suit flight. (Covered by `epic-01-station-block-grid.md`, `epic-02-refinery-conversion.md`, `epic-03-refinery-terminal.md`)

## Not in this phase

- 3D `2.5 m` wireframe ghost block preview (cyan valid / red invalid) when aiming with `[2] Steel Block` selected (Phase 04)
- Consuming `SteelBlock` items on `LMB` to place permanent blocks onto the station `BlockGrid` (Phase 04)
- Grinding/removing station blocks, conveyor networks, power grids, or save/load persistence

## Epics

Epics run sequentially (`01` -> `02` -> `03`) because each extends `StationGridNode`, `GameRoot`, and the suit interaction loop on top of the previous slice; none are independent.

1. `epic-01-station-block-grid.md` — Seed the prebuilt station on `BlockGrid` (`2.5 m` cells), render cell geometry/seams and Jolt colliders in `StationGridNode`, and resolve raycast target `Int3` cells in `F3` telemetry (`DC-1`, `DC-2`, `DC-7`).
2. `epic-02-refinery-conversion.md` — Add hot-reloadable refinery tuning, tick-based `RefinerySim` (`IronOre` -> `SteelBlock`) and bounded suit transfers, background station refining, and `[F] Open Refinery` visor prompts (`DC-3`, `DC-4`, `DC-7`).
3. `epic-03-refinery-terminal.md` — Build the modal slate-cyan `RefineryTerminalNode` UI (`[E]` deposit, live amber progress bar, `[R]` withdraw, `[F]`/`[Esc]` close) with thruster/tool modal lock and hotbar slot `[2]` updates (`DC-5`, `DC-6`, `DC-7`).

## Risks

- **Raycast boundary-seam flip on `2.5 m` cube faces:** A Jolt raycast striking a `2.5 m` block face at e.g. `z = +1.25` can land `1e-6` outside the face due to single-precision float rounding and map into the adjacent empty cell. `BlockGrid.ResolveHitCell` must step slightly inward along `-hitNormal` before converting local coordinates to `Int3`.
- **Modal input collisions (`Esc`, `F`, `E`, `LMB`):** `ShipInputReader` currently binds `Esc` to pause mouse-capture toggle, `LMB` (when cursor is `Visible`) to mouse recapture, `E` to roll-right axis, and `F` to `TryBoardShip`. When `IsRefineryOpen` is true, `GameRoot` must intercept `Esc`/`F` to close the terminal, suppress `LMB` recapture and `E`-roll flight input, and keep suit dampeners active via `FlightInput.Zero`.
- **Excessive interior block count on station startup:** Seeding a solid `20 m x 15 m x 50 m` volume (`960` cells) creates needless hidden interior meshes and shapes. `StationBlueprint.CreateDefault()` should seed a sculpted ~50–80 block orbital hub with radiator spars so silhouette readability from `500 m` stays crisp while per-cell mesh/collider setup runs in a couple of milliseconds.
