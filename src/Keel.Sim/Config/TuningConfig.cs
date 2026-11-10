namespace Keel.Sim.Config;

public sealed record TuningConfig(
    float ShipMass,
    float LinearThrust,
    float AngularTorque,
    float DampenerStrength,
    float MaxLinearSpeed,
    float MaxAngularSpeed,
    float MouseSensitivity,
    float SuitMass = 95.0f,
    float SuitLinearThrust = 1400.0f,
    float SuitAngularTorque = 220.0f,
    float SuitMaxLinearSpeed = 25.0f,
    float ShipInteractRange = 6.0f,
    float DrillRange = 5.0f,
    float DrillRate = 1.5f,
    float SuitCargoCapacity = 50.0f,
    float RefineryCycleSeconds = 2.0f,
    float RefineryOrePerBlock = 2.0f,
    float RefineryInteractRange = 5.0f,
    float RefineryBufferCapacity = 100.0f)
{
    public static TuningConfig Default { get; } = new(
        ShipMass: 1200.0f,
        LinearThrust: 24000.0f,
        AngularTorque: 4800.0f,
        DampenerStrength: 4.0f,
        MaxLinearSpeed: 80.0f,
        MaxAngularSpeed: 2.5f,
        MouseSensitivity: 0.025f,
        SuitMass: 95.0f,
        SuitLinearThrust: 1400.0f,
        SuitAngularTorque: 220.0f,
        SuitMaxLinearSpeed: 25.0f,
        ShipInteractRange: 6.0f,
        DrillRange: 5.0f,
        DrillRate: 1.5f,
        SuitCargoCapacity: 50.0f,
        RefineryCycleSeconds: 2.0f,
        RefineryOrePerBlock: 2.0f,
        RefineryInteractRange: 5.0f,
        RefineryBufferCapacity: 100.0f);
}