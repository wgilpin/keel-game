using Godot;

namespace Keel.Game.Suit;

public static class SuitToolMeshBuilder
{
    private static readonly Color ToolBodyColor = new(0x64 / 255f, 0x74 / 255f, 0x8B / 255f, 1.0f); // Slate-500
    private static readonly Color ToolAccentColor = new(0x94 / 255f, 0xA3 / 255f, 0xB8 / 255f, 1.0f); // Slate-400
    private static readonly Color ToolHandleColor = new(0x1E / 255f, 0x29 / 255f, 0x3B / 255f, 1.0f); // Slate-800

    public static Node3D CreateToolMesh()
    {
        var root = new Node3D { Name = "SuitToolMesh" };

        var bodyMat = new StandardMaterial3D
        {
            AlbedoColor = ToolBodyColor,
            Roughness = 0.5f,
            Metallic = 0.4f,
        };

        var accentMat = new StandardMaterial3D
        {
            AlbedoColor = ToolAccentColor,
            Roughness = 0.4f,
            Metallic = 0.6f,
        };

        var handleMat = new StandardMaterial3D
        {
            AlbedoColor = ToolHandleColor,
            Roughness = 0.9f,
            Metallic = 0.1f,
        };

        // Frame the camera on the lower right
        // Position tool relative to SuitCamera: +X right, -Y down, -Z forward
        // Tool body chassis
        var body = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(0.08f, 0.12f, 0.35f), Material = bodyMat },
            Position = new Vector3(0.28f, -0.22f, -0.45f),
        };
        root.AddChild(body);

        // Tool emitter / drill nozzle tip extending forward
        var nozzle = new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = 0.02f, BottomRadius = 0.035f, Height = 0.15f, Material = accentMat },
            Position = new Vector3(0.28f, -0.20f, -0.65f),
            RotationDegrees = new Vector3(90.0f, 0.0f, 0.0f),
        };
        root.AddChild(nozzle);

        // Grip handle
        var handle = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(0.05f, 0.14f, 0.06f), Material = handleMat },
            Position = new Vector3(0.28f, -0.32f, -0.38f),
            RotationDegrees = new Vector3(-15.0f, 0.0f, 0.0f),
        };
        root.AddChild(handle);

        return root;
    }
}
