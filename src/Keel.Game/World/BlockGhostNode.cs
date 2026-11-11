using Godot;

namespace Keel.Game.World;

public partial class BlockGhostNode : Node3D
{
    // Design tokens from docs/sdlc/design.md
    private static readonly Color ValidFillColor = new(0x38 / 255f, 0xBD / 255f, 0xF8 / 255f, 0.35f);
    private static readonly Color ValidWireColor = new(0x38 / 255f, 0xBD / 255f, 0xF8 / 255f, 0.90f);
    private static readonly Color InvalidFillColor = new(0xEF / 255f, 0x44 / 255f, 0x44 / 255f, 0.35f);
    private static readonly Color InvalidWireColor = new(0xEF / 255f, 0x44 / 255f, 0x44 / 255f, 0.90f);

    private MeshInstance3D _fillMeshInstance = null!;
    private MeshInstance3D _wireMeshInstance = null!;
    private StandardMaterial3D _validFillMat = null!;
    private StandardMaterial3D _validWireMat = null!;
    private StandardMaterial3D _invalidFillMat = null!;
    private StandardMaterial3D _invalidWireMat = null!;

    public override void _Ready()
    {
        Name = "BlockGhost";

        _validFillMat = new StandardMaterial3D
        {
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            AlbedoColor = ValidFillColor,
            Roughness = 0.5f,
            Metallic = 0.1f,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
        };

        _invalidFillMat = new StandardMaterial3D
        {
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            AlbedoColor = InvalidFillColor,
            Roughness = 0.5f,
            Metallic = 0.1f,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
        };

        _validWireMat = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = ValidWireColor,
        };

        _invalidWireMat = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = InvalidWireColor,
        };

        // Fill box: slightly proud of 2.50m (2.505m) to avoid z-fighting with adjacent face
        _fillMeshInstance = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(2.505f, 2.505f, 2.505f) },
            MaterialOverride = _validFillMat,
        };
        AddChild(_fillMeshInstance);

        // 12-edge wireframe cage at 2.52m
        _wireMeshInstance = new MeshInstance3D
        {
            Mesh = CreateWireframeCageMesh(2.52f),
            MaterialOverride = _validWireMat,
        };
        AddChild(_wireMeshInstance);

        Visible = false;
    }

    public void ShowGhost(Vector3 stationLocalCenter, bool isValid)
    {
        Position = stationLocalCenter;
        _fillMeshInstance.MaterialOverride = isValid ? _validFillMat : _invalidFillMat;
        _wireMeshInstance.MaterialOverride = isValid ? _validWireMat : _invalidWireMat;
        Visible = true;
    }

    public void HideGhost()
    {
        Visible = false;
    }

    private static ImmediateMesh CreateWireframeCageMesh(float size)
    {
        float h = size * 0.5f;
        var mesh = new ImmediateMesh();

        // 12 edges of a box
        Vector3[] corners =
        {
            new(-h, -h, -h), // 0
            new( h, -h, -h), // 1
            new( h,  h, -h), // 2
            new(-h,  h, -h), // 3
            new(-h, -h,  h), // 4
            new( h, -h,  h), // 5
            new( h,  h,  h), // 6
            new(-h,  h,  h), // 7
        };

        int[,] edges =
        {
            { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 0 }, // Back face
            { 4, 5 }, { 5, 6 }, { 6, 7 }, { 7, 4 }, // Front face
            { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 }, // Connecting edges
        };

        mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
        for (int i = 0; i < 12; i++)
        {
            mesh.SurfaceAddVertex(corners[edges[i, 0]]);
            mesh.SurfaceAddVertex(corners[edges[i, 1]]);
        }
        mesh.SurfaceEnd();

        return mesh;
    }
}
