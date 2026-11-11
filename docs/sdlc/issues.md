# Issues — Keel

One line per issue, newest last. Status is `open`, `scheduled`, `fixed`,
or `dropped`. This file records issues. It never records stage status.

## ISS-1 — Ship cockpit HUD lacks station target marker or navigation bearing

**Raised:** 2026-11-11
**Status:** fixed 2026-11-11
**Lands in:** fixed on main

While piloting the ship, the Cockpit HUD displays no directional station navigation marker or bearing cue, unlike the Suit Visor HUD. Fixed by adding top-center bearing tape and landmark bearings to CockpitHudNode. Proved by CockpitHudTests.ContactsModel_ComputesStationAndAsteroidBearingsForCockpit.
