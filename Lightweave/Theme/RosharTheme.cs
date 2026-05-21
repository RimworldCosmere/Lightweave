using Cosmere.Lightweave.Tokens;
using UnityEngine;

namespace Cosmere.Lightweave.Theme;

[LightweaveTheme("roshar", "CL_Theme_Roshar", Order = 30)]
public static class RosharTheme {
    public static Theme Build(Font body, Font bodyBold, Font heading, Font display, Font mono) {
        Dictionary<ThemeSlot, Color> colors = new Dictionary<ThemeSlot, Color> {
            [ThemeSlot.SurfacePrimary] = new Color(0.940f, 0.935f, 0.905f, 0.97f),
            [ThemeSlot.SurfaceRaised] = new Color(0.980f, 0.975f, 0.945f, 1.00f),
            [ThemeSlot.SurfaceSunken] = new Color(0.880f, 0.870f, 0.840f, 1f),
            [ThemeSlot.SurfaceTranslucent] = new Color(0.940f, 0.930f, 0.900f, 0.35f),
            [ThemeSlot.SurfaceAccent] = new Color(0.175f, 0.470f, 0.515f, 1.00f),
            [ThemeSlot.SurfaceShadow] = new Color(0.090f, 0.105f, 0.130f, 0.20f),
            [ThemeSlot.SurfaceInput] = new Color(1.000f, 0.995f, 0.965f, 1.00f),
            [ThemeSlot.SurfaceDisabled] = new Color(0.860f, 0.855f, 0.825f, 1.00f),
            [ThemeSlot.TextPrimary] = new Color(0.140f, 0.155f, 0.180f),
            [ThemeSlot.TextSecondary] = new Color(0.320f, 0.340f, 0.375f),
            [ThemeSlot.TextMuted] = new Color(0.480f, 0.495f, 0.525f),
            [ThemeSlot.TextOnAccent] = new Color(0.985f, 0.985f, 0.970f),
            [ThemeSlot.TextOnDanger] = new Color(0.985f, 0.975f, 0.970f),
            [ThemeSlot.BorderDefault] = new Color(0.600f, 0.640f, 0.660f, 1f),
            [ThemeSlot.BorderSubtle] = new Color(0.770f, 0.790f, 0.790f, 1f),
            [ThemeSlot.BorderFocus] = new Color(0.280f, 0.680f, 0.740f, 1f),
            [ThemeSlot.BorderHover] = new Color(0.440f, 0.505f, 0.540f, 1f),
            [ThemeSlot.BorderOff] = new Color(0.560f, 0.600f, 0.620f, 1f),
            [ThemeSlot.BorderDanger] = new Color(0.720f, 0.250f, 0.230f, 1f),
            [ThemeSlot.StatusWarning] = new Color(0.720f, 0.540f, 0.180f),
            [ThemeSlot.StatusDanger] = new Color(0.680f, 0.220f, 0.200f),
            [ThemeSlot.StatusSuccess] = new Color(0.300f, 0.560f, 0.340f),
            [ThemeSlot.HoverTint] = new Color(0.141f, 0.239f, 0.420f, 0.12f),
            [ThemeSlot.ActiveTint] = new Color(0.141f, 0.239f, 0.420f, 0.15f),
            [ThemeSlot.AccentSoft] = new Color(0.141f, 0.239f, 0.420f, 0.08f),
            [ThemeSlot.OverlayDim] = new Color(0.070f, 0.085f, 0.105f, 0.55f),
            [ThemeSlot.MapPreviewTint] = new Color(0.820f, 0.810f, 0.780f, 1.00f),
            [ThemeSlot.MetadataLabel] = new Color(0.600f, 0.615f, 0.640f),
            [ThemeSlot.SurfaceTranslucentDark] = new Color(0.140f, 0.155f, 0.180f, 1.00f),
            [ThemeSlot.SurfaceGhostHover] = new Color(0.700f, 0.700f, 0.685f, 1.00f),
            [ThemeSlot.ScrimDefault] = new Color(0.090f, 0.105f, 0.130f, 1.00f),
            [ThemeSlot.SurfaceTooltip] = new Color(0.030f, 0.040f, 0.058f, 1.00f),
            [ThemeSlot.BorderTooltip] = new Color(0.500f, 0.700f, 0.820f, 0.32f),
            [ThemeSlot.Glass1] = new Color(0.847f, 0.831f, 0.776f, 0.55f),
            [ThemeSlot.Glass2] = new Color(0.831f, 0.812f, 0.753f, 0.85f),
            [ThemeSlot.Glass3] = new Color(0.831f, 0.812f, 0.753f, 0.95f),
            [ThemeSlot.GlassFrost] = new Color(0.788f, 0.765f, 0.702f, 0.82f),
            [ThemeSlot.AccentGlow] = new Color(0.373f, 0.769f, 0.824f, 0.55f),
            [ThemeSlot.ShelfTint] = new Color(0.110f, 0.133f, 0.188f, 0.05f),
            [ThemeSlot.WindowHeaderTint] = new Color(0.627f, 0.486f, 0.243f, 0.14f),
            [ThemeSlot.WindowFooterTint] = new Color(0.627f, 0.486f, 0.243f, 0.10f),
            [ThemeSlot.ButtonPrimaryFill] = new Color(0.165f, 0.275f, 0.467f, 1.00f),
            [ThemeSlot.ButtonPrimaryFillHover] = new Color(0.188f, 0.314f, 0.533f, 1.00f),
            [ThemeSlot.ShadowModal] = new Color(0.078f, 0.094f, 0.133f, 0.30f),
            [ThemeSlot.ShadowCard] = new Color(0.078f, 0.094f, 0.133f, 0.25f),
            [ThemeSlot.ShadowPopover] = new Color(0.078f, 0.094f, 0.133f, 0.30f),
            [ThemeSlot.ShadowTooltip] = new Color(0.078f, 0.094f, 0.133f, 0.30f),
            [ThemeSlot.ShadowToast] = new Color(0.078f, 0.094f, 0.133f, 0.25f),
            [ThemeSlot.ShadowRim] = new Color(0.078f, 0.094f, 0.133f, 0.22f),
            [ThemeSlot.ShadowInsetTop] = new Color(0.929f, 0.902f, 0.827f, 0.50f),
            [ThemeSlot.ShadowInsetTopStrong] = new Color(0.929f, 0.902f, 0.827f, 0.60f),
            [ThemeSlot.TextShadowEmboss] = new Color(1.000f, 0.902f, 0.706f, 0.30f),
            [ThemeSlot.TextShadowDeep] = new Color(0.929f, 0.902f, 0.827f, 0.55f),
            [ThemeSlot.TextShadowDisplay] = new Color(0.000f, 0.000f, 0.000f, 0.50f),
            [ThemeSlot.GrainTint] = new Color(0.110f, 0.129f, 0.188f, 0.14f),
        };
        return BaseTheme.Compose(colors, body, bodyBold, heading, display, mono);
    }
}