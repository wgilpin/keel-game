namespace Keel.Sim.Hud;

public readonly record struct CockpitSystemsStatus(
    bool DampenersEnabled,
    bool ProximityAlert,
    bool BoundaryWarning);
