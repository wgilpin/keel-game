# Phase 02 — Zero-G Suit and Mining

**Goal:** Let the player exit the cockpit into a zero-g suit and drill discrete asteroid ore deposits into suit cargo.

## Scope

Extends `Keel.Sim` and `Keel.Game` from cockpit-only flight into EVA zero-g suit play and discrete resource extraction. Adds `PilotModeStateMachine` (`SeatedInCockpit` / `FloatingInSuit`), suit and mining tuning fields in `config/tuning.json`, `VisorBearingTapeModel` and `SuitVisorModel`, `Inventory` (`ItemId.IronOre`), `OreDepositState`, and `DrillSim` in `Keel.Sim`; adds `SuitRigNode` (`RigidBody3D` + first-person tool mesh), exterior `#94A3B8` ship hull geometry, the slate-cyan **Suit Visor HUD** (`SuitVisorHudNode`), and `#D97706` mineable asteroid ore seams with raycast drill extraction in `Keel.Game`.

## Done criteria

1. **DC-1** — Pressing `F` in the cockpit transitions the player into `FloatingInSuit` at a hull-clear rotated exit offset inheriting the ship's linear velocity, and pressing `F` in the suit re-enters `SeatedInCockpit` when within `shipInteractRange` while refusing re-entry beyond `shipInteractRange`. (Covered by `epic-01-suit-eva.md`)
2. **DC-2** — `config/tuning.json` loads suit flight and mining tuning values (`suitMass`, `suitLinearThrust`, `suitAngularTorque`, `suitMaxLinearSpeed`, `shipInteractRange`, `drillRange`, `drillRate`, `suitCargoCapacity`) with safe fallback on malformed/invalid JSON, and suit 6DoF flight obeys normalized thrust, `suitMaxLinearSpeed` clamping, and overshoot-free `Z` inertial dampeners independently of the ship. (Covered by `epic-01-suit-eva.md`)
3. **DC-3** — The Suit Visor bearing tape projects 3D world landmarks (`SHIP`, `STATION`, `ASTEROID`) into signed local horizontal azimuths across arbitrary 6DoF camera orientations, clamping off-span and rear targets to the correct left/right tape edge and wrapping compass degree ticks cleanly across `±180°`. (Covered by `epic-02-suit-visor-hud.md`)
4. **DC-4** — The Suit Visor status and hotbar model switches active slots across `[1]`–`[5]` (`[1] Drill` active by default, `[2] Steel Block` showing count `0` dimmed), clamps cargo fill state to `[0, suitCargoCapacity]` with a `CargoFull` flag at capacity, and surfaces contextual `[F] Board Ship` prompts within `shipInteractRange`. (Covered by `epic-02-suit-visor-hud.md`)
5. **DC-5** — Holding fire (`LMB`) in `FloatingInSuit` with `[1] Drill` active on an ore deposit within `drillRange` accumulates fractional cycle progress and transfers discrete `IronOre` units 1-for-1 from the deposit into suit `Inventory` until the deposit reaches `0` (`Depleted`). (Covered by `epic-03-asteroid-mining.md`)
6. **DC-6** — Drill extraction halts without losing deposit yield or overflowing `Inventory` when aiming past `drillRange`, when slots `[2]`–`[5]` are selected, when the deposit is `Depleted`, or when suit `Inventory` reaches `suitCargoCapacity` (`CargoFull`), and collected `IronOre` persists in suit cargo across boarding and re-exiting the ship. (Covered by `epic-03-asteroid-mining.md`)
7. **DC-7** — Launching `Keel.Game` in Godot 4.7 (.NET) lets Will press `F` to exit the `#94A3B8` ship into the zero-g suit with the cyan Suit Visor HUD (`SHIP`/`STATION` bearing tape, bottom-left status + `CARGO` bar, 5-slot hotbar, right-hand 3D tool mesh, and `F3` mode readout), jet to a metallic `#D97706` ore deposit on the asteroid, hold `LMB` to see the amber extraction arc and drill beam fill `CARGO` with `IronOre`, and press `F` near the ship to return to the amber Cockpit HUD. (Covered by `epic-01-suit-eva.md`, `epic-02-suit-visor-hud.md`, `epic-03-asteroid-mining.md`)

## Not in this phase

- Station `BlockGrid` data model and block-based station rendering (Phase 03)
- Refinery Terminal modal panel and `IronOre` -> `SteelBlock` conversion (Phase 03)
- 3D `2.5m` wireframe placement ghost cube and placing `SteelBlock` items onto the station (Phase 04)
- Walking inside a moving ship interior or voxel asteroid terrain deformation

## Epics

Epics run sequentially (`01` -> `02` -> `03`) because each extends `GameRoot` and the active suit loop on top of the previous slice; none are independent.

1. `epic-01-suit-eva.md` — Exit and re-enter the ship (`F`) and fly a zero-g Jolt `RigidBody3D` suit with independent 6DoF tuning, dampeners, exterior ship hull, and `F3` mode telemetry (`DC-1`, `DC-2`, `DC-7`).
2. `epic-02-suit-visor-hud.md` — Render the slate-cyan Suit Visor HUD with `SHIP`/`STATION` bearing tape, bottom-left speed/dampener/cargo stack, 5-slot hotbar, and boarding prompts (`DC-3`, `DC-4`, `DC-7`).
3. `epic-03-asteroid-mining.md` — Attach discrete `#D97706` `IronOre` deposits to the asteroid and raycast-drill ore into capacity-bounded suit `Inventory` with reticle extraction feedback (`DC-5`, `DC-6`, `DC-7`).

## Risks

- **Suit-ship collision kick on exit:** Spawning the suit collider overlapping `ShipRigNode`'s `3.0m x 1.8m x 4.0m` box collider will cause Jolt penetration impulse; `PilotModeStateMachine.ExitCockpit` must place the suit clear of the ship's bounding box (e.g. `+2.6m` lateral or `+2.2m` dorsal in ship-local space) with matched linear velocity.
- **6DoF bearing tape seam flip at `±180°`:** Standard Euler yaw breaks when pitched vertically or rolled in zero-g; `VisorBearingTapeModel` must project landmark vectors directly into the suit camera's local `(Right, -Forward)` plane and guard zero-length vectors.
- **Raycast self-hit in suit:** Forward physics raycasts from `SuitCamera` will strike `SuitRigNode`'s own collider unless `SuitRigNode.GetRid()` is explicitly excluded in `PhysicsRayQueryParameters3D`.
