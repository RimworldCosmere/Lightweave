using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using static Cosmere.Lightweave.Typography.Typography;

namespace Cosmere.Lightweave.Typography;

[Doc(
    Id = "eyebrow",
    Summary = "Small uppercase tracked label used above titles or to mark sections.",
    WhenToUse = "Section eyebrows above headings, metadata-row labels, status group prefixes. Always uppercase.",
    SourcePath = "Lightweave/Typography/Eyebrow.cs",
    ShowRtl = false
)]
public static class Eyebrow {
    public static LightweaveNode Create(
        [DocParam("Eyebrow text. Will be rendered upper-cased.")]
        string content,
        [DocParam("Text accent color. When set, overrides the default muted text color. Defaults to the resolved style / muted slot.")]
        ColorRef? accent = null,
        [DocParam("When true, draws a hairline rule filling the remaining width after the label (left-aligned only).")]
        bool trailingRule = false,
        [DocParam("Inline style override.", TypeOverride = "Style?", DefaultOverride = "null")]
        Style? style = null,
        [DocParam("Additional class names merged after the base 'eyebrow' class.", TypeOverride = "string[]?", DefaultOverride = "null")]
        string[]? classes = null,
        [DocParam("Stable id for state-style lookup.", TypeOverride = "string?", DefaultOverride = "null")]
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        string upper = content?.ToUpperInvariant() ?? string.Empty;

        if (accent != null) {
            style = style.HasValue
                ? style.Value with { TextColor = accent }
                : new Style { TextColor = accent };
        }

        Tracking? styleTracking = style?.LetterSpacing;
        bool noTracking = !styleTracking.HasValue || Mathf.Approximately(styleTracking.Value.Em, 0f);
        if (noTracking && !trailingRule) {
            return Text.Create(
                upper,
                style: style,
                classes: StyleExtensions.PrependClass("eyebrow", classes),
                id: id,
                line: line,
                file: file
            );
        }

        LightweaveNode node = NodeBuilder.New($"Eyebrow:{upper}", line, file);
        node.ApplyStyling("eyebrow", style, classes, id);

        float ruleThicknessPx = new Rem(1f / 16f).ToPixels();
        float ruleGapPx = SpacingScale.Sm.ToPixels();

        int ResolveLetterSpacing() {
            Style s = node.GetResolvedStyle();
            Tracking? t = s.LetterSpacing;
            if (!t.HasValue) {
                return 0;
            }
            Rem fontSize = s.FontSize ?? new Rem(0.75f);
            return Mathf.Max(0, Mathf.RoundToInt(t.Value.ToPixels(fontSize.ToFontPx())));
        }

        GUIStyle ResolveGuiStyle() {
            Theme.Theme theme = RenderContext.Current.Theme;
            Style s = node.GetResolvedStyle();
            FontRef? fr = s.FontFamily;
            Font font = fr switch {
                FontRef.Literal lit => lit.Value,
                FontRef.Role role => theme.GetFont(role.RoleValue),
                _ => theme.GetFont(FontRole.Body),
            };
            Rem fontSize = s.FontSize ?? new Rem(0.75f);
            FontStyle weight = s.FontWeight ?? FontStyle.Normal;
            int pixelSize = Mathf.RoundToInt(fontSize.ToFontPx());
            return GuiStyleCache.GetOrCreate(font, pixelSize, weight);
        }

        int[] MeasureCharWidths(GUIStyle gs) {
            int[] widths = new int[upper.Length];
            for (int i = 0; i < upper.Length; i++) {
                GUIContent gc = new GUIContent(upper[i].ToString());
                widths[i] = Mathf.CeilToInt(gs.CalcSize(gc).x);
            }
            return widths;
        }

        int MeasureTotalWidth(int[] widths, int letterSpacing) {
            int total = 0;
            for (int i = 0; i < widths.Length; i++) {
                total += widths[i];
                if (i < widths.Length - 1) {
                    total += letterSpacing;
                }
            }
            return total;
        }

        node.Measure = _ => {
            if (string.IsNullOrEmpty(upper)) {
                return 0f;
            }
            GUIStyle gs = ResolveGuiStyle();
            Style s = node.GetResolvedStyle();
            Rem fontSize = s.FontSize ?? new Rem(0.75f);
            int pixelSize = Mathf.RoundToInt(fontSize.ToFontPx());
            float descenderPad = Mathf.Max(2f, pixelSize * 0.25f);
            return Mathf.Ceil(TextMeasureCache.Height(gs, upper, float.MaxValue) + descenderPad);
        };

        node.MeasureWidth = () => {
            if (string.IsNullOrEmpty(upper)) {
                return 0f;
            }
            GUIStyle gs = ResolveGuiStyle();
            int letterSpacing = ResolveLetterSpacing();
            int[] widths = MeasureCharWidths(gs);
            return MeasureTotalWidth(widths, letterSpacing);
        };

        node.Paint = (rect, _) => {
            if (string.IsNullOrEmpty(upper) && !trailingRule) {
                return;
            }
            Theme.Theme theme = RenderContext.Current.Theme;
            Style s = node.GetResolvedStyle();
            GUIStyle gs = ResolveGuiStyle();
            int letterSpacing = ResolveLetterSpacing();
            int[] widths = MeasureCharWidths(gs);
            int totalW = MeasureTotalWidth(widths, letterSpacing);
            TextAlign align = s.TextAlign ?? TextAlign.Start;
            TextAnchor anchor = ResolveAnchor(align, RenderContext.Current.Direction);
            int startX = anchor switch {
                TextAnchor.MiddleCenter or TextAnchor.UpperCenter or TextAnchor.LowerCenter
                    => Mathf.FloorToInt(rect.x + (rect.width - totalW) * 0.5f),
                TextAnchor.MiddleRight or TextAnchor.UpperRight or TextAnchor.LowerRight
                    => Mathf.FloorToInt(rect.xMax - totalW),
                _ => Mathf.FloorToInt(rect.x),
            };
            int y = Mathf.FloorToInt(rect.y);
            int h = Mathf.CeilToInt(rect.height);
            ColorRef? cr = s.TextColor;
            Color c = cr switch {
                ColorRef.Literal lit => lit.Value,
                ColorRef.Token tok => theme.GetColor(tok.Slot),
                _ => theme.GetColor(ThemeSlot.TextMuted),
            };
            gs.alignment = TextAnchor.MiddleLeft;
            gs.clipping = TextClipping.Overflow;

            int cursor = startX;
            for (int i = 0; i < upper.Length; i++) {
                string ch = upper[i].ToString();
                TextDraw.DrawWithStyle(new Rect(cursor, y, widths[i], h), ch, gs, c);
                cursor += widths[i] + letterSpacing;
            }

            if (trailingRule && align == TextAlign.Start) {
                float ruleX = startX + totalW + ruleGapPx;
                if (ruleX < rect.xMax) {
                    float ruleY = rect.y + (rect.height - ruleThicknessPx) * 0.5f;
                    Color ruleColor = theme.GetColor(ThemeSlot.BorderSubtle);
                    PaintBox.Fill(new Rect(ruleX, ruleY, rect.xMax - ruleX, ruleThicknessPx), ruleColor);
                }
            }
        };
        return node;
    }

    [DocVariant("CL_Playground_Label_Default")]
    public static DocSample DocsDefault() {
        return new DocSample(() => Eyebrow.Create("section header"));
    }

    [DocVariant("CL_Playground_Label_Accented")]
    public static DocSample DocsAccented() {
        return new DocSample(() => Eyebrow.Create("framerate", style: new Style { TextColor = (ColorRef)ThemeSlot.SurfaceAccent }));
    }

    [DocVariant("CL_Playground_Label_TrailingRule")]
    public static DocSample DocsTrailingRule() {
        return new DocSample(() => Eyebrow.Create(
            "section",
            accent: ThemeSlot.SurfaceAccent,
            trailingRule: true,
            style: new Style { LetterSpacing = Tracking.Of(0.28f), Width = Length.Stretch }
        ));
    }

    [DocUsage]
    public static DocSample DocsUsage() {
        return new DocSample(() => Eyebrow.Create("display"));
    }
}
