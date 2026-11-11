using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;
using Keel.Sim.Config;
using Keel.Sim.Hud;
using SimVector3 = System.Numerics.Vector3;

namespace Keel.Game.UI;

public partial class CockpitHudNode : CanvasLayer
{
    private static readonly Color AmberBg = new(0x12 / 255f, 0x0C / 255f, 0x06 / 255f, 0.82f);
    private static readonly Color AmberBorder = new(0x66 / 255f, 0x36 / 255f, 0x08 / 255f, 1.0f);
    private static readonly Color AmberPrimary = new(0xFF / 255f, 0x8C / 255f, 0x1A / 255f, 1.0f);
    private static readonly Color AmberMuted = new(0xA3 / 255f, 0x6A / 255f, 0x30 / 255f, 1.0f);
    private static readonly Color AmberAccent = new(0x38 / 255f, 0xBD / 255f, 0xF8 / 255f, 1.0f);
    private static readonly Color AmberDanger = new(0xEF / 255f, 0x44 / 255f, 0x44 / 255f, 1.0f);

    private ReticleCanvasControl _reticle = null!;
    private BearingTapeControl _bearingTape = null!;

    // Panels
    private Label _contactsLabel = null!;
    private Label _speedValueLabel = null!;
    private ProgressBar _speedBar = null!;
    private Label _speedCapLabel = null!;
    private Label _systemsLabel = null!;
    private Label _infoLogLabel = null!;

    private readonly HudInfoLog _infoLog = new();

    public HudInfoLog InfoLog => _infoLog;

    public override void _Ready()
    {
        Layer = 10;

        _reticle = new ReticleCanvasControl();
        AddChild(_reticle);

        var monoFont = new SystemFont
        {
            FontNames = new[] { "JetBrains Mono", "Consolas", "Courier New" },
        };

        // Root container for HUD panels
        var rootControl = new Control
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        rootControl.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(rootControl);

        // 0. Top-Center Bearing Tape (480px, clear of top-left debug overlay)
        _bearingTape = new BearingTapeControl();
        _bearingTape.SetAnchorsPreset(Control.LayoutPreset.CenterTop);
        _bearingTape.Position = new Vector2(-240, 24);
        rootControl.AddChild(_bearingTape);

        // 1. Bottom-Left Contacts Panel
        var contactsPanel = CreateStyledPanel(new Vector2(240, 100));
        contactsPanel.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
        contactsPanel.Position = new Vector2(32, -132);

        var contactsBox = new VBoxContainer();
        contactsPanel.AddChild(contactsBox);

        var contactsHeader = CreateLabel("CONTACTS", monoFont, 12, AmberMuted);
        contactsBox.AddChild(contactsHeader);

        _contactsLabel = CreateLabel("STATION   --\nASTEROID  --", monoFont, 14, AmberPrimary);
        contactsBox.AddChild(_contactsLabel);
        rootControl.AddChild(contactsPanel);

        // 2. Bottom-Center Speed Console
        var speedPanel = CreateStyledPanel(new Vector2(220, 100));
        speedPanel.SetAnchorsPreset(Control.LayoutPreset.CenterBottom);
        speedPanel.Position = new Vector2(-110, -132);

        var speedBox = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        speedPanel.AddChild(speedBox);

        _speedValueLabel = CreateLabel("0.0 m/s", monoFont, 24, AmberPrimary);
        _speedValueLabel.HorizontalAlignment = HorizontalAlignment.Center;
        speedBox.AddChild(_speedValueLabel);

        _speedBar = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 100,
            Value = 0,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(190, 8),
        };
        var barFill = new StyleBoxFlat { BgColor = AmberPrimary, CornerRadiusTopLeft = 1, CornerRadiusTopRight = 1, CornerRadiusBottomLeft = 1, CornerRadiusBottomRight = 1 };
        var barBg = new StyleBoxFlat { BgColor = AmberBg, BorderColor = AmberBorder, BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1 };
        _speedBar.AddThemeStyleboxOverride("fill", barFill);
        _speedBar.AddThemeStyleboxOverride("background", barBg);
        speedBox.AddChild(_speedBar);

        _speedCapLabel = CreateLabel("MAX 80.0 m/s", monoFont, 12, AmberMuted);
        _speedCapLabel.HorizontalAlignment = HorizontalAlignment.Center;
        speedBox.AddChild(_speedCapLabel);
        rootControl.AddChild(speedPanel);

        // 3. Bottom-Right Systems Panel
        var systemsPanel = CreateStyledPanel(new Vector2(240, 100));
        systemsPanel.SetAnchorsPreset(Control.LayoutPreset.BottomRight);
        systemsPanel.Position = new Vector2(-272, -132);

        var systemsBox = new VBoxContainer();
        systemsPanel.AddChild(systemsBox);

        var systemsHeader = CreateLabel("SYSTEMS", monoFont, 12, AmberMuted);
        systemsBox.AddChild(systemsHeader);

        _systemsLabel = CreateLabel("[■] DAMPENERS [Z]\n[■] PROXIMITY\n[■] ARENA BUBBLE", monoFont, 14, AmberPrimary);
        systemsBox.AddChild(_systemsLabel);
        rootControl.AddChild(systemsPanel);

        // 4. Top-Right INFO Feed
        var infoPanel = CreateStyledPanel(new Vector2(300, 90));
        infoPanel.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        infoPanel.Position = new Vector2(-332, 24);

        var infoBox = new VBoxContainer();
        infoPanel.AddChild(infoBox);

        var infoHeader = CreateLabel("INFO LOG", monoFont, 12, AmberMuted);
        infoBox.AddChild(infoHeader);

        _infoLogLabel = CreateLabel("SYS: FLIGHT ONLINE", monoFont, 14, AmberPrimary);
        infoBox.AddChild(_infoLogLabel);
        rootControl.AddChild(infoPanel);

        _infoLog.Push("SYS: FLIGHT COMPUTER ONLINE");
        _infoLog.Push("NAV: 20 KM ARENA INITIALIZED");
    }

    public void UpdateHud(
        SimVector3 shipLocalVelocity,
        SimVector3 shipWorldPosition,
        float currentSpeed,
        TuningConfig tuning,
        bool dampenersEnabled,
        bool isAtBoundary,
        float cameraFov)
    {
        var inv = CultureInfo.InvariantCulture;

        // 0. Bearing tape
        Camera3D? cam = GetViewport()?.GetCamera3D();
        var camForward = cam != null
            ? new SimVector3(-cam.GlobalBasis.Z.X, -cam.GlobalBasis.Z.Y, -cam.GlobalBasis.Z.Z)
            : -SimVector3.UnitZ;
        var camRight = cam != null
            ? new SimVector3(cam.GlobalBasis.X.X, cam.GlobalBasis.X.Y, cam.GlobalBasis.X.Z)
            : SimVector3.UnitX;

        IReadOnlyList<BearingMarkerResult> bearings = CockpitContactsModel.ComputeBearings(
            shipWorldPosition,
            camForward,
            camRight);

        float heading = VisorBearingTapeModel.ComputeHeadingDegrees(camForward);
        IReadOnlyList<CompassTick> ticks = VisorBearingTapeModel.ComputeTicks(heading);
        _bearingTape.UpdateData(ticks, bearings);

        // 1. Reticle projection
        _reticle.UpdateProjection(shipLocalVelocity, cameraFov);

        // 2. Contacts
        IReadOnlyList<LandmarkContact> contacts = CockpitContactsModel.Compute(shipWorldPosition);
        if (contacts.Count >= 2)
        {
            _contactsLabel.Text = $"{contacts[0].Name,-9} {contacts[0].FormattedDistance,7}\n{contacts[1].Name,-9} {contacts[1].FormattedDistance,7}";
        }

        // 3. Speed & Throttle
        _speedValueLabel.Text = string.Create(inv, $"{currentSpeed,5:F1} m/s");
        _speedBar.MaxValue = tuning.MaxLinearSpeed;
        _speedBar.Value = currentSpeed;
        _speedCapLabel.Text = string.Create(inv, $"MAX {tuning.MaxLinearSpeed:F1} m/s");

        // 4. Systems Status
        string dmpCheckbox = dampenersEnabled ? "[■] DAMPENERS [Z]" : "[ ] DAMPENERS [Z]";
        string bubbleCheckbox = isAtBoundary ? "[!] ARENA BUBBLE" : "[■] ARENA BUBBLE";
        _systemsLabel.Text = $"{dmpCheckbox}\n[■] PROXIMITY\n{bubbleCheckbox}";

        // 5. Info Log Feed
        if (_infoLog.Entries.Count > 0)
        {
            _infoLogLabel.Text = string.Join("\n", _infoLog.Entries);
        }
    }

    private static PanelContainer CreateStyledPanel(Vector2 minSize)
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = minSize,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };

        var boxStyle = new StyleBoxFlat
        {
            BgColor = AmberBg,
            BorderColor = AmberBorder,
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
        panel.AddThemeStyleboxOverride("panel", boxStyle);
        return panel;
    }

    private static Label CreateLabel(string text, Font font, int fontSize, Color color)
    {
        var label = new Label
        {
            Text = text,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        label.AddThemeFontOverride("font", font);
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }
}
