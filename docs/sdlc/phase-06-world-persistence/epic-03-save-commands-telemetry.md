# Epic 03 — Save Commands, HUD Feedback & Telemetry

**Phase:** 06 World Persistence (Save & Load)
**Goal:** Add `F5` quicksave and `F9` live quickload commands, clean up active modals/beams on reload, surface persistence feedback on Cockpit/Suit HUDs and `F3` telemetry, and prove full-loop rollback conservation.
**Done criteria covered:** DC-5, DC-6, DC-7

## Behaviour

Pressing `F5` at any point during flight or EVA immediately saves the world to `saves/world.json`, and pressing `F9` reloads `saves/world.json` live without restarting the game—closing any open Refinery Terminal modal, hiding placement ghosts and tool beams, switching cockpit/suit cameras and HUDs when `PilotMode` changes, and rolling back all world state to the saved checkpoint. Save and load actions push `SYS: WORLD SAVED`, `SYS: WORLD LOADED`, or `ERR: SAVE LOAD FAILED` messages to the Cockpit HUD `InfoLog`, briefly display a status prompt on the Suit Visor HUD while in EVA, and update a `SAV` status line in the `F3` debug telemetry overlay.

## Acceptance criteria

- **AC-1** — Given a live gameplay session where the player mines ore, refines blocks, places a station block, records a save snapshot (`F5`), and subsequently mutates ship/suit transforms, inventories, station blocks, refinery buffers, and ore deposits, when the saved snapshot is reloaded (`F9`), then all simulation states (`PilotMode`, ship/suit kinematics, `SuitInventory`, `BlockGrid`, `RefineryState`, and `OreDepositState` reserves) revert identically to the saved checkpoint with zero item or block leaks.
- **AC-2** — Given `ShipInputReader` and `GameRoot`, when `F5` (`ConsumeQuickSaveToggle`) or `F9` (`ConsumeQuickLoadToggle`) is pressed (including while the Refinery Terminal modal is open), then `F5` atomically writes `saves/world.json` and `F9` reloads `saves/world.json`, closing `_refineryTerminal`, restoring mouse capture, resetting `_drillProgress`/`_grinderProgress`, hiding drill/grinder beams and `BlockGhostNode`, and synchronizing camera/HUD visibility with the loaded `PilotMode`.
- **AC-3** — Given `PersistenceSession` in `Keel.Sim.Persistence`, when a save or load succeeds or fails, then a transient notification flash timer activates and decays cleanly over `Tick(deltaSeconds)`, `SuitVisorModel.Compute` surfaces the active save/load notification (`WORLD SAVED`, `WORLD LOADED`, or `SAVE LOAD FAILED`) while the flash is active, `CockpitHudNode.InfoLog` receives the corresponding `SYS:` / `ERR:` entry, and `DebugTelemetryFormatter.Format` renders `SAV  SAVED`, `SAV  LOADED`, `SAV  OK`, `SAV  NONE`, or `SAV  ERR (<reason>)`.

## Tests

- `WorldPersistenceTests.FullLoop_QuickloadRollsBackMutatedWorldStateAndConservesItems` — proves end-to-end mine -> refine -> place -> save -> mutate/deconstruct -> quickload rollback across all world state modules with exact item and block conservation (`AC-1`).
- `PersistenceSessionTests.Session_TracksSaveLoadFlashTimerAndSurfacesErrorsInTelemetry` — proves `PersistenceSession` flash timer decay, error reporting/clearing, and `DebugTelemetryFormatter` `SAV` status formatting (`AC-3`).
- `SuitVisorHudTests.VisorModel_SurfacesTransientSaveLoadNotification` — proves transient Suit Visor save/load feedback tone and expiration back to normal contextual prompts (`AC-3`).

## Files

- `src/Keel.Sim/Persistence/PersistenceSession.cs`
- `src/Keel.Sim/Hud/SuitVisorModel.cs`
- `src/Keel.Sim/Telemetry/DebugTelemetryFormatter.cs`
- `src/Keel.Game/Input/ShipInputReader.cs`
- `src/Keel.Game/UI/SuitVisorHudNode.cs`
- `src/Keel.Game/GameRoot.cs`
- `tests/Keel.Sim.Tests/Persistence/WorldPersistenceTests.cs`
- `tests/Keel.Sim.Tests/Persistence/PersistenceSessionTests.cs`
- `tests/Keel.Sim.Tests/Hud/SuitVisorHudTests.cs`

## Out of scope

- Multi-slot save selection dialogs or confirmation modals
- Rebinding `F5` / `F9` keys via config files

## Notes

- Model `PersistenceSession` directly after `TuningSession` (`FlashDurationSeconds = 2.0f`) so flash timing and error tracking follow the exact same pure-C# pattern in `Keel.Sim`.
- Handle edge-triggered `Key.F5` and `Key.F9` (`Pressed: true, Echo: false`) in `ShipInputReader.HandleInputEvent`, and ensure `GameRoot._Input` processes `ConsumeQuickSaveToggle()` / `ConsumeQuickLoadToggle()` before the `_isRefineryOpen` early return so saving or reloading still works if the Refinery Terminal is open.
