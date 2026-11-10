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
        }
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
