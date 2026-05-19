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
            [ThemeSlot.InteractionHover] = new Color(0.102f, 0.122f, 0.180f, 1.00f),
            [ThemeSlot.InteractionPress] = new Color(0.039f, 0.051f, 0.094f, 1.00f),
            [ThemeSlot.AccentMuted] = new Color(0.478f, 0.216f, 0.267f, 0.75f),
            [ThemeSlot.OverlayDim] = new Color(0.078f, 0.094f, 0.141f, 0.55f),
            [ThemeSlot.MapPreviewTint] = new Color(0.729f, 0.702f, 0.620f, 1.00f),
            [ThemeSlot.MetadataLabel] = new Color(0.416f, 0.400f, 0.353f),
            [ThemeSlot.SurfaceTranslucentDark] = new Color(0.102f, 0.122f, 0.180f, 1.00f),
            [ThemeSlot.SurfaceGhostHover] = new Color(0.784f, 0.761f, 0.690f, 1.00f),
            [ThemeSlot.ScrimDefault] = new Color(0.078f, 0.094f, 0.141f, 1.00f),
            [ThemeSlot.SurfaceTooltip] = new Color(0.102f, 0.122f, 0.180f, 1.00f),
            [ThemeSlot.BorderTooltip] = new Color(0.722f, 0.573f, 0.227f, 0.45f),
        };
        return BaseTheme.Compose(colors, body, bodyBold, heading, display, mono);
    }
}