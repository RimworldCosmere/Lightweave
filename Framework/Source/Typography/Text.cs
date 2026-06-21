using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Surfaces;
using static Cosmere.Lightweave.Doc.DocChips;
using static Cosmere.Lightweave.Typography.Typography;

namespace Cosmere.Lightweave.Typography;

public static partial class Typography {
    [Doc(
        Id = "text",
        Summary = "Single-line or wrapped text rendered with theme font and color.",
        WhenToUse = "Body copy, inline labels, or any text content. The foundation for Heading, Label, Caption, and Code.",
        SourcePath = "Lightweave/Typography/Text.cs",
        ShowRtl = true
    )]
    public static class Text {
        public static LightweaveNode Create(
            [DocParam("Text content to display.")]
            string content,
            [DocParam("Wrap to multiple lines when content exceeds available width.")]
            bool wrap = false,
            [DocParam("When not wrapping, clip the content to the available width with a trailing ellipsis instead of hard-cutting glyphs.")]
            bool truncate = false,
            [DocParam("Parse Unity rich-text tags (<b>, <i>, <color=...>, etc.) instead of rendering them literally.")]
            bool richText = false,
            [DocParam("Style applied to the text (FontFamily/FontSize/TextColor/TextAlign/FontWeight/etc).", TypeOverride = "Style?", DefaultOverride = "null")]
            Style? style = null,
            [DocParam("Additional class names merged after the base 'text' class.", TypeOverride = "string[]?", DefaultOverride = "null")]
            string[]? classes = null,
            [DocParam("Stable id for state-style lookup.", TypeOverride = "string?", DefaultOverride = "null")]
            string? id = null,
            [CallerLineNumber] int line = 0,
            [CallerFilePath] string file = ""
        ) {
            LightweaveNode node = NodeBuilder.New($"Text:{content}", line, file);
            node.ApplyStyling("text", style, classes, id);

            GUIStyle ResolveGuiStyle() {
                Style s = node.GetResolvedStyle();
                Theme.Theme theme = RenderContext.Current.Theme;
                FontRef? fr = s.FontFamily;
                Rem fontSize = s.FontSize ?? new Rem(1f);
                FontStyle weight = s.FontWeight ?? FontStyle.Normal;
                int pixelSize = Mathf.RoundToInt(fontSize.ToFontPx());
                GUIStyle guiStyle = fr switch {
                    FontRef.Literal lit => GuiStyleCache.GetOrCreate(lit.Value, pixelSize, weight),
                    FontRef.Role role => GuiStyleCache.GetOrCreate(theme, role.RoleValue, pixelSize, weight),
                    _ => GuiStyleCache.GetOrCreate(theme, FontRole.Body, pixelSize, weight),
                };
                guiStyle.wordWrap = wrap;
                guiStyle.richText = richText;
                return guiStyle;
            }

            node.Measure = availableWidth => {
                if (string.IsNullOrEmpty(content)) {
                    return 0f;
                }

                GUIStyle gs = ResolveGuiStyle();
                float h = wrap
                    ? TextMeasureCache.Height(gs, content, availableWidth)
                    : TextMeasureCache.Height(gs, content, float.MaxValue);
                Style s = node.GetResolvedStyle();
                Rem fontSize = s.FontSize ?? new Rem(1f);
                int pixelSize = Mathf.RoundToInt(fontSize.ToFontPx());
                float descenderPad = Mathf.Max(2f, pixelSize * 0.25f);
                return Mathf.Ceil(h + descenderPad);
            };

            int ResolveLetterSpacingPx() {
                Style s = node.GetResolvedStyle();
                Tracking? t = s.LetterSpacing;
                if (!t.HasValue) {
                    return 0;
                }
                Rem fontSize = s.FontSize ?? new Rem(1f);
                return Mathf.RoundToInt(t.Value.ToPixels(fontSize.ToFontPx()));
            }

            bool UseTracking() {
                if (wrap) {
                    return false;
                }
                Style s = node.GetResolvedStyle();
                Tracking? t = s.LetterSpacing;
                return t.HasValue && !Mathf.Approximately(t.Value.Em, 0f);
            }

            node.MeasureWidth = () => {
                if (string.IsNullOrEmpty(content)) {
                    return 0f;
                }

                GUIStyle gs = ResolveGuiStyle();
                if (!UseTracking()) {
                    return Mathf.Ceil(TextMeasureCache.Size(gs, content).x);
                }

                int letterSpacing = ResolveLetterSpacingPx();
                TrackedGlyphRun run = TrackedTextGlyphCache.GetOrCreate(gs.font, gs.fontSize, gs.fontStyle, letterSpacing, content);
                return Mathf.Ceil(run.TotalWidth);
            };

            float MeasureRun(string text, GUIStyle gs, bool tracking, int letterSpacing) {
                if (!tracking) {
                    return TextMeasureCache.Size(gs, text).x;
                }
                return TrackedTextGlyphCache.GetOrCreate(gs.font, gs.fontSize, gs.fontStyle, letterSpacing, text).TotalWidth;
            }

            int cachedTruncWidth = -1;
            string cachedTruncResult = content;

            string ResolveTruncated(float availWidth, GUIStyle gs, bool tracking, int letterSpacing) {
                int wkey = Mathf.RoundToInt(availWidth);
                if (cachedTruncWidth == wkey) {
                    return cachedTruncResult;
                }
                cachedTruncWidth = wkey;
                if (availWidth <= 0f || MeasureRun(content, gs, tracking, letterSpacing) <= availWidth) {
                    cachedTruncResult = content;
                    return content;
                }
                const string ellipsis = "…";
                if (MeasureRun(ellipsis, gs, tracking, letterSpacing) > availWidth) {
                    cachedTruncResult = "";
                    return "";
                }
                int lo = 0;
                int hi = content.Length;
                while (lo < hi) {
                    int mid = (lo + hi + 1) / 2;
                    if (MeasureRun(content.Substring(0, mid) + ellipsis, gs, tracking, letterSpacing) <= availWidth) {
                        lo = mid;
                    }
                    else {
                        hi = mid - 1;
                    }
                }
                cachedTruncResult = content.Substring(0, lo) + ellipsis;
                return cachedTruncResult;
            }

            node.Draw = rect => {
                Theme.Theme theme = RenderContext.Current.Theme;
                Style s = node.GetResolvedStyle();
                GUIStyle gs = ResolveGuiStyle();
                TextAlign align = s.TextAlign ?? TextAlign.Start;
                TextAnchor anchor = ResolveAnchor(align, RenderContext.Current.Direction);
                if (wrap) {
                    anchor = anchor switch {
                        TextAnchor.MiddleLeft => TextAnchor.UpperLeft,
                        TextAnchor.MiddleRight => TextAnchor.UpperRight,
                        TextAnchor.MiddleCenter => TextAnchor.UpperCenter,
                        _ => anchor,
                    };
                }

                ColorRef? cr = s.TextColor;
                Color c = cr switch {
                    ColorRef.Literal lit => lit.Value,
                    ColorRef.Token tok => theme.GetColor(tok.Slot),
                    _ => theme.GetColor(ThemeSlot.TextPrimary),
                };

                bool tracking = UseTracking();
                int letterSpacing = tracking ? ResolveLetterSpacingPx() : 0;
                string display = truncate && !wrap && !string.IsNullOrEmpty(content)
                    ? ResolveTruncated(rect.width, gs, tracking, letterSpacing)
                    : content;

                if (!tracking) {
                    gs.alignment = anchor;
                    gs.clipping = TextClipping.Clip;
                    TextDraw.DrawWithStyle(rect, display, gs, c);
                    return;
                }

                TrackedGlyphRun run = TrackedTextGlyphCache.GetOrCreate(gs.font, gs.fontSize, gs.fontStyle, letterSpacing, display);
                TrackedTextDraw.Draw(run, rect, anchor, c);
            };
            return node;
        }

        [DocVariant("CL_Playground_Label_Normal")]
        public static DocSample DocsNormal() {
            string sample = (string)"CL_Playground_Text_Sample".Translate();
            return new DocSample(() => Text.Create(sample, style: new Style { FontSize = new Rem(0.9375f) }));
        }

        [DocVariant("CL_Playground_Label_Accented")]
        public static DocSample DocsAccented() {
            string sample = (string)"CL_Playground_Text_Sample".Translate();
            return new DocSample(() =>
                Text.Create(
                    sample,
                    style: new Style {
                        FontSize = new Rem(0.9375f),
                        TextColor = ThemeSlot.SurfaceAccent,
                        FontWeight = FontStyle.Bold,
                    }
                )
            );
        }

        [DocVariant("CL_Playground_Label_Muted")]
        public static DocSample DocsMuted() {
            string sample = (string)"CL_Playground_Text_Sample".Translate();
            return new DocSample(() => Text.Create(
                sample,
                style: new Style { FontSize = new Rem(0.9375f), TextColor = ThemeSlot.TextMuted }
            ));
        }

        [DocVariant("CL_Playground_Label_Truncated")]
        public static DocSample DocsTruncated() {
            string sample = (string)"CL_Playground_Text_TruncateSample".Translate();
            return new DocSample(() => Box.Create(
                children: kids => kids.Add(Text.Create(
                    sample,
                    truncate: true,
                    style: new Style { FontSize = new Rem(0.9375f) }
                )),
                style: new Style { Width = Length.Rem(12f) }
            ));
        }

        [DocState("CL_Playground_Label_Default")]
        public static DocSample DocsDefaultState() {
            string sample = (string)"CL_Playground_Text_Sample".Translate();
            return new DocSample(() => Text.Create(sample, style: new Style { FontSize = new Rem(0.9375f) }));
        }

        [DocState("CL_Playground_Label_Muted")]
        public static DocSample DocsMutedState() {
            string sample = (string)"CL_Playground_Text_Sample".Translate();
            return new DocSample(() => Text.Create(
                sample,
                style: new Style { FontSize = new Rem(0.9375f), TextColor = ThemeSlot.TextMuted }
            ));
        }

        [DocUsage]
        public static DocSample DocsUsage() {
            return new DocSample(() =>
                Text.Create("Stormlight burns within him.", style: new Style { FontSize = new Rem(0.9375f) })
            );
        }
    }
}
