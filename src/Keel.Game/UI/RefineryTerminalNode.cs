using System;
using System.Globalization;
using Godot;
using Keel.Sim.Hud;

namespace Keel.Game.UI;

public partial class RefineryTerminalNode : CanvasLayer
{
    // Design tokens from docs/sdlc/design.md
    private static readonly Color TerminalDim = new(0x05 / 255f, 0x07 / 255f, 0x0B / 255f, 0.55f);
    private static readonly Color TerminalBg = new(0x10 / 255f, 0x1A / 255f, 0x26 / 255f, 0.95f);
    private static readonly Color TerminalSurface = new(0x16 / 255f, 0x22 / 255f, 0x2F / 255f, 0.95f);
    private static readonly Color TerminalBorder = new(0x2B / 255f, 0x42 / 255f, 0x57 / 255f, 1.0f);
    private static readonly Color TerminalText = new(0xE6 / 255f, 0xED / 255f, 0xF3 / 255f, 1.0f);
    private static readonly Color TerminalMuted = new(0x8B / 255f, 0x9E / 255f, 0xB0 / 255f, 1.0f);
    private static readonly Color TerminalPrimary = new(0x38 / 255f, 0xBD / 255f, 0xF8 / 255f, 1.0f);
    private static readonly Color TerminalActive = new(0xF5 / 255f, 0x9E / 255f, 0x0B / 255f, 1.0f);
    private static readonly Color TerminalDanger = new(0xEF / 255f, 0x44 / 255f, 0x44 / 255f, 1.0f);

    public event Action? DepositRequested;
    public event Action? WithdrawRequested;
    public event Action? CloseRequested;

    private ColorRect _dimBackdrop = null!;
    private PanelContainer _cardPanel = null!;

    // UI elements
    private Label _statusLabel = null!;
    private ProgressBar _progressBar = null!;
    private Label _inputOreLabel = null!;
    private Button _depositButton = null!;
    private Label _outputBlockLabel = null!;
    private Button _withdrawButton = null!;
    private Label _suitSummaryLabel = null!;

    public override void _Ready()
    {
        Layer = 20; // Above visor HUD (Layer 10)

        var font = new SystemFont
        {
            FontNames = new[] { "JetBrains Mono", "Consolas", "Courier New" },
        };

        // 1. Fullscreen dimming backdrop
        _dimBackdrop = new ColorRect
        {
            Color = TerminalDim,
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        _dimBackdrop.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(_dimBackdrop);

        // 2. Center Card Panel (540x330px)
        _cardPanel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(540, 330),
        };
        _cardPanel.SetAnchorsPreset(Control.LayoutPreset.Center);
        _cardPanel.Position = new Vector2(-270, -165);

        var cardStyle = new StyleBoxFlat
        {
            BgColor = TerminalBg,
            BorderColor = TerminalBorder,
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 2,
            CornerRadiusTopRight = 2,
            CornerRadiusBottomLeft = 2,
            CornerRadiusBottomRight = 2,
            ContentMarginLeft = 24,
            ContentMarginRight = 24,
            ContentMarginTop = 20,
            ContentMarginBottom = 20,
        };
        _cardPanel.AddThemeStyleboxOverride("panel", cardStyle);
        _dimBackdrop.AddChild(_cardPanel);

        var contentBox = new VBoxContainer();
        contentBox.AddThemeConstantOverride("separation", 16);
        _cardPanel.AddChild(contentBox);

        // Header Row
        var headerRow = new HBoxContainer();
        var titleLabel = CreateLabel("STATION REFINERY TERMINAL", font, 16, TerminalPrimary);
        titleLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        headerRow.AddChild(titleLabel);

        var closeBtn = new Button
        {
            Text = "[F] / [Esc] CLOSE",
            CustomMinimumSize = new Vector2(130, 28),
        };
        StyleButton(closeBtn, font, TerminalSurface, TerminalBorder, TerminalMuted);
        closeBtn.Pressed += () => CloseRequested?.Invoke();
        headerRow.AddChild(closeBtn);
        contentBox.AddChild(headerRow);

        // Input Row (Iron Ore)
        var inputPanel = CreateRowPanel(font, "INPUT: IRON ORE", out _inputOreLabel, "[E] DEPOSIT ALL", out _depositButton);
        _depositButton.Pressed += () => DepositRequested?.Invoke();
        contentBox.AddChild(inputPanel);

        // Refining Progress Bar & Status Text
        var progressContainer = new VBoxContainer();
        progressContainer.AddThemeConstantOverride("separation", 6);

        _statusLabel = CreateLabel("IDLE", font, 13, TerminalActive);
        _statusLabel.HorizontalAlignment = HorizontalAlignment.Center;
        progressContainer.AddChild(_statusLabel);

        _progressBar = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 100,
            Value = 0,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(480, 14),
        };
        var barFill = new StyleBoxFlat
        {
            BgColor = TerminalActive,
            CornerRadiusTopLeft = 1,
            CornerRadiusTopRight = 1,
            CornerRadiusBottomLeft = 1,
            CornerRadiusBottomRight = 1,
        };
        var barBg = new StyleBoxFlat
        {
            BgColor = TerminalSurface,
            BorderColor = TerminalBorder,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
        };
        _progressBar.AddThemeStyleboxOverride("fill", barFill);
        _progressBar.AddThemeStyleboxOverride("background", barBg);
        progressContainer.AddChild(_progressBar);
        contentBox.AddChild(progressContainer);

        // Output Row (Steel Blocks)
        var outputPanel = CreateRowPanel(font, "OUTPUT: STEEL BLOCKS", out _outputBlockLabel, "[R] WITHDRAW ALL", out _withdrawButton);
        _withdrawButton.Pressed += () => WithdrawRequested?.Invoke();
        contentBox.AddChild(outputPanel);

        // Footer Summary (Suit Inventory)
        _suitSummaryLabel = CreateLabel("SUIT CARGO: 0 / 50", font, 12, TerminalMuted);
        _suitSummaryLabel.HorizontalAlignment = HorizontalAlignment.Center;
        contentBox.AddChild(_suitSummaryLabel);

        Visible = false;
    }

    public void UpdateTerminal(in RefineryTerminalViewModel vm)
    {
        var inv = CultureInfo.InvariantCulture;

        _statusLabel.Text = vm.StatusMessage;
        _progressBar.Value = vm.CycleProgress * 100f;

        _inputOreLabel.Text = string.Create(inv, $"{vm.InputOreCount} / {vm.InputOreCapacity} ORE");
        _outputBlockLabel.Text = string.Create(inv, $"{vm.OutputBlockCount} / {vm.OutputBlockCapacity} BLOCKS");

        _depositButton.Disabled = !vm.CanDeposit;
        _depositButton.Modulate = vm.CanDeposit ? Colors.White : new Color(1, 1, 1, 0.45f);

        _withdrawButton.Disabled = !vm.CanWithdraw;
        _withdrawButton.Modulate = vm.CanWithdraw ? Colors.White : new Color(1, 1, 1, 0.45f);

        _suitSummaryLabel.Text = string.Create(inv,
            $"SUIT CARGO: {vm.SuitOreCount + vm.SuitBlockCount} / {vm.SuitOreCount + vm.SuitBlockCount + vm.SuitRemainingCapacity} " +
            $"(Iron Ore: {vm.SuitOreCount}, Steel Blocks: {vm.SuitBlockCount})");
    }

    private static PanelContainer CreateRowPanel(
        Font font,
        string rowTitle,
        out Label countLabel,
        string buttonText,
        out Button actionButton)
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(480, 52),
        };
        var style = new StyleBoxFlat
        {
            BgColor = TerminalSurface,
            BorderColor = TerminalBorder,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 2,
            CornerRadiusTopRight = 2,
            CornerRadiusBottomLeft = 2,
            CornerRadiusBottomRight = 2,
            ContentMarginLeft = 14,
            ContentMarginRight = 14,
            ContentMarginTop = 8,
            ContentMarginBottom = 8,
        };
        panel.AddThemeStyleboxOverride("panel", style);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);
        panel.AddChild(row);

        var title = CreateLabel(rowTitle, font, 12, TerminalMuted);
        title.CustomMinimumSize = new Vector2(160, 0);
        row.AddChild(title);

        countLabel = CreateLabel("0 / 100", font, 14, TerminalText);
        countLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddChild(countLabel);

        actionButton = new Button
        {
            Text = buttonText,
            CustomMinimumSize = new Vector2(140, 32),
        };
        StyleButton(actionButton, font, TerminalBg, TerminalPrimary, TerminalText);
        row.AddChild(actionButton);

        return panel;
    }

    private static void StyleButton(Button btn, Font font, Color bg, Color border, Color text)
    {
        btn.AddThemeFontOverride("font", font);
        btn.AddThemeFontSizeOverride("font_size", 12);
        btn.AddThemeColorOverride("font_color", text);

        var normal = new StyleBoxFlat
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
        };
        btn.AddThemeStyleboxOverride("normal", normal);
        btn.AddThemeStyleboxOverride("hover", normal);
        btn.AddThemeStyleboxOverride("pressed", normal);
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
