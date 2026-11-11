using System.Text.Json;

namespace Keel.Sim.Config;

public sealed record TuningLoadResult(
    TuningConfig Config,
    bool IsSuccess,
    string? ErrorMessage);

public static class TuningConfigLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static TuningLoadResult LoadOrFallback(string? json, TuningConfig fallback)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new TuningLoadResult(fallback, IsSuccess: false, "Tuning config JSON is missing or empty.");
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<TuningConfigDto>(json, JsonOptions);
            if (parsed is null)
            {
                return new TuningLoadResult(fallback, IsSuccess: false, "Tuning config JSON deserialized to null.");
            }

            var config = new TuningConfig(
                ShipMass: parsed.ShipMass ?? fallback.ShipMass,
                LinearThrust: parsed.LinearThrust ?? fallback.LinearThrust,
                AngularTorque: parsed.AngularTorque ?? fallback.AngularTorque,
                DampenerStrength: parsed.DampenerStrength ?? fallback.DampenerStrength,
                MaxLinearSpeed: parsed.MaxLinearSpeed ?? fallback.MaxLinearSpeed,
                MaxAngularSpeed: parsed.MaxAngularSpeed ?? fallback.MaxAngularSpeed,
                MouseSensitivity: parsed.MouseSensitivity ?? fallback.MouseSensitivity,
                SuitMass: parsed.SuitMass ?? fallback.SuitMass,
                SuitLinearThrust: parsed.SuitLinearThrust ?? fallback.SuitLinearThrust,
                SuitAngularTorque: parsed.SuitAngularTorque ?? fallback.SuitAngularTorque,
                SuitMaxLinearSpeed: parsed.SuitMaxLinearSpeed ?? fallback.SuitMaxLinearSpeed,
                ShipInteractRange: parsed.ShipInteractRange ?? fallback.ShipInteractRange,
                DrillRange: parsed.DrillRange ?? fallback.DrillRange,
                DrillRate: parsed.DrillRate ?? fallback.DrillRate,
                SuitCargoCapacity: parsed.SuitCargoCapacity ?? fallback.SuitCargoCapacity,
                RefineryCycleSeconds: parsed.RefineryCycleSeconds ?? fallback.RefineryCycleSeconds,
                RefineryOrePerBlock: parsed.RefineryOrePerBlock ?? fallback.RefineryOrePerBlock,
                RefineryInteractRange: parsed.RefineryInteractRange ?? fallback.RefineryInteractRange,
                RefineryBufferCapacity: parsed.RefineryBufferCapacity ?? fallback.RefineryBufferCapacity,
                BuildRange: parsed.BuildRange ?? fallback.BuildRange);

            string? validationError = Validate(config);
            if (validationError is not null)
            {
                return new TuningLoadResult(fallback, IsSuccess: false, validationError);
            }

            return new TuningLoadResult(config, IsSuccess: true, ErrorMessage: null);
        }
        catch (JsonException ex)
        {
            return new TuningLoadResult(fallback, IsSuccess: false, ex.Message);
        }
    }

    private static string? Validate(TuningConfig config)
    {
        if (!IsPositiveFinite(config.ShipMass)) return "shipMass must be a positive finite number.";
        if (!IsPositiveFinite(config.LinearThrust)) return "linearThrust must be a positive finite number.";
        if (!IsPositiveFinite(config.AngularTorque)) return "angularTorque must be a positive finite number.";
        if (!IsPositiveFinite(config.DampenerStrength)) return "dampenerStrength must be a positive finite number.";
        if (!IsPositiveFinite(config.MaxLinearSpeed)) return "maxLinearSpeed must be a positive finite number.";
        if (!IsPositiveFinite(config.MaxAngularSpeed)) return "maxAngularSpeed must be a positive finite number.";
        if (!IsPositiveFinite(config.MouseSensitivity)) return "mouseSensitivity must be a positive finite number.";
        if (!IsPositiveFinite(config.SuitMass)) return "suitMass must be a positive finite number.";
        if (!IsPositiveFinite(config.SuitLinearThrust)) return "suitLinearThrust must be a positive finite number.";
        if (!IsPositiveFinite(config.SuitAngularTorque)) return "suitAngularTorque must be a positive finite number.";
        if (!IsPositiveFinite(config.SuitMaxLinearSpeed)) return "suitMaxLinearSpeed must be a positive finite number.";
        if (!IsPositiveFinite(config.ShipInteractRange)) return "shipInteractRange must be a positive finite number.";
        if (!IsPositiveFinite(config.DrillRange)) return "drillRange must be a positive finite number.";
        if (!IsPositiveFinite(config.DrillRate)) return "drillRate must be a positive finite number.";
        if (!IsPositiveFinite(config.SuitCargoCapacity)) return "suitCargoCapacity must be a positive finite number.";
        if (!IsPositiveFinite(config.RefineryCycleSeconds)) return "refineryCycleSeconds must be a positive finite number.";
        if (!IsPositiveFinite(config.RefineryOrePerBlock) || config.RefineryOrePerBlock < 1.0f) return "refineryOrePerBlock must be >= 1.";
        if (!IsPositiveFinite(config.RefineryInteractRange)) return "refineryInteractRange must be a positive finite number.";
        if (!IsPositiveFinite(config.RefineryBufferCapacity) || config.RefineryBufferCapacity < 1.0f) return "refineryBufferCapacity must be >= 1.";
        if (!IsPositiveFinite(config.BuildRange)) return "buildRange must be a positive finite number.";
        return null;
    }

    private static bool IsPositiveFinite(float value) => float.IsFinite(value) && value > 0f;

    private sealed class TuningConfigDto
    {
        public float? ShipMass { get; init; }
        public float? LinearThrust { get; init; }
        public float? AngularTorque { get; init; }
        public float? DampenerStrength { get; init; }
        public float? MaxLinearSpeed { get; init; }
        public float? MaxAngularSpeed { get; init; }
        public float? MouseSensitivity { get; init; }
        public float? SuitMass { get; init; }
        public float? SuitLinearThrust { get; init; }
        public float? SuitAngularTorque { get; init; }
        public float? SuitMaxLinearSpeed { get; init; }
        public float? ShipInteractRange { get; init; }
        public float? DrillRange { get; init; }
        public float? DrillRate { get; init; }
        public float? SuitCargoCapacity { get; init; }
        public float? RefineryCycleSeconds { get; init; }
        public float? RefineryOrePerBlock { get; init; }
        public float? RefineryInteractRange { get; init; }
        public float? RefineryBufferCapacity { get; init; }
        public float? BuildRange { get; init; }
    }
}