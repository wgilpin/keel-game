# Epic 03 — Space Arena

**Phase:** 01 Cockpit Flight Tracer
**Goal:** Populate the 20 km space arena with lit grey-box station and asteroid landmarks, dark 3D cockpit canopy struts, space-dust drift cues, and a 20 km origin boundary guard.
**Done criteria covered:** DC-5, DC-7

## Behaviour

Flying the ship now has clear spatial and motion cues in 3D grey-box space. Dark `#0F172A` 3D canopy struts frame the cockpit view, directional sunlight plus cool `#1E293B` ambient fill illuminates a landmark grey-box station and a dark basalt asteroid inside the 20 km bubble, close-range `20m` space-dust particles streak past to make 6DoF drift immediately readable, and flying past `20,000 m` from the origin clamps outward velocity so single-precision coordinates stay crisp.

## Acceptance criteria

- **AC-1** — Given a ship position and velocity evaluated by `ArenaBoundary.Evaluate`, when inside `19,500 m` state is returned untouched, and when outside `20,000 m` with outward radial velocity, position clamps to the `20,000 m` sphere and outward radial velocity clamps to `<= 0` while preserving tangential and inward motion.
- **AC-2** — Given space-dust particle coordinates and a moving camera across both positive and negative world coordinates, when `SpaceDustField.WrapToCameraBox` updates a `20 m` cube (`[-10 m, +10 m]` relative to camera), then every point wraps Euclidean-cleanly into `[-10 m, +10 m]` on all axes without negative-remainder drift or array reallocation.

## Tests

- `SpaceArenaTests.Boundary_ClampsOutwardMotionBeyond20Km` — verifies untouched interior flight vs. `20 km` spherical clamp and outward radial velocity removal (`AC-1`).
- `SpaceArenaTests.DustField_EuclideanWrapsAcrossNegativeAndPositiveCoords` — verifies Euclidean modulo wrapping across negative and positive camera displacements (`AC-2`).

## Files

- `src/Keel.Sim/Arena/SpaceArenaLayout.cs`
- `src/Keel.Sim/Arena/ArenaBoundary.cs`
- `src/Keel.Sim/Arena/SpaceDustField.cs`
- `src/Keel.Game/World/SpaceEnvironmentBuilder.cs`
- `src/Keel.Game/World/LandmarkBuilder.cs`
- `src/Keel.Game/World/SpaceDustNode.cs`
- `src/Keel.Game/Flight/CockpitCanopyBuilder.cs`
- `src/Keel.Game/GameRoot.cs`
- `tests/Keel.Sim.Tests/Arena/SpaceArenaTests.cs`

## Out of scope

- Discrete mineable ore deposits on the asteroid (Phase 02)
- `BlockGrid` cell data structure on the station (Phase 03)
- Amber 2D Ship Cockpit HUD panels (`epic-04`)

## Notes

- Use the exact 3D readability tokens from [design.md](../../design.md): space backdrop `#05070B`, directional sun `#FFF6E5` (`1.2` energy), ambient sky fill `#1E293B` (`0.35` energy), ship canopy struts `#0F172A`, station landmark `#64748B` with `#F59E0B` port accent, asteroid landmark `#27272A`, and space-dust particles `#CBD5E1` (35% alpha).
- Give both the station and asteroid `StaticBody3D` Jolt colliders so flying into them collides physically. Do not write tests asserting static constant landmark coordinates (`CLAUDE.md`).
