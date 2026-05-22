using Cosmere.Lightweave.Tokens;
using UnityEngine;

namespace Cosmere.Lightweave.Theme;

[LightweaveTheme("scadrial", "CL_Theme_Scadrial", Order = 20)]
public static class ScadrialTheme {
    public static Theme Build(Font body, Font bodyBold, Font heading, Font display, Font mono) {
        Dictionary<ThemeSlot, Color> colors = new Dictionary<ThemeSlot, Color> {
            [ThemeSlot.SurfacePrimary] = new Color(0.088f, 0.106f, 0.130f, 0.96f),
            [ThemeSlot.SurfaceRaised] = new Color(0.138f, 0.158f, 0.188f, 1.00f),
            [ThemeSlot.SurfaceSunken] = new Color(0.052f, 0.062f, 0.078f, 1f),
            [ThemeSlot.SurfaceTranslucent] = new Color(0.020f, 0.025f, 0.035f, 0.35f),
            [ThemeSlot.SurfaceAccent] = new Color(0.420f, 0.580f, 0.760f, 0.95f),
            [ThemeSlot.SurfaceShadow] = new Color(0.000f, 0.000f, 0.000f, 0.40f),
            [ThemeSlot.SurfaceInput] = new Color(0.170f, 0.195f, 0.230f, 1.00f),
            [ThemeSlot.SurfaceDisabled] = new Color(0.210f, 0.235f, 0.270f, 1.00f),
            [ThemeSlot.TextPrimary] = new Color(0.920f, 0.940f, 0.960f),
            [ThemeSlot.TextSecondary] = new Color(0.780f, 0.820f, 0.860f),
            [ThemeSlot.TextMuted] = new Color(0.560f, 0.600f, 0.660f),
            [ThemeSlot.TextOnAccent] = new Color(0.060f, 0.080f, 0.120f),
            [ThemeSlot.TextOnDanger] = new Color(0.985f, 0.980f, 0.985f),
            [ThemeSlot.BorderDefault] = new Color(0.380f, 0.430f, 0.500f, 1f),
            [ThemeSlot.BorderSubtle] = new Color(0.240f, 0.270f, 0.315f, 1f),
            [ThemeSlot.BorderFocus] = new Color(0.480f, 0.680f, 0.860f, 1f),
            [ThemeSlot.BorderHover] = new Color(0.540f, 0.590f, 0.660f, 1f),
            [ThemeSlot.BorderOff] = new Color(0.340f, 0.385f, 0.450f, 1f),
            [ThemeSlot.BorderDanger] = new Color(0.860f, 0.380f, 0.335f, 1f),
            [ThemeSlot.StatusWarning] = new Color(0.900f, 0.720f, 0.300f),
            [ThemeSlot.StatusDanger] = new Color(0.820f, 0.340f, 0.300f),
            [ThemeSlot.StatusSuccess] = new Color(0.500f, 0.720f, 0.460f),
            [ThemeSlot.StatusInfo] = new Color(0.400f, 0.560f, 0.700f),
            [ThemeSlot.HoverTint] = new Color(0.761f, 0.353f, 0.180f, 0.14f),
            [ThemeSlot.ActiveTint] = new Color(0.761f, 0.353f, 0.180f, 0.18f),
            [ThemeSlot.AccentSoft] = new Color(0.761f, 0.353f, 0.180f, 0.10f),
            [ThemeSlot.OverlayDim] = new Color(0.020f, 0.025f, 0.035f, 0.62f),
            [ThemeSlot.MapPreviewTint] = new Color(0.165f, 0.185f, 0.220f, 1.00f),
            [ThemeSlot.MetadataLabel] = new Color(0.420f, 0.450f, 0.495f),
            [ThemeSlot.SurfaceTranslucentDark] = new Color(0.020f, 0.025f, 0.035f, 1.00f),
            [ThemeSlot.SurfaceGhostHover] = new Color(0.090f, 0.115f, 0.150f, 1.00f),
            [ThemeSlot.ScrimDefault] = new Color(0.000f, 0.000f, 0.000f, 1.00f),
            [ThemeSlot.SurfaceTooltip] = new Color(0.045f, 0.040f, 0.050f, 1.00f),
            [ThemeSlot.BorderTooltip] = new Color(0.580f, 0.620f, 0.700f, 0.30f),
            [ThemeSlot.Glass1] = new Color(0.102f, 0.125f, 0.157f, 0.55f),
            [ThemeSlot.Glass2] = new Color(0.102f, 0.125f, 0.157f, 0.78f),
            [ThemeSlot.Glass3] = new Color(0.067f, 0.086f, 0.114f, 0.92f),
            [ThemeSlot.GlassFrost] = new Color(0.047f, 0.071f, 0.094f, 0.70f),
            [ThemeSlot.AccentGlow] = new Color(0.847f, 0.831f, 0.910f, 0.45f),
            [ThemeSlot.ShelfTint] = new Color(0.024f, 0.039f, 0.063f, 0.30f),
            [ThemeSlot.WindowHeaderTint] = new Color(0.482f, 0.302f, 0.541f, 0.14f),
            [ThemeSlot.WindowFooterTint] = new Color(0.482f, 0.302f, 0.541f, 0.10f),
            [ThemeSlot.ButtonPrimaryFill] = new Color(0.784f, 0.384f, 0.227f, 1.00f),
            [ThemeSlot.ButtonPrimaryFillHover] = new Color(0.839f, 0.447f, 0.290f, 1.00f),
            [ThemeSlot.ShadowModal] = new Color(0.024f, 0.039f, 0.063f, 0.65f),
            [ThemeSlot.ShadowCard] = new Color(0.024f, 0.039f, 0.063f, 0.55f),
            [ThemeSlot.ShadowPopover] = new Color(0.024f, 0.039f, 0.063f, 0.65f),
            [ThemeSlot.ShadowTooltip] = new Color(0.024f, 0.039f, 0.063f, 0.60f),
            [ThemeSlot.ShadowToast] = new Color(0.024f, 0.039f, 0.063f, 0.50f),
            [ThemeSlot.ShadowRim] = new Color(0.024f, 0.039f, 0.063f, 0.55f),
            [ThemeSlot.ShadowInsetTop] = new Color(0.847f, 0.831f, 0.910f, 0.06f),
            [ThemeSlot.ShadowInsetTopStrong] = new Color(0.957f, 0.925f, 0.878f, 0.32f),
            [ThemeSlot.TextShadowEmboss] = new Color(0.957f, 0.925f, 0.878f, 0.30f),
            [ThemeSlot.TextShadowDeep] = new Color(0.000f, 0.000f, 0.000f, 0.50f),
            [ThemeSlot.TextShadowDisplay] = new Color(0.000f, 0.000f, 0.000f, 0.55f),
            [ThemeSlot.GrainTint] = new Color(0.722f, 0.761f, 0.820f, 0.12f),
        };
        return BaseTheme.Compose(colors, body, bodyBold, heading, display, mono);
    }
}