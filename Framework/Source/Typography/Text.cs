using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;
using Cosmere.Lightweave.Layout;
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
                GUIContent guiContent = new GUIContent(content);
                float h = wrap
                    ? gs.CalcHeight(guiContent, availableWidth)
                    : gs.CalcHeight(guiContent, float.MaxValue);
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
                    return Mathf.Ceil(gs.CalcSize(new GUIContent(content)).x);
                }

                int letterSpacing = ResolveLetterSpacingPx();
                float total = 0f;
                GUIContent gc = new GUIContent();
                for (int i = 0; i < content.Length; i++) {
                    gc.text = content[i].ToString();
                    total += gs.CalcSize(gc).x;
                    if (i < content.Length - 1) {
                        total += letterSpacing;
                    }
                }
                return Mathf.Ceil(total);
            };

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

                if (!UseTracking()) {
                    gs.alignment = anchor;
                    gs.clipping = TextClipping.Clip;
                    TextDraw.DrawWithStyle(rect, content, gs, c);
                    return;
                }

                int letterSpacing = ResolveLetterSpacingPx();
                gs.alignment = TextAnchor.MiddleLeft;
                gs.clipping = TextClipping.Overflow;
                GUIContent gc = new GUIContent();
                float totalW = 0f;
                for (int i = 0; i < content.Length; i++) {
                    gc.text = content[i].ToString();
                    totalW += Mathf.Ceil(gs.CalcSize(gc).x);
                    if (i < content.Length - 1) {
                        totalW += letterSpacing;
                    }
                }
                float startX = anchor switch {
                    TextAnchor.MiddleCenter or TextAnchor.UpperCenter or TextAnchor.LowerCenter
                        => Mathf.Floor(rect.x + (rect.width - totalW) * 0.5f),
                    TextAnchor.MiddleRight or TextAnchor.UpperRight or TextAnchor.LowerRight
                        => Mathf.Floor(rect.xMax - totalW),
                    _ => Mathf.Floor(rect.x),
                };
                float cursor = startX;
                for (int i = 0; i < content.Length; i++) {
                    gc.text = content[i].ToString();
                    float w = Mathf.Ceil(gs.CalcSize(gc).x);
                    TextDraw.DrawWithStyle(new Rect(cursor, rect.y, w, rect.height), gc.text, gs, c);
                    cursor += w + letterSpacing;
                }
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
