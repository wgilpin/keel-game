using Godot;
using Keel.Sim.Hud;
using SimVector2 = System.Numerics.Vector2;
using SimVector3 = System.Numerics.Vector3;

namespace Keel.Game.UI;

public partial class ReticleCanvasControl : Control
{
    public enum ReticleColorTheme
    {
        Amber,
        Cyan,
    }

    private static readonly Color AmberPrimary = new(0xFF / 255f, 0x8C / 255f, 0x1A / 255f, 0.9f);
    private static readonly Color AmberRing = new(0x66 / 255f, 0x36 / 255f, 0x08 / 255f, 0.6f);
    private static readonly Color AmberBoresight = new(0xFF / 255f, 0x8C / 255f, 0x1A / 255f, 0.5f);
    private static readonly Color AmberAccent = new(0xF5 / 255f, 0x9E / 255f, 0x0B / 255f, 0.9f);

    private static readonly Color CyanPrimary = new(0x38 / 255f, 0xBD / 255f, 0xF8 / 255f, 0.9f);
    private static readonly Color CyanRing = new(0x2B / 255f, 0x42 / 255f, 0x57 / 255f, 0.6f);
    private static readonly Color CyanBoresight = new(0x38 / 255f, 0xBD / 255f, 0xF8 / 255f, 0.5f);
    private static readonly Color CyanAccent = new(0x38 / 255f, 0xBD / 255f, 0xF8 / 255f, 0.9f);

    public const float RingRadius = 180.0f;

    public ReticleColorTheme ColorTheme { get; set; } = ReticleColorTheme.Amber;
    public new ReticleColorTheme Theme { get => ColorTheme; set => ColorTheme = value; }

    public float ExtractionProgress { get; set; } = 0.0f;
    public bool IsExtracting { get; set; } = false;
    public bool IsTargetOutOfRange { get; set; } = false;

    private ReticleProjectionResult _projection;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsPreset(LayoutPreset.FullRect);
    }

    public void UpdateProjection(SimVector3 shipLocalVelocity, float fovDegrees)
    {
        Vector2 viewport = GetViewportRect().Size;
        var simViewport = new SimVector2(viewport.X, viewport.Y);
        _projection = CockpitReticleProjector.Project(shipLocalVelocity, simViewport, fovDegrees, RingRadius);
        QueueRedraw();
    }

    public override void _Draw()
    {
        Vector2 center = Size * 0.5f;

        Color primary = Theme == ReticleColorTheme.Cyan ? CyanPrimary : AmberPrimary;
        Color ringColor = Theme == ReticleColorTheme.Cyan ? CyanRing : AmberRing;
        Color boresightColor = Theme == ReticleColorTheme.Cyan ? CyanBoresight : AmberBoresight;
        Color clampedColor = Theme == ReticleColorTheme.Cyan ? AmberAccent : CyanAccent;

        if (IsTargetOutOfRange)
        {
            primary.A *= 0.4f;
            ringColor.A *= 0.4f;
            boresightColor.A *= 0.4f;
            clampedColor.A *= 0.4f;
        }

        // 1. Central HUD Clamp Ring
        DrawArc(center, RingRadius, 0.0f, Mathf.Tau, 64, ringColor, 1.5f, antialiased: true);

        // 2. Fixed Center Boresight
        const float boreArm = 8.0f;
        const float boreGap = 4.0f;
        DrawLine(new Vector2(center.X - boreArm - boreGap, center.Y), new Vector2(center.X - boreGap, center.Y), boresightColor, 1.5f, antialiased: true);
        DrawLine(new Vector2(center.X + boreGap, center.Y), new Vector2(center.X + boreArm + boreGap, center.Y), boresightColor, 1.5f, antialiased: true);
        DrawLine(new Vector2(center.X, center.Y - boreArm - boreGap), new Vector2(center.X, center.Y - boreGap), boresightColor, 1.5f, antialiased: true);
        DrawLine(new Vector2(center.X, center.Y + boreGap), new Vector2(center.X, center.Y + boreArm + boreGap), boresightColor, 1.5f, antialiased: true);

        // 2b. Amber Extraction Progress Arc around center boresight
        if (IsExtracting && ExtractionProgress > 0.0f)
        {
            const float arcRadius = 24.0f;
            float startAngle = -Mathf.Pi * 0.5f;
            float sweepAngle = Mathf.Clamp(ExtractionProgress, 0.0f, 1.0f) * Mathf.Tau;
            DrawArc(center, arcRadius, startAngle, startAngle + sweepAngle, 36, AmberAccent, 2.5f, antialiased: true);
        }

        // 3. Prograde Marker (Open Circle with 3 radial ticks: top, left, right)
        if (_projection.Prograde.IsVisible)
        {
            var pPos = new Vector2(_projection.Prograde.ScreenPosition.X, _projection.Prograde.ScreenPosition.Y);
            Color pColor = _projection.Prograde.IsClampedToRing ? clampedColor : primary;
            const float pRadius = 7.0f;

            DrawArc(pPos, pRadius, 0.0f, Mathf.Tau, 24, pColor, 1.5f, antialiased: true);
            DrawLine(new Vector2(pPos.X, pPos.Y - pRadius), new Vector2(pPos.X, pPos.Y - pRadius - 4.0f), pColor, 1.5f, antialiased: true);
            DrawLine(new Vector2(pPos.X - pRadius, pPos.Y), new Vector2(pPos.X - pRadius - 4.0f, pPos.Y), pColor, 1.5f, antialiased: true);
            DrawLine(new Vector2(pPos.X + pRadius, pPos.Y), new Vector2(pPos.X + pRadius + 4.0f, pPos.Y), pColor, 1.5f, antialiased: true);
        }

        // 4. Retrograde Marker (Circle with Crossed X)
        if (_projection.Retrograde.IsVisible)
        {
            var rPos = new Vector2(_projection.Retrograde.ScreenPosition.X, _projection.Retrograde.ScreenPosition.Y);
            Color rColor = _projection.Retrograde.IsClampedToRing ? clampedColor : primary;
            const float rRadius = 7.0f;
            const float d = 4.5f;

            DrawArc(rPos, rRadius, 0.0f, Mathf.Tau, 24, rColor, 1.5f, antialiased: true);
            DrawLine(new Vector2(rPos.X - d, rPos.Y - d), new Vector2(rPos.X + d, rPos.Y + d), rColor, 1.5f, antialiased: true);
            DrawLine(new Vector2(rPos.X - d, rPos.Y + d), new Vector2(rPos.X + d, rPos.Y - d), rColor, 1.5f, antialiased: true);
        }
    }
}
