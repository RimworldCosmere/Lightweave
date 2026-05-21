using Cosmere.Lightweave.Tokens;
using UnityEngine;

namespace Cosmere.Lightweave.Types;

public enum InsetEdge {
    Top,
    Bottom,
}

public abstract record ShadowSpec {
    public static ShadowSpec Of(ThemeSlot slot) {
        return new SlotRef(slot);
    }

    public static implicit operator ShadowSpec(ThemeSlot s) {
        return Of(s);
    }

    public sealed record SlotRef(ThemeSlot Slot) : ShadowSpec;

    public sealed record Drop(
        ColorRef Color,
        Vector2 OffsetPx,
        float BlurPx,
        float SpreadPx
    ) : ShadowSpec;

    public sealed record Layered(Drop[] Stops) : ShadowSpec;

    public sealed record Inset(
        ColorRef Color,
        float HeightPx,
        InsetEdge Edge
    ) : ShadowSpec;
}
