# Epic 02 — Suit Visor HUD

**Phase:** 02 Zero-G Suit and Mining
**Goal:** Render the slate-cyan Suit Visor HUD in `FloatingInSuit` with a `SHIP`/`STATION` horizontal bearing tape, bottom-left status and cargo stack, 5-slot hotbar, and contextual boarding prompts.
**Done criteria covered:** DC-3, DC-4, DC-7

## Behaviour

Switching to `FloatingInSuit` hides the warm amber `CockpitHudNode` and activates the cool slate-cyan (`#38BDF8` over `#101A26`) `SuitVisorHudNode` from [design.md](../../design.md). Across the top center, a `480px` **Visor Bearing Tape** plots scrolling yaw ticks and live `SHIP`, `STATION`, and `ASTEROID` diamond markers with `m`/`km` distances that clamp to the left or right tape edge when behind or off-span so landmarks are never lost in zero-g. The bottom-left **Suit Status Stack** shows `m/s` speed, `Z: ON` (cyan) / `Z: OFF` (amber) dampener badge, and `CARGO 0 / 50` capacity bar; the bottom-center **5-Slot Hotbar** highlights `[1] Drill` by default (`1`–`5` keys switch slots, `[2] Steel Block` shows `0` dimmed) beneath contextual prompts (`[F] Board Ship` when in boarding range).

## Acceptance criteria

- **AC-1** — Given suit camera world position, arbitrary 6DoF camera `Right` and `Forward` basis vectors, and landmark world positions (`SHIP`, `STATION`, `ASTEROID`), when `VisorBearingTapeModel.Compute` projects horizontal bearings across the tape's FOV span, then on-span targets map proportionally into `[-1, +1]` un-clamped, off-span and rear targets clamp to `-1` (left) or `+1` (right) without flipping across the `±180°` rear seam, and zero-distance coincident targets return finite values without `NaN`.
- **AC-2** — Given camera heading angles sweeping across `0°` and the `-180°` / `+180°` wraparound seam, when `VisorBearingTapeModel.ComputeTicks` generates compass degree ticks for the visible span, then tick normalized X positions stay strictly ordered inside `[-1, +1]` with wrapped `[0, 360)` compass labels.
- **AC-3** — Given suit cargo count/capacity, hotbar slot key input, and suit-to-ship distance vs `shipInteractRange`, when `SuitVisorModel.Compute` evaluates HUD state, then invalid slot indices leave the active `[1]`–`[5]` slot unchanged, cargo fill ratio clamps to `[0, 1]` flagging `IsCargoFull` at `>= capacity`, and the `[F] Board Ship` prompt activates if and only if distance `<= shipInteractRange`.

## Tests

- `SuitVisorHudTests.BearingTape_ProjectsAzimuthsAndClampsOffSpanAndRearLandmarks` — covers 6DoF horizontal plane projection, FOV normalization, rear/off-span edge clamping, and zero-distance safety (`AC-1`).
- `SuitVisorHudTests.BearingTape_WrapsCompassTicksCleanlyAcross180Seam` — covers degree tick generation and wraparound ordering across `±180°` (`AC-2`).
- `SuitVisorHudTests.VisorModel_ClampsHotbarSelectionCargoFillAndBoardingPrompt` — covers `[1]`–`[5]` slot bounds, `[0, 1]` cargo ratio + `IsCargoFull`, and `shipInteractRange` prompt gating (`AC-3`).

## Files

- `src/Keel.Sim/Hud/VisorBearingTapeModel.cs`
- `src/Keel.Sim/Hud/SuitVisorModel.cs`
- `src/Keel.Game/UI/SuitVisorHudNode.cs`
- `src/Keel.Game/UI/BearingTapeControl.cs`
- `src/Keel.Game/UI/ReticleCanvasControl.cs`
- `src/Keel.Game/Input/ShipInputReader.cs`
- `src/Keel.Game/GameRoot.cs`
- `tests/Keel.Sim.Tests/Hud/SuitVisorHudTests.cs`

## Out of scope

- Raycast asteroid drilling, drill extraction ring progress, and `IronOre` item collection (`epic-03`)
- Refinery Terminal modal (`Phase 03`) and `SteelBlock` 3D ghost placement (`Phase 04`)

## Notes

- Use exact Suit Visor tokens from [design.md](../../design.md): background `#101A26` (82% alpha), surface `#16222F` (92% alpha), border `#2B4257`, text `#E6EDF3`, muted `#8B9EB0`, primary `#38BDF8`, active/warning `#F59E0B`, danger `#EF4444`, `56x56px` hotbar slots, and `480px` top-center bearing tape clear of the top-left `F3` `DebugOverlayNode`. Do not write unit tests asserting colour constants (`CLAUDE.md`).
- Reuse or parameterize `ReticleCanvasControl` so the suit visor shares 6DoF prograde/retrograde drift pips in `#38BDF8` cyan while seated cockpit mode keeps `#FF8C1A` amber.
