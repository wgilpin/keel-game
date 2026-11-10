using Godot;

namespace Keel.Game.Flight;

public static class ShipHullBuilder
{
    private static readonly Color HullColor = new(0x94 / 255f, 0xA3 / 255f, 0xB8 / 255f, 1.0f);
    private static readonly Color TrimColor = new(0x33 / 255f, 0x41 / 255f, 0x55 / 255f, 1.0f);

    public static Node3D CreateHull()
    {
        var hullRoot = new Node3D { Name = "ShipExteriorHull" };

        var hullMat = new StandardMaterial3D
        {
            AlbedoColor = HullColor,
            Roughness = 0.65f,
            Metallic = 0.35f,
        };

        var trimMat = new StandardMaterial3D
        {
            AlbedoColor = TrimColor,
            Roughness = 0.8f,
            Metallic = 0.2f,
        };

        // Main hull body matching collision shape: 3.0m wide, 1.8m high, 4.0m long
        var mainBody = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(3.0f, 1.8f, 4.0f), Material = hullMat },
            Position = Vector3.Zero,
        };
        hullRoot.AddChild(mainBody);

        // Cockpit canopy windshield glass frame exterior
        var canopyMesh = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(1.6f, 0.6f, 1.5f), Material = trimMat },
            Position = new Vector3(0.0f, 0.9f, -0.8f),
        };
        hullRoot.AddChild(canopyMesh);

        // Rear engine thruster pods
        var leftEngine = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(0.8f, 0.8f, 1.2f), Material = trimMat },
            Position = new Vector3(-1.2f, 0.0f, 2.2f),
        };
        hullRoot.AddChild(leftEngine);

        var rightEngine = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(0.8f, 0.8f, 1.2f), Material = trimMat },
            Position = new Vector3(1.2f, 0.0f, 2.2f),
        };
        hullRoot.AddChild(rightEngine);

        return hullRoot;
    }
}
