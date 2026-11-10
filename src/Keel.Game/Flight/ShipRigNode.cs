using System;
using Godot;
using Keel.Sim.Arena;
using Keel.Sim.Config;
using Keel.Sim.Flight;
using SimVector3 = System.Numerics.Vector3;

namespace Keel.Game.Flight;

public partial class ShipRigNode : RigidBody3D
{
    public Camera3D CockpitCamera { get; private set; } = null!;
    public bool DampenersEnabled { get; set; } = true;

    public override void _Ready()
    {
        GravityScale = 0.0f;
        LinearDampMode = DampMode.Replace;
        AngularDampMode = DampMode.Replace;
        LinearDamp = 0.0f;
        AngularDamp = 0.0f;
        CanSleep = false;

        var hullShape = new CollisionShape3D
        {
            Shape = new BoxShape3D { Size = new Vector3(3.0f, 1.8f, 4.0f) },
        };
        AddChild(hullShape);

        CockpitCamera = new Camera3D
        {
            Fov = 75.0f,
            Near = 0.1f,
            Far = 30000.0f,
            Current = true,
            Position = new Vector3(0.0f, 0.35f, -0.6f),
        };
        CockpitCamera.AddChild(CockpitCanopyBuilder.CreateCanopy());
        AddChild(CockpitCamera);

        SyncMassAndInertia(TuningConfig.Default.ShipMass);
    }

    public void ApplyFlightStep(in FlightInput input, TuningConfig tuning, float deltaSeconds)
    {
        SyncMassAndInertia(tuning.ShipMass);

        Basis worldToLocal = GlobalBasis.Transposed();
        Vector3 localLin = worldToLocal * LinearVelocity;
        Vector3 localAng = worldToLocal * AngularVelocity;

        var state = new FlightState(
            ToSim(localLin),
            ToSim(localAng),
            DampenersEnabled);

        FlightStepResult step = FlightKinematics.Step(in state, in input, tuning, deltaSeconds);

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
