# Design — Keel

**Look:** Industrial orbital telemetry

## Surfaces

- **Ship Cockpit HUD (`SeatedInCockpit`):** Framed by dark 3D canopy struts and an *Elite Dangerous*-style warm amber three-panel lower dashboard (Left: station/asteroid contacts & distance; Center: boresight, drifting 6DoF prograde/retrograde pips, and speed bar; Right: square system status checkboxes and top-right `INFO` log). Built as three separate console widgets so later phases can let the pilot focus/expand individual panels.
- **Suit Visor HUD (`FloatingInSuit`):** Full-border cool cyan visor combining *Elite Odyssey*, *Space Engineers*, and *7 Days to Die*—subtle helmet border frame, top-center horizontal bearing tape marking `SHIP` and `STATION`, bottom-left stacked status panel (speed, dampeners, cargo bar), bottom-center numbered 5-slot toolbar, and a simple right-hand 3D grey-box tool mesh.
- **Refinery Terminal Panel:** Compact industrial transfer panel opened with `F` when aiming at the station refinery port; shows `IronOre` input, active refining progress bar, `SteelBlock` output, and single-key transfer controls (`[E]`, `[R]`, `[Esc]`).
- **Debug Telemetry Overlay:** Top-left developer readout (on by default, toggled via `F3`) displaying raw world `XYZ`, linear/angular velocity vectors, state machine mode, target `Int3` grid cell, and `tuning.json` live-reload indicator, positioned clear of the top-center bearing tape and bottom-left status card.

## Primary flow

1. **Ship Cockpit HUD:** Spawn seated behind dark 3D canopy struts with the amber 3-panel console active; align using the left contacts readout and drifting 6DoF prograde pip, fly to the asteroid through visible space-dust streaks, toggle `[Z]` dampeners (`[■] DAMPENERS`), and press `F` to exit.
2. **Suit Visor HUD (Mining):** Switch immediately to the open cyan visor; orient via the top-center bearing tape, jet to the rust-gold ore seam with `[1] Drill` active on the bottom toolbar, and hold `LMB` to watch the drill extraction ring and bottom-left `CARGO` bar fill with `IronOre`.
3. **Suit Visor HUD -> Refinery Terminal Panel:** Follow the `STATION` pip on the top-center bearing tape to the amber-marked refinery block, press `F` to open the terminal, press `E` to deposit all ore, watch the conversion bar finish, and press `R` to move `SteelBlock` items into slot `[2]` on the bottom hotbar.
4. **Suit Visor HUD (Block Placement):** Press `2` to select `Steel Block` (showing stack count in the slot corner), aim at an adjacent `2.5m` station block face to project a translucent cyan 3D wireframe ghost cube, and click `LMB` to snap the block onto the grid.

## Tokens

- Colour (Cockpit HUD — holographic amber): background `#120C06` (80% alpha), border `#663608`, primary `#FF8C1A`, accent `#38BDF8` (cyan target highlight), muted text `#A36A30`, danger `#EF4444`
- Colour (Suit Visor & Terminal — slate cyan): background `#101A26` (82% alpha), surface `#16222F` (92% alpha), border `#2B4257`, text `#E6EDF3`, muted text `#8B9EB0`, primary `#38BDF8`, warning/active `#F59E0B` (drilling, refining, or dampeners off), danger `#EF4444`
- Colour (3D Grey-Box World & Readability): space backdrop `#05070B` with close-range `20m` space-dust drift particles (`#CBD5E1`, 35% alpha); directional sun `#FFF6E5` (`1.2` energy) + ambient sky fill `#1E293B` (`0.35` energy so shadowed faces stay visible); ship hull `#94A3B8` with `#0F172A` interior canopy struts; station armor block `#64748B` with `#334155` `2.5m` cell seam lines; refinery block `#475569` with `#F59E0B` port trim; asteroid body `#27272A` with metallic `#D97706` ore deposit; placement ghost `#38BDF8` (35% alpha valid) / `#EF4444` (35% alpha invalid)
- Type: `Consolas` / `JetBrains Mono` monospace, scale `12px` (keycaps, slot numbers, debug rows), `14px` (HUD labels, contacts, log lines), `18px` (panel headers & target callouts), `24px` (primary `m/s` speed readout)
- Spacing: `4px` base unit (`4px`, `8px`, `12px`, `16px`, `24px`)
- Radius: `2px`
- Shadow: `none` (`1px` crisp borders drawn in Godot `Control` / `StyleBoxFlat`)

## Components

- **3D Canopy Struts & Suit Tool Mesh:** Procedural dark framing bars attached to the cockpit camera in `SeatedInCockpit`, and a simple first-person right-hand grey-box tool block in `FloatingInSuit` so mode switches are unmistakable even with HUD hidden.
- **Center Reticle & 6DoF Vector Pips:** Fixed center boresight crosshair plus screen-projected prograde (open circle) and retrograde (crossed circle) velocity markers that clamp to a central HUD ring when off-axis.
- **Cockpit 3-Panel Dashboard:** Angled bottom-left Contacts card (`STATION`, `ASTEROID` range in `m`/`km`), bottom-center Speed & Throttle bar, and bottom-right Systems list with *Elite*-style square status boxes (`[■] DAMPENERS [Z]`, `[■] PROXIMITY`) plus top-right 3-line `INFO` feed.
- **Visor Bearing Tape:** Top-center horizontal compass bar (`480px` wide) plotting relative yaw ticks and dynamic `SHIP` / `STATION` diamond markers with distance labels so the player never loses landmarks in 3D space.
- **Suit Status Stack:** Bottom-left *Space Engineers* / *7D2D*-style compact panel showing digital speed (`0.0 m/s`), dampener badge (`Z: ON` in cyan / `Z: OFF` in amber), and segmented `CARGO` capacity bar (`0 / 50`).
- **Numbered 5-Slot Hotbar:** Bottom-center horizontal strip of `56x56px` slots (`[1] Drill`, `[2] Steel Block`, `[3]`–`[5]` empty) with top-left key number, highlighted active border, bottom-right stack count (`7D2D` style), and bottom progress strip.
- **3D Grid Ghost Cube & Context Prompt:** Translucent `2.5m` wireframe box snapped to the hit station face normal, paired with a bottom-center prompt above the hotbar (`[F] Exit Cockpit`, `[LMB] Drill Iron Ore`, `[F] Open Refinery`, `[LMB] Place Steel Block`).
- **Refinery Transfer Card & Debug Readout:** Centered 2-row input/output terminal card with progress bar, and an unstyled top-left monospace key-value debug overlay (`F3`).

## States

- **Empty:** Suit `CARGO` bar reads `0 / 50` in muted `#8B9EB0`; hotbar slot `[2]` shows count `0` dimmed and suppresses the 3D ghost cube (`NO STEEL BLOCKS — REFINE ORE AT STATION`); Refinery terminal shows `IDLE — [E] DEPOSIT IRON ORE`.
- **Loading / Active Progress:** Drilling draws an amber extraction arc around the reticle, emits hit sparks/beam on the deposit, and increments `IronOre`; Refinery terminal animates an amber progress bar (`REFINING 1.2s / 2.0s`).
- **Error / Invalid:** Dampeners off turns the `[Z]` indicator amber (`DAMPENERS OFF`); aiming past `5m` tool range dims the reticle; aiming a block at an occupied or non-adjacent cell turns the 3D ghost cube and prompt `#EF4444` (`INVALID CELL`); full suit inventory shows `#EF4444` (`CARGO FULL`).
- **No-permission / Modal Lock:** Opening the Refinery Terminal Panel locks flight/suit thrusters and tool fire, dims the background visor slightly, and releases the mouse cursor (`Visible`) while keeping `[E]`, `[R]`, and `[Esc]`/`[F]` keyboard shortcuts active.

## Responsive

- Desktop 16:9+ landscape: authored against a `1920x1080` minimum reference canvas using Godot `CanvasLayer` corner/edge anchors (top-left debug, top-center bearing tape, top-right info log, center reticle, bottom-left status, bottom-center hotbar, bottom-right cockpit systems) so `2560x1440` and `3840x2160` preserve clean open sightlines in the center viewport.

## Accessibility

- High contrast (`> 10:1` text over `#120C06` and `#101A26` panels) plus cool ambient 3D fill lighting so neither HUD text nor shadowed asteroid faces disappear against dark space.
- Every colour cue (amber cockpit vs. cyan visor, amber dampeners-off, red invalid cell/full cargo) is paired with explicit text or shape symbology (`[■]`/`[□]`, prograde vs. retrograde glyph, text callouts).
- Every terminal and mode action works via single keystrokes (`F`, `Z`, `1`–`2`, `E`, `R`, `Esc`, `F3`) with no drag-and-drop required.

## Where the screens live

- Stitch project id: none
- Stitch design system id: none
- Claude Design project id: none

## Out of scope

- Interactive freelook-focused multi-tab cockpit MFD sub-screens (the 3 cockpit panels are static widgets in the first slice, ready to expand later)
- Custom 2D icon artwork or textured 3D models (all HUD widgets use procedural `Control` shapes/text and 3D objects use clean colour-coded primitives)
- Title screen, pause/settings menus, or full grid-inventory drag-and-drop windows
- Gamepad or mobile layouts
