using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;
using Keel.Sim.Config;
using Keel.Sim.Hud;
using Keel.Sim.Mining;
using SimVector3 = System.Numerics.Vector3;

namespace Keel.Game.UI;

public partial class SuitVisorHudNode : CanvasLayer
{
    private static readonly Color VisorBg = new(0x10 / 255f, 0x1A / 255f, 0x26 / 255f, 0.82f);
    private static readonly Color VisorSurface = new(0x16 / 255f, 0x22 / 255f, 0x2F / 255f, 0.92f);
    private static readonly Color VisorBorder = new(0x2B / 255f, 0x42 / 255f, 0x57 / 255f, 1.0f);
    private static readonly Color VisorText = new(0xE6 / 255f, 0xED / 255f, 0xF3 / 255f, 1.0f);
    private static readonly Color VisorMuted = new(0x8B / 255f, 0x9E / 255f, 0xB0 / 255f, 1.0f);
    private static readonly Color VisorPrimary = new(0x38 / 255f, 0xBD / 255f, 0xF8 / 255f, 1.0f);
    private static readonly Color VisorWarning = new(0xF5 / 255f, 0x9E / 255f, 0x0B / 255f, 1.0f);
    private static readonly Color VisorDanger = new(0xEF / 255f, 0x44 / 255f, 0x44 / 255f, 1.0f);

    private ReticleCanvasControl _reticle = null!;
    private BearingTapeControl _bearingTape = null!;

    // Status stack
    private Label _speedValueLabel = null!;
    private Label _dampenerBadge = null!;
    private Label _cargoLabel = null!;
    private ProgressBar _cargoBar = null!;

    // Context prompt
    private Label _contextPromptLabel = null!;

    // Hotbar slots
    private readonly PanelContainer[] _slotPanels = new PanelContainer[5];
    private readonly Label[] _slotLabels = new Label[5];
    private readonly Label[] _slotCountLabels = new Label[5];

    private int _currentSlot = SuitVisorModel.DefaultSlot;

    public int CurrentActiveSlot => _currentSlot;

    public override void _Ready()
    {
        Layer = 10;

        _reticle = new ReticleCanvasControl
        {
            ColorTheme = ReticleCanvasControl.ReticleColorTheme.Cyan,
        };
        AddChild(_reticle);

        var monoFont = new SystemFont
        {
            FontNames = new[] { "JetBrains Mono", "Consolas", "Courier New" },
        };

        var rootControl = new Control
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        rootControl.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(rootControl);

        // 1. Top-Center Bearing Tape (480px, clear of top-left debug overlay)
        _bearingTape = new BearingTapeControl();
        _bearingTape.SetAnchorsPreset(Control.LayoutPreset.CenterTop);
        _bearingTape.Position = new Vector2(-240, 24);
        rootControl.AddChild(_bearingTape);

        // 2. Bottom-Left Suit Status Stack
        var statusPanel = CreateStyledPanel(new Vector2(230, 110), VisorBg, VisorBorder);
        statusPanel.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
        statusPanel.Position = new Vector2(32, -142);

        var statusBox = new VBoxContainer();
        statusPanel.AddChild(statusBox);

        var statusHeader = CreateLabel("SUIT TELEMETRY", monoFont, 11, VisorMuted);
        statusBox.AddChild(statusHeader);

        _speedValueLabel = CreateLabel("0.0 m/s", monoFont, 22, VisorPrimary);
        statusBox.AddChild(_speedValueLabel);

        _dampenerBadge = CreateLabel("Z: ON", monoFont, 13, VisorPrimary);
        statusBox.AddChild(_dampenerBadge);

        _cargoLabel = CreateLabel("CARGO 0 / 50", monoFont, 12, VisorText);
        statusBox.AddChild(_cargoLabel);

        _cargoBar = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 50,
            Value = 0,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(200, 6),
        };
        var barFill = new StyleBoxFlat { BgColor = VisorPrimary, CornerRadiusTopLeft = 1, CornerRadiusTopRight = 1, CornerRadiusBottomLeft = 1, CornerRadiusBottomRight = 1 };
        var barBg = new StyleBoxFlat { BgColor = VisorSurface, BorderColor = VisorBorder, BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1 };
        _cargoBar.AddThemeStyleboxOverride("fill", barFill);
        _cargoBar.AddThemeStyleboxOverride("background", barBg);
        statusBox.AddChild(_cargoBar);

        rootControl.AddChild(statusPanel);

        // 3. Bottom-Center Contextual Boarding Prompt & 5-Slot Hotbar
        var hotbarRoot = new VBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        hotbarRoot.SetAnchorsPreset(Control.LayoutPreset.CenterBottom);
        hotbarRoot.Position = new Vector2(-160, -100);

        _contextPromptLabel = CreateLabel("[F] Board Ship", monoFont, 14, VisorPrimary);
        _contextPromptLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _contextPromptLabel.Visible = false;
        hotbarRoot.AddChild(_contextPromptLabel);

        var hotbarRow = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        hotbarRow.AddThemeConstantOverride("separation", 8);

        for (int i = 0; i < 5; i++)
        {
            int slotNumber = i + 1;
            var slotPanel = new PanelContainer
            {
                CustomMinimumSize = new Vector2(56, 56),
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };

            var slotInner = new VBoxContainer
            {
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            slotPanel.AddChild(slotInner);

            var keyLabel = CreateLabel($"[{slotNumber}]", monoFont, 11, VisorMuted);
            slotInner.AddChild(keyLabel);

            string initialName = slotNumber == 1 ? "Drill" : (slotNumber == 2 ? "Steel" : "");
            Color initialColor = slotNumber == 1 ? VisorPrimary : VisorMuted;
            var nameLabel = CreateLabel(initialName, monoFont, 11, initialColor);
            nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
            slotInner.AddChild(nameLabel);

            var countLabel = CreateLabel(slotNumber == 2 ? "0" : "", monoFont, 10, VisorMuted);
            countLabel.HorizontalAlignment = HorizontalAlignment.Right;
            slotInner.AddChild(countLabel);

            _slotPanels[i] = slotPanel;
            _slotLabels[i] = nameLabel;
            _slotCountLabels[i] = countLabel;

            UpdateSlotStyle(i, isSelected: slotNumber == 1);
            hotbarRow.AddChild(slotPanel);
        }

        hotbarRoot.AddChild(hotbarRow);
        rootControl.AddChild(hotbarRoot);
    }

    public void UpdateHud(
        SimVector3 suitLocalVelocity,
        SimVector3 suitWorldPosition,
        SimVector3 cameraForward,
        SimVector3 cameraRight,
        SimVector3 shipWorldPosition,
        float currentSpeed,
        bool dampenersEnabled,
        float cameraFov,
        TuningConfig tuning,
        int? slotInput,
        int cargoCount = 0,
        int steelBlockCount = 0,
        DrillStatus drillStatus = DrillStatus.Idle,
        float drillProgress = 0.0f,
        bool isAimingAtDeposit = false)
    {
        var inv = CultureInfo.InvariantCulture;

        // 1. Reticle vector pips & extraction arc
        _reticle.ExtractionProgress = drillProgress;
        _reticle.IsExtracting = drillStatus == DrillStatus.Drilling;
        _reticle.IsTargetOutOfRange = drillStatus == DrillStatus.OutOfRange && isAimingAtDeposit;
        _reticle.UpdateProjection(suitLocalVelocity, cameraFov);

        // 2. Bearing tape
        IReadOnlyList<BearingMarkerResult> markers = VisorBearingTapeModel.Compute(
            suitWorldPosition,
            cameraForward,
            cameraRight,
            shipWorldPosition);

        float heading = VisorBearingTapeModel.ComputeHeadingDegrees(cameraForward);
        IReadOnlyList<CompassTick> ticks = VisorBearingTapeModel.ComputeTicks(heading);

        _bearingTape.UpdateData(ticks, markers);

        // 3. Suit Visor Model evaluation
        float distToShip = (shipWorldPosition - suitWorldPosition).Length();
        SuitVisorState visorState = SuitVisorModel.Compute(
            currentSlot: _currentSlot,
            slotInput: slotInput,
            cargoCount: cargoCount,
            cargoCapacity: (int)MathF.Round(tuning.SuitCargoCapacity),
            suitToShipDistance: distToShip,
            shipInteractRange: tuning.ShipInteractRange,
            currentSpeed: currentSpeed,
            dampenersEnabled: dampenersEnabled,
            steelBlockCount: steelBlockCount);

        _currentSlot = visorState.ActiveSlot;

        // 4. Update status stack
        _speedValueLabel.Text = string.Create(inv, $"{currentSpeed,5:F1} m/s");

        if (dampenersEnabled)
        {
            _dampenerBadge.Text = "Z: ON";
            _dampenerBadge.AddThemeColorOverride("font_color", VisorPrimary);
        }
        else
        {
            _dampenerBadge.Text = "Z: OFF";
            _dampenerBadge.AddThemeColorOverride("font_color", VisorWarning);
        }

        _cargoLabel.Text = $"CARGO {visorState.CargoCount} / {visorState.CargoCapacity}";
        _cargoBar.MaxValue = visorState.CargoCapacity;
        _cargoBar.Value = visorState.CargoCount;

        var barFillStyle = new StyleBoxFlat
        {
            BgColor = visorState.IsCargoFull ? VisorDanger : VisorPrimary,
            CornerRadiusTopLeft = 1,
            CornerRadiusTopRight = 1,
            CornerRadiusBottomLeft = 1,
            CornerRadiusBottomRight = 1,
        };
        _cargoBar.AddThemeStyleboxOverride("fill", barFillStyle);

        // 5. Contextual prompt
        if (visorState.CanBoardShip && !string.IsNullOrEmpty(visorState.BoardingPrompt))
        {
            _contextPromptLabel.Text = visorState.BoardingPrompt;
            _contextPromptLabel.AddThemeColorOverride("font_color", VisorPrimary);
            _contextPromptLabel.Visible = true;
        }
        else if (isAimingAtDeposit && _currentSlot == 1)
        {
            if (drillStatus == DrillStatus.CargoFull || visorState.IsCargoFull)
            {
                _contextPromptLabel.Text = "CARGO FULL";
                _contextPromptLabel.AddThemeColorOverride("font_color", VisorDanger);
                _contextPromptLabel.Visible = true;
            }
            else if (drillStatus == DrillStatus.Depleted)
            {
                _contextPromptLabel.Text = "DEPLETED";
                _contextPromptLabel.AddThemeColorOverride("font_color", VisorMuted);
                _contextPromptLabel.Visible = true;
            }
            else if (drillStatus == DrillStatus.OutOfRange)
            {
                _contextPromptLabel.Text = "OUT OF RANGE";
                _contextPromptLabel.AddThemeColorOverride("font_color", VisorWarning);
                _contextPromptLabel.Visible = true;
            }
            else
            {
                _contextPromptLabel.Text = "[LMB] Drill Iron Ore";
                _contextPromptLabel.AddThemeColorOverride("font_color", VisorWarning);
                _contextPromptLabel.Visible = true;
            }
        }
        else
        {
            _contextPromptLabel.Visible = false;
        }

        // 6. Hotbar slots
        for (int i = 0; i < 5; i++)
        {
            HotbarSlot slot = visorState.Slots[i];
            bool isSelected = slot.Index == _currentSlot;
            UpdateSlotStyle(i, isSelected);

            _slotLabels[i].Text = slot.Name == "Empty" ? "" : slot.Name;
            _slotLabels[i].AddThemeColorOverride("font_color", slot.IsDimmed ? VisorMuted : (isSelected ? VisorPrimary : VisorText));

            if (slot.Index == 2)
            {
                _slotCountLabels[i].Text = slot.Count.ToString();
                _slotCountLabels[i].AddThemeColorOverride("font_color", slot.IsDimmed ? VisorMuted : VisorText);
            }
            else
            {
                _slotCountLabels[i].Text = "";
            }
        }
    }

    private void UpdateSlotStyle(int slotIndex, bool isSelected)
    {
        var boxStyle = new StyleBoxFlat
        {
            BgColor = isSelected ? VisorSurface : VisorBg,
            BorderColor = isSelected ? VisorPrimary : VisorBorder,
            BorderWidthLeft = isSelected ? 2 : 1,
            BorderWidthTop = isSelected ? 2 : 1,
            BorderWidthRight = isSelected ? 2 : 1,
            BorderWidthBottom = isSelected ? 2 : 1,
            CornerRadiusTopLeft = 2,
            CornerRadiusTopRight = 2,
            CornerRadiusBottomLeft = 2,
            CornerRadiusBottomRight = 2,
            ContentMarginLeft = 4,
            ContentMarginRight = 4,
            ContentMarginTop = 4,
            ContentMarginBottom = 4,
        };
        _slotPanels[slotIndex].AddThemeStyleboxOverride("panel", boxStyle);
    }

    private static PanelContainer CreateStyledPanel(Vector2 minSize, Color bg, Color border)
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = minSize,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };

        var boxStyle = new StyleBoxFlat
        {
            BgColor = bg,
            BorderColor = border,
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
