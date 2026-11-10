using Godot;

namespace Keel.Game.Flight;

public static class CockpitCanopyBuilder
{
    private static readonly Color CanopyStrutColor = new(0x0F / 255f, 0x17 / 255f, 0x2A / 255f, 1.0f);

    public static Node3D CreateCanopy()
    {
        var canopyRoot = new Node3D { Name = "CockpitCanopy" };

        var strutMaterial = new StandardMaterial3D
        {
            AlbedoColor = CanopyStrutColor,
            Roughness = 0.85f,
            Metallic = 0.1f,
        };

        // Camera is at Position (0, 0.35, -0.6) with FOV 75 deg.
        // Place canopy struts framing the forward viewport in local camera space.
        // 1. Lower console dashboard sill (just below sightline)
        AddStrut(canopyRoot, strutMaterial,
            size: new Vector3(1.6f, 0.08f, 0.3f),
            pos: new Vector3(0.0f, -0.32f, -0.65f),
            rot: Vector3.Zero);

        // 2. Top windshield frame
        AddStrut(canopyRoot, strutMaterial,
            size: new Vector3(1.5f, 0.06f, 0.25f),
            pos: new Vector3(0.0f, 0.42f, -0.65f),
            rot: Vector3.Zero);

        // 3. Left A-pillar (angled strut)
        AddStrut(canopyRoot, strutMaterial,
            size: new Vector3(0.07f, 0.85f, 0.15f),
            pos: new Vector3(-0.65f, 0.06f, -0.68f),
            rot: new Vector3(0.0f, 0.0f, -12.0f));

        // 4. Right A-pillar (angled strut)
        AddStrut(canopyRoot, strutMaterial,
            size: new Vector3(0.07f, 0.85f, 0.15f),
            pos: new Vector3(0.65f, 0.06f, -0.68f),
            rot: new Vector3(0.0f, 0.0f, 12.0f));

        return canopyRoot;
    }

    private static void AddStrut(Node3D parent, Material material, Vector3 size, Vector3 pos, Vector3 rot)
    {
        var box = new BoxMesh
        {
            Size = size,
            Material = material,
        };
        var instance = new MeshInstance3D
        {
            Mesh = box,
            Position = pos,
            RotationDegrees = rot,
        };
        parent.AddChild(instance);
    }
}
