using System;
using System.Numerics;

namespace Keel.Sim.Pilot;

public readonly record struct SuitSpawnState(
    Vector3 Position,
    Quaternion Orientation,
    Vector3 LinearVelocity,
    Vector3 AngularVelocity);

public sealed class PilotModeStateMachine
{
    public static readonly Vector3 DefaultExitOffset = new(-2.5f, 0.0f, 0.0f);

    public PilotMode Mode { get; private set; } = PilotMode.SeatedInCockpit;

    public PilotMode CurrentMode => Mode;

    public SuitSpawnState ExitCockpit(
        Vector3 shipPosition,
        Quaternion shipOrientation,
        Vector3 shipLinearVelocity,
        Vector3? customExitOffset = null)
    {
        if (Mode != PilotMode.SeatedInCockpit)
        {
            throw new InvalidOperationException("Cannot exit cockpit when not seated in cockpit.");
        }

        Mode = PilotMode.FloatingInSuit;
        Vector3 localOffset = customExitOffset ?? DefaultExitOffset;
        Vector3 worldOffset = Vector3.Transform(localOffset, shipOrientation);
        Vector3 spawnPosition = shipPosition + worldOffset;

        return new SuitSpawnState(
            Position: spawnPosition,
            Orientation: shipOrientation,
            LinearVelocity: shipLinearVelocity,
            AngularVelocity: Vector3.Zero);
    }

    public bool TryBoardShip(Vector3 shipPosition, Vector3 suitPosition, float shipInteractRange)
    {
        if (Mode != PilotMode.FloatingInSuit)
        {
            return false;
        }

        float distance = Vector3.Distance(shipPosition, suitPosition);
        if (distance <= shipInteractRange)
        {
            Mode = PilotMode.SeatedInCockpit;
            return true;
        }

        return false;
    }

    public void Reset(PilotMode mode = PilotMode.SeatedInCockpit) => Mode = mode;
}
