# Project Rules — Keel

## Testing

- **Do not test the obvious.** Never write unit tests for constants, colour/design tokens, simple DTOs/records, getters/setters, trivial boolean toggles, basic string concatenation, or constraints the C# compiler already enforces.
- **Test real behaviour and edge cases only.** Keep the test suite lean and high-signal: test non-trivial math (6DoF kinematics, dampener convergence, 3D projection, Euclidean wrapping, grid adjacency), stateful invariants (inventory/refinery transfers), and failure recovery (malformed config fallback).
- **Prefer fewer, cohesive behavioural tests** over dozens of micro-tests per class. Keep epic acceptance criteria (`AC-N`) focused on meaningful behaviours so `/sdlc:review` does not force boilerplate tests.

## Workflow

- The agent cannot judge 6DoF flight or drill feel; Will evaluates feel in Godot and tunes `config/tuning.json`.
- Do not start long-running background dev servers (`npm run start:dev`, editor watchers, etc.) from the agent.
