using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;

namespace Cosmere.Lightweave.Data;

[Doc(
    Id = "count-tag",
    Summary = "Boxed monospace count/label badge: square corners, hairline border, mono text. Muted or accent tone.",
    WhenToUse = "Section counts on tab strips, the 'Impact 3 / 10' complexity pill, an always-on 'LOCKED' marker. Reach for Chip instead when you need a dot, a divider-count, or an interactive toggle.",
    SourcePath = "Lightweave/Data/CountTag.cs"
)]
public static class CountTag {
    private static readonly Rem FontSize = new Rem(0.625f);
    private static readonly Rem Height = new Rem(1.125f);
    private static readonly Rem PadX = new Rem(0.5f);
    private const float TrackingEm = 0.08f;

    public static LightweaveNode Create(
        [DocParam("Badge text. Rendered verbatim (case preserved) in the mono font.")]
        string label,
        [DocParam("Muted (dark scrim, hairline border, muted text) or Accent (accent-soft fill, accent-glow border, accent text).")]
        CountTagTone tone = CountTagTone.Muted,
        [DocParam("Inline style override.", TypeOverride = "Style?", DefaultOverride = "null")]
        Style? style = null,
        [DocParam("Additional class names merged after the base 'count-tag' class.", TypeOverride = "string[]?", DefaultOverride = "null")]
        string[]? classes = null,
        [DocParam("Stable id for state-style lookup.", TypeOverride = "string?", DefaultOverride = "null")]
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        string text = label ?? string.Empty;

        LightweaveNode node = NodeBuilder.New($"CountTag:{text}", line, file);
        node.ApplyStyling("count-tag", style, classes, id);
        node.PreferredHeight = Height.ToPixels();

        node.Measure = _ => Height.ToPixels();

        node.MeasureWidth = () => {
            float trackingPx = TrackingEm * FontSize.ToFontPx();
            float textW = string.IsNullOrEmpty(text) ? 0f : TextDraw.MeasureTracked(text, FontRole.Mono, FontSize, trackingPx);
            return Mathf.Ceil(textW + PadX.ToPixels() * 2f);
        };

        node.Draw = rect => {
            (ThemeSlot bg, ThemeSlot border, ThemeSlot textSlot) = tone == CountTagTone.Accent
                ? (ThemeSlot.AccentSoft, ThemeSlot.AccentGlow, ThemeSlot.SurfaceAccent)
                : (ThemeSlot.SurfaceTranslucentDark, ThemeSlot.BorderSubtle, ThemeSlot.TextMuted);

            PaintBox.Draw(
                rect,
                BackgroundSpec.Of(bg),
                BorderSpec.All(new Rem(1f / 16f), border),
                RadiusSpec.None
            );

            if (string.IsNullOrEmpty(text)) {
                return;
            }

            float trackingPx = TrackingEm * FontSize.ToFontPx();
            TextDraw.DrawTracked(rect, text, FontRole.Mono, FontSize, TextAnchor.MiddleCenter, textSlot, trackingPx);
        };

        return node;
    }

    [DocUsage]
    public static DocSample DocsUsage() {
        return new DocSample(() => CountTag.Create("5"));
    }

    [DocVariant("CL_Playground_CountTag_Muted")]
    public static DocSample DocsMuted() {
        return new DocSample(() => CountTag.Create("12"));
    }

    [DocVariant("CL_Playground_CountTag_Accent")]
    public static DocSample DocsAccent() {
        return new DocSample(() => CountTag.Create("Impact 3 / 10", CountTagTone.Accent));
    }

    [DocVariant("CL_Playground_CountTag_Locked")]
    public static DocSample DocsLocked() {
        return new DocSample(() => CountTag.Create("LOCKED", CountTagTone.Accent));
    }
}
