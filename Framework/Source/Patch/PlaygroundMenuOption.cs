using System;
using UnityEngine;
using Verse;

namespace Cosmere.Lightweave.Patch;

public class PlaygroundMenuOption : ListableOption {
    private const string Glyph = "✦";
    private static readonly Vector2 IconSize = new Vector2(24f, 24f);

    public PlaygroundMenuOption(string label, Action action) : base(label, action, null) {
        minHeight = IconSize.y;
    }

    public override float DrawOption(Vector2 pos, float width) {
        float labelWidth = width - IconSize.x - 3f;
        float labelHeight = Text.CalcHeight(label, labelWidth);
        float rowHeight = Mathf.Max(minHeight, labelHeight);
        Rect rowRect = new Rect(pos.x, pos.y, width, rowHeight);

        GameFont prevFont = Text.Font;
        TextAnchor prevAnchor = Text.Anchor;
        Color prevColor = GUI.color;

        Rect iconRect = new Rect(pos.x, pos.y + rowHeight / 2f - IconSize.y / 2f, IconSize.x, IconSize.y);
        GUI.color = Mouse.IsOver(rowRect) ? Widgets.MouseoverOptionColor : Color.white;
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(iconRect, Glyph);

        GUI.color = Color.white;
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
        Widgets.Label(new Rect(rowRect.xMax - labelWidth, pos.y, labelWidth, labelHeight), label);

        Text.Font = prevFont;
        Text.Anchor = prevAnchor;
        GUI.color = prevColor;

        if (Widgets.ButtonInvisible(rowRect)) {
            action?.Invoke();
        }

        return rowHeight;
    }
}
