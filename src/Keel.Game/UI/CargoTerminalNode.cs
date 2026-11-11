using System;
using System.Globalization;
using Godot;
using Keel.Sim.Hud;

namespace Keel.Game.UI;

public partial class CargoTerminalNode : CanvasLayer
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

    public event Action? DepositOreRequested;
    public event Action? WithdrawOreRequested;
    public event Action? DepositBlocksRequested;
    public event Action? WithdrawBlocksRequested;
    public event Action? DepositAllRequested;
    public event Action? WithdrawAllRequested;
    public event Action? CloseRequested;

    private ColorRect _dimBackdrop = null!;
    private PanelContainer _cardPanel = null!;

    // UI elements
    private Label _titleLabel = null!;
    private Label _statusLabel = null!;
    private Label _powerLabel = null!;
    private ProgressBar _progressBar = null!;

    private Label _oreCountLabel = null!;
    private Button _depositOreButton = null!;
    private Button _withdrawOreButton = null!;

    private Label _blockCountLabel = null!;
    private Button _depositBlockButton = null!;
    private Button _withdrawBlockButton = null!;

    private Button _depositAllButton = null!;
    private Button _withdrawAllButton = null!;

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

        // 2. Center Card Panel (580x420px)
        _cardPanel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(580, 420),
        };
        _cardPanel.SetAnchorsPreset(Control.LayoutPreset.Center);
        _cardPanel.Position = new Vector2(-290, -210);

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
        contentBox.AddThemeConstantOverride("separation", 14);
        _cardPanel.AddChild(contentBox);

        // Header Row
        var headerRow = new HBoxContainer();
        _titleLabel = CreateLabel("CARGO CONTAINER TERMINAL", font, 16, TerminalPrimary);
        _titleLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        headerRow.AddChild(_titleLabel);

        var closeBtn = new Button
        {
            Text = "[F] / [Esc] CLOSE",
            CustomMinimumSize = new Vector2(130, 28),
        };
        StyleButton(closeBtn, font, TerminalSurface, TerminalBorder, TerminalMuted);
        closeBtn.Pressed += () => CloseRequested?.Invoke();
        headerRow.AddChild(closeBtn);
        contentBox.AddChild(headerRow);

        // Status & Power Bar
        var progressContainer = new VBoxContainer();
        progressContainer.AddThemeConstantOverride("separation", 4);

        var statusRow = new HBoxContainer();
        _statusLabel = CreateLabel("STORED 0 / 500", font, 13, TerminalPrimary);
        _statusLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        statusRow.AddChild(_statusLabel);

        _powerLabel = CreateLabel("PWR  --", font, 12, TerminalMuted);
        statusRow.AddChild(_powerLabel);
        progressContainer.AddChild(statusRow);

        _progressBar = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 100,
            Value = 0,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(520, 12),
        };
        var barFill = new StyleBoxFlat
        {
            BgColor = TerminalPrimary,
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

        // Row 1: Iron Ore transfers
        var orePanel = CreateTransferRow(
            font,
            "IRON ORE",
            out _oreCountLabel,
            "[1] DEPOSIT",
            out _depositOreButton,
            "[2] WITHDRAW",
            out _withdrawOreButton);
        _depositOreButton.Pressed += () => DepositOreRequested?.Invoke();
        _withdrawOreButton.Pressed += () => WithdrawOreRequested?.Invoke();
        contentBox.AddChild(orePanel);

        // Row 2: Steel Block transfers
        var blockPanel = CreateTransferRow(
            font,
            "STEEL BLOCKS",
            out _blockCountLabel,
            "[3] DEPOSIT",
            out _depositBlockButton,
            "[4] WITHDRAW",
            out _withdrawBlockButton);
        _depositBlockButton.Pressed += () => DepositBlocksRequested?.Invoke();
        _withdrawBlockButton.Pressed += () => WithdrawBlocksRequested?.Invoke();
        contentBox.AddChild(blockPanel);

        // Row 3: Bulk transfers
        var bulkPanel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(520, 44),
        };
        var bulkStyle = new StyleBoxFlat
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
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 6,
            ContentMarginBottom = 6,
        };
        bulkPanel.AddThemeStyleboxOverride("panel", bulkStyle);

        var bulkRow = new HBoxContainer();
        bulkRow.AddThemeConstantOverride("separation", 16);
        bulkPanel.AddChild(bulkRow);

        var bulkTitle = CreateLabel("BULK TRANSFERS", font, 12, TerminalMuted);
        bulkTitle.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        bulkRow.AddChild(bulkTitle);

        _depositAllButton = new Button
        {
            Text = "[E] DEPOSIT ALL",
            CustomMinimumSize = new Vector2(130, 30),
        };
        StyleButton(_depositAllButton, font, TerminalBg, TerminalPrimary, TerminalText);
        _depositAllButton.Pressed += () => DepositAllRequested?.Invoke();
        bulkRow.AddChild(_depositAllButton);

        _withdrawAllButton = new Button
        {
            Text = "[R] WITHDRAW ALL",
            CustomMinimumSize = new Vector2(130, 30),
        };
        StyleButton(_withdrawAllButton, font, TerminalBg, TerminalPrimary, TerminalText);
        _withdrawAllButton.Pressed += () => WithdrawAllRequested?.Invoke();
        bulkRow.AddChild(_withdrawAllButton);
        contentBox.AddChild(bulkPanel);

        // Footer Summary (Suit Inventory)
        _suitSummaryLabel = CreateLabel("SUIT CARGO: 0 / 50", font, 12, TerminalMuted);
        _suitSummaryLabel.HorizontalAlignment = HorizontalAlignment.Center;
        contentBox.AddChild(_suitSummaryLabel);

        Visible = false;
    }

    public void UpdateTerminal(in CargoTerminalViewModel vm)
    {
        var inv = CultureInfo.InvariantCulture;

        _titleLabel.Text = string.Create(inv, $"CARGO CONTAINER ({vm.Cell.X}, {vm.Cell.Y}, {vm.Cell.Z})");
        _statusLabel.Text = vm.StatusMessage;
        _powerLabel.Text = vm.PowerMessage;
        _progressBar.Value = vm.ContainerFillRatio * 100f;

        _oreCountLabel.Text = string.Create(inv, $"{vm.ContainerOreCount} IN BOX");
        _depositOreButton.Disabled = !vm.CanDepositOre;
        _depositOreButton.Modulate = vm.CanDepositOre ? Colors.White : new Color(1, 1, 1, 0.45f);
        _withdrawOreButton.Disabled = !vm.CanWithdrawOre;
        _withdrawOreButton.Modulate = vm.CanWithdrawOre ? Colors.White : new Color(1, 1, 1, 0.45f);

        _blockCountLabel.Text = string.Create(inv, $"{vm.ContainerBlockCount} IN BOX");
        _depositBlockButton.Disabled = !vm.CanDepositBlocks;
        _depositBlockButton.Modulate = vm.CanDepositBlocks ? Colors.White : new Color(1, 1, 1, 0.45f);
        _withdrawBlockButton.Disabled = !vm.CanWithdrawBlocks;
        _withdrawBlockButton.Modulate = vm.CanWithdrawBlocks ? Colors.White : new Color(1, 1, 1, 0.45f);

        _depositAllButton.Disabled = !vm.CanDepositAll;
        _depositAllButton.Modulate = vm.CanDepositAll ? Colors.White : new Color(1, 1, 1, 0.45f);
        _withdrawAllButton.Disabled = !vm.CanWithdrawAll;
        _withdrawAllButton.Modulate = vm.CanWithdrawAll ? Colors.White : new Color(1, 1, 1, 0.45f);

        _suitSummaryLabel.Text = string.Create(inv,
            $"SUIT CARGO: {vm.SuitTotalCount} / {vm.SuitCapacity} " +
            $"(Iron Ore: {vm.SuitOreCount}, Steel Blocks: {vm.SuitBlockCount})");
    }

    private static PanelContainer CreateTransferRow(
        Font font,
        string itemTitle,
        out Label countLabel,
        string depButtonText,
        out Button depButton,
        string withButtonText,
        out Button withButton)
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(520, 48),
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
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 6,
            ContentMarginBottom = 6,
        };
        panel.AddThemeStyleboxOverride("panel", style);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 10);
        panel.AddChild(row);

        var title = CreateLabel(itemTitle, font, 12, TerminalMuted);
        title.CustomMinimumSize = new Vector2(120, 0);
        row.AddChild(title);

        countLabel = CreateLabel("0 IN BOX", font, 13, TerminalText);
        countLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddChild(countLabel);

        depButton = new Button
        {
            Text = depButtonText,
            CustomMinimumSize = new Vector2(105, 30),
        };
        StyleButton(depButton, font, TerminalBg, TerminalPrimary, TerminalText);
        row.AddChild(depButton);

        withButton = new Button
        {
            Text = withButtonText,
            CustomMinimumSize = new Vector2(105, 30),
        };
        StyleButton(withButton, font, TerminalBg, TerminalPrimary, TerminalText);
        row.AddChild(withButton);

        return panel;
    }

    private static void StyleButton(Button btn, Font font, Color bg, Color border, Color text)
    {
        btn.AddThemeFontOverride("font", font);
        btn.AddThemeFontSizeOverride("font_size", 11);
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
