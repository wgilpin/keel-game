# Epic 03 — Refinery Terminal

**Phase:** 03 Station Refinery
**Goal:** Build the modal slate-cyan `RefineryTerminalNode` panel opened with `F` on the station refinery block so the player can deposit `IronOre` (`E`), watch live refining progress, and withdraw `SteelBlock` items (`R`) onto hotbar slot `[2]`.
**Done criteria covered:** DC-5, DC-6, DC-7

## Behaviour

Jetting within `refineryInteractRange` (`5.0 m`) of the station's amber-trimmed `Refinery` block and pressing `F` opens the centered slate-cyan **Refinery Terminal Panel** (`RefineryTerminalNode`), dimming the Suit Visor slightly, releasing the mouse cursor (`Visible`), and locking suit 6DoF thrusters, mouse look, and `LMB` drill fire while suit inertial dampeners continue holding the player steady in zero-g. Inside the terminal, pressing `E` (or clicking Deposit) moves all fitting `IronOre` from suit cargo into the refinery input row; the status line and `#F59E0B` amber progress bar animate through `REFINING 1.2s / 2.0s` (or show `IDLE — [E] DEPOSIT IRON ORE` / `OUTPUT FULL — [R] WITHDRAW`); and pressing `R` (or clicking Withdraw) moves refined `SteelBlock` items into suit cargo, immediately updating hotbar slot `[2] Steel Block` with the new stack count and undimmed highlight. Pressing `F` or `Esc` closes the terminal, re-captures the mouse cursor, and restores full suit control while any remaining `IronOre` keeps refining in the background.

## Acceptance criteria

- **AC-1** — Given `FloatingInSuit` mode across varying ship distances, refinery distances, and targeted `BlockType` values, when `F` (interact) and `Esc` (cancel) inputs are processed by the modal terminal state machine, then `F` opens the terminal if and only if aiming at `BlockType.Refinery` within `refineryInteractRange` (taking precedence over `TryBoardShip`), pressing `F` or `Esc` while open closes the terminal without triggering `TryBoardShip` or leaving modal state open, and drifting beyond the tether limit (`refineryInteractRange + 1.5 m`) or entering `SeatedInCockpit` forces `IsRefineryOpen` to `false`.
- **AC-2** — Given an active terminal session (`IsRefineryOpen == true`), when modal input gating and terminal actions execute across `Idle`, `Refining`, and `OutputFull` states, then suit thrust/rotation and drill fire inputs are masked to zero while `[E]` invokes `RefinerySim.DepositFromSuit`, `[R]` invokes `RefinerySim.WithdrawToSuit` (incrementing `SuitInventory[ItemId.SteelBlock]` and hotbar slot `[2]` count, preserved across `TryBoardShip` and `ExitCockpit`), and `RefineryTerminalModel` reports normalized `[0, 1]` cycle progress and status state while `RefinerySim.Step` continues advancing whether `IsRefineryOpen` is `true` or `false`.

## Tests

- `RefineryTerminalTests.InteractAndCancel_PrioritizeRefineryOpenCloseOverShipBoardingAndAutoCloseOutOfTether` — proves `F` precedence over ship boarding when targeting `Refinery` in range, clean `F`/`Esc` modal close without side-effect boarding, and tether/cockpit auto-close (`AC-1`).
- `RefineryTerminalTests.ModalSession_MasksFlightAndDrillWhileAllowingDepositWithdrawAndBackgroundRefining` — proves flight/drill input masking while open, `[E]` deposit -> background/foreground `Step` -> `[R]` withdraw into slot `[2]`, `[0, 1]` progress normalization, and `SteelBlock` cargo persistence across ship boarding (`AC-2`).

## Files

- `src/Keel.Sim/Pilot/PilotModeStateMachine.cs`
- `src/Keel.Sim/Hud/RefineryTerminalModel.cs`
- `src/Keel.Game/Input/ShipInputReader.cs`
- `src/Keel.Game/UI/RefineryTerminalNode.cs`
- `src/Keel.Game/GameRoot.cs`
- `tests/Keel.Sim.Tests/Refinery/RefineryTerminalTests.cs`

## Out of scope

- Projecting a 3D `2.5 m` wireframe placement ghost cube when slot `[2] Steel Block` is selected (`Phase 04`)
- Clicking `LMB` to place `SteelBlock` items onto the station `BlockGrid` (`Phase 04`)
- Drag-and-drop inventory grids or partial-stack split sliders ([design.md](../../design.md))

## Notes

- Use exact terminal tokens from [design.md](../../design.md): backdrop dim over Suit Visor, panel background `#101A26` (82% alpha), surface `#16222F` (92% alpha), border `#2B4257` (`1px` crisp, `2px` radius), text `#E6EDF3`, muted `#8B9EB0`, primary `#38BDF8`, active progress `#F59E0B`, danger `#EF4444`. Do not write unit tests asserting UI colour constants (`CLAUDE.md`).
- Guard `ShipInputReader` / `GameRoot._Input` carefully when `IsRefineryOpen == true`:
  - `Esc` and `F` close the terminal and set `Godot.Input.MouseMode = Captured` instead of toggling pause mouse release.
  - `LMB` clicks while `IsRefineryOpen == true` must NOT trigger `ConsumeMouseRecaptureClick()` (so clicking Deposit/Withdraw/Close buttons with the visible cursor works cleanly) and must NOT fire the suit drill.
  - Discrete `E` (`Deposit`) and `R` (`Withdraw`) key presses (`Pressed: true, Echo: false`) trigger transfers while `ConsumeFlightInput` is ignored (`FlightInput.Zero` passed to `SuitRigNode.ApplyFlightStep` so `Z` dampeners remain active).
