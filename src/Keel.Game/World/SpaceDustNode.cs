using System;
using Godot;
using Keel.Sim.Arena;
using SimVector3 = System.Numerics.Vector3;

namespace Keel.Game.World;

public partial class SpaceDustNode : MultiMeshInstance3D
{
    private static readonly Color DustColor = new(0xCB / 255f, 0xD5 / 255f, 0xE1 / 255f, 0.35f);

    private readonly SpaceDustField _field;
    private readonly SimVector3[] _positionBuffer;
    private MultiMesh _multiMesh = null!;

    public SpaceDustNode(int particleCount = SpaceDustField.DefaultParticleCount, float boxSize = SpaceDustField.DefaultBoxSize)
    {
        _field = new SpaceDustField(particleCount, boxSize);
        _positionBuffer = new SimVector3[particleCount];
    }

    public override void _Ready()
    {
        var material = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = DustColor,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        };

        var particleMesh = new BoxMesh
        {
            Size = new Vector3(0.04f, 0.04f, 0.04f),
            Material = material,
        };

        _multiMesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            InstanceCount = _field.ParticleCount,
            Mesh = particleMesh,
        };

        Multimesh = _multiMesh;
        TopLevel = true; // Use global world coordinates for instances
    }

    public void UpdateDust(Vector3 cameraGlobalPosition)
    {
        var camSim = new SimVector3(cameraGlobalPosition.X, cameraGlobalPosition.Y, cameraGlobalPosition.Z);
        _field.WrapToCameraBox(camSim, _positionBuffer);

        for (int i = 0; i < _positionBuffer.Length; i++)
        {
            SimVector3 p = _positionBuffer[i];
            var transform = new Transform3D(Basis.Identity, new Vector3(p.X, p.Y, p.Z));
            _multiMesh.SetInstanceTransform(i, transform);
        }
    }
}
