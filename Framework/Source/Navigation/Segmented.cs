using System;
using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Hooks;
using Cosmere.Lightweave.Icons;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Cosmere.Lightweave.Navigation;

[Doc(
    Id = "segmented",
    Summary = "Rectangular grouped selector for a small set of choices, with optional count badges.",
    WhenToUse = "Toggle between 2-5 mutually exclusive filters or modes.",
    SourcePath = "Lightweave/Navigation/Segmented.cs",
    ShowRtl = true
)]
public static class Segmented {
    public static LightweaveNode Create<T>(
        [DocParam("Currently selected segment value.")]
        T value,
        [DocParam("All segment values in display order.")]
        IReadOnlyList<T> items,
        [DocParam("Maps a segment value to its label.")]
        Func<T, string> labelFn,
        [DocParam("Invoked when the user picks a different segment.")]
        Action<T> onChange,
        [DocParam("Optional per-segment count badge text (rendered in mono next to the label).")]
        Func<T, string?>? countFn = null,
        [DocParam("Optional per-segment tooltip text, shown as a Lightweave rich-text tooltip on hover. Return null/empty to skip a segment.", TypeOverride = "Func<T, string?>?", DefaultOverride = "null")]
        Func<T, string?>? tooltipFn = null,
        [DocParam("Side to anchor the per-segment tooltips. Defaults to mouse-follow; pass Top for a strip pinned to the screen bottom so the tooltip rises above it.", TypeOverride = "TooltipSide?", DefaultOverride = "null")]
        Data.TooltipSide? tooltipSide = null,
        [DocParam("When false, drops the enclosing border box so the control sits flush (full-bleed); the active fill and dividers reach the rect edges and the parent supplies the framing.")]
        bool bordered = true,
        [DocParam("When true (default) labels render in BodyBold for legibility at small pill sizes. Pass false to use the regular Body weight.")]
        bool bold = true,
        [DocParam("Optional label font size. Defaults to 0.75rem; raise it for larger tab strips.")]
        Rem? labelSize = null,
        [DocParam("Optional label font role. Overrides the bold/regular Body choice; pass Display for a prominent tab strip.", TypeOverride = "FontRole?", DefaultOverride = "null")]
        FontRole? labelRole = null,
        [DocParam("When true, each count badge renders inside a boxed CountTag-style frame (hairline border, square corners) instead of bare mono text.")]
        bool boxedCount = false,
        [DocParam("Optional per-segment lock state. Return null to omit the lock glyph for a segment; true draws a closed lock, false an open lock. The glyph sits left of the label and toggles via onLockToggle without selecting the segment.", TypeOverride = "Func<T, bool?>?", DefaultOverride = "null")]
        Func<T, bool?>? lockFn = null,
        [DocParam("Invoked when a segment's lock glyph is clicked (only fires for segments where lockFn returns non-null).", TypeOverride = "Action<T>?", DefaultOverride = "null")]
        Action<T>? onLockToggle = null,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        LightweaveNode node = NodeBuilder.New($"Segmented<{typeof(T).Name}>", line, file);
        node.ApplyStyling("segmented", style, classes, id);
        Style resolved0 = node.GetResolvedStyle();
        node.PreferredHeight = resolved0.Height is { Mode: Length.Kind.Rem } h0
            ? h0.ToPixels(0f, 0f)
            : new Rem(1.75f).ToPixels();

        Rem labelSizeResolved = labelSize ?? new Rem(0.75f);
        Rem countSize = new Rem(0.656f);
        FontRole labelFontRole = labelRole ?? (bold ? FontRole.BodyBold : FontRole.Body);
        Rem countBoxPad = new Rem(0.375f);

        node.MeasureWidth = () => {
            int count = items.Count;
            if (count == 0) {
                return 0f;
            }
            float labelTrackingPx = Mathf.Round(labelSizeResolved.ToFontPx() * 0.12f);
            float padPx = SpacingScale.Md.ToPixels();
            float gapPx = SpacingScale.Sm.ToPixels();
            float total = 0f;
            for (int i = 0; i < count; i++) {
                string label = labelFn(items[i]) ?? string.Empty;
                float w = TextDraw.MeasureTracked(label, labelFontRole, labelSizeResolved, labelTrackingPx);
                string? cnt = countFn?.Invoke(items[i]);
                if (!string.IsNullOrEmpty(cnt)) {
                    float cntW = TextDraw.Measure(cnt!, FontRole.Mono, countSize).x;
                    if (boxedCount) {
                        cntW += countBoxPad.ToPixels() * 2f;
                    }
                    w += gapPx + cntW;
                }
                if (lockFn != null && lockFn(items[i]).HasValue) {
                    w += gapPx + labelSizeResolved.ToFontPx();
                }
                total += w + padPx * 2f;
            }
            return Mathf.Ceil(total);
        };

        node.Paint = (rect, _) => {
            Theme.Theme theme = RenderContext.Current.Theme;
            Direction dir = RenderContext.Current.Direction;
            bool rtl = dir == Direction.Rtl;

            float borderPx = new Rem(1f / 16f).ToPixels();
            RadiusSpec radius = RadiusSpec.All(RadiusScale.None);

            int count = items.Count;
            if (count == 0) {
                if (bordered) {
                    PaintBox.Draw(rect, null, BorderSpec.All(new Rem(1f / 16f), ThemeSlot.BorderDefault), radius);
                }
                return;
            }

            Rect inner;
            if (bordered) {
                PaintBox.Draw(rect, null, BorderSpec.All(new Rem(1f / 16f), ThemeSlot.BorderDefault), radius);
                inner = new Rect(
                    rect.x + borderPx,
                    rect.y + borderPx,
                    rect.width - borderPx * 2f,
                    rect.height - borderPx * 2f
                );
            }
            else {
                inner = rect;
            }
            float segmentWidth = inner.width / count;
            float labelTrackingPx = Mathf.Round(labelSizeResolved.ToFontPx() * 0.12f);
            float gapPx = SpacingScale.Sm.ToPixels();

            int activeIndex = -1;
            for (int i = 0; i < count; i++) {
                if (EqualityComparer<T>.Default.Equals(items[i], value)) {
                    activeIndex = i;
                    break;
                }
            }

            Event e = Event.current;

            for (int i = 0; i < count; i++) {
                int logicalIndex = rtl ? count - 1 - i : i;
                T item = items[logicalIndex];
                bool active = logicalIndex == activeIndex;

                Rect segRect = new Rect(inner.x + i * segmentWidth, inner.y, segmentWidth, inner.height);
                LightweaveHitTracker.Track(segRect);

                string? segTip = tooltipFn?.Invoke(item);
                if (!string.IsNullOrEmpty(segTip)) {
                    string tip = segTip!;
                    Adapter.AsTooltip.Attach(
                        segRect,
                        () => Typography.Typography.Text.Create(tip, wrap: true, richText: true),
                        key: tip.GetHashCode() ^ logicalIndex,
                        side: tooltipSide
                    );
                }

                if (active) {
                    // Tab-style active treatment: a subtle warm fill plus a gold underline along the
                    // bottom edge, rather than a solid gold pill. Reads as a selected tab and lets the
                    // label keep its accent color instead of inverting to dark-on-gold.
                    PaintBox.Draw(segRect, BackgroundSpec.Of(ThemeSlot.ActiveTint), null, radius);
                    float underlinePx = new Rem(2f / 16f).ToPixels();
                    Rect underline = new Rect(segRect.x, segRect.yMax - underlinePx, segRect.width, underlinePx);
                    PaintBox.Draw(underline, BackgroundSpec.Of(ThemeSlot.SurfaceAccent), null, null);
                }
                else {
                    PaintBox.DrawHighlightIfMouseover(segRect, radius);
                    MouseoverSounds.DoRegion(segRect);
                }

                string label = labelFn(item) ?? string.Empty;
                string? badge = countFn?.Invoke(item);
                bool? lockState = lockFn?.Invoke(item);
                Color textColor = theme.GetColor(active ? ThemeSlot.SurfaceAccent : ThemeSlot.TextSecondary);

                float lockGlyphPx = lockState.HasValue ? labelSizeResolved.ToFontPx() : 0f;
                float lockGapPx = lockState.HasValue ? gapPx : 0f;
                float labelW = TextDraw.MeasureTracked(label, labelFontRole, labelSizeResolved, labelTrackingPx);
                float badgeTextW = string.IsNullOrEmpty(badge) ? 0f : TextDraw.Measure(badge!, FontRole.Mono, countSize).x;
                float badgeW = badgeTextW <= 0f ? 0f : badgeTextW + (boxedCount ? countBoxPad.ToPixels() * 2f : 0f);
                float groupW = lockGlyphPx + lockGapPx + labelW + (badgeW > 0f ? gapPx + badgeW : 0f);
                float startX = segRect.x + (segRect.width - groupW) * 0.5f;

                Rect lockRect = default;
                if (lockState.HasValue) {
                    lockRect = new Rect(startX, segRect.y, lockGlyphPx, segRect.height);
                    IconRef lockIcon = lockState.Value ? Icons.Phosphor.LockSimple : Icons.Phosphor.LockSimpleOpen;
                    Color lockColor = theme.GetColor(lockState.Value ? ThemeSlot.SurfaceAccent : ThemeSlot.TextMuted);
                    TextDraw.Draw(lockRect, lockIcon.Glyph, FontRole.Body, labelSizeResolved, TextAnchor.MiddleCenter, lockColor, fontOverride: lockIcon.ResolveFont());
                    PaintBox.DrawHighlightIfMouseover(lockRect, radius);
                }

                float labelStartX = startX + lockGlyphPx + lockGapPx;
                Rect labelRect = new Rect(labelStartX, segRect.y, labelW, segRect.height);
                TextDraw.DrawTracked(labelRect, label, labelFontRole, labelSizeResolved, TextAnchor.MiddleLeft, textColor, labelTrackingPx);

                if (badgeW > 0f) {
                    Rect badgeRect = new Rect(labelStartX + labelW + gapPx, segRect.y, badgeW, segRect.height);
                    if (boxedCount) {
                        float boxH = countSize.ToFontPx() + new Rem(0.25f).ToPixels();
                        Rect boxRect = new Rect(badgeRect.x, segRect.y + (segRect.height - boxH) * 0.5f, badgeW, boxH);
                        ThemeSlot boxBorder = active ? ThemeSlot.AccentGlow : ThemeSlot.BorderSubtle;
                        ThemeSlot boxText = active ? ThemeSlot.SurfaceAccent : ThemeSlot.TextMuted;
                        PaintBox.Draw(boxRect, BackgroundSpec.Of(ThemeSlot.SurfaceTranslucentDark), BorderSpec.All(new Rem(1f / 16f), boxBorder), RadiusSpec.None);
                        TextDraw.Draw(boxRect, badge!, FontRole.Mono, countSize, TextAnchor.MiddleCenter, boxText);
                    }
                    else {
                        Color badgeColor = new Color(textColor.r, textColor.g, textColor.b, active ? 0.7f : 0.55f);
                        TextDraw.Draw(badgeRect, badge!, FontRole.Mono, countSize, TextAnchor.MiddleLeft, badgeColor);
                    }
                }

                if (i < count - 1) {
                    Rect dividerRect = new Rect(segRect.xMax - borderPx / 2f, inner.y, borderPx, inner.height);
                    PaintBox.Draw(dividerRect, BackgroundSpec.Of(ThemeSlot.BorderDefault), null, null);
                }

                if (e.type == EventType.MouseUp && e.button == 0 && segRect.Contains(e.mousePosition) && LightweaveHitTracker.IsTopmost(segRect)) {
                    if (lockState.HasValue && onLockToggle != null && lockRect.Contains(e.mousePosition)) {
                        onLockToggle(item);
                    }
                    else {
                        onChange?.Invoke(item);
                    }
                    e.Use();
                }

                // Bridge seam: the inline Event.current hit-test above is invisible to LightweaveRimBridge
                // (it never touches Widgets.ButtonInvisible), so GABS cannot target a segment. RegisterSegment
                // is a no-op in the framework, but the bridge Postfix-patches it to register each segment as
                // an actionable compound control and returns true when a synthetic activation targets it.
                if (RegisterSegment(segRect, label, active)) {
                    onChange?.Invoke(item);
                }
            }
        };

        return node;
    }

    // Per-segment registration seam for LightweaveRimBridge. Called once per visible segment from the
    // Paint loop. Returns false in the framework (real clicks go through the inline Event.current
    // hit-test in Create). LightweaveRimBridge Postfix-patches this to expose each segment to GABS and
    // sets the result to true when a headless activation targets the segment, at which point Create
    // invokes onChange for that segment. Non-generic and closure-free so it is Harmony-patchable and
    // allocates nothing per frame.
    public static bool RegisterSegment(Rect segRect, string label, bool active) {
        return false;
    }

    [DocVariant("CL_Playground_Label_Default")]
    public static DocSample DocsDefault() {
        Hooks.Hooks.StateHandle<string> selected = Hooks.Hooks.UseState<string>("all");

        string[] segments = new[] { "all", "armor", "weapons", "tools" };
        return new DocSample(() => 
            Segmented.Create(
                selected.Value,
                segments,
                v => v switch {
                    "armor" => (string)"CL_Playground_Navigation_Segmented_Armor".Translate(),
                    "weapons" => (string)"CL_Playground_Navigation_Segmented_Weapons".Translate(),
                    "tools" => (string)"CL_Playground_Navigation_Segmented_Tools".Translate(),
                    _ => (string)"CL_Playground_Navigation_Segmented_All".Translate(),
                },
                v => selected.Set(v)
            )
        );
    }

    [DocVariant("CL_Playground_Label_FullBleed")]
    public static DocSample DocsFullBleed() {
        Hooks.Hooks.StateHandle<string> selected = Hooks.Hooks.UseState<string>("all");

        string[] segments = new[] { "all", "manual", "auto" };
        return new DocSample(() =>
            Segmented.Create(
                selected.Value,
                segments,
                v => v switch {
                    "manual" => (string)"CL_Playground_Navigation_Segmented_Weapons".Translate(),
                    "auto" => (string)"CL_Playground_Navigation_Segmented_Tools".Translate(),
                    _ => (string)"CL_Playground_Navigation_Segmented_All".Translate(),
                },
                v => selected.Set(v),
                countFn: v => v switch { "manual" => "55", "auto" => "5", _ => "60" },
                bordered: false,
                style: new Style { Width = Length.Stretch }
            )
        );
    }

    [DocVariant("CL_Playground_Navigation_Segmented_DisplayTabs")]
    public static DocSample DocsDisplayTabs() {
        Hooks.Hooks.StateHandle<string> selected = Hooks.Hooks.UseState<string>("memes");

        string[] segments = new[] { "memes", "precepts", "rituals" };
        return new DocSample(() =>
            Segmented.Create(
                selected.Value,
                segments,
                v => v switch {
                    "precepts" => (string)"CL_Playground_Navigation_Segmented_Precepts".Translate(),
                    "rituals" => (string)"CL_Playground_Navigation_Segmented_Rituals".Translate(),
                    _ => (string)"CL_Playground_Navigation_Segmented_Memes".Translate(),
                },
                v => selected.Set(v),
                countFn: v => v switch {
                    "precepts" => "6",
                    "rituals" => "2",
                    _ => "3",
                },
                labelRole: FontRole.Display,
                boxedCount: true
            )
        );
    }

    [DocUsage]
    public static DocSample DocsUsage() {
        return new DocSample(() => {
            Hooks.Hooks.StateHandle<string> selected = Hooks.Hooks.UseState<string>("all");
            string[] segments = new[] { "all", "armor", "weapons" };
            return Segmented.Create(
                selected.Value,
                segments,
                v => v,
                v => selected.Set(v)
            );
        });
    }
}