using Godot;
using Keel.Sim.Arena;

namespace Keel.Game.World;

public static class LandmarkBuilder
{
    private static readonly Color StationArmorColor = new(0x64 / 255f, 0x74 / 255f, 0x8B / 255f, 1.0f);
    private static readonly Color StationPortColor = new(0xF5 / 255f, 0x9E / 255f, 0x0B / 255f, 1.0f);
    private static readonly Color AsteroidColor = new(0x27 / 255f, 0x27 / 255f, 0x2A / 255f, 1.0f);

    public static Node3D CreateStation()
    {
        var root = new Node3D { Name = "StationLandmark" };
        var simPos = SpaceArenaLayout.StationPosition;
        root.Position = new Vector3(simPos.X, simPos.Y, simPos.Z);

        var staticBody = new StaticBody3D();
        root.AddChild(staticBody);

        var armorMaterial = new StandardMaterial3D
        {
            AlbedoColor = StationArmorColor,
            Roughness = 0.6f,
            Metallic = 0.2f,
        };

        var portMaterial = new StandardMaterial3D
        {
            AlbedoColor = StationPortColor,
            EmissionEnabled = true,
            Emission = StationPortColor,
            EmissionEnergyMultiplier = 0.8f,
            Roughness = 0.4f,
        };

        // Main station hub: 20m x 15m x 50m
        var mainHullMesh = new BoxMesh { Size = new Vector3(20f, 15f, 50f), Material = armorMaterial };
        var mainMeshInstance = new MeshInstance3D { Mesh = mainHullMesh };
        staticBody.AddChild(mainMeshInstance);

        var mainShape = new CollisionShape3D
        {
            Shape = new BoxShape3D { Size = new Vector3(20f, 15f, 50f) },
        };
        staticBody.AddChild(mainShape);

        // Amber docking ring on front face (+Z side of hub)
        var portMesh = new BoxMesh { Size = new Vector3(8f, 8f, 2f), Material = portMaterial };
        var portInstance = new MeshInstance3D
        {
            Mesh = portMesh,
            Position = new Vector3(0f, 0f, 26f),
        };
        staticBody.AddChild(portInstance);

        var portShape = new CollisionShape3D
        {
            Shape = new BoxShape3D { Size = new Vector3(8f, 8f, 2f) },
            Position = new Vector3(0f, 0f, 26f),
        };
        staticBody.AddChild(portShape);

        // Transverse solar/radiator spar: 60m x 2m x 10m
        var sparMesh = new BoxMesh { Size = new Vector3(60f, 2f, 10f), Material = armorMaterial };
        var sparInstance = new MeshInstance3D { Mesh = sparMesh };
        staticBody.AddChild(sparInstance);

        var sparShape = new CollisionShape3D
        {
            Shape = new BoxShape3D { Size = new Vector3(60f, 2f, 10f) },
        };
        staticBody.AddChild(sparShape);

        return root;
    }

    public static Node3D CreateAsteroid()
    {
        var root = new Node3D { Name = "AsteroidLandmark" };
        var simPos = SpaceArenaLayout.AsteroidPosition;
        root.Position = new Vector3(simPos.X, simPos.Y, simPos.Z);

        var staticBody = new StaticBody3D();
        root.AddChild(staticBody);

        var asteroidMaterial = new StandardMaterial3D
        {
            AlbedoColor = AsteroidColor,
            Roughness = 0.9f,
            Metallic = 0.05f,
        };

        // Irregular dark basalt asteroid body: 60m x 45m x 55m
        var bodyMesh = new SphereMesh
        {
            Radius = 28f,
            Height = 56f,
            RadialSegments = 16,
            Rings = 12,
            Material = asteroidMaterial,
        };
        var bodyInstance = new MeshInstance3D
        {
            Mesh = bodyMesh,
            Scale = new Vector3(1.1f, 0.85f, 1.25f),
        };
        staticBody.AddChild(bodyInstance);

        var bodyShape = new CollisionShape3D
        {
            Shape = new SphereShape3D { Radius = 28f },
            Scale = new Vector3(1.1f, 0.85f, 1.25f),
        };
        staticBody.AddChild(bodyShape);

        // Mineable metallic rust-gold (#D97706) ore seams on sunlit, station/origin-facing face
        root.AddChild(new OreDepositNode(30) { Position = new Vector3(-20.0f, 6.0f, 22.0f) });
        root.AddChild(new OreDepositNode(25) { Position = new Vector3(-12.0f, 14.0f, 20.0f) });
        root.AddChild(new OreDepositNode(35) { Position = new Vector3(-24.0f, -6.0f, 18.0f) });
        root.AddChild(new OreDepositNode(30) { Position = new Vector3(-8.0f, -4.0f, 30.0f) });

        return root;
    }
}
