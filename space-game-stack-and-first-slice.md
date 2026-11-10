# Keel: Stack and First Slice

*Date: 2026-11-10. "Keel" is a provisional name.*

## Concept

- First-person space game: spaceships, mining, base building, ship building.
- Like Space Engineers, but more about space and less about planets.
- Long-term theme: bootstrap a civilisation.
- Views: on foot / in suit, or ship cockpit.
- Single player, PC.
- Purpose: fun. The full scope is too large, so the first slice covers the basics only.

## Development machine

- **PC (Windows), RTX 3090, 64 GB RAM.** Development and play happen on the same machine, so flight feel is tuned on the target hardware.
- The hardware is much more than the first slice needs. It does not constrain the choice of engine.
- A Mac build is possible later, but it is not a goal.

## Recommended stack

**Godot 4.7 (.NET editor build), C#, default Jolt physics, official engine build.**

| Choice | Reason |
|---|---|
| Godot 4.7 | Scenes and scripts are plain text, so Claude Code can read and edit the full project. Pin the patch version, because early 4.7 had regressions. |
| C# | See "Why C#" below. |
| Jolt physics | Default 3D physics engine for new projects since Godot 4.6. |
| Official build | No custom engine compile until a feature needs one. |

### Why C#

The code is mostly written by an agent and rarely read by a human, so the language is selected for the agent.

- **Compiler checks.** Static types catch many errors in agent-written code before the game runs.
- **Performance.** Grid data, block meshes, and inventory simulation are loop-heavy. C# is much faster than GDScript for that work.
- **Tests.** The .NET test tools are mature. Simulation logic can be tested from the command line without the engine.
- **Stable, official support.** C# is an official Godot language with stable interfaces.
- **Human fallback.** Will is a former professional C# developer (after Delphi), so the code stays readable to him when needed.

Costs:

- Needs the .NET build of the Godot editor and the .NET SDK.
- Each change needs a compile. GDScript reloads faster.
- A later double-precision engine build also needs the C# assemblies rebuilt with the same setting. Current state of that process is not verified.
- C# access to the Zylann voxel module is probably available through its Mono builds. Completeness is not verified.

### Alternatives considered

- **GDScript.** Faster reload and close to Python, but weaker checks and slower loops. Can still be mixed in for small scene scripts.
- **Rust (Bevy, or community Godot bindings).** Stricter compiler, but Bevy changes its interfaces often, has no mature editor, and compiles slowly.
- **Unreal 5.** Has double-precision world coordinates in the standard build (not verified in this research) and looks better with no effort. Cost: C++, long compile times, binary assets and Blueprints, and a workflow that is less friendly to code agents.

## Project structure

- **`Keel.Sim`: plain C# library, no engine dependency.** Grids, inventory, refinery logic, recipes. Covered by unit tests that the agent runs from the command line.
- **`Keel.Game`: thin Godot layer.** Input, rendering, physics, scene setup. Calls into `Keel.Sim`.
- **Scenes built mostly from code.** Scene files are text, but an agent edits them less reliably than code. Keep scene files small.

## Working practices for agent-built game code

1. **The agent cannot play the game.** Tests prove that logic is correct. They cannot tell if flight feels good. The human is the only check on feel.
2. **Tuned values live outside the code.** Thrust, drag, drill rate, and mouse response are exported editor properties or values in one config file, so they can be changed while the game runs.
3. **On-screen debug overlay from the start.** Runtime bugs are visual and do not produce a stack trace. The overlay shows velocity, position, grid coordinates, and physics state.
4. **Simulation first, visuals second.** Each feature lands in `Keel.Sim` with tests before it gets a Godot representation.

## Technical risks that shape the design

### 1. Scale

- Double precision in Godot ("large world coordinates") needs a custom engine compile. The official builds leave it off.
- The Godot documentation puts the practical single-precision limit at about 65 km from the origin.
- **Decision for the slice:** keep everything inside 20 km.
- **Later:** select between a floating origin and a double-precision build.

### 2. Voxel asteroids

- Zylann Voxel Tools is a C++ module that is compiled into the engine. Precompiled builds are available.
- **Decision for the slice:** no voxels. Use asteroids with discrete ore deposits that the drill removes.

### 3. Walking inside a ship that moves

- This is the hardest problem in the genre. Moving reference frames and character controllers conflict.
- **Decision for the slice:** the player is either in the pilot seat or in zero-g in the suit. This also fits "more space, fewer planets".

### Combined cost

Double precision plus voxels plus C# means a custom engine build with all three. That is possible, but it is a real maintenance cost.

## First slice

One closed loop: **fly, mine, refine, place one block.**

1. Fly a prebuilt ship with Newtonian six-degrees-of-freedom control from the cockpit view.
2. Exit the ship and move in the suit with a thruster pack.
3. Drill an ore deposit on one asteroid. The ore goes into an inventory.
4. Carry the ore to one refinery block on a small prebuilt station.
5. Receive one type of structural block from the refinery.
6. Place that block on the station grid.

### Excluded from the slice

- Procedural generation
- Power
- Conveyors
- Save and load
- Gravity
- Ship construction
- Enemies

## Analysis

- The loop is the full game in miniature. Each later feature is a wider version of one step.
- The largest risk is feel, not architecture. If flight and the drill are not fun with grey boxes, more systems do not fix that. Tune steps 1 to 3 before steps 4 to 6.
- Ship construction is the same grid code as station construction, plus a rigid body and thrust. Do the static station grid first.
- "Bootstrap a civilisation" implies automation and logistics. That is a different game from first-person construction. Decide later which of the two is the centre.

## Open decisions

- Floating origin or double-precision build, when the world grows past 20 km.
- Voxel asteroids or discrete deposits in the long term.
- Whether the player can walk inside a ship that moves.
- Centre of the game: first-person construction, or automation and logistics.
- Final name.

## Next step

Create the Godot .NET project skeleton with `Keel.Sim` and `Keel.Game`, then build steps 1 and 2 (ship flight and suit movement) with the debug overlay.

## Sources

- [Godot 4 in 2026: New Updates, C# Support & Fast Iteration](https://gtstu.com/?p=4758)
- [gamedev-godot skill notes (4.7 release state)](https://www.skills.sh/vasilyu1983/ai-agents-public/gamedev-godot)
- [Large World Coordinates Demo, Godot Asset Store](https://store.godotengine.org/asset/godot-foundation/large-world-coordinates-demo/)
- [Large world coordinates, Godot docs](https://github.com/godotengine/godot-docs/blob/4.2/tutorials/physics/large_world_coordinates.rst)
- [Getting Voxel Tools](https://voxel-tools.readthedocs.io/en/latest/getting_the_module/)
- [C# basics, Godot docs](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_basics.html)
