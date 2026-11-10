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
                MouseSensitivity: parsed.MouseSensitivity ?? fallback.MouseSensitivity);

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
    }
}
