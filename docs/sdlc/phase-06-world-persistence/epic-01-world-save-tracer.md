# Epic 01 — World Save Tracer & Startup Resume

**Phase:** 06 World Persistence (Save & Load)
**Goal:** Persist `PilotMode`, ship and suit 6DoF kinematics, active hotbar slot, and `SuitInventory` atomically to `saves/world.json` on window close and auto-resume on launch with safe malformed-file fallback.
**Done criteria covered:** DC-1, DC-2, DC-7

## Behaviour

When the player closes the game window, `GameRoot` captures a snapshot of the active `PilotMode` (`SeatedInCockpit` or `FloatingInSuit`), ship 6DoF transform/velocity/dampeners, suit 6DoF transform/velocity/dampeners/active hotbar slot, and `SuitInventory` item counts, and writes clean indented JSON atomically to `saves/world.json`. On the next launch, `GameRoot` automatically loads `saves/world.json` and restores the player into the exact cockpit or EVA suit state, pose, velocity, dampener setting, hotbar slot, and cargo inventory. If `saves/world.json` does not exist or contains malformed JSON or non-finite values, the loader rejects it cleanly and starts a fresh default session without crashing.

## Acceptance criteria

- **AC-1** — Given a `WorldSaveData` snapshot in either `SeatedInCockpit` or `FloatingInSuit` mode with non-zero ship/suit positions, orientations, linear/angular velocities, dampener flags, active hotbar slot, and `SuitInventory` items (`IronOre`, `SteelBlock`), when serialized via `WorldSaveSerializer.Serialize` and parsed via `WorldSaveSerializer.Deserialize`, then `IsSuccess` is `true`, orientations are normalized unit quaternions, hotbar slots clamp to `[1, 5]`, and restored `PilotModeStateMachine` and `SuitInventory` match the saved values.
- **AC-2** — Given empty, malformed, or schema-invalid JSON, or payloads containing `NaN`/`Infinity` floats, degenerate zero quaternions, negative item counts, or total item counts exceeding `SuitInventory.Capacity`, when `WorldSaveSerializer.Deserialize` executes, then it returns `IsSuccess == false` with a non-empty `ErrorMessage` (or safely normalizes degenerate quaternions to `Quaternion.Identity` where specified) without throwing unhandled exceptions.
- **AC-3** — Given `WorldSaveStore.SaveToFile(savePath, data)`, when saving to a path whose parent directory may not yet exist, then it creates the directory, writes formatted JSON to a sibling `.tmp` file, and atomically replaces `savePath` leaving no `.tmp` artifact behind; and when `LoadFromFile` reads a missing or corrupted file, it returns `IsSuccess == false` without throwing.
- **AC-4** — Given `GameRoot` in `Keel.Game`, when the window receives `NotificationWMCloseRequest`, then `GameRoot` saves the current world state to `saves/world.json` while scene nodes are still inside the tree, and when `GameRoot._Ready` runs with a valid `saves/world.json` present, it restores ship and suit `RigidBody3D` transforms/velocities, camera/HUD visibility for the saved `PilotMode`, active hotbar slot, and `SuitInventory`.

## Tests

- `WorldPersistenceTests.RoundTrip_PreservesShipSuitPilotModeAndInventoryInvariants` — proves JSON round-trip of `PilotMode`, 6DoF ship/suit kinematics, quaternion normalization, hotbar slot clamping, and `SuitInventory` restoration (`AC-1`).
- `WorldPersistenceTests.Deserialize_RejectsMalformedJsonAndNonFiniteKinematicsSafely` — proves safe failure recovery on empty/corrupt JSON, non-finite kinematics, and negative/overflowing inventory payloads (`AC-2`).
- `WorldPersistenceTests.FileStore_WritesAtomicallyAndHandlesMissingOrCorruptSaveFiles` — proves directory creation, atomic `.tmp` replacement on overwrite, and safe missing/corrupt file results (`AC-3`).

## Files

- `.gitignore`
- `src/Keel.Sim/Persistence/WorldSaveData.cs`
- `src/Keel.Sim/Persistence/WorldSaveSerializer.cs`
- `src/Keel.Sim/Persistence/WorldSaveStore.cs`
- `src/Keel.Sim/Persistence/WorldStateMapper.cs`
- `src/Keel.Game/UI/SuitVisorHudNode.cs`
- `src/Keel.Game/GameRoot.cs`
- `tests/Keel.Sim.Tests/Persistence/WorldPersistenceTests.cs`

## Out of scope

- Station `BlockGrid`, `RefineryState`, and asteroid `OreDepositState` persistence (`epic-02`)
- `F5` / `F9` in-game hotkeys and HUD / `F3` persistence status readouts (`epic-03`)

## Notes

- Keep `Keel.Sim.Persistence` 100% free of `GodotSharp` references ([ADR 0001](../../adr/0001-engine-free-keel-sim-assembly.md)); use `System.Numerics.Vector3`, `System.Numerics.Quaternion`, and `System.Text.Json` with `WriteIndented = true` and `JsonStringEnumConverter` so `saves/world.json` is clean and human-readable.
- Add `saves/` to `.gitignore` so local playtest saves never dirty the git working tree.
- Resolve `saves/world.json` relative to the repository root when `config/tuning.json` exists alongside it (mirroring `GameRoot.ResolveConfigPath`), falling back to `ProjectSettings.GlobalizePath("user://saves/world.json")` in standalone exported builds.
- Hook `_Notification(int what)` for `NotificationWMCloseRequest` (guarded by `IsInsideTree()` and a single-save guard) rather than `_ExitTree()`, ensuring child `RigidBody3D` global transforms are still valid when read.

---

## Build note — 2026-11-11

**Result:** green

### Tasks done

- Added `saves/` entry to `.gitignore` to prevent session saves from dirtying version control.
- Implemented `KinematicsSaveData`, `SuitSaveData`, `InventoryItemSaveData`, and `WorldSaveData` in `Keel.Sim.Persistence`.
- Implemented `WorldSaveSerializer` with camelCase indented formatting, finite-float validation, degenerate quaternion normalization to `Quaternion.Identity`, hotbar slot clamping to `[1, 5]`, and cargo inventory capacity bounds checking.
- Implemented atomic file persistence in `WorldSaveStore` writing to `.tmp` before renaming, with automatic directory creation and safe missing/corrupt load error handling.
- Implemented `WorldStateMapper` domain model mapping between `WorldSaveData`, `PilotModeStateMachine`, and `SuitInventory`.
- Added `SetActiveSlot(int slot)` to `SuitVisorHudNode` to restore active hotbar slots on load.
- Wired startup auto-load (`LoadWorldStateOnStartup`) and exit auto-save (`NotificationWMCloseRequest`) in `GameRoot`.
- Added unit tests in `WorldPersistenceTests` covering JSON serialization round-trips, malformed/non-finite recovery, and atomic file store operations.

### Tests added

- `tests/Keel.Sim.Tests/Persistence/WorldPersistenceTests.cs`:
  - `RoundTrip_PreservesShipSuitPilotModeAndInventoryInvariants`
  - `Deserialize_RejectsMalformedJsonSafely`
  - `Deserialize_RejectsMalformedJsonAndNonFiniteKinematicsSafely`
  - `FileStore_WritesAtomicallyAndHandlesMissingOrCorruptSaveFiles`

### Suite result

- Command: `dotnet test Keel.sln`
- Output: `Passed!  - Failed:     0, Passed:    56, Skipped:     0, Total:    56, Duration: 134 ms - Keel.Sim.Tests.dll (net8.0)`

### Deviations from the plan

- Configured `IncludeFields = true` and `NumberHandling = AllowNamedFloatingPointLiterals` on `JsonSerializerOptions` in `WorldSaveSerializer` to ensure `System.Numerics.Vector3` / `Quaternion` fields serialize and deserialize correctly.

### Follow-ups

- None.

---

## Review — 2026-11-11

**Verdict:** pass

### Acceptance criteria

| AC | Met | Proved by |
|---|---|---|
| AC-1 | yes | `WorldPersistenceTests.RoundTrip_PreservesShipSuitPilotModeAndInventoryInvariants` |
| AC-2 | yes | `WorldPersistenceTests.Deserialize_RejectsMalformedJsonSafely`, `WorldPersistenceTests.Deserialize_RejectsMalformedJsonAndNonFiniteKinematicsSafely` |
| AC-3 | yes | `WorldPersistenceTests.FileStore_WritesAtomicallyAndHandlesMissingOrCorruptSaveFiles` |
| AC-4 | yes | `GameRoot.SaveWorldState`, `GameRoot.LoadWorldStateOnStartup` |

### Drift

None. Upholds ADR 0001 with simulation persistence in `Keel.Sim.Persistence` completely free of engine references.

### Findings

None.

### Follow-ups

None.


