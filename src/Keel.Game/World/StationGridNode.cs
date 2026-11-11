using System;
using System.Collections.Generic;
using Godot;
using Keel.Sim.Arena;
using Keel.Sim.Grid;
using Keel.Sim.Refinery;
using SimVector3 = System.Numerics.Vector3;

namespace Keel.Game.World;

public partial class StationGridNode : Node3D
{
    // Design tokens from docs/sdlc/design.md
    private static readonly Color ArmorColor = new(0x64 / 255f, 0x74 / 255f, 0x8B / 255f, 1.0f);
    private static readonly Color SeamColor = new(0x33 / 255f, 0x41 / 255f, 0x55 / 255f, 1.0f);
    private static readonly Color RefineryColor = new(0x47 / 255f, 0x55 / 255f, 0x69 / 255f, 1.0f);
    private static readonly Color RefineryTrimColor = new(0xF5 / 255f, 0x9E / 255f, 0x0B / 255f, 1.0f);
    private static readonly Color CoreColor = new(0x47 / 255f, 0x55 / 255f, 0x69 / 255f, 1.0f);

    private readonly BlockGrid _grid;
    private StaticBody3D _staticBody = null!;
    private readonly Dictionary<Int3, Node3D> _blockVisuals = new();
    private readonly RefineryState _refinery = new();
    private BlockGhostNode _ghostNode = null!;

    public BlockGrid Grid => _grid;
    public StaticBody3D StaticBody => _staticBody;
    public RefineryState Refinery => _refinery;
    public BlockGhostNode GhostNode => _ghostNode;

    public StationGridNode(BlockGrid? initialGrid = null)
    {
        Name = "StationGrid";
        _grid = initialGrid ?? StationBlueprint.CreateDefault();
    }

    public override void _Ready()
    {
        SimVector3 simPos = SpaceArenaLayout.StationPosition;
        Position = new Vector3(simPos.X, simPos.Y, simPos.Z);

        _staticBody = new StaticBody3D { Name = "StationBody" };
        AddChild(_staticBody);

        _ghostNode = new BlockGhostNode();
        AddChild(_ghostNode);

        BuildAllBlockVisualsAndColliders();
    }

    private void BuildAllBlockVisualsAndColliders()
    {
        foreach (var kvp in _grid.Blocks)
        {
            AddBlockVisualAndCollider(kvp.Key, kvp.Value);
        }
    }

    public void AddBlockVisualAndCollider(Int3 cell, BlockType type)
    {
        if (_blockVisuals.ContainsKey(cell))
        {
            return;
        }

        Vector3 localCenter = new(
            cell.X * BlockGrid.CellSize,
            cell.Y * BlockGrid.CellSize,
            cell.Z * BlockGrid.CellSize);

        var blockRoot = new Node3D
        {
            Name = $"Block_{cell.X}_{cell.Y}_{cell.Z}",
            Position = localCenter,
        };

        // 1. Materials based on block type
        Material blockMat = type switch
        {
            BlockType.Refinery => new StandardMaterial3D
            {
                AlbedoColor = RefineryColor,
                Roughness = 0.5f,
                Metallic = 0.3f,
            },
            BlockType.Core => new StandardMaterial3D
            {
                AlbedoColor = CoreColor,
                Roughness = 0.6f,
                Metallic = 0.4f,
            },
            _ => new StandardMaterial3D
            {
                AlbedoColor = ArmorColor,
                Roughness = 0.65f,
                Metallic = 0.2f,
            },
        };

        // 2. Seam line framing: slightly smaller main block (2.46m) over darker seam box (2.50m)
        // gives clean 2.5m grid seams per design.md
        var seamMesh = new BoxMesh
        {
            Size = new Vector3(BlockGrid.CellSize, BlockGrid.CellSize, BlockGrid.CellSize),
            Material = new StandardMaterial3D
            {
                AlbedoColor = SeamColor,
                Roughness = 0.9f,
            },
        };
        blockRoot.AddChild(new MeshInstance3D { Mesh = seamMesh });

        var faceMesh = new BoxMesh
        {
            Size = new Vector3(BlockGrid.CellSize - 0.04f, BlockGrid.CellSize - 0.04f, BlockGrid.CellSize - 0.04f),
            Material = blockMat,
        };
        blockRoot.AddChild(new MeshInstance3D { Mesh = faceMesh });

        // 3. For Refinery block: glowing amber port trim on front (+Z) face
        if (type == BlockType.Refinery)
        {
            var trimMesh = new BoxMesh
            {
                Size = new Vector3(1.6f, 1.6f, 0.1f),
                Material = new StandardMaterial3D
                {
                    AlbedoColor = RefineryTrimColor,
                    EmissionEnabled = true,
                    Emission = RefineryTrimColor,
                    EmissionEnergyMultiplier = 1.0f,
                    Roughness = 0.3f,
                },
            };
            var trimInstance = new MeshInstance3D
            {
                Mesh = trimMesh,
                Position = new Vector3(0f, 0f, (BlockGrid.CellSize * 0.5f) + 0.02f),
            };
            blockRoot.AddChild(trimInstance);
        }

        // 4. Jolt Collision Shape
        var colShape = new CollisionShape3D
        {
            Shape = new BoxShape3D { Size = new Vector3(BlockGrid.CellSize, BlockGrid.CellSize, BlockGrid.CellSize) },
            Position = localCenter,
        };
        _staticBody.AddChild(colShape);

        _staticBody.AddChild(blockRoot);
        _blockVisuals[cell] = blockRoot;
    }

    /// <summary>
    /// Resolves a world raycast hit on this station node into local Int3 coordinates and BlockType.
    /// </summary>
    public bool ResolveRaycastHit(
        Vector3 globalHitPosition,
        Vector3 globalHitNormal,
        out Int3 cell,
        out BlockType type)
    {
        // Transform global hit pos and normal into station-local space
        Transform3D invTransform = GlobalTransform.AffineInverse();
        Vector3 localPos = invTransform * globalHitPosition;
        Vector3 localNormal = invTransform.Basis * globalHitNormal;
        if (localNormal.LengthSquared() > 0.0001f)
        {
            localNormal = localNormal.Normalized();
        }

        var simLocalPos = new SimVector3(localPos.X, localPos.Y, localPos.Z);
        var simLocalNormal = new SimVector3(localNormal.X, localNormal.Y, localNormal.Z);

        return _grid.ResolveHitCell(simLocalPos, simLocalNormal, out cell, out type);
    }

    /// <summary>
    /// Transforms world raycast hit position and outward normal into station-local SimVector3 coordinates.
    /// </summary>
    public bool TryGetStationLocalRay(
        Vector3 globalHitPosition,
        Vector3 globalHitNormal,
        out SimVector3 localPos,
        out SimVector3 localNormal)
    {
        Transform3D invTransform = GlobalTransform.AffineInverse();
        Vector3 lPos = invTransform * globalHitPosition;
        Vector3 lNorm = invTransform.Basis * globalHitNormal;
        if (lNorm.LengthSquared() > 0.0001f)
        {
            lNorm = lNorm.Normalized();
        }

        localPos = new SimVector3(lPos.X, lPos.Y, lPos.Z);
        localNormal = new SimVector3(lNorm.X, lNorm.Y, lNorm.Z);
        return true;
    }
}
