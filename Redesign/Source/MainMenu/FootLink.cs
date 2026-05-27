using System;
using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;

namespace Cosmere.Lightweave.Redesign.MainMenu;

public static class FootLink {
    public static LightweaveNode Create(
        string label,
        Action onClick,
        bool indicateMenu = false,
        bool expanded = false,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        Hooks.Hooks.RefHandle<float> flipRatio = Hooks.Hooks.UseRef<float>(expanded ? 1f : 0f, line, file + "#foot-flip");

        LightweaveNode node = NodeBuilder.New($"FootLink:{label}", line, file);
        node.ApplyStyling("foot-link", style, classes, id);
        node.PreferredHeight = new Rem(2f).ToPixels();
        node.MeasureWidth = () => MeasureWidth(label, indicateMenu);

        node.Paint = (rect, _) => {
            InteractionState state = InteractionState.Resolve(rect, null, false);
            bool active = state.Hovered || state.Pressed;
            ThemeSlot slot = active ? ThemeSlot.SurfaceAccent : ThemeSlot.TextSecondary;
            Color color = RenderContext.Current.Theme.GetColor(slot);

            string upper = (label ?? string.Empty).ToUpperInvariant();
            Rem fontSize = new Rem(0.65625f);
            int pixelSize = Mathf.RoundToInt(fontSize.ToFontPx());
            float tracking = pixelSize * 0.18f;
            float chevronGap = pixelSize * 0.6f;
            float chevronW = indicateMenu ? pixelSize * 0.8f : 0f;

            float labelW = TextDraw.MeasureTracked(upper, FontRole.Mono, fontSize, tracking);
            float totalW = labelW + (indicateMenu ? chevronGap + chevronW : 0f);
            float startX = rect.x + (rect.width - totalW) * 0.5f;

            Rect labelRect = new Rect(startX, rect.y, labelW, rect.height);
            TextDraw.DrawTracked(labelRect, upper, FontRole.Mono, fontSize, TextAnchor.MiddleLeft, slot, tracking);

            if (indicateMenu) {
                float target = expanded ? 1f : 0f;
                float dt = Time.unscaledDeltaTime;
                flipRatio.Current = Mathf.MoveTowards(flipRatio.Current, target, dt / 0.16f);

                float cx = startX + labelW + chevronGap + chevronW * 0.5f;
                float cy = rect.y + rect.height * 0.5f;
                DrawChevron(cx, cy, chevronW, color, flipRatio.Current);
            }

            InteractionFeedback.Apply(rect, true, true);
            node.MeasuredRect = rect;

            Event e = Event.current;
            if (e.type == EventType.MouseUp && e.button == 0 && rect.Contains(e.mousePosition)) {
                onClick?.Invoke();
                e.Use();
            }
        };
        return node;
    }

    private static float MeasureWidth(string label, bool indicateMenu) {
        Rem fontSize = new Rem(0.65625f);
        int pixelSize = Mathf.RoundToInt(fontSize.ToFontPx());
        string upper = (label ?? string.Empty).ToUpperInvariant();
        float tracking = pixelSize * 0.18f;
        float w = TextDraw.MeasureTracked(upper, FontRole.Mono, fontSize, tracking);
        if (indicateMenu) {
            w += pixelSize * 0.6f + pixelSize * 0.8f;
        }
        return w + new Rem(1f).ToPixels();
    }


    private static void DrawChevron(float cx, float cy, float size, Color color, float flipRatio) {
        if (Event.current.type != EventType.Repaint) {
            return;
        }

        float boxSize = size * 1.4f;
        Rect chevronRect = RectSnap.Snap(new Rect(cx - boxSize * 0.5f, cy - boxSize * 0.5f, boxSize, boxSize));

        float scaleY = 1f - 2f * flipRatio;
        Texture2D tex = ChevronTextureCache.Down(strokePx: 38f, armSpan: 0.7f, armHeight: 0.42f);
        using (ScaleScope.Around(new Vector2(1f, scaleY), chevronRect.center)) {
            PaintBox.DrawTexture(chevronRect, tex, color);
        }
    }

    
}
