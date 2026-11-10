using System;
using System.Collections.Generic;
using Godot;
using Keel.Sim.Hud;

namespace Keel.Game.UI;

public partial class BearingTapeControl : Control
{
    private static readonly Color VisorBg = new(0x10 / 255f, 0x1A / 255f, 0x26 / 255f, 0.82f);
    private static readonly Color VisorBorder = new(0x2B / 255f, 0x42 / 255f, 0x57 / 255f, 1.0f);
    private static readonly Color CyanPrimary = new(0x38 / 255f, 0xBD / 255f, 0xF8 / 255f, 1.0f);
    private static readonly Color TextMuted = new(0x8B / 255f, 0x9E / 255f, 0xB0 / 255f, 1.0f);
    private static readonly Color AmberWarning = new(0xF5 / 255f, 0x9E / 255f, 0x0B / 255f, 1.0f);

    private IReadOnlyList<CompassTick> _ticks = Array.Empty<CompassTick>();
    private IReadOnlyList<BearingMarkerResult> _markers = Array.Empty<BearingMarkerResult>();

    private SystemFont? _font;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        CustomMinimumSize = new Vector2(480, 44);

        _font = new SystemFont
        {
            FontNames = new[] { "JetBrains Mono", "Consolas", "Courier New" },
        };
    }

    public void UpdateData(IReadOnlyList<CompassTick> ticks, IReadOnlyList<BearingMarkerResult> markers)
    {
        _ticks = ticks;
        _markers = markers;
        QueueRedraw();
    }

    public override void _Draw()
    {
        Vector2 size = Size;
        float halfW = size.X * 0.5f;

        // 1. Background and border
        DrawRect(new Rect2(Vector2.Zero, size), VisorBg, filled: true);
        DrawRect(new Rect2(Vector2.Zero, size), VisorBorder, filled: false, width: 1.0f);

        // 2. Fixed Center Caret (Boresight Heading Pointer)
        DrawLine(new Vector2(halfW, 0), new Vector2(halfW, 8), CyanPrimary, 2.0f);
        DrawLine(new Vector2(halfW - 4, 0), new Vector2(halfW + 4, 0), CyanPrimary, 2.0f);

        float usableHalfSpan = halfW - 14.0f;

        // 3. Degree Ticks and Labels
        for (int i = 0; i < _ticks.Count; i++)
        {
            CompassTick tick = _ticks[i];
            float tickX = halfW + tick.NormalizedX * usableHalfSpan;

            float tickLen = tick.IsMajor ? 7.0f : 4.0f;
            Color tickColor = tick.IsMajor ? TextMuted : new Color(TextMuted.R, TextMuted.G, TextMuted.B, 0.5f);

            DrawLine(new Vector2(tickX, 0), new Vector2(tickX, tickLen), tickColor, 1.0f);

            if (tick.IsMajor && _font != null)
            {
                // Draw 3-digit / degree label under tick
                Vector2 labelPos = new(tickX - 9.0f, 18.0f);
                DrawString(_font, labelPos, tick.Label, HorizontalAlignment.Left, -1, 10, TextMuted);
            }
        }

        // 4. Landmark Markers (SHIP, STATION, ASTEROID)
        for (int i = 0; i < _markers.Count; i++)
        {
            BearingMarkerResult marker = _markers[i];
            float markerX = halfW + marker.NormalizedX * usableHalfSpan;
            float markerY = size.Y - 14.0f;

            Color markerColor = marker.IsClamped ? AmberWarning : CyanPrimary;

            // Draw diamond
            const float dSize = 4.5f;
            Vector2[] diamond =
            {
                new(markerX, markerY - dSize),
                new(markerX + dSize, markerY),
                new(markerX, markerY + dSize),
                new(markerX - dSize, markerY),
            };
            DrawColoredPolygon(diamond, markerColor);

            if (_font != null)
            {
                string tag = $"{marker.Name} {marker.FormattedDistance}";
                float tagX = Mathf.Clamp(markerX - 16.0f, 4.0f, size.X - 80.0f);
                Vector2 tagPos = new(tagX, markerY - 6.0f);
                DrawString(_font, tagPos, tag, HorizontalAlignment.Left, -1, 9, markerColor);
            }
        }
    }
}
