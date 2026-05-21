using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;

namespace Cosmere.Lightweave.MainMenu;

public static class GlyphIcon {
    public static LightweaveNode Create(string glyph, Rem? sizeOverride = null) {
        LightweaveNode node = NodeBuilder.New($"GlyphIcon:{glyph}");
        Rem size = sizeOverride ?? new Rem(1f);
        float pxSize = size.ToPixels();
        node.PreferredHeight = pxSize;
        node.MeasureWidth = () => pxSize;

        node.Paint = (rect, _) => {
            if (Event.current.type != EventType.Repaint) {
                return;
            }
            TextDraw.Draw(rect, glyph, FontRole.Body, new Rem(0.8125f), TextAnchor.MiddleCenter, ThemeSlot.TextMuted);
        };
        return node;
    }
}
