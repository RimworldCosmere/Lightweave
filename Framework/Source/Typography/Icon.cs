using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Cosmere.Lightweave.Layout;
using static Cosmere.Lightweave.Doc.DocChips;
using static Cosmere.Lightweave.Typography.Typography;
using Verse;

namespace Cosmere.Lightweave.Typography;

public static partial class Typography {
    [Doc(
        Id = "icon",
        Summary = "Themed bitmap icon scaled to fit and tinted by a theme color.",
        WhenToUse = "Inline glyphs in buttons, list rows, and toolbars.",
        SourcePath = "Lightweave/Typography/Icon.cs"
    )]
    public static class Icon {
        public static LightweaveNode Create(
            [DocParam("Source texture. Use a stuff-tintable grayscale asset for theme-driven coloring.")]
            Texture texture,
            [DocParam("Square size in Rem. Defaults to 1.5rem.")]
            Rem? size = null,
            [DocParam("Mirror horizontally when the layout direction is RTL.")]
            bool mirrorInRtl = false,
            [DocParam("Inline style override.", TypeOverride = "Style?", DefaultOverride = "null")]
            Style? style = null,
            [DocParam("Additional class names merged after the base 'icon' class.", TypeOverride = "string[]?", DefaultOverride = "null")]
            string[]? classes = null,
            [DocParam("Stable id for state-style lookup.", TypeOverride = "string?", DefaultOverride = "null")]
            string? id = null,
            [CallerLineNumber] int line = 0,
            [CallerFilePath] string file = ""
        ) {
            LightweaveNode node = NodeBuilder.New("Icon", line, file);
            node.ApplyStyling("icon", style, classes, id);
            float pxSize = (size ?? new Rem(1.5f)).ToPixels();
            // A fixed Width/Height style (e.g. a faction badge tile larger than its glyph) drives the
            // measured box so parents like HStack center the whole tile, not just the inner glyph.
            // Falls back to the glyph size when no fixed dimension is set (the common case).
            Style boxStyle = node.GetResolvedStyle();
            float boxHeight = boxStyle.Height is { Mode: Length.Kind.Rem } fixedH ? fixedH.ToPixels(0f, 0f) : pxSize;
            float boxWidth = boxStyle.Width is { Mode: Length.Kind.Rem } fixedW ? fixedW.ToPixels(0f, 0f) : pxSize;
            node.PreferredHeight = boxHeight;
            node.MeasureWidth = () => boxWidth;
            node.Paint = (rect, _) => {
                Theme.Theme theme = RenderContext.Current.Theme;
                Style s = node.GetResolvedStyle();
                float drawPx = Mathf.Min(pxSize, Mathf.Min(rect.width, rect.height));
                Rect r = new Rect(
                    rect.x + (rect.width - drawPx) / 2f,
                    rect.y + (rect.height - drawPx) / 2f,
                    drawPx,
                    drawPx
                );
                ColorRef? cr = s.TextColor;
                Color c = cr switch {
                    ColorRef.Literal lit => lit.Value,
                    ColorRef.Token tok => theme.GetColor(tok.Slot),
                    _ => Color.white,
                };
                Matrix4x4 saved = default;
                bool pushed = false;
                if (mirrorInRtl) {
                    saved = IconMirror.PushIfRtl(r, RenderContext.Current.Direction);
                    pushed = true;
                }

                PaintBox.DrawTexture(r, texture, c, ScaleMode.ScaleToFit);
                if (pushed) {
                    IconMirror.Pop(saved);
                }
            };
            return node;
        }

        [DocVariant("CL_Playground_Label_Default")]
        public static DocSample DocsDefault() {
            return new DocSample(() => Icon.Create(TexButton.Info, new Rem(1.5f), style: new Style { TextColor = ThemeSlot.TextPrimary }));
        }

        [DocVariant("CL_Playground_Label_Accent")]
        public static DocSample DocsAccent() {
            return new DocSample(() => Icon.Create(TexButton.Search, new Rem(1.5f), style: new Style { TextColor = ThemeSlot.SurfaceAccent }));
        }

        [DocVariant("CL_Playground_Label_Muted")]
        public static DocSample DocsMuted() {
            return new DocSample(() => Icon.Create(TexButton.CloseXSmall, new Rem(1.5f), style: new Style { TextColor = ThemeSlot.TextMuted }));
        }

        [DocUsage]
        public static DocSample DocsUsage() {
            return new DocSample(() => Icon.Create(TexButton.Plus, new Rem(1.5f), style: new Style { TextColor = ThemeSlot.TextPrimary }));
        }
    }
}
