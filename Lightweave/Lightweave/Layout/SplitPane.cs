using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;
using static Cosmere.Lightweave.Hooks.Hooks;
using static Cosmere.Lightweave.Typography.Typography;
using Text = Cosmere.Lightweave.Typography.Typography.Text;

namespace Cosmere.Lightweave.Layout;

[Doc(
    Id = "splitpane",
    Summary = "Resizable two-pane split with a draggable divider.",
    WhenToUse = "Let the user trade space between two regions — tree | list, list | detail, viewport | inspector. Nest two for a three-pane layout.",
    SourcePath = "Lightweave/Layout/SplitPane.cs"
)]
public static class SplitPane {
    private static readonly Rem DividerThickness = new Rem(0.625f);
    private static readonly Rem ThreadWidth = new Rem(1f / 16f);
    private static readonly Rem GripDot = new Rem(0.1875f);
    private static readonly Rem DefaultMin = new Rem(6.25f);

    public static LightweaveNode Create(
        [DocParam("Content for the first pane (left / top).")]
        LightweaveNode first,
        [DocParam("Content for the second pane (right / bottom).")]
        LightweaveNode second,
        [DocParam("Axis the divider runs across.")]
        SplitOrientation orientation = SplitOrientation.Horizontal,
        [DocParam("First pane's share of total at mount (0–1).")]
        float initialFraction = 0.5f,
        [DocParam("Minimum size of the first pane.", TypeOverride = "Rem", DefaultOverride = "6.25")]
        Rem? minFirst = null,
        [DocParam("Minimum size of the second pane.", TypeOverride = "Rem", DefaultOverride = "6.25")]
        Rem? minSecond = null,
        [DocParam("Fires while dragging and on release with the new fraction. Use to persist.")]
        Action<float>? onFractionChanged = null,
        [DocParam("Floating \"42% · 58%\" badge during drag.")]
        bool showFractionBadge = true,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        LightweaveNode node = NodeBuilder.New("SplitPane", line, file);
        node.ApplyStyling("splitpane", style, classes, id);
        node.Children.Add(first);
        node.Children.Add(second);

        bool isV = orientation == SplitOrientation.Vertical;
        float seedFraction = Mathf.Clamp01(initialFraction);

        Rect frameDivider = Rect.zero;
        bool frameLit = false;
        bool frameDragging = false;
        int framePctFirst = 50;

        node.MeasureWidth = () => {
            float thickness = DividerThickness.ToPixels();
            if (isV) {
                float a = first.MeasureWidth?.Invoke() ?? 0f;
                float b = second.MeasureWidth?.Invoke() ?? 0f;
                return Mathf.Max(a, b);
            }

            float fw = first.MeasureWidth?.Invoke() ?? 0f;
            float sw = second.MeasureWidth?.Invoke() ?? 0f;
            return fw + thickness + sw;
        };

        node.Measure = availableWidth => {
            float thickness = DividerThickness.ToPixels();
            if (isV) {
                float fh = first.Measure?.Invoke(availableWidth) ?? first.PreferredHeight ?? 0f;
                float sh = second.Measure?.Invoke(availableWidth) ?? second.PreferredHeight ?? 0f;
                return fh + thickness + sh;
            }

            float half = thickness * 0.5f;
            float firstW = Mathf.Max(0f, availableWidth * seedFraction - half);
            float secondW = Mathf.Max(0f, availableWidth * (1f - seedFraction) - half);
            float a = first.Measure?.Invoke(firstW) ?? first.PreferredHeight ?? 0f;
            float b = second.Measure?.Invoke(secondW) ?? second.PreferredHeight ?? 0f;
            return Mathf.Max(a, b);
        };

        node.Layout = rect => {
            RefHandle<float> fractionRef = UseRef(seedFraction, line, file + "#frac");
            RefHandle<bool> draggingRef = UseRef(false, line, file + "#drag");

            float origin = isV ? rect.y : rect.x;
            float extent = isV ? rect.yMax : rect.xMax;
            float total = isV ? rect.height : rect.width;
            float thickness = DividerThickness.ToPixels();
            float half = thickness * 0.5f;
            float minFirstPx = (minFirst ?? DefaultMin).ToPixels();
            float minSecondPx = (minSecond ?? DefaultMin).ToPixels();

            float ClampFraction(float f) {
                if (total <= 0f) {
                    return f;
                }

                float lo = minFirstPx / total;
                float hi = 1f - minSecondPx / total;
                if (lo >= hi) {
                    return 0.5f;
                }

                return Mathf.Clamp(f, lo, hi);
            }

            Event e = Event.current;
            float fraction = ClampFraction(fractionRef.Current);

            if (e.type == EventType.MouseDown && e.button == 0) {
                float boundaryHit = origin + fraction * total;
                Rect hitRect = isV
                    ? new Rect(rect.x, boundaryHit - half, rect.width, thickness)
                    : new Rect(boundaryHit - half, rect.y, thickness, rect.height);
                if (hitRect.Contains(e.mousePosition)) {
                    draggingRef.Current = true;
                    e.Use();
                }
            }
            else if (draggingRef.Current && (e.type == EventType.MouseDrag || e.type == EventType.MouseMove)) {
                float pos = (isV ? e.mousePosition.y : e.mousePosition.x) - origin;
                float next = ClampFraction(total > 0f ? pos / total : fraction);
                if (!Mathf.Approximately(next, fractionRef.Current)) {
                    fractionRef.Current = next;
                    onFractionChanged?.Invoke(next);
                }

                fraction = next;
                e.Use();
            }
            else if (draggingRef.Current && (e.type == EventType.MouseUp || e.rawType == EventType.MouseUp)) {
                draggingRef.Current = false;
                onFractionChanged?.Invoke(fractionRef.Current);
                if (e.type == EventType.MouseUp) {
                    e.Use();
                }
            }

            float boundary = origin + fraction * total;
            Rect dividerRect = isV
                ? new Rect(rect.x, boundary - half, rect.width, thickness)
                : new Rect(boundary - half, rect.y, thickness, rect.height);

            if (first.IsInFlow()) {
                first.MeasuredRect = isV
                    ? new Rect(rect.x, rect.y, rect.width, Mathf.Max(0f, boundary - half - rect.y))
                    : new Rect(rect.x, rect.y, Mathf.Max(0f, boundary - half - rect.x), rect.height);
            }

            if (second.IsInFlow()) {
                float start = boundary + half;
                second.MeasuredRect = isV
                    ? new Rect(rect.x, start, rect.width, Mathf.Max(0f, extent - start))
                    : new Rect(start, rect.y, Mathf.Max(0f, extent - start), rect.height);
            }

            frameDivider = dividerRect;
            frameDragging = draggingRef.Current;
            framePctFirst = Mathf.RoundToInt(fraction * 100f);
        };

        node.Draw = rect => {
            Theme.Theme theme = RenderContext.Current.Theme;
            Rect dividerRect = frameDivider;
            InteractionState st = InteractionState.Resolve(dividerRect, null, false);
            bool dragging = frameDragging || st.Pressed;
            bool lit = dragging || st.Hovered;
            frameLit = lit;

            ThemeSlot threadSlot = lit ? ThemeSlot.SurfaceAccent : ThemeSlot.BorderSubtle;
            float threadPx = ThreadWidth.ToPixels();
            Rect threadRect = isV
                ? new Rect(dividerRect.x, dividerRect.center.y - threadPx * 0.5f, dividerRect.width, threadPx)
                : new Rect(dividerRect.center.x - threadPx * 0.5f, dividerRect.y, threadPx, dividerRect.height);
            PaintBox.Draw(threadRect, BackgroundSpec.Of(threadSlot), null, null);

            if (lit) {
                Color dotColor = theme.GetColor(ThemeSlot.SurfaceAccent);
                float dotPx = GripDot.ToPixels();
                float gap = dotPx * 1.8f;
                Vector2 c = dividerRect.center;
                RadiusSpec dotRadius = RadiusSpec.All(RadiusScale.Full);
                for (int i = -1; i <= 1; i++) {
                    Rect dot = isV
                        ? new Rect(c.x + i * gap - dotPx * 0.5f, c.y - dotPx * 0.5f, dotPx, dotPx)
                        : new Rect(c.x - dotPx * 0.5f, c.y + i * gap - dotPx * 0.5f, dotPx, dotPx);
                    PaintBox.Draw(dot, BackgroundSpec.Of(dotColor), null, dotRadius);
                }
            }

            if (showFractionBadge && dragging) {
                Rect badgeDivider = dividerRect;
                int pctFirst = framePctFirst;
                bool vertical = isV;
                RenderContext.Current.PendingOverlays.Enqueue(() =>
                    DrawFractionBadge(badgeDivider, vertical, pctFirst));
            }
        };

        return node;
    }

    private static void DrawFractionBadge(Rect dividerRect, bool isV, int pctFirst) {
        Theme.Theme theme = RenderContext.Current.Theme;
        int pctSecond = 100 - pctFirst;
        string text = string.Format(
            CultureInfo.InvariantCulture,
            "{0:00}% · {1:00}%",
            pctFirst,
            pctSecond
        );

        Font font = theme.GetFont(FontRole.Mono);
        int px = Mathf.RoundToInt(new Rem(0.72f).ToFontPx());
        GUIStyle textStyle = GuiStyleCache.GetOrCreate(font, px, FontStyle.Normal);
        textStyle.alignment = TextAnchor.MiddleCenter;

        float padX = SpacingScale.Sm.ToPixels();
        float padY = new Rem(0.3f).ToPixels();
        Vector2 textSize = textStyle.CalcSize(new GUIContent(text));
        float w = textSize.x + padX * 2f;
        float h = textSize.y + padY * 2f;

        float offset = new Rem(0.875f).ToPixels();
        Rect badge = isV
            ? new Rect(dividerRect.x + offset, dividerRect.center.y - h - offset, w, h)
            : new Rect(dividerRect.center.x + offset, dividerRect.y + offset, w, h);

        PaintBox.Draw(
            badge,
            BackgroundSpec.Of(ThemeSlot.SurfaceTooltip),
            BorderSpec.All(ThreadWidth, ThemeSlot.BorderTooltip),
            RadiusSpec.All(RadiusScale.Sm)
        );
        TextDraw.DrawWithStyle(badge, text, textStyle, theme.GetColor(ThemeSlot.SurfaceAccent));
    }

    private static LightweaveNode DemoPane(string tagKey, string titleKey, string bodyKey) {
        return Card.Create(
            c => {
                c.Add(Caption.Create((string)tagKey.Translate()));
                c.Add(Heading.Create(5, (string)titleKey.Translate()));
                c.Add(Text.Create((string)bodyKey.Translate(), wrap: true));
            },
            style: new Style { Height = Length.Stretch }
        );
    }

    [DocUsage]
    public static DocSample DocsUsage() {
        return new DocSample(() => Create(
            DemoPane("CL_Playground_SplitPane_Pane1Tag", "CL_Playground_SplitPane_Pane1Title", "CL_Playground_SplitPane_Pane1Body"),
            DemoPane("CL_Playground_SplitPane_Pane2Tag", "CL_Playground_SplitPane_Pane2Title", "CL_Playground_SplitPane_Pane2Body"),
            initialFraction: 0.42f,
            style: new Style { Width = Length.Stretch, Height = Length.Stretch }
        ));
    }

    [DocVariant("CL_Playground_Label_Horizontal")]
    public static DocSample DocsHorizontal() {
        return new DocSample(() => Create(
            DemoPane("CL_Playground_SplitPane_Pane1Tag", "CL_Playground_SplitPane_Pane1Title", "CL_Playground_SplitPane_Pane1Body"),
            DemoPane("CL_Playground_SplitPane_Pane2Tag", "CL_Playground_SplitPane_Pane2Title", "CL_Playground_SplitPane_Pane2Body"),
            initialFraction: 0.42f,
            style: new Style { Width = Length.Stretch, Height = Length.Stretch }
        ));
    }

    [DocVariant("CL_Playground_Label_Vertical")]
    public static DocSample DocsVertical() {
        return new DocSample(() => Create(
            DemoPane("CL_Playground_SplitPane_TopTag", "CL_Playground_SplitPane_TopTitle", "CL_Playground_SplitPane_TopBody"),
            DemoPane("CL_Playground_SplitPane_BottomTag", "CL_Playground_SplitPane_BottomTitle", "CL_Playground_SplitPane_BottomBody"),
            orientation: SplitOrientation.Vertical,
            initialFraction: 0.55f,
            style: new Style { Width = Length.Stretch, Height = Length.Stretch }
        ));
    }

    [DocVariant("CL_Playground_Label_Nested")]
    public static DocSample DocsNested() {
        return new DocSample(() => Create(
            DemoPane("CL_Playground_SplitPane_TreeTag", "CL_Playground_SplitPane_TreeTitle", "CL_Playground_SplitPane_TreeBody"),
            Create(
                DemoPane("CL_Playground_SplitPane_ListTag", "CL_Playground_SplitPane_ListTitle", "CL_Playground_SplitPane_ListBody"),
                DemoPane("CL_Playground_SplitPane_DetailTag", "CL_Playground_SplitPane_DetailTitle", "CL_Playground_SplitPane_DetailBody"),
                initialFraction: 0.7f,
                minFirst: new Rem(15f),
                style: new Style { Width = Length.Stretch, Height = Length.Stretch }
            ),
            initialFraction: 0.22f,
            minFirst: new Rem(8.75f),
            style: new Style { Width = Length.Stretch, Height = Length.Stretch }
        ), useFullSource: true);
    }

    [DocState("CL_Playground_Label_Rest")]
    public static DocSample DocsRest() {
        return new DocSample(() => Create(
            DemoPane("CL_Playground_SplitPane_Pane1Tag", "CL_Playground_SplitPane_Pane1Title", "CL_Playground_SplitPane_Pane1Body"),
            DemoPane("CL_Playground_SplitPane_Pane2Tag", "CL_Playground_SplitPane_Pane2Title", "CL_Playground_SplitPane_Pane2Body"),
            style: new Style { Width = Length.Stretch, Height = Length.Stretch }
        ));
    }

    [DocState("CL_Playground_Label_Hover")]
    public static DocSample DocsHover() {
        return new DocSample(() => Create(
            DemoPane("CL_Playground_SplitPane_Pane1Tag", "CL_Playground_SplitPane_Pane1Title", "CL_Playground_SplitPane_Pane1Body"),
            DemoPane("CL_Playground_SplitPane_Pane2Tag", "CL_Playground_SplitPane_Pane2Title", "CL_Playground_SplitPane_Pane2Body"),
            style: new Style { Width = Length.Stretch, Height = Length.Stretch }
        ));
    }

    [DocState("CL_Playground_Label_Dragging")]
    public static DocSample DocsDragging() {
        return new DocSample(() => Create(
            DemoPane("CL_Playground_SplitPane_Pane1Tag", "CL_Playground_SplitPane_Pane1Title", "CL_Playground_SplitPane_Pane1Body"),
            DemoPane("CL_Playground_SplitPane_Pane2Tag", "CL_Playground_SplitPane_Pane2Title", "CL_Playground_SplitPane_Pane2Body"),
            style: new Style { Width = Length.Stretch, Height = Length.Stretch }
        ));
    }
}
