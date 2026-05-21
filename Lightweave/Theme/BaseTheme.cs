using System.Collections.Generic;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;

namespace Cosmere.Lightweave.Theme;

public static class BaseTheme {
    public static Dictionary<FontRole, Font> BuildFonts(
        Font body,
        Font bodyBold,
        Font heading,
        Font display,
        Font mono
    ) {
        return new Dictionary<FontRole, Font> {
            [FontRole.Body] = body,
            [FontRole.BodyBold] = bodyBold,
            [FontRole.Heading] = heading,
            [FontRole.Display] = display,
            [FontRole.Label] = body,
            [FontRole.Caption] = body,
            [FontRole.Mono] = mono,
        };
    }

    public static Dictionary<RadiusScale, float> BuildRadii() {
        return new Dictionary<RadiusScale, float> {
            [RadiusScale.None] = 0f,
            [RadiusScale.Xs] = 2f,
            [RadiusScale.Sm] = 4f,
            [RadiusScale.Md] = 6f,
            [RadiusScale.Lg] = 8f,
            [RadiusScale.Xl] = 10f,
            [RadiusScale.Full] = 9999f,
        };
    }

    public static Dictionary<RadiusScale, float> BuildFlatRadii() {
        return new Dictionary<RadiusScale, float> {
            [RadiusScale.None] = 0f,
            [RadiusScale.Xs] = 0f,
            [RadiusScale.Sm] = 0f,
            [RadiusScale.Md] = 0f,
            [RadiusScale.Lg] = 0f,
            [RadiusScale.Xl] = 0f,
            [RadiusScale.Full] = 9999f,
        };
    }

    public static Dictionary<ThemeSlot, ShadowSpec> BuildShadows(Dictionary<ThemeSlot, Color> colors) {
        Color ResolveColor(ThemeSlot slot, Color fallback) {
            return colors.TryGetValue(slot, out Color c) ? c : fallback;
        }
        Color black60 = new Color(0f, 0f, 0f, 0.60f);
        Color black50 = new Color(0f, 0f, 0f, 0.50f);
        Color black55 = new Color(0f, 0f, 0f, 0.55f);
        Color black45 = new Color(0f, 0f, 0f, 0.45f);
        Color insetSoft = new Color(1f, 0.902f, 0.706f, 0.05f);
        Color insetStrong = new Color(1f, 0.902f, 0.706f, 0.40f);
        return new Dictionary<ThemeSlot, ShadowSpec> {
            [ThemeSlot.ShadowModal] = new ShadowSpec.Drop(
                ResolveColor(ThemeSlot.ShadowModal, black60), new Vector2(0f, 40f), 80f, 0f),
            [ThemeSlot.ShadowCard] = new ShadowSpec.Drop(
                ResolveColor(ThemeSlot.ShadowCard, black50), new Vector2(0f, 20f), 60f, 0f),
            [ThemeSlot.ShadowPopover] = new ShadowSpec.Drop(
                ResolveColor(ThemeSlot.ShadowPopover, black60), new Vector2(0f, 24f), 60f, 0f),
            [ThemeSlot.ShadowTooltip] = new ShadowSpec.Drop(
                ResolveColor(ThemeSlot.ShadowTooltip, black55), new Vector2(0f, 6f), 22f, 0f),
            [ThemeSlot.ShadowToast] = new ShadowSpec.Drop(
                ResolveColor(ThemeSlot.ShadowToast, black45), new Vector2(0f, 8f), 28f, 0f),
            [ThemeSlot.ShadowRim] = new ShadowSpec.Drop(
                ResolveColor(ThemeSlot.ShadowRim, black55), new Vector2(0f, 4f), 22f, 0f),
            [ThemeSlot.ShadowInsetTop] = new ShadowSpec.Inset(
                ResolveColor(ThemeSlot.ShadowInsetTop, insetSoft), 1f, InsetEdge.Top),
            [ThemeSlot.ShadowInsetTopStrong] = new ShadowSpec.Inset(
                ResolveColor(ThemeSlot.ShadowInsetTopStrong, insetStrong), 1f, InsetEdge.Top),
        };
    }

    public static Theme Compose(
        Dictionary<ThemeSlot, Color> colors,
        Font body,
        Font bodyBold,
        Font heading,
        Font display,
        Font mono,
        Dictionary<RadiusScale, float>? radii = null,
        Dictionary<ThemeSlot, ShadowSpec>? shadows = null
    ) {
        Dictionary<FontRole, Font> fonts = BuildFonts(body, bodyBold, heading, display, mono);
        Dictionary<RadiusScale, float> resolvedRadii = radii ?? BuildRadii();
        Dictionary<ThemeSlot, ShadowSpec> resolvedShadows = shadows ?? BuildShadows(colors);
        return new Theme(colors, fonts, resolvedRadii, resolvedShadows);
    }
}