using Godot;

namespace Keel.Game.World;

public static class SpaceEnvironmentBuilder
{
    private static readonly Color SpaceBackdrop = new(0x05 / 255f, 0x07 / 255f, 0x0B / 255f, 1.0f);
    private static readonly Color AmbientSkyColor = new(0x1E / 255f, 0x29 / 255f, 0x3B / 255f, 1.0f);
    private static readonly Color SunLightColor = new(0xFF / 255f, 0xF6 / 255f, 0xE5 / 255f, 1.0f);

    public static WorldEnvironment CreateEnvironment()
    {
        var env = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Color,
            BackgroundColor = SpaceBackdrop,
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = AmbientSkyColor,
            AmbientLightEnergy = 0.35f,
        };

        return new WorldEnvironment { Environment = env };
    }

    public static DirectionalLight3D CreateSunlight()
    {
        var sun = new DirectionalLight3D
        {
            LightColor = SunLightColor,
            LightEnergy = 1.2f,
            ShadowEnabled = false,
        };

        // Angled down and across the scene for readable surface relief.
        sun.RotationDegrees = new Vector3(-35.0f, -40.0f, 0.0f);
        return sun;
    }
}
