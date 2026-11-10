# Epic 04 — Cockpit HUD

**Phase:** 01 Cockpit Flight Tracer
**Goal:** Render the *Elite*-inspired warm amber 3-panel Ship Cockpit HUD with prograde/retrograde flight pips, landmark contact ranges, square system status boxes, and top-right `INFO` log.
**Done criteria covered:** DC-6, DC-7

## Behaviour

Seated in the cockpit, the player sees the warm holographic amber (`#FF8C1A` over `#120C06`) Cockpit HUD from `design.md`: a fixed center boresight with drifting 6DoF prograde (open circle) and retrograde (crossed circle) velocity markers, a bottom-left Contacts panel showing live distances to `STATION` and `ASTEROID`, a bottom-center `m/s` speed and cap bar, a bottom-right Systems panel with square status indicators (`[■] DAMPENERS [Z]`, `[■] PROXIMITY`), and a 3-line top-right `INFO` feed logging flight events and `tuning.json` reloads.

## Acceptance criteria

- **AC-1** — Given camera basis/FOV, viewport size, and ship linear velocity, when `CockpitReticleProjector.Project` runs, then both markers hide below `0.5 m/s`, project accurately inside the central HUD ring when on-axis above `0.5 m/s`, and clamp to the HUD ring perimeter in the correct direction when off-axis or behind the camera.
- **AC-2** — Given ship position and flight events, when `CockpitContactsModel.Compute` and `HudInfoLog.Push` run, then contacts return sorted nearest-first formatted in `m` (`< 1,000 m`) or one-decimal `km` (`>= 1,000 m`), and `HudInfoLog` retains at most the 3 newest entries in chronological order.

## Tests

- `CockpitHudTests.ReticleProjector_HandlesDeadzoneProjectionAndRearClamping` — covers `< 0.5 m/s` hide threshold, on-axis screen projection, and off-axis/rear ring clamping (`AC-1`).
- `CockpitHudTests.ContactsAndInfoLog_FormatRangesAndCapHistoryAtThree` — covers nearest-first `m`/`km` range formatting and bounded 3-entry ring buffer ordering (`AC-2`).

## Files

- `src/Keel.Sim/Hud/CockpitReticleProjector.cs`
- `src/Keel.Sim/Hud/CockpitContactsModel.cs`
- `src/Keel.Sim/Hud/CockpitSystemsModel.cs`
- `src/Keel.Sim/Hud/HudInfoLog.cs`
- `src/Keel.Sim/Hud/HudThemeTokens.cs`
- `src/Keel.Game/UI/CockpitHudNode.cs`
- `src/Keel.Game/UI/ReticleCanvasControl.cs`
- `src/Keel.Game/GameRoot.cs`
- `tests/Keel.Sim.Tests/Hud/CockpitHudTests.cs`

## Out of scope

- Interactive freelook-focused MFD sub-tabs (the left, center, and right cockpit panels are separate modular `Control` widgets, static in Phase 01)
- Cyan Suit Visor HUD, bearing tape, and 5-slot hotbar (Phase 02)
- Refinery Terminal modal (Phase 03)

## Notes

- Build the Left Contacts panel, Center Speed/Reticle widget, Right Systems panel, and Top-Right `INFO` feed as separate child `Control` classes under `CockpitHudNode` (`CanvasLayer`) using `design.md` tokens (`#FF8C1A` primary, `#120C06` 80% backing, `#663608` border, `2px` radius). Do not write unit tests asserting colour constants or simple boolean checkbox state (`CLAUDE.md`).
- Draw the boresight, HUD clamp ring, open prograde circle, and crossed retrograde circle procedurally in `ReticleCanvasControl._Draw()`.
