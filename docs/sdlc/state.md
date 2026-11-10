# SDLC state

Project: Keel
Updated: 2026-11-10

## Stack

- Language: C# (.NET 10 SDK)
- Framework: Godot 4.7 (.NET official editor build) + Jolt Physics 3D (default)
- Database: In-memory state + JSON config files (no persistence in first slice)
- Tests: xUnit via `dotnet test Keel.sln`
- Deploy: Local Windows PC (x64)

## Stages

- concept: approved 2026-11-10
- explore: skipped
- architecture: approved 2026-11-10
- design: approved 2026-11-10
- phases: approved 2026-11-10

## Phases

- [x] 01-cockpit-flight — done
  - [x] epic-01-tracer-skeleton — merged 2026-11-10
  - [x] epic-02-newtonian-flight — merged 2026-11-10
  - [x] epic-03-space-arena — merged 2026-11-10
  - [x] epic-04-cockpit-hud — merged 2026-11-10
- [ ] 02-suit-mining — in progress
  - [x] epic-01-suit-eva — merged 2026-11-10
  - [x] epic-02-suit-visor-hud — merged 2026-11-10
  - [x] epic-03-asteroid-mining — reviewed 2026-11-10
- [ ] 03-station-refinery — pending
- [ ] 04-grid-construction — pending

## Notes for a fresh session

- Ensure `.NET 10 SDK` and `Godot 4.7 (.NET)` are installed and on PATH before Stage 6 (`/sdlc:build-epic`).
- The agent cannot play the game; Will is the only check on flight and drill feel.
- Do not overdo tests (`CLAUDE.md`): test non-obvious math, state rules, and error recovery only—never constants, tokens, trivial toggles, or DTOs.
