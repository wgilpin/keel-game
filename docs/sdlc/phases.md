# Phases — Keel

Phase 01 stands up the `Keel.Sim` and `Keel.Game` stack end-to-end with 6DoF cockpit flight so flight feel can be tuned immediately. Phase 02 adds zero-g suit movement and discrete asteroid mining, completing the grey-box feel gate. Phase 03 introduces the `BlockGrid`-backed station and refinery conversion loop, and Phase 04 closes the first slice with 3D grid-snapped block placement.

## Phase 01 — Cockpit Flight Tracer

**Goal:** Prove the `Keel.Sim` and `Keel.Game` stack end-to-end with a playable Newtonian 6DoF ship, hot-reloaded tuning config, amber Cockpit HUD, and `F3` telemetry overlay.

**Works after this phase:** Launch into a 20 km grey-box space scene with space-dust drift cues, fly a Jolt `RigidBody3D` ship around landmark station and asteroid meshes, toggle inertial dampeners (`Z`), and tune feel live via `config/tuning.json`.

**Not in this phase:** Exiting the cockpit, zero-g suit movement, Suit Visor HUD, mining, inventories, refinery, and `BlockGrid`.

**Depends on:** nothing

## Phase 02 — Zero-G Suit and Mining

**Goal:** Let the player exit the cockpit into a zero-g suit and drill discrete asteroid ore deposits into suit cargo.

**Works after this phase:** Press `F` to leave and re-enter the ship, fly in zero-g with the cyan Suit Visor HUD (Ship/Station bearing tape, status stack, 5-slot hotbar), and hold `LMB` with `[1] Drill` on an ore seam to fill suit cargo with `IronOre`.

**Not in this phase:** Station `BlockGrid`, refinery terminal, `SteelBlock` items, and placing blocks.

**Depends on:** Phase 01

## Phase 03 — Station Refinery

**Goal:** Seed the prebuilt station on `BlockGrid` and convert mined `IronOre` into `SteelBlock` items through the Refinery Terminal.

**Works after this phase:** Jet to the station's amber-trimmed refinery block (rendered from `BlockGrid`), press `F` to open the Refinery Terminal, deposit `IronOre` (`E`), watch tick-based refining run in `Keel.Sim`, and withdraw `SteelBlock` items (`R`) onto hotbar slot `[2]`.

**Not in this phase:** 3D block placement preview and placing blocks onto the station grid.

**Depends on:** Phase 02

## Phase 04 — Station Grid Construction

**Goal:** Snap and place refined `SteelBlock` items onto adjacent faces of the station `BlockGrid` to close the gameplay loop.

**Works after this phase:** Select `[2] Steel Block` on the suit hotbar, aim at a station face to preview a cyan (valid) or red (invalid) `2.5m` wireframe ghost cube, and click `LMB` to consume one `SteelBlock` and add permanent station geometry and collision.

**Not in this phase:** Removing/grinding blocks, dynamic ship grids, conveyors, power, save/load, and worlds beyond 20 km.

**Depends on:** Phase 03
