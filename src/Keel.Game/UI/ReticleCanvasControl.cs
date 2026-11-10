using Godot;
using Keel.Sim.Hud;
using SimVector2 = System.Numerics.Vector2;
using SimVector3 = System.Numerics.Vector3;

namespace Keel.Game.UI;

public partial class ReticleCanvasControl : Control
{
    private static readonly Color AmberPrimary = new(0xFF / 255f, 0x8C / 255f, 0x1A / 255f, 0.9f);
    private static readonly Color AmberRing = new(0x66 / 255f, 0x36 / 255f, 0x08 / 255f, 0.6f);
    private static readonly Color AmberBoresight = new(0xFF / 255f, 0x8C / 255f, 0x1A / 255f, 0.5f);
    private static readonly Color CyanAccent = new(0x38 / 255f, 0xBD / 255f, 0xF8 / 255f, 0.9f);

    public const float RingRadius = 180.0f;

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

        // 1. Central HUD Clamp Ring
        DrawArc(center, RingRadius, 0.0f, Mathf.Tau, 64, AmberRing, 1.5f, antialiased: true);

        // 2. Fixed Center Boresight
        const float boreArm = 8.0f;
        const float boreGap = 4.0f;
        DrawLine(new Vector2(center.X - boreArm - boreGap, center.Y), new Vector2(center.X - boreGap, center.Y), AmberBoresight, 1.5f, antialiased: true);
        DrawLine(new Vector2(center.X + boreGap, center.Y), new Vector2(center.X + boreArm + boreGap, center.Y), AmberBoresight, 1.5f, antialiased: true);
        DrawLine(new Vector2(center.X, center.Y - boreArm - boreGap), new Vector2(center.X, center.Y - boreGap), AmberBoresight, 1.5f, antialiased: true);
        DrawLine(new Vector2(center.X, center.Y + boreGap), new Vector2(center.X, center.Y + boreArm + boreGap), AmberBoresight, 1.5f, antialiased: true);

        // 3. Prograde Marker (Open Circle with 3 radial ticks: top, left, right)
        if (_projection.Prograde.IsVisible)
        {
            var pPos = new Vector2(_projection.Prograde.ScreenPosition.X, _projection.Prograde.ScreenPosition.Y);
            Color pColor = _projection.Prograde.IsClampedToRing ? CyanAccent : AmberPrimary;
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
            Color rColor = _projection.Retrograde.IsClampedToRing ? CyanAccent : AmberPrimary;
            const float rRadius = 7.0f;
            const float d = 4.5f;

            DrawArc(rPos, rRadius, 0.0f, Mathf.Tau, 24, rColor, 1.5f, antialiased: true);
            DrawLine(new Vector2(rPos.X - d, rPos.Y - d), new Vector2(rPos.X + d, rPos.Y + d), rColor, 1.5f, antialiased: true);
            DrawLine(new Vector2(rPos.X - d, rPos.Y + d), new Vector2(rPos.X + d, rPos.Y - d), rColor, 1.5f, antialiased: true);
        }
    }
}
