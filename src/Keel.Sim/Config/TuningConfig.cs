namespace Keel.Sim.Config;

public sealed record TuningConfig(
    float ShipMass,
    float LinearThrust,
    float AngularTorque,
    float DampenerStrength,
    float MaxLinearSpeed,
    float MaxAngularSpeed,
    float MouseSensitivity)
{
    public static TuningConfig Default { get; } = new(
        ShipMass: 1200.0f,
        LinearThrust: 24000.0f,
        AngularTorque: 4800.0f,
        DampenerStrength: 4.0f,
        MaxLinearSpeed: 80.0f,
        MaxAngularSpeed: 2.5f,
        MouseSensitivity: 0.0025f);
}
