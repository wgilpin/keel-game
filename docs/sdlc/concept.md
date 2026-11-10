# Keel

## Problem

Existing space-engineering games—primarily *Space Engineers*—spend too much of their design budget on planets, surface gravity, and survival bloat instead of zero-g space construction and the long-term arc of bootstrapping a civilisation in space. Today's workaround is playing *Space Engineers* with space-start settings or mods, which keeps the planetary baggage and never reaches a real civilisation-building loop.

## Who it is for

Will, playing single-player on PC. The goal is fun and personal satisfaction: building ships and stations in zero-g and growing them toward an orbital civilisation, built in small slices that can be played and tuned on one machine.

## The smallest thing that proves it

One closed loop in grey-box space inside a 20 km bubble:

1. Fly a prebuilt ship with Newtonian 6DoF control from the cockpit view.
2. Exit the ship and move in zero-g in a suit using a thruster pack.
3. Drill a discrete ore deposit on an asteroid into an inventory.
4. Carry the ore to a refinery block on a small prebuilt station.
5. Receive one type of structural block from the refinery.
6. Place that block onto the station grid.

If flying, jetpacking, and drilling do not feel good with grey boxes, the idea stops here.

## Out of scope

- Planets and surface gravity
- Voxel terrain and voxel asteroids (use discrete ore deposits instead)
- Walking inside a moving ship (player is either seated in the cockpit or floating in the suit)
- Worlds larger than 20 km from the origin
- Procedural generation
- Power networks and conveyors
- Ship construction (station grid only in the first slice)
- Save and load
- Enemies and combat
- Multiplayer

## Open questions

- Is the long-term centre of the game first-person construction, or automation and logistics?
- When the world grows past 20 km, should coordinates use a floating origin or a double-precision engine build?
- Should asteroids stay discrete ore deposits long-term or move to voxels?
- Will the player ever walk inside a ship while it is moving?
- What is the final name for the game ("Keel" is provisional)?
