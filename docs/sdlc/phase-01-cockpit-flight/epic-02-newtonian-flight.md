# Epic 02 — Newtonian Flight

**Phase:** 01 Cockpit Flight Tracer
**Goal:** Drive a Jolt `RigidBody3D` ship from the cockpit using `Keel.Sim` 6DoF thrust, torque, speed clamping, and toggleable inertial dampeners (`Z`).
**Done criteria covered:** DC-3, DC-4

## Behaviour

The player spawns seated in a Jolt `RigidBody3D` ship with zero gravity and zero built-in drag. Pressing `W`/`S`, `A`/`D`, and `Space`/`Ctrl`, moving the mouse (pitch/yaw), and pressing `Q`/`E` (roll) accelerates and rotates the ship via forces and torques computed in `Keel.Sim.FlightKinematics`. Pressing `Z` toggles inertial dampeners between pure Newtonian drift (`OFF`) and smooth, zero-overshoot counter-thrust braking (`ON`), with live velocity and dampener state shown in the `F3` overlay.

## Acceptance criteria

- **AC-1** — Given 6DoF translation and rotation inputs, when `FlightKinematics.Step` computes forces and velocities, then diagonal translation inputs are normalized against over-thrust, torque scales by `MouseSensitivity` and `AngularTorque`, and forward acceleration clamps at `MaxLinearSpeed` / `MaxAngularSpeed` while still permitting braking and lateral vectoring.
- **AC-2** — Given `DampenersEnabled = false` and zero control input while moving and rotating, when `FlightKinematics.Step` advances across ticks, then linear and angular velocities remain unchanged.
- **AC-3** — Given `DampenersEnabled = true` and zero control input from non-zero linear and angular velocity, when `FlightKinematics.Step` integrates across both `60 Hz` and coarse `20 Hz` frame steps, then speed decreases monotonically to `0` without reversing sign on any axis.

## Tests

- `FlightKinematicsTests.ThrustAndTorque_RespectsNormalizationAndSpeedCaps` — verifies diagonal normalization, `MaxLinearSpeed`/`MaxAngularSpeed` caps, and braking/lateral thrust at cap (`AC-1`).
- `FlightKinematicsTests.DampenersOff_MaintainsNewtonianDrift` — proves zero counter-force/torque in drift mode (`AC-2`).
- `FlightKinematicsTests.DampenersOn_StopsWithoutOvershoot` — simulates `60 Hz` and `20 Hz` steps to zero and asserts no axis sign flip (`AC-3`).

## Files

- `src/Keel.Sim/Flight/FlightInput.cs`
- `src/Keel.Sim/Flight/FlightState.cs`
- `src/Keel.Sim/Flight/FlightKinematics.cs`
- `src/Keel.Game/Flight/ShipRigNode.cs`
- `src/Keel.Game/Input/ShipInputReader.cs`
- `src/Keel.Game/GameRoot.cs`
- `tests/Keel.Sim.Tests/Flight/FlightKinematicsTests.cs`

## Out of scope

- Exiting the cockpit or suit movement (Phase 02)
- 3D landmarks, canopy struts, and space-dust cues (`epic-03`)
- Amber 3-panel Cockpit HUD (`epic-04`)

## Notes

- Configure `ShipRigNode` (`RigidBody3D`) with `GravityScale = 0`, `LinearDamp = 0`, `AngularDamp = 0`, and `LinearDampMode = Replace` / `AngularDampMode = Replace` so Jolt applies no hidden drag behind `Keel.Sim`.
- Capture the mouse cursor (`Input.MouseModeEnum.Captured`) on startup and allow `Esc` to toggle capture while editing `config/tuning.json`.
