using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;

namespace Cosmere.Lightweave.MainMenu;

public static class AccentStripe {
    public static LightweaveNode Create() {
        LightweaveNode node = NodeBuilder.New("AccentStripe");
        node.PreferredHeight = 2f;
        node.Paint = (rect, _) => {
            if (Event.current.type != EventType.Repaint) {
                return;
            }
            Color accent = RenderContext.Current.Theme.GetColor(ThemeSlot.SurfaceAccent);
            accent.a = 0.85f;
            PaintBox.Fill(rect, accent);
        };
        return node;
    }

    

    
}

public static class LightweaveWordmark {
    public static LightweaveNode Create() {
        LightweaveNode node = NodeBuilder.New("LightweaveWordmark");
        node.PreferredHeight = new Rem(1.0f).ToPixels();
        node.Paint = (rect, _) => {
            if (Event.current.type != EventType.Repaint) {
                return;
            }

            string powered = "CL_MainMenu_PoweredBy".Translate();
            string mark = "Lightweave";
            Rem fontSize = new Rem(0.7f);
            Vector2 poweredSize = TextDraw.Measure(powered, FontRole.Body, fontSize);
            Vector2 markSize = TextDraw.Measure(mark, FontRole.Body, fontSize);
            float gap = 6f;
            float totalWidth = poweredSize.x + gap + markSize.x;
            float startX = rect.x + (rect.width - totalWidth) * 0.5f;

            Rect poweredRect = new Rect(startX, rect.y, poweredSize.x, rect.height);
            Rect markRect = new Rect(startX + poweredSize.x + gap, rect.y, markSize.x, rect.height);

            using (TintScope.Opacity(0.85f)) {
                TextDraw.Draw(poweredRect, powered, FontRole.Body, fontSize, TextAnchor.MiddleCenter, ThemeSlot.TextMuted);
                TextDraw.Draw(markRect, mark, FontRole.Body, fontSize, TextAnchor.MiddleCenter, ThemeSlot.SurfaceAccent);
            }
        };
        return node;
    }

    

    
}
