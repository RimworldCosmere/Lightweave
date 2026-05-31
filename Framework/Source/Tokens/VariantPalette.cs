using Cosmere.Lightweave.Input;

namespace Cosmere.Lightweave.Tokens;

public sealed class VariantSpec {
    public System.Func<InteractionState, ThemeSlot?> Background { get; init; } = _ => ThemeSlot.SurfacePrimary;
    public System.Func<InteractionState, ThemeSlot> Foreground { get; init; } = _ => ThemeSlot.TextPrimary;
    public System.Func<InteractionState, ThemeSlot?> Border { get; init; } = _ => ThemeSlot.BorderDefault;
    public ThemeSlot? GhostForeground { get; init; }
    public ThemeSlot? GhostBorder { get; init; }
}

public static class VariantPalette {
    private static readonly System.Collections.Generic.Dictionary<Variant, VariantSpec> registry =
        new System.Collections.Generic.Dictionary<Variant, VariantSpec> {
            [Variant.Primary] = new VariantSpec {
                Background = _ => ThemeSlot.SurfaceAccent,
                Foreground = _ => ThemeSlot.TextOnAccent,
                Border = _ => ThemeSlot.BorderDefault,
                GhostForeground = ThemeSlot.SurfaceAccent,
                GhostBorder = ThemeSlot.SurfaceAccent,
            },
            [Variant.Secondary] = new VariantSpec {
                Background = state => {
                    if (state.Pressed) {
                        return ThemeSlot.SurfaceSunken;
                    }

                    if (state.Hovered) {
                        return ThemeSlot.SurfaceRaised;
                    }

                    return ThemeSlot.SurfaceTranslucent;
                },
                Foreground = _ => ThemeSlot.TextPrimary,
                Border = state => state.Hovered ? ThemeSlot.BorderHover : ThemeSlot.BorderDefault,
                GhostForeground = null,
                GhostBorder = null,
            },
            [Variant.Ghost] = new VariantSpec {
                Background = _ => (ThemeSlot?)null,
                Foreground = _ => ThemeSlot.TextPrimary,
                Border = state => state.Hovered ? (ThemeSlot?)ThemeSlot.BorderHover : null,
                GhostForeground = null,
                GhostBorder = null,
            },
            [Variant.Danger] = new VariantSpec {
                Background = _ => ThemeSlot.StatusDanger,
                Foreground = _ => ThemeSlot.TextOnDanger,
                Border = _ => ThemeSlot.BorderDefault,
                GhostForeground = ThemeSlot.StatusDanger,
                GhostBorder = ThemeSlot.StatusDanger,
            },
            [Variant.Frosted] = new VariantSpec {
                Background = _ => ThemeSlot.SurfacePrimary,
                Foreground = _ => ThemeSlot.TextPrimary,
                Border = state => state.Hovered || state.Pressed ? ThemeSlot.BorderHover : ThemeSlot.BorderSubtle,
                GhostForeground = null,
                GhostBorder = null,
            },
            [Variant.Quiet] = new VariantSpec {
                Background = _ => (ThemeSlot?)null,
                Foreground = state => state.Hovered || state.Pressed ? ThemeSlot.TextPrimary : ThemeSlot.TextMuted,
                Border = _ => (ThemeSlot?)null,
                GhostForeground = null,
                GhostBorder = null,
            },
            [Variant.Solid] = new VariantSpec {
                Background = state => {
                    if (state.Pressed) {
                        return ThemeSlot.SurfaceSunken;
                    }

                    if (state.Hovered) {
                        return ThemeSlot.SurfaceRaised;
                    }

                    return ThemeSlot.SurfaceTranslucent;
                },
                Foreground = state => state.Hovered || state.Pressed ? ThemeSlot.TextPrimary : ThemeSlot.TextSecondary,
                Border = _ => (ThemeSlot?)null,
                GhostForeground = null,
                GhostBorder = null,
            },
        };

    public static void Register(Variant variant, VariantSpec spec) {
        if (spec == null) {
            throw new System.ArgumentNullException(nameof(spec));
        }

        registry[variant] = spec;
    }

    public static bool IsRegistered(Variant variant) {
        return registry.ContainsKey(variant);
    }

    private static VariantSpec Get(Variant variant) {
        if (registry.TryGetValue(variant, out VariantSpec spec)) {
            return spec;
        }

        return registry[Variant.Primary];
    }

    public static ThemeSlot? Background(Variant variant, InteractionState state, bool ghost = false) {
        if (state.Disabled) {
            return ghost ? (ThemeSlot?)null : ThemeSlot.SurfaceDisabled;
        }

        if (ghost) {
            return null;
        }

        return Get(variant).Background(state);
    }

    public static ThemeSlot Foreground(Variant variant, InteractionState state, bool ghost = false) {
        if (state.Disabled) {
            return ThemeSlot.TextMuted;
        }

        VariantSpec spec = Get(variant);
        if (ghost) {
            return spec.GhostForeground ?? ThemeSlot.TextPrimary;
        }

        return spec.Foreground(state);
    }

    public static ThemeSlot? Border(Variant variant, InteractionState state, bool ghost = false) {
        if (state.Disabled) {
            return ghost ? (ThemeSlot?)null : ThemeSlot.BorderSubtle;
        }

        VariantSpec spec = Get(variant);
        if (ghost) {
            return spec.GhostBorder;
        }

        return spec.Border(state);
    }

    public static float OverlayAlpha(InteractionState state) {
        if (state.Disabled) {
            return 0f;
        }

        if (state.Pressed) {
            return 0.22f;
        }

        if (state.Hovered) {
            return 0.14f;
        }

        return 0f;
    }
}
