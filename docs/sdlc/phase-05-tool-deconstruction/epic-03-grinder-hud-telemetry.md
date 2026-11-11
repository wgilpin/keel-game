# Epic 03 — Grinder HUD Prompts & Telemetry

**Phase:** 05 Tool Deconstruction & Block Recycling
**Goal:** Update `SuitVisorModel` for hotbar slot `[3] Grinder`, contextual prompts (`[LMB] Deconstruct Block`, `CANNOT DECONSTRUCT CORE`, `CARGO FULL`, `CANNOT SPLIT GRID`, `OUT OF RANGE`), out-of-range reticle dimming, `F3` deconstruct telemetry, and end-to-end recycling conservation invariants.
**Done criteria covered:** DC-5, DC-6, DC-7

## Behaviour

When hotbar slot `[3]` is selected, the Suit Visor HUD displays `Grinder` as equipped. When aiming at a station block, the HUD surfaces contextual prompts: `[LMB] Deconstruct Block` (`#38BDF8`) on valid targets within `grinderRange`, `CANNOT DECONSTRUCT CORE` (`#EF4444`) on station Core blocks, `CARGO FULL` (`#EF4444`) when suit cargo cannot accept refunded blocks, `CANNOT SPLIT GRID` (`#EF4444`) when removal would partition the station, and `OUT OF RANGE` (`#F59E0B` with dimmed center reticle) on station hits beyond `grinderRange`. The `F3` debug telemetry overlay displays active grinder target cell and progress percentage. Placing a block from slot `[2]` and subsequently deconstructing it with slot `[3]` conserves `SteelBlock` inventory counts 1-for-1.

## Acceptance criteria

- **AC-1** — Given `SuitVisorModel.Compute`, when hotbar slot `3` is active, then slot 3 renders `"Grinder"` (count `1`, not dimmed), and when aiming at a deconstructable station block within `grinderRange`, surfaces `[LMB] Deconstruct Block` with `PromptTone.Primary`, while hits beyond `grinderRange` surface `OUT OF RANGE` (`PromptTone.Warning`, `isTargetOutOfRange == true`).
- **AC-2** — Given slot `3` active while aiming at a `BlockType.Core` cell, or with `SuitInventory.IsFull == true`, or at a cell that would disconnect the grid, when `SuitVisorModel.Compute` evaluates, then it surfaces `CANNOT DECONSTRUCT CORE`, `CARGO FULL`, or `CANNOT SPLIT GRID` with `PromptTone.Danger`, taking priority over ship boarding when aiming at the station block.
- **AC-3** — Given a placed `ArmorBlock`, when the player executes a full build-and-deconstruct cycle (place from slot `2`, switch to slot `3`, deconstruct to completion), then the suit's `SteelBlock` inventory count returns 1-for-1 to its pre-placement count, `BlockGrid.IsConnected()` remains true throughout, and `DebugTelemetryFormatter` formats `GRID (x, y, z) -> DECONSTRUCT (progress%)` during active grinding.

## Tests

- `SuitVisorHudTests.VisorModel_SurfacesGrinderSlotAndContextualPrompts` — proves slot 3 mapping to `Grinder`, valid in-range prompt `[LMB] Deconstruct Block`, and `OUT OF RANGE` reticle dimming (`AC-1`).
- `SuitVisorHudTests.VisorModel_HandlesCoreCargoFullAndOutOfRangeForGrinder` — proves `CANNOT DECONSTRUCT CORE`, `CARGO FULL`, and `CANNOT SPLIT GRID` error prompt priorities over ship boarding (`AC-2`).
- `GrinderSimTests.ClosedLoop_PlaceAndDeconstructConservesSteelBlocksAndGridConnectivity` — proves exact 1-for-1 item conservation through placement and deconstruction, connectivity maintenance, and zero-leak state transitions (`AC-3`).
- `DebugTelemetryFormatterTests.Format_SurfacesGrinderProgress` — proves `F3` telemetry output shows grinder target cell, block type, and progress percentage (`AC-3`).

## Files

- `src/Keel.Sim/HUD/SuitVisorModel.cs`
- `src/Keel.Sim/Telemetry/DebugTelemetryFormatter.cs`
- `src/Keel.Game/UI/SuitVisorHudNode.cs`
- `src/Keel.Game/GameRoot.cs`
- `tests/Keel.Sim.Tests/HUD/SuitVisorHudTests.cs`
- `tests/Keel.Sim.Tests/Telemetry/DebugTelemetryFormatterTests.cs`
- `tests/Keel.Sim.Tests/Grid/GrinderSimTests.cs`

## Out of scope

- Multi-slot tool wheel customization or hotbar remapping
- Save/load persistence of partially deconstructed blocks

## Notes

- In `SuitVisorModel.cs`, ensure `activeSlot == 3` prompts take precedence over `[F] Board Ship` when aiming at a station block, mirroring the behavior of slots `1` (drill) and `2` (placement).
- Update `SuitVisorHudNode.cs` so slot label for slot 3 displays `Grinder` cleanly without clipping or unwanted count display.
- Extend `DebugTelemetrySnapshot` with optional `Int3? GrinderTargetCell` and `float? GrinderProgress` fields.

---

## Build note — 2026-11-11

**Result:** green

### Tasks done

- Updated `SuitVisorModel` to label hotbar slot `[3]` as `"Grinder"`, added `grinderStatus` and `grinderRange` parameters, and wired contextual prompts (`[LMB] Deconstruct Block`, `CANNOT DECONSTRUCT CORE`, `CARGO FULL`, `CANNOT SPLIT GRID`, `OUT OF RANGE`).
- Updated `DebugTelemetryFormatter` to format deconstruction telemetry (`-> DECONSTRUCT (progress%)`) in the `F3` overlay snapshot.
- Updated `SuitVisorHudNode` to route grinder status and deconstruction progress into `SuitVisorModel` and the reticle sweep arc.
- Updated `GameRoot` to pass grinder telemetry to `SuitVisorHudNode` and `DebugOverlayNode`.
- Added unit tests in `SuitVisorHudTests`, `DebugTelemetryFormatterTests`, and `GrinderSimTests` covering all acceptance criteria.

### Tests added

- `tests/Keel.Sim.Tests/HUD/SuitVisorHudTests.cs`:
  - `VisorModel_SurfacesGrinderSlotAndContextualPrompts`
  - `VisorModel_HandlesCoreCargoFullAndOutOfRangeForGrinder`
- `tests/Keel.Sim.Tests/Telemetry/DebugTelemetryFormatterTests.cs`:
  - `Format_SurfacesGrinderProgress`
- `tests/Keel.Sim.Tests/Grid/GrinderSimTests.cs`:
  - `ClosedLoop_PlaceAndDeconstructConservesSteelBlocksAndGridConnectivity`

### Suite result

- Command: `dotnet test Keel.sln`
- Output: `Passed!  - Failed:     0, Passed:    49, Skipped:     0, Total:    49, Duration: 103 ms - Keel.Sim.Tests.dll (net8.0)`

### Deviations from the plan

- None.

### Follow-ups

- None.

---

## Review — 2026-11-11

**Verdict:** pass

### Acceptance criteria

| AC | Met | Proved by |
|---|---|---|
| AC-1 | yes | `SuitVisorHudTests.VisorModel_SurfacesGrinderSlotAndContextualPrompts` |
| AC-2 | yes | `SuitVisorHudTests.VisorModel_HandlesCoreCargoFullAndOutOfRangeForGrinder` |
| AC-3 | yes | `GrinderSimTests.ClosedLoop_PlaceAndDeconstructConservesSteelBlocksAndGridConnectivity`, `DebugTelemetryFormatterTests.Format_SurfacesGrinderProgress` |

### Drift

None. Preserves pure C# HUD model evaluation in `Keel.Sim.Hud` and telemetry snapshot formatting in `Keel.Sim.Telemetry`.

### Findings

None.

### Follow-ups

None.


