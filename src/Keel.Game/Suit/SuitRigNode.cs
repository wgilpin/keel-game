using System;
using Godot;
using Keel.Sim.Arena;
using Keel.Sim.Config;
using Keel.Sim.Flight;
using SimVector3 = System.Numerics.Vector3;

namespace Keel.Game.Suit;

public partial class SuitRigNode : RigidBody3D
{
    public Camera3D SuitCamera { get; private set; } = null!;
    public bool DampenersEnabled { get; set; } = true;

    private MeshInstance3D _drillBeamInstance = null!;
    private ImmediateMesh _drillBeamMesh = null!;
    private StandardMaterial3D _drillBeamMaterial = null!;

    public override void _Ready()
    {
        GravityScale = 0.0f;
        LinearDampMode = DampMode.Replace;
        AngularDampMode = DampMode.Replace;
        LinearDamp = 0.0f;
        AngularDamp = 0.0f;
        CanSleep = false;

        var suitShape = new CollisionShape3D
        {
            Shape = new CapsuleShape3D { Radius = 0.4f, Height = 1.8f },
        };
        AddChild(suitShape);

        SuitCamera = new Camera3D
        {
            Fov = 75.0f,
            Near = 0.05f,
            Far = 30000.0f,
            Current = false,
            Position = new Vector3(0.0f, 0.5f, 0.0f),
        };
        SuitCamera.AddChild(SuitToolMeshBuilder.CreateToolMesh());
        AddChild(SuitCamera);

        _drillBeamMaterial = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = new Color(0xF5 / 255f, 0x9E / 255f, 0x0B / 255f, 1.0f),
        };
        _drillBeamMesh = new ImmediateMesh();
        _drillBeamInstance = new MeshInstance3D
        {
            Mesh = _drillBeamMesh,
            TopLevel = true,
            Visible = false,
        };
        AddChild(_drillBeamInstance);

        SyncMassAndInertia(TuningConfig.Default.SuitMass);
        SetActive(false);
    }

    public void Spawn(Vector3 worldPosition, Godot.Quaternion worldOrientation, Vector3 linearVelocity, Vector3 angularVelocity)
    {
        GlobalPosition = worldPosition;
        GlobalBasis = new Basis(worldOrientation);
        LinearVelocity = linearVelocity;
        AngularVelocity = angularVelocity;
    }

    public void SetActive(bool active)
    {
        Visible = active;
        ProcessMode = active ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
        SuitCamera.Current = active;
        if (!active)
        {
            LinearVelocity = Vector3.Zero;
            AngularVelocity = Vector3.Zero;
            SetDrillBeam(false, Vector3.Zero);
        }
    }

    public void SetDrillBeam(bool active, Vector3 worldHitPoint)
    {
        if (_drillBeamInstance == null)
        {
            return;
        }

        if (!active)
        {
            _drillBeamInstance.Visible = false;
            return;
        }

        Vector3 toolMuzzle = SuitCamera.GlobalPosition + (SuitCamera.GlobalBasis * new Vector3(0.25f, -0.2f, -0.4f));
        _drillBeamMesh.ClearSurfaces();
        _drillBeamMesh.SurfaceBegin(Mesh.PrimitiveType.Lines, _drillBeamMaterial);
        _drillBeamMesh.SurfaceAddVertex(toolMuzzle);
        _drillBeamMesh.SurfaceAddVertex(worldHitPoint);
        _drillBeamMesh.SurfaceEnd();
        _drillBeamInstance.Visible = true;
    }

    public void ApplyFlightStep(in FlightInput input, TuningConfig tuning, float deltaSeconds)
    {
        SyncMassAndInertia(tuning.SuitMass);

        Basis worldToLocal = GlobalBasis.Transposed();
        Vector3 localLin = worldToLocal * LinearVelocity;
        Vector3 localAng = worldToLocal * AngularVelocity;

        var state = new FlightState(
            ToSim(localLin),
            ToSim(localAng),
            DampenersEnabled);

        FlightStepResult step = FlightKinematics.StepSuit(in state, in input, tuning, deltaSeconds);

        ApplyCentralForce(GlobalBasis * ToGodot(step.Force));
        ApplyTorque(GlobalBasis * ToGodot(step.Torque));

        var boundEval = ArenaBoundary.Evaluate(ToSim(GlobalPosition), ToSim(LinearVelocity));
        if (boundEval.IsAtOrBeyondBoundary)
        {
            GlobalPosition = ToGodot(boundEval.Position);
            LinearVelocity = ToGodot(boundEval.Velocity);
        }
    }

    private void SyncMassAndInertia(float configuredMass)
    {
        if (configuredMass <= 0f)
        {
            return;
        }

        if (MathF.Abs(Mass - configuredMass) > 1e-3f)
        {
            Mass = configuredMass;
            Inertia = new Vector3(configuredMass, configuredMass, configuredMass);
        }
    }

    private static SimVector3 ToSim(Vector3 v) => new(v.X, v.Y, v.Z);
    private static Vector3 ToGodot(SimVector3 v) => new(v.X, v.Y, v.Z);
}
