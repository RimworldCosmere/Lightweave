using System;
using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Hooks;
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
        [DocParam("When false, drops the enclosing border box so the control sits flush (full-bleed); the active fill and dividers reach the rect edges and the parent supplies the framing.")]
        bool bordered = true,
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

        Rem labelSize = new Rem(0.75f);
        Rem countSize = new Rem(0.656f);

        node.MeasureWidth = () => {
            int count = items.Count;
            if (count == 0) {
                return 0f;
            }
            float labelTrackingPx = Mathf.Round(labelSize.ToFontPx() * 0.12f);
            float padPx = SpacingScale.Md.ToPixels();
            float gapPx = SpacingScale.Sm.ToPixels();
            float total = 0f;
            for (int i = 0; i < count; i++) {
                string label = labelFn(items[i]) ?? string.Empty;
                float w = TextDraw.MeasureTracked(label, FontRole.Body, labelSize, labelTrackingPx);
                string? cnt = countFn?.Invoke(items[i]);
                if (!string.IsNullOrEmpty(cnt)) {
                    w += gapPx + TextDraw.Measure(cnt!, FontRole.Mono, countSize).x;
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
            float labelTrackingPx = Mathf.Round(labelSize.ToFontPx() * 0.12f);
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

                if (active) {
                    Color top = theme.GetColor(ThemeSlot.SurfaceAccent);
                    Color.RGBToHSV(top, out float hue, out float sat, out float val);
                    Color bottom = Color.HSVToRGB(hue, sat, val * 0.78f);
                    bottom.a = top.a;
                    PaintBox.Draw(segRect, new BackgroundSpec.Gradient(GradientTextureCache.Vertical(top, bottom)), null, radius);
                }
                else {
                    PaintBox.DrawHighlightIfMouseover(segRect, radius);
                    MouseoverSounds.DoRegion(segRect);
                }

                string label = labelFn(item) ?? string.Empty;
                string? badge = countFn?.Invoke(item);
                Color textColor = theme.GetColor(active ? ThemeSlot.TextOnAccent : ThemeSlot.TextSecondary);

                float labelW = TextDraw.MeasureTracked(label, FontRole.Body, labelSize, labelTrackingPx);
                float badgeW = string.IsNullOrEmpty(badge) ? 0f : TextDraw.Measure(badge!, FontRole.Mono, countSize).x;
                float groupW = labelW + (badgeW > 0f ? gapPx + badgeW : 0f);
                float startX = segRect.x + (segRect.width - groupW) * 0.5f;

                Rect labelRect = new Rect(startX, segRect.y, labelW, segRect.height);
                TextDraw.DrawTracked(labelRect, label, FontRole.Body, labelSize, TextAnchor.MiddleLeft, textColor, labelTrackingPx);

                if (badgeW > 0f) {
                    Rect badgeRect = new Rect(startX + labelW + gapPx, segRect.y, badgeW, segRect.height);
                    Color badgeColor = new Color(textColor.r, textColor.g, textColor.b, active ? 0.7f : 0.55f);
                    TextDraw.Draw(badgeRect, badge!, FontRole.Mono, countSize, TextAnchor.MiddleLeft, badgeColor);
                }

                if (i < count - 1) {
                    Rect dividerRect = new Rect(segRect.xMax - borderPx / 2f, inner.y, borderPx, inner.height);
                    PaintBox.Draw(dividerRect, BackgroundSpec.Of(ThemeSlot.BorderDefault), null, null);
                }

                if (e.type == EventType.MouseUp && e.button == 0 && segRect.Contains(e.mousePosition)) {
                    onChange?.Invoke(item);
                    e.Use();
                }
            }
        };

        return node;
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