# Phases — Keel

Phase 01 stands up the `Keel.Sim` and `Keel.Game` stack end-to-end with 6DoF cockpit flight so flight feel can be tuned immediately. Phase 02 adds zero-g suit movement and discrete asteroid mining, completing the grey-box feel gate. Phase 03 introduces the `BlockGrid`-backed station and refinery conversion loop, and Phase 04 closes the first slice with 3D grid-snapped block placement. Phase 05 adds tool deconstruction and block recycling, Phase 06 adds world persistence, Phase 07 introduces power and cargo storage, Phase 08 enables dynamic ship construction and flight, and Phase 09 expands exploration across the 20 km arena.

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

## Phase 05 — Tool Deconstruction & Block Recycling

**Goal:** Add an angle grinder / salvage tool on hotbar slot `[3]` to deconstruct placed blocks and refund `SteelBlock` items into suit cargo.

**Works after this phase:** Select `[3] Grinder` on the suit hotbar, aim at a non-Core station block within `5.0 m`, hold `LMB` to run deconstruction progress with visual cutting beam feedback, and recover `1` `SteelBlock` into suit cargo while removing the block and its Jolt collider from the station grid.

**Not in this phase:** Deconstructing the `Core` block, deconstructing when suit cargo is full, deconstructing blocks that would split the grid into disconnected pieces, dynamic ship construction, and save/load persistence.

**Depends on:** Phase 04

## Phase 06 — World Persistence (Save & Load)

**Goal:** Persist ship transform/velocity, suit inventory, station `BlockGrid`, asteroid ore states, and refinery buffers to clean local JSON so sessions resume seamlessly.

**Works after this phase:** Save current game state on exit or command, restart the game, and resume with the ship, suit cargo, modified station blocks, asteroid ore reserves, and refinery progress preserved.

**Not in this phase:** Multi-slot save managers, cloud sync, or save file version migration.

**Depends on:** Phase 05

## Phase 07 — Station Power & Cargo Storage

**Goal:** Add Solar Panel blocks for passive power generation and Cargo Container blocks for bulk storage with terminal transfer interfaces.

**Works after this phase:** Place Solar Panels to generate station power and Cargo Containers to store large ore/block reserves beyond suit cargo limits, accessible via terminal interaction.

**Not in this phase:** Dynamic ship construction, conveyors, and complex wiring.

**Depends on:** Phase 06

## Phase 08 — Dynamic Ship Construction & Flight

**Goal:** Construct new free-floating ships on dynamic `RigidBody3D` grids with thrusters, gyros, and cockpits, and pilot player-built craft in 6DoF.

**Works after this phase:** Place an anchor block in open space, build a ship hull with functional thrusters, gyro, and cockpit, enter the cockpit, and pilot the custom craft in 6DoF Newtonian flight.

**Not in this phase:** Weapon systems, ship-to-ship docking collars, and pressurized interiors.

**Depends on:** Phase 07

## Phase 09 — Multi-Asteroid Exploration & New Ores

**Goal:** Populate the 20 km arena with a multi-body asteroid cluster featuring diverse ore deposits (Silicon, Cobalt, Nickel) and updated refinery recipes.

**Works after this phase:** Fly across the 20 km space arena to multiple asteroid landmarks, discover and drill distinct ore types, and produce specialized component blocks.

**Not in this phase:** Procedural infinite galaxy, celestial orbital mechanics, or NPC factions.

**Depends on:** Phase 08

