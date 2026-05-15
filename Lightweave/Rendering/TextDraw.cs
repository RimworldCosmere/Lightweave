using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;

namespace Cosmere.Lightweave.Rendering;

public static class TextDraw {
    public static void Draw(
        Rect rect,
        string text,
        FontRole role,
        Rem fontSize,
        TextAnchor anchor,
        ThemeSlot color,
        FontStyle fontStyle = FontStyle.Normal
    ) {
        if (string.IsNullOrEmpty(text)) {
            return;
        }

        Theme.Theme theme = RenderContext.Current.Theme;
        int pixelSize = Mathf.RoundToInt(fontSize.ToFontPx());
        GUIStyle style = GuiStyleCache.GetOrCreate(theme, role, pixelSize, fontStyle);
        style.alignment = anchor;

        Color saved = GUI.color;
        GUI.color = theme.GetColor(color);
        GUI.Label(RectSnap.SnapText(rect), text, style);
        GUI.color = saved;
    }
}
