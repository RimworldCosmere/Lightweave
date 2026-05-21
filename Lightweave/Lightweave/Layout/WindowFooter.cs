using System;
using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;

namespace Cosmere.Lightweave.Layout;

public static class WindowFooter {
    public static LightweaveNode Create(
        LightweaveNode? content = null,
        LightweaveNode? actions = null,
        bool drawDivider = true,
        bool showResizeGrip = false,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        bool hasContent = content != null;
        bool hasActions = actions != null;

        Rem padX = new Rem(1.375f);
        Rem padY = new Rem(0.875f);

        LightweaveNode row = HStack.Create(SpacingScale.Md, h => {
            if (hasContent) {
                h.AddFlex(content!);
            }
            else {
                h.AddFlex(Spacer.Flex());
            }
            if (hasActions) {
                h.AddHug(actions!);
            }
        }, style: new Style {
            Padding = new EdgeInsets(
                Top: padY,
                Right: padX,
                Bottom: padY,
                Left: padX
            ),
        });

        LightweaveNode inner = Stack.Create(SpacingScale.None, s => {
            if (drawDivider) {
                s.Add(Divider.Horizontal());
            }
            s.Add(row);
        });

        LightweaveNode root = Box.Create(
            children: b => b.Add(inner),
            style: style,
            classes: StyleExtensions.PrependClass("window-footer", classes),
            id: id,
            line: line,
            file: file
        );

        Action<Rect, Action>? innerPaint = root.Paint;
        root.Paint = (rect, paintChildren) => {
            LightweaveWindowContext.PublishFooter(rect, showResizeGrip);
            innerPaint?.Invoke(rect, paintChildren);
            if (showResizeGrip) {
                Theme.Theme theme = RenderContext.Current.Theme;
                bool rtl = RenderContext.Current.Direction == Direction.Rtl;
                DrawResizeGrip(rect, theme, rtl);
            }
        };

        return root;
    }

    private static void DrawResizeGrip(Rect footerRect, Theme.Theme theme, bool rtl) {
        float pad = SpacingScale.Xxs.ToPixels();
        float dot = new Rem(0.125f).ToPixels();
        float gap = new Rem(0.125f).ToPixels();
        float startX = rtl ? footerRect.x + pad : footerRect.xMax - pad - (dot + gap) * 3f + gap;
        float baseY = footerRect.yMax - pad - dot;

        Color hint = theme.GetColor(ThemeSlot.TextMuted);

        for (int row = 0; row < 3; row++) {
            for (int col = row; col < 3; col++) {
                float x = startX + col * (dot + gap);
                float y = baseY - row * (dot + gap);
                Rect d = new Rect(x, y, dot, dot);
                PaintBox.Fill(d, hint);
            }
        }
    }
}
