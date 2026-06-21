using System.Collections.Generic;
using Cosmere.Lightweave.Settings;
using UnityEngine;
using Verse;

namespace Cosmere.Lightweave.Fonts;

public static class GameFontOverride {
    private static readonly Dictionary<GUIStyle, int> baselineByStyle = new();
    private const float OpenSansFitScale = 0.92f;

    public static void Apply() {
        Font? baseFont = LightweaveFonts.OpenSansRegular;
        if (baseFont == null || !baseFont.dynamic) {
            return;
        }

        float scale = LightweaveMod.Settings?.FontScale ?? 1f;
        ApplyToStyles(Text.fontStyles, baseFont, scale);
        ApplyToStyles(Text.textFieldStyles, baseFont, scale);
        ApplyToStyles(Text.textAreaStyles, baseFont, scale);
        ApplyToStyles(Text.textAreaReadOnlyStyles, baseFont, scale);
    }

    private static void ApplyToStyles(GUIStyle[] styles, Font baseFont, float scale) {
        for (int i = 0; i < styles.Length; i++) {
            GUIStyle style = styles[i];
            if (style == null) {
                continue;
            }

            if (!baselineByStyle.TryGetValue(style, out int baseline)) {
                Font? original = style.font;
                int originalSize = original != null ? original.fontSize : style.fontSize;
                baseline = style.fontSize == 0 && originalSize > 0 ? originalSize + 2 : style.fontSize;
                baselineByStyle[style] = baseline;
            }

            style.font = baseFont;
            style.fontSize = Mathf.Max(1, Mathf.RoundToInt(baseline * scale * OpenSansFitScale));
        }
    }
}
