using Godot;
using Keel.Sim.Telemetry;

namespace Keel.Game.UI;

public partial class DebugOverlayNode : CanvasLayer
{
    private PanelContainer _panel = null!;
    private Label _label = null!;
    private StyleBoxFlat _boxStyle = null!;

    private static readonly Color BackingColor = new(0x10 / 255f, 0x1A / 255f, 0x26 / 255f, 0.82f);
    private static readonly Color BorderNormal = new(0x2B / 255f, 0x42 / 255f, 0x57 / 255f, 1.0f);
    private static readonly Color BorderReload = new(0x38 / 255f, 0xBD / 255f, 0xF8 / 255f, 1.0f);
    private static readonly Color BorderError = new(0xEF / 255f, 0x44 / 255f, 0x44 / 255f, 1.0f);
    private static readonly Color TextPrimary = new(0xE6 / 255f, 0xED / 255f, 0xF3 / 255f, 1.0f);

    public override void _Ready()
    {
        Layer = 20;

        _panel = new PanelContainer
        {
            Position = new Vector2(16, 16),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };

        _boxStyle = new StyleBoxFlat
        {
            BgColor = BackingColor,
            BorderColor = BorderNormal,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 2,
            CornerRadiusTopRight = 2,
            CornerRadiusBottomLeft = 2,
            CornerRadiusBottomRight = 2,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 8,
            ContentMarginBottom = 8,
        };
        _panel.AddThemeStyleboxOverride("panel", _boxStyle);

        var monoFont = new SystemFont
        {
            FontNames = new[] { "JetBrains Mono", "Consolas", "Courier New" },
        };

        _label = new Label();
        _label.AddThemeFontOverride("font", monoFont);
        _label.AddThemeFontSizeOverride("font_size", 12);
        _label.AddThemeColorOverride("font_color", TextPrimary);

        _panel.AddChild(_label);
        AddChild(_panel);
    }

    public void ToggleVisibility()
    {
        _panel.Visible = !_panel.Visible;
    }

    public void Render(in DebugTelemetrySnapshot snapshot)
    {
        _label.Text = DebugTelemetryFormatter.Format(in snapshot);
        _boxStyle.BorderColor = !string.IsNullOrEmpty(snapshot.LastConfigError)
            ? BorderError
            : snapshot.IsReloadFlashActive
                ? BorderReload
                : BorderNormal;
    }
}
