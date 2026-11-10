using System.Numerics;

namespace Keel.Sim.Arena;

public static class SpaceArenaLayout
{
    public const float SoftBoundaryRadius = 19500.0f;
    public const float HardBoundaryRadius = 20000.0f;

    public static Vector3 ShipSpawnPosition => new(0f, 0f, 0f);
    public static Vector3 StationPosition => new(0f, 0f, -500f);
    public static Vector3 AsteroidPosition => new(600f, 150f, -1200f);
}
