using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Theme;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;

namespace Cosmere.Lightweave.Input;

public static class InputSurface {
    public static readonly Rem PaddingY = new Rem(0.25f);
    public static readonly Rem PaddingX = SelectorTrigger.PaddingX;

    private static GUIStyle? chromelessTextFieldStyle;
    private static GUIStyle? chromelessTextAreaStyle;

    public static GUIStyle ConfigureChromelessTextAreaStyle(Font? font, int pixelSize, Color textColor) {
        if (chromelessTextAreaStyle == null) {
            chromelessTextAreaStyle = new GUIStyle(GUI.skin.textArea);
            chromelessTextAreaStyle.normal.background = null;
            chromelessTextAreaStyle.hover.background = null;
            chromelessTextAreaStyle.active.background = null;
            chromelessTextAreaStyle.focused.background = null;
            chromelessTextAreaStyle.border = new RectOffset(0, 0, 0, 0);
            chromelessTextAreaStyle.padding = new RectOffset(0, 0, 0, 0);
            chromelessTextAreaStyle.margin = new RectOffset(0, 0, 0, 0);
            chromelessTextAreaStyle.overflow = new RectOffset(0, 0, 0, 0);
            chromelessTextAreaStyle.alignment = TextAnchor.UpperLeft;
            chromelessTextAreaStyle.clipping = TextClipping.Clip;
            chromelessTextAreaStyle.wordWrap = true;
            chromelessTextAreaStyle.richText = false;
        }

        if (font != null) {
            chromelessTextAreaStyle.font = font;
        }

        chromelessTextAreaStyle.fontSize = pixelSize;
        chromelessTextAreaStyle.normal.textColor = textColor;
        chromelessTextAreaStyle.hover.textColor = textColor;
        chromelessTextAreaStyle.active.textColor = textColor;
        chromelessTextAreaStyle.focused.textColor = textColor;
        return chromelessTextAreaStyle;
    }

    public static GUIStyle ConfigureChromelessTextFieldStyle(Font? font, int pixelSize, Color textColor) {
        if (chromelessTextFieldStyle == null) {
            chromelessTextFieldStyle = new GUIStyle(GUI.skin.textField);
            chromelessTextFieldStyle.normal.background = null;
            chromelessTextFieldStyle.hover.background = null;
            chromelessTextFieldStyle.active.background = null;
            chromelessTextFieldStyle.focused.background = null;
            chromelessTextFieldStyle.border = new RectOffset(0, 0, 0, 0);
            chromelessTextFieldStyle.padding = new RectOffset(0, 0, 0, 0);
            chromelessTextFieldStyle.margin = new RectOffset(0, 0, 0, 0);
            chromelessTextFieldStyle.overflow = new RectOffset(0, 0, 0, 0);
            chromelessTextFieldStyle.alignment = TextAnchor.MiddleLeft;
            chromelessTextFieldStyle.clipping = TextClipping.Clip;
            chromelessTextFieldStyle.wordWrap = false;
            chromelessTextFieldStyle.richText = false;
        }

        if (font != null) {
            chromelessTextFieldStyle.font = font;
        }

        chromelessTextFieldStyle.fontSize = pixelSize;
        chromelessTextFieldStyle.normal.textColor = textColor;
        chromelessTextFieldStyle.hover.textColor = textColor;
        chromelessTextFieldStyle.active.textColor = textColor;
        chromelessTextFieldStyle.focused.textColor = textColor;
        return chromelessTextFieldStyle;
    }

    public static ThemeSlot ResolveBorderSlot(InteractionState state, Variant variant = default) {
        if (variant == Variant.Danger) {
            return ThemeSlot.BorderDanger;
        }

        if (state.Focused) {
            return ThemeSlot.BorderFocus;
        }

        if (state.Hovered) {
            return ThemeSlot.BorderHover;
        }

        return ThemeSlot.BorderDefault;
    }

    public static ThemeSlot ResolveToggleBorderSlot(InteractionState state, bool isOn) {
        if (state.Disabled) {
            return ThemeSlot.BorderOff;
        }

        if (state.Hovered) {
            return ThemeSlot.BorderHover;
        }

        if (isOn) {
            return ThemeSlot.BorderFocus;
        }

        return ThemeSlot.BorderOff;
    }

    public static ThemeSlot? ResolveSurfaceSlot(InteractionState state, Variant variant = default) {
        if (variant == Variant.Primary) {
            return state.Disabled ? ThemeSlot.SurfaceDisabled : ThemeSlot.SurfaceInput;
        }
        if (variant == Variant.Danger) {
            return VariantPalette.Background(Variant.Secondary, state);
        }
        return VariantPalette.Background(variant, state);
    }

    public static ThemeSlot ResolveAccentFillSlot(Variant variant) {
        return variant == Variant.Danger ? ThemeSlot.StatusDanger : ThemeSlot.SurfaceAccent;
    }

    public static ThemeSlot ResolveOnAccentSlot(Variant variant) {
        return variant == Variant.Danger ? ThemeSlot.TextOnDanger : ThemeSlot.TextOnAccent;
    }

    public static void Draw(Rect rect, InteractionState state, Variant variant = default) {
        ThemeSlot borderSlot = ResolveBorderSlot(state, variant);
        ThemeSlot? surfaceSlot = ResolveSurfaceSlot(state, variant);

        BackgroundSpec? bg = surfaceSlot.HasValue ? BackgroundSpec.Of(surfaceSlot.Value) : null;
        BorderSpec border = BorderSpec.All(new Rem(1f / 16f), borderSlot);
        RadiusSpec radius = RadiusSpec.All(RadiusScale.Sm);
        PaintBox.Draw(rect, bg, border, radius);
    }

    public static void DrawInputChrome(Rect rect, InteractionState state, Variant variant = default) {
        if (variant.Id == null) {
            variant = Variant.Secondary;
        }
        Draw(rect, state, variant);
    }

    public static void DrawBackground(Rect rect, InteractionState state, Variant variant = default) {
        ThemeSlot? surfaceSlot = ResolveSurfaceSlot(state, variant);
        BackgroundSpec? bg = surfaceSlot.HasValue ? BackgroundSpec.Of(surfaceSlot.Value) : null;
        RadiusSpec radius = RadiusSpec.All(RadiusScale.Sm);
        PaintBox.Draw(rect, bg, null, radius);
    }

    public static void DrawBorder(Rect rect, InteractionState state) {
        ThemeSlot borderSlot = ResolveBorderSlot(state);
        BorderSpec border = BorderSpec.All(new Rem(1f / 16f), borderSlot);
        RadiusSpec radius = RadiusSpec.All(RadiusScale.Sm);
        PaintBox.Draw(rect, null, border, radius);
    }

    public static void DrawPlaceholder(Rect inner, string? placeholder, Theme.Theme theme, TextAnchor anchor = TextAnchor.MiddleLeft) {
        if (string.IsNullOrEmpty(placeholder)) {
            return;
        }

        Font font = theme.GetFont(FontRole.Body);
        int size = Mathf.RoundToInt(new Rem(1f).ToFontPx());
        GUIStyle style = GuiStyleCache.GetOrCreate(font, size);
        style.alignment = anchor;
        TextDraw.DrawWithStyle(inner, placeholder, style, theme.GetColor(ThemeSlot.TextMuted));
    }

    public static void DrawReadOnlyValue(Rect inner, string? value, Theme.Theme theme, TextAnchor anchor = TextAnchor.MiddleLeft) {
        Font font = theme.GetFont(FontRole.Body);
        int size = Mathf.RoundToInt(new Rem(1f).ToFontPx());
        GUIStyle style = GuiStyleCache.GetOrCreate(font, size);
        style.alignment = anchor;
        TextDraw.DrawWithStyle(inner, value ?? string.Empty, style, theme.GetColor(ThemeSlot.TextMuted));
    }
}