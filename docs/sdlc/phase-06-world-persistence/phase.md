# Phase 06 — World Persistence (Save & Load)

**Goal:** Persist ship transform/velocity, suit inventory, station `BlockGrid`, asteroid ore states, and refinery buffers to clean local JSON so sessions resume seamlessly.

## Scope

Introduces engine-free JSON world persistence in `Keel.Sim.Persistence` and wires session auto-save, startup auto-resume, and on-demand quicksave/quickload across `Keel.Game`. Adds `WorldSaveData`, `WorldSaveSerializer`, `WorldSaveStore`, `WorldStateMapper`, and `PersistenceSession` in `Keel.Sim.Persistence` to capture, validate, and restore `PilotMode`, ship and suit 6DoF kinematics (`Position`, normalized `Orientation`, `LinearVelocity`, `AngularVelocity`, `DampenersEnabled`), active suit hotbar slot, `SuitInventory` stacks, station `BlockGrid` cells, `RefineryState` buffers and active `CycleProgress`, and deterministic `OreDepositState` reserves; writes indented human-readable JSON atomically to `saves/world.json` via temporary file swap (`saves/world.json.tmp` -> `saves/world.json`) and ignores `saves/` in `.gitignore`; extends `StationGridNode` with `ReplaceGrid` to dynamically rebuild visual meshes and Jolt `BoxShape3D` colliders on load; assigns deterministic `DepositId` keys to asteroid seams in `LandmarkBuilder` / `OreDepositNode` and refreshes depleted visuals on load; hooks `GameRoot._Ready` startup auto-resume, `NotificationWMCloseRequest` exit auto-save, and `F5` / `F9` quicksave/quickload commands in `ShipInputReader`; and surfaces persistence status across the Cockpit HUD `InfoLog`, Suit Visor HUD contextual prompt, and `F3` debug telemetry overlay.

## Done criteria

1. **DC-1** — `WorldSaveSerializer` serializes and deserializes `PilotMode`, ship 6DoF kinematics (`Position`, normalized `Orientation`, `LinearVelocity`, `AngularVelocity`, `DampenersEnabled`), suit 6DoF kinematics (`Position`, normalized `Orientation`, `LinearVelocity`, `AngularVelocity`, `DampenersEnabled`, `ActiveHotbarSlot`), and `SuitInventory` item stacks to formatted human-readable JSON, and rejects empty, malformed, or non-finite (`NaN`/`Infinity`) payloads with `IsSuccess == false` and zero unhandled exceptions. (Covered by `epic-01-world-save-tracer.md`)
2. **DC-2** — `WorldSaveStore` writes world snapshots atomically to `saves/world.json` via temporary file replacement (`saves/world.json.tmp` -> `saves/world.json`), automatically saves the live session when the game window receives `NotificationWMCloseRequest`, automatically restores `saves/world.json` on `GameRoot` startup when valid, and falls back cleanly to a fresh default world session when `saves/world.json` is absent or corrupted. (Covered by `epic-01-world-save-tracer.md`)
3. **DC-3** — Saving and loading preserves all station `BlockGrid` modifications (both player-placed `ArmorBlock` cells and deconstructed blueprint cells), validates on load that `BlockType.Core` exists at `(0, 0, 0)` and `BlockGrid.IsConnected()` holds (rejecting disconnected or coreless grids), and dynamically rebuilds `StationGridNode` visual meshes and Jolt `BoxShape3D` colliders to match the loaded grid. (Covered by `epic-02-world-grid-resources.md`)
4. **DC-4** — Saving and loading preserves station `RefineryState` (`InputInventory` ore count, `OutputInventory` steel block count, `CycleProgress` clamped to `[0, 1)`, and `RefineryStatus`) and deterministic asteroid `OreDepositState` reserves by `DepositId` (`RemainingUnits` clamped to `[0, InitialUnits]`), resuming mid-cycle refining without item loss and immediately updating depleted ore seam visuals on load. (Covered by `epic-02-world-grid-resources.md`)
5. **DC-5** — Pressing `F5` in-game triggers an immediate atomic save to `saves/world.json`, and pressing `F9` reloads `saves/world.json` live into the active scene—closing any open Refinery Terminal modal, clearing active drill/grinder beams and placement ghosts, switching camera/HUD active state when `PilotMode` differs, and rolling back ship, suit, inventory, station grid, refinery, and asteroid deposit states to the saved checkpoint. (Covered by `epic-03-save-commands-telemetry.md`)
6. **DC-6** — Save and load operations push status entries (`SYS: WORLD SAVED`, `SYS: WORLD LOADED`, or `ERR: SAVE LOAD FAILED`) to the Cockpit HUD `InfoLog`, surface a timed status notification on the Suit Visor HUD when floating in the suit, and display persistence status (`SAV  SAVED`, `SAV  LOADED`, `SAV  NONE`, or `SAV  ERR (...)`) in the `F3` debug telemetry overlay. (Covered by `epic-03-save-commands-telemetry.md`)
7. **DC-7** — Launching `Keel.Game` in Godot 4.7 (.NET) lets Will fly the ship, exit in the zero-g suit, mine asteroid ore to depletion, start a refinery batch, place and deconstruct station blocks, save via `F5` or window close, restart (or press `F9` after further changes), and verify ship pose/velocity, suit mode/cargo/hotbar slot, modified station geometry and Jolt collision, depleted ore seam visuals, and active refinery progress all resume seamlessly. (Covered by `epic-01-world-save-tracer.md`, `epic-02-world-grid-resources.md`, `epic-03-save-commands-telemetry.md`)

## Not in this phase

- Multi-slot save managers, named save slots, or pause/load menus
- Cloud sync or remote storage
- Backward-compatible save file schema version migrations
- Saving transient held-button drill or grinder partial progress bars
- Station power networks, bulk cargo containers, or dynamic player-built ship grids

## Epics

Epics run sequentially (`01` -> `02` -> `03`) because each extends `WorldSaveData`, `WorldSaveSerializer`, and `GameRoot` snapshot application on top of the previous slice; none are independent.

1. `epic-01-world-save-tracer.md` — Core world JSON snapshot (`PilotMode`, ship & suit 6DoF kinematics, hotbar slot, `SuitInventory`), atomic `saves/world.json` file store, exit auto-save, startup auto-resume, and safe malformed-save fallback (`DC-1`, `DC-2`, `DC-7`).
2. `epic-02-world-grid-resources.md` — Persist and validate modified station `BlockGrid` topology with dynamic `StationGridNode` mesh/collider rebuild, mid-cycle `RefineryState` buffers/progress, and deterministic `OreDepositState` reserves and depleted visuals (`DC-3`, `DC-4`, `DC-7`).
3. `epic-03-save-commands-telemetry.md` — On-demand `F5` quicksave and `F9` live quickload commands, modal/beam cleanup on reload, Cockpit HUD `InfoLog` and Suit Visor notifications, `F3` persistence telemetry, and full-loop rollback conservation (`DC-5`, `DC-6`, `DC-7`).

## Risks

- **Truncated or corrupted `world.json` on abrupt debugger termination:** Writing directly to `saves/world.json` can corrupt the existing save if the process is terminated mid-write (e.g., stopping the VS Code debugger). `WorldSaveStore` writes to `saves/world.json.tmp` first and commits via `File.Move(tempPath, savePath, overwrite: true)`.
- **Invalid global transforms during Godot tree teardown:** Reading `GlobalPosition` and `GlobalBasis` inside `_ExitTree()` can fail or return local coordinates once child nodes begin leaving the scene tree. `GameRoot` hooks `_Notification(int what)` on `NotificationWMCloseRequest` while all scene nodes remain inside the tree.
- **Duplicate Jolt colliders or stale modal locks on live `F9` reload:** Re-populating `StationGridNode` during an active session without freeing prior `CollisionShape3D` nodes would leave invisible ghost colliders on removed cells, and reloading from an open Refinery Terminal into `SeatedInCockpit` would strand mouse capture. `StationGridNode.ReplaceGrid` clears and frees all existing visuals and colliders before rebuilding, and `GameRoot.ApplyWorldSnapshot` closes active modals and clears active tool beams.
