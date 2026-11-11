# Phase 05 — Tool Deconstruction & Block Recycling

**Goal:** Add an angle grinder / salvage tool on hotbar slot `[3]` to deconstruct placed blocks and refund `SteelBlock` items into suit cargo.

## Scope

Extends the station grid construction loop across `Keel.Sim` and `Keel.Game` by allowing the player in `FloatingInSuit` mode to deconstruct placed station blocks and reclaim materials. Adds hot-reloadable `grinderRange` (`5.0 m` default) and `grinderRate` (`1.0` progress/s default) tuning parameters in `config/tuning.json`; introduces `GrinderSim` in `Keel.Sim.Grid` to track held-`LMB` deconstruction progress, enforce range and line-of-sight checks, protect station `BlockType.Core` from removal, guard against cargo overflow (`CargoFull`), verify grid connectivity preservation (`WouldSplitGrid`), and atomically execute `BlockGrid.RemoveBlock` paired with `SuitInventory.Add(ItemId.SteelBlock, 1)`; updates `StationGridNode` in `Keel.Game.World` with dynamic visual and Jolt `BoxShape3D` collider removal (`RemoveBlockVisualAndCollider`) to immediately clear raycasts, placement previews, and suit collision; introduces a visible cutting beam/sparks line on `SuitRigNode` during active deconstruction; updates `SuitVisorModel` and `SuitVisorHudNode` to map hotbar slot `[3]` to `Grinder` with contextual prompts (`[LMB] Deconstruct Block`, `CANNOT DECONSTRUCT CORE`, `CARGO FULL`, `CANNOT SPLIT GRID`, `OUT OF RANGE`) and out-of-range reticle dimming; and extends `DebugTelemetryFormatter` to surface target block cell, block type, and deconstruction progress percentage in the `F3` overlay.

## Done criteria

1. **DC-1** — `config/tuning.json` loads `grinderRange` (`5.0 m` default) and `grinderRate` (`1.0` progress/s default) with hot-reload support and falls back safely to the previous `TuningConfig` on malformed JSON or non-positive/non-finite values, and aiming in `FloatingInSuit` with `[3] Grinder` active resolves station face hits within `grinderRange` to the target `Int3` cell and `BlockType`, while hits beyond `grinderRange` or on unoccupied space return `OutOfRange` or `NoTarget`. (Covered by `epic-01-grinder-sim.md`)
2. **DC-2** — Holding `LMB` with slot `[3] Grinder` active while aiming at an `ArmorBlock` within `grinderRange` accumulates deconstruction progress at `grinderRate` per second; reaching `1.0` progress atomically removes the block from `BlockGrid`, awards `1` `SteelBlock` to `SuitInventory`, and resets progress to `0.0`, while releasing `LMB` or switching targeted cells before completion halts or resets progress without removing the block or awarding items. (Covered by `epic-01-grinder-sim.md`)
3. **DC-3** — Deconstruction validation rejects block removal with zero state mutation whenever the targeted block is `BlockType.Core` (`CannotDeconstructCore`), `SuitInventory` is full (`CargoFull`), or removing the block would partition the remaining occupied blocks into disconnected components (`WouldSplitGrid`), leaving both `BlockGrid` and `SuitInventory` unchanged. (Covered by `epic-01-grinder-sim.md`)
4. **DC-4** — In `Keel.Game`, deconstructing an `ArmorBlock` invokes `StationGridNode.RemoveBlockVisualAndCollider`, which removes the visual mesh and the Jolt `BoxShape3D` collider from the scene tree and frees them, immediately updating station raycast hit detection, suit collision clearance, and placement ghost validity. (Covered by `epic-02-station-block-removal.md`)
5. **DC-5** — Suit Visor HUD slot `[3]` displays `Grinder` (active when selected, dimmed never), displays a cutting beam line on `SuitRigNode` from the tool muzzle to the struck point while `LMB` is held, and surfaces contextual prompts: `[LMB] Deconstruct Block` (`#38BDF8`) on valid targets, `CANNOT DECONSTRUCT CORE` (`#EF4444`) on Core blocks, `CARGO FULL` (`#EF4444`) when cargo is full, `CANNOT SPLIT GRID` (`#EF4444`) when removal would split the station, and `OUT OF RANGE` (`#F59E0B` with dimmed center reticle) on hits beyond `grinderRange`. (Covered by `epic-03-grinder-hud-telemetry.md`)
6. **DC-6** — Executing a complete build-and-recycle loop (place a `SteelBlock` from hotbar slot `[2]`, switch to slot `[3] Grinder`, and deconstruct the placed block) conserves suit `SteelBlock` count 1-for-1, maintains `BlockGrid.IsConnected()`, surfaces active grinder cell and progress in `F3` telemetry, and preserves input isolation so ship boarding (`[F]`) and tool hotbar selection (`[1]`–`[5]`) operate without cross-talk. (Covered by `epic-03-grinder-hud-telemetry.md`)
7. **DC-7** — Launching `Keel.Game` in Godot 4.7 (.NET) lets Will fly in the zero-g suit to the station, select hotbar slot `[3] Grinder`, aim at a placed `ArmorBlock` within `5.0 m`, hold `LMB` to view the cutting beam and deconstruction prompt, confirm the block and its Jolt collider vanish upon completion while suit cargo receives `1` `SteelBlock`, and verify aiming at the station `Core` block displays `CANNOT DECONSTRUCT CORE` with zero block removal. (Covered by `epic-01-grinder-sim.md`, `epic-02-station-block-removal.md`, `epic-03-grinder-hud-telemetry.md`)

## Not in this phase

- Deconstructing `BlockType.Core` or `BlockType.Refinery` station systems
- Splitting stations into dynamic floating multi-grid debris
- Partial component salvage or scrap metal byproduct items
- Ship block deconstruction or dynamic ship grids
- Power networks, conveyors, save/load persistence, and coordinates beyond 20 km

## Epics

Epics run sequentially (`01` -> `02` -> `03`) because each builds directly on top of the simulation logic and visual nodes established by the previous slice; none are independent.

1. `epic-01-grinder-sim.md` — Core grinder simulation: `grinderRange`/`grinderRate` tuning, progress accumulation, atomic `BlockGrid` removal with `SuitInventory` refund, Core protection, cargo capacity check, and grid connectivity guard (`DC-1`, `DC-2`, `DC-3`, `DC-7`).
2. `epic-02-station-block-removal.md` — Dynamic visual and Jolt collider removal on `StationGridNode`, held-`LMB` physics frame ticking in `GameRoot`, and tool cutting beam rendering on `SuitRigNode` (`DC-4`, `DC-7`).
3. `epic-03-grinder-hud-telemetry.md` — Hotbar slot `[3] Grinder` visor activation, contextual prompts (`[LMB] Deconstruct Block`, `CANNOT DECONSTRUCT CORE`, `CARGO FULL`, `CANNOT SPLIT GRID`, `OUT OF RANGE`), out-of-range reticle dimming, `F3` telemetry, and closed-loop recycling conservation invariants (`DC-5`, `DC-6`, `DC-7`).

## Risks

- **Orphaning disconnected grid segments in space:** If the player deconstructs an interior structural block that connects two wings of the station, the disconnected wing would remain part of the same `StaticBody3D` without physical support or separate dynamic physics. `GrinderSim` evaluates `BlockGrid.IsConnected()` prior to committing removal, rejecting deconstruction with `WouldSplitGrid` (`CANNOT SPLIT GRID`) if removal would partition the station into multiple disjoint components.
- **Cargo overflow item loss on deconstruction:** If a player with a full suit inventory (50/50) deconstructs a block, failing to check capacity before block removal would either delete the refunded `SteelBlock` into the void or corrupt inventory constraints. `GrinderSim` enforces `inventory.CanAccept(ItemId.SteelBlock, 1)` before committing block removal, reporting `CargoFull` and halting deconstruction until the player frees suit cargo space.
- **Accidental Core station destruction:** The station `BlockType.Core` at `(0, 0, 0)` is the anchor for coordinates and station identity. Deconstructing the core block would leave the station anchorless. `GrinderSim` explicitly checks `targetType == BlockType.Core` and prevents deconstruction, surfacing `CANNOT DECONSTRUCT CORE`.
