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
        FontStyle fontStyle = FontStyle.Normal,
        TextClipping clipping = TextClipping.Overflow,
        Font? fontOverride = null
    ) {
        if (string.IsNullOrEmpty(text)) {
            return;
        }

        Theme.Theme theme = RenderContext.Current.Theme;
        int pixelSize = Mathf.RoundToInt(fontSize.ToFontPx());
        GUIStyle style = fontOverride != null
            ? GuiStyleCache.GetOrCreate(fontOverride, pixelSize, fontStyle)
            : GuiStyleCache.GetOrCreate(theme, role, pixelSize, fontStyle);
        style.alignment = anchor;
        style.clipping = clipping;

        Color saved = GUI.color;
        GUI.color = theme.GetColor(color);
        GUI.Label(RectSnap.SnapText(rect), text, style);
        GUI.color = saved;
    }


    public static void DrawTracked(
        Rect rect,
        string text,
        FontRole role,
        Rem fontSize,
        TextAnchor anchor,
        ThemeSlot color,
        float tracking,
        FontStyle fontStyle = FontStyle.Normal,
        Font? fontOverride = null
    ) {
        if (string.IsNullOrEmpty(text)) {
            return;
        }

        Theme.Theme theme = RenderContext.Current.Theme;
        int pixelSize = Mathf.RoundToInt(fontSize.ToFontPx());
        GUIStyle style = fontOverride != null
            ? GuiStyleCache.GetOrCreate(fontOverride, pixelSize, fontStyle)
            : GuiStyleCache.GetOrCreate(theme, role, pixelSize, fontStyle);
        style.alignment = anchor;

        float totalW = 0f;
        GUIContent gc = new GUIContent();
        for (int i = 0; i < text.Length; i++) {
            gc.text = text[i].ToString();
            totalW += style.CalcSize(gc).x;
            if (i < text.Length - 1) {
                totalW += tracking;
            }
        }

        float cursor;
        switch (anchor) {
            case TextAnchor.UpperRight:
            case TextAnchor.MiddleRight:
            case TextAnchor.LowerRight:
                cursor = rect.xMax - totalW;
                break;
            case TextAnchor.UpperCenter:
            case TextAnchor.MiddleCenter:
            case TextAnchor.LowerCenter:
                cursor = rect.x + (rect.width - totalW) * 0.5f;
                break;
            default:
                cursor = rect.x;
                break;
        }

        Color saved = GUI.color;
        GUI.color = theme.GetColor(color);
        for (int i = 0; i < text.Length; i++) {
            gc.text = text[i].ToString();
            float w = style.CalcSize(gc).x;
            GUI.Label(RectSnap.SnapText(new Rect(cursor, rect.y, w, rect.height)), gc.text, style);
            cursor += w + tracking;
        }
        GUI.color = saved;
    }

    public static Vector2 Measure(string text, FontRole role, Rem fontSize, FontStyle fontStyle = FontStyle.Normal, Font? fontOverride = null) {
        if (string.IsNullOrEmpty(text)) {
            return Vector2.zero;
        }

        Theme.Theme theme = RenderContext.Current.Theme;
        int pixelSize = Mathf.RoundToInt(fontSize.ToFontPx());
        GUIStyle style = fontOverride != null
            ? GuiStyleCache.GetOrCreate(fontOverride, pixelSize, fontStyle)
            : GuiStyleCache.GetOrCreate(theme, role, pixelSize, fontStyle);
        return style.CalcSize(new GUIContent(text));
    }


    public static void DrawWithStyle(Rect rect, string text, GUIStyle style, Color color) {
        if (string.IsNullOrEmpty(text)) {
            return;
        }

        Color saved = GUI.color;
        GUI.color = color;
        GUI.Label(RectSnap.SnapText(rect), text, style);
        GUI.color = saved;
    }

    public static float MeasureTracked(string text, FontRole role, Rem fontSize, float tracking, FontStyle fontStyle = FontStyle.Normal, Font? fontOverride = null) {
        if (string.IsNullOrEmpty(text)) {
            return 0f;
        }

        Theme.Theme theme = RenderContext.Current.Theme;
        int pixelSize = Mathf.RoundToInt(fontSize.ToFontPx());
        GUIStyle style = fontOverride != null
            ? GuiStyleCache.GetOrCreate(fontOverride, pixelSize, fontStyle)
            : GuiStyleCache.GetOrCreate(theme, role, pixelSize, fontStyle);

        float totalW = 0f;
        GUIContent gc = new GUIContent();
        for (int i = 0; i < text.Length; i++) {
            gc.text = text[i].ToString();
            totalW += style.CalcSize(gc).x;
            if (i < text.Length - 1) {
                totalW += tracking;
            }
        }
        return totalW;
    }

    public static void DrawTracked(
        Rect rect,
        string text,
        FontRole role,
        Rem fontSize,
        TextAnchor anchor,
        Color color,
        float tracking,
        FontStyle fontStyle = FontStyle.Normal,
        Font? fontOverride = null
    ) {
        if (string.IsNullOrEmpty(text)) {
            return;
        }

        Theme.Theme theme = RenderContext.Current.Theme;
        int pixelSize = Mathf.RoundToInt(fontSize.ToFontPx());
        GUIStyle style = fontOverride != null
            ? GuiStyleCache.GetOrCreate(fontOverride, pixelSize, fontStyle)
            : GuiStyleCache.GetOrCreate(theme, role, pixelSize, fontStyle);
        style.alignment = anchor;

        float totalW = 0f;
        GUIContent gc = new GUIContent();
        for (int i = 0; i < text.Length; i++) {
            gc.text = text[i].ToString();
            totalW += style.CalcSize(gc).x;
            if (i < text.Length - 1) {
                totalW += tracking;
            }
        }

        float cursor;
        switch (anchor) {
            case TextAnchor.UpperRight:
            case TextAnchor.MiddleRight:
            case TextAnchor.LowerRight:
                cursor = rect.xMax - totalW;
                break;
            case TextAnchor.UpperCenter:
            case TextAnchor.MiddleCenter:
            case TextAnchor.LowerCenter:
                cursor = rect.x + (rect.width - totalW) * 0.5f;
                break;
            default:
                cursor = rect.x;
                break;
        }

        Color saved = GUI.color;
        GUI.color = color;
        for (int i = 0; i < text.Length; i++) {
            gc.text = text[i].ToString();
            float w = style.CalcSize(gc).x;
            GUI.Label(RectSnap.SnapText(new Rect(cursor, rect.y, w, rect.height)), gc.text, style);
            cursor += w + tracking;
        }
        GUI.color = saved;
    }
}
