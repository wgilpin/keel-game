using Godot;
using Keel.Sim.Inventory;
using Keel.Sim.Mining;

namespace Keel.Game.World;

public partial class OreDepositNode : StaticBody3D
{
    public static readonly Color ActiveColor = new(0xD9 / 255f, 0x77 / 255f, 0x06 / 255f, 1.0f);
    public static readonly Color DepletedColor = new(0x3F / 255f, 0x3F / 255f, 0x46 / 255f, 1.0f);

    public OreDepositState State { get; }

    private MeshInstance3D _meshInstance = null!;
    private StandardMaterial3D _activeMaterial = null!;
    private StandardMaterial3D _depletedMaterial = null!;
    private Vector3 _originalScale = Vector3.One;

    public OreDepositNode() : this(30, ItemId.IronOre)
    {
    }

    public OreDepositNode(int yieldUnits, ItemId oreType = ItemId.IronOre)
    {
        State = new OreDepositState(yieldUnits, oreType);
    }

    public override void _Ready()
    {
        _activeMaterial = new StandardMaterial3D
        {
            AlbedoColor = ActiveColor,
            Metallic = 0.85f,
            Roughness = 0.35f,
        };

        _depletedMaterial = new StandardMaterial3D
        {
            AlbedoColor = DepletedColor,
            Metallic = 0.1f,
            Roughness = 0.9f,
        };

        var mesh = new SphereMesh
        {
            Radius = 2.5f,
            Height = 3.0f,
            RadialSegments = 16,
            Rings = 8,
            Material = _activeMaterial,
        };

        _meshInstance = new MeshInstance3D
        {
            Mesh = mesh,
            Scale = new Vector3(1.2f, 0.5f, 1.2f),
        };
        AddChild(_meshInstance);

        var shape = new CollisionShape3D
        {
            Shape = new SphereShape3D { Radius = 2.5f },
            Scale = new Vector3(1.2f, 0.5f, 1.2f),
        };
        AddChild(shape);

        _originalScale = _meshInstance.Scale;
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        if (_meshInstance == null)
        {
            return;
        }

        if (State.IsDepleted)
        {
            _meshInstance.MaterialOverride = _depletedMaterial;
            _meshInstance.Scale = _originalScale * 0.92f;
        }
        else
        {
            _meshInstance.MaterialOverride = _activeMaterial;
            _meshInstance.Scale = _originalScale;
        }
    }
}
