using Cosmere.Lightweave.Tokens;
using UnityEngine;

namespace Cosmere.Lightweave.Theme;

[LightweaveTheme("cosmere", "CL_Theme_Cosmere", Order = 10)]
public static class CosmereTheme {
    public static Theme Build(Font body, Font bodyBold, Font heading, Font display, Font mono) {
        Dictionary<ThemeSlot, Color> colors = new Dictionary<ThemeSlot, Color> {
            [ThemeSlot.SurfacePrimary] = new Color(0.847f, 0.827f, 0.769f, 0.97f),
            [ThemeSlot.SurfaceRaised] = new Color(0.890f, 0.875f, 0.816f, 1.00f),
            [ThemeSlot.SurfaceSunken] = new Color(0.761f, 0.737f, 0.663f, 1.00f),
            [ThemeSlot.SurfaceTranslucent] = new Color(0.847f, 0.827f, 0.769f, 0.35f),
            [ThemeSlot.SurfaceAccent] = new Color(0.557f, 0.133f, 0.192f, 1.00f),
            [ThemeSlot.SurfaceShadow] = new Color(0.102f, 0.122f, 0.180f, 0.25f),
            [ThemeSlot.SurfaceInput] = new Color(0.925f, 0.910f, 0.855f, 1.00f),
            [ThemeSlot.SurfaceDisabled] = new Color(0.714f, 0.694f, 0.627f, 1.00f),
            [ThemeSlot.TextPrimary] = new Color(0.102f, 0.122f, 0.180f),
            [ThemeSlot.TextSecondary] = new Color(0.220f, 0.243f, 0.302f),
            [ThemeSlot.TextMuted] = new Color(0.361f, 0.380f, 0.451f),
            [ThemeSlot.TextOnAccent] = new Color(0.953f, 0.925f, 0.847f),
            [ThemeSlot.TextOnDanger] = new Color(0.953f, 0.925f, 0.847f),
            [ThemeSlot.BorderDefault] = new Color(0.420f, 0.408f, 0.341f, 1f),
            [ThemeSlot.BorderSubtle] = new Color(0.627f, 0.604f, 0.525f, 1f),
            [ThemeSlot.BorderFocus] = new Color(0.722f, 0.573f, 0.227f, 1f),
            [ThemeSlot.BorderHover] = new Color(0.557f, 0.133f, 0.192f, 1f),
            [ThemeSlot.BorderOff] = new Color(0.541f, 0.518f, 0.447f, 1f),
            [ThemeSlot.BorderDanger] = new Color(0.557f, 0.133f, 0.192f, 1f),
            [ThemeSlot.StatusWarning] = new Color(0.722f, 0.573f, 0.227f),
            [ThemeSlot.StatusDanger] = new Color(0.557f, 0.133f, 0.192f),
            [ThemeSlot.StatusSuccess] = new Color(0.290f, 0.420f, 0.361f),
            [ThemeSlot.StatusInfo] = new Color(0.330f, 0.480f, 0.610f),
            [ThemeSlot.HoverTint] = new Color(0.722f, 0.573f, 0.227f, 0.18f),
            [ThemeSlot.ActiveTint] = new Color(0.722f, 0.573f, 0.227f, 0.16f),
            [ThemeSlot.AccentSoft] = new Color(0.557f, 0.133f, 0.192f, 0.08f),
            [ThemeSlot.OverlayDim] = new Color(0.078f, 0.094f, 0.141f, 0.55f),
            [ThemeSlot.MapPreviewTint] = new Color(0.729f, 0.702f, 0.620f, 1.00f),
            [ThemeSlot.MetadataLabel] = new Color(0.416f, 0.400f, 0.353f),
            [ThemeSlot.SurfaceTranslucentDark] = new Color(0.102f, 0.122f, 0.180f, 1.00f),
            [ThemeSlot.SurfaceGhostHover] = new Color(0.784f, 0.761f, 0.690f, 1.00f),
            [ThemeSlot.ScrimDefault] = new Color(0.078f, 0.094f, 0.141f, 1.00f),
            [ThemeSlot.SurfaceTooltip] = new Color(0.102f, 0.122f, 0.180f, 1.00f),
            [ThemeSlot.BorderTooltip] = new Color(0.722f, 0.573f, 0.227f, 0.45f),
            [ThemeSlot.Glass1] = new Color(0.953f, 0.925f, 0.847f, 0.55f),
            [ThemeSlot.Glass2] = new Color(0.890f, 0.875f, 0.816f, 0.85f),
            [ThemeSlot.Glass3] = new Color(0.890f, 0.875f, 0.816f, 0.95f),
            [ThemeSlot.GlassFrost] = new Color(0.847f, 0.827f, 0.769f, 0.82f),
            [ThemeSlot.AccentGlow] = new Color(0.557f, 0.133f, 0.192f, 0.40f),
            [ThemeSlot.ShelfTint] = new Color(0.102f, 0.122f, 0.180f, 0.04f),
            [ThemeSlot.WindowHeaderTint] = new Color(0.722f, 0.573f, 0.227f, 0.16f),
            [ThemeSlot.WindowFooterTint] = new Color(0.722f, 0.573f, 0.227f, 0.10f),
            [ThemeSlot.ButtonPrimaryFill] = new Color(0.659f, 0.165f, 0.227f, 1.00f),
            [ThemeSlot.ButtonPrimaryFillHover] = new Color(0.698f, 0.176f, 0.247f, 1.00f),
            [ThemeSlot.ShadowModal] = new Color(0.078f, 0.094f, 0.141f, 0.30f),
            [ThemeSlot.ShadowCard] = new Color(0.078f, 0.094f, 0.141f, 0.25f),
            [ThemeSlot.ShadowPopover] = new Color(0.078f, 0.094f, 0.141f, 0.30f),
            [ThemeSlot.ShadowTooltip] = new Color(0.078f, 0.094f, 0.141f, 0.30f),
            [ThemeSlot.ShadowToast] = new Color(0.078f, 0.094f, 0.141f, 0.25f),
            [ThemeSlot.ShadowRim] = new Color(0.078f, 0.094f, 0.141f, 0.20f),
            [ThemeSlot.ShadowInsetTop] = new Color(1.000f, 1.000f, 1.000f, 0.45f),
            [ThemeSlot.ShadowInsetTopStrong] = new Color(1.000f, 1.000f, 1.000f, 0.55f),
            [ThemeSlot.TextShadowEmboss] = new Color(1.000f, 0.902f, 0.706f, 0.30f),
            [ThemeSlot.TextShadowDeep] = new Color(1.000f, 1.000f, 1.000f, 0.50f),
            [ThemeSlot.TextShadowDisplay] = new Color(0.000f, 0.000f, 0.000f, 0.50f),
            [ThemeSlot.GrainTint] = new Color(0.102f, 0.122f, 0.180f, 0.13f),
        };
        return BaseTheme.Compose(colors, body, bodyBold, heading, display, mono);
    }
}