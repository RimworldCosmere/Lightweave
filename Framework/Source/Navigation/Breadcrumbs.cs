using System;
using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;

namespace Cosmere.Lightweave.Navigation;

[Doc(
    Id = "breadcrumbs",
    Summary = "Inline path with chevrons that collapses on overflow.",
    WhenToUse = "Show ancestry through a hierarchy users can navigate back through.",
    SourcePath = "Lightweave/Navigation/Breadcrumbs.cs",
    ShowRtl = true
)]
public static class Breadcrumbs {
    private const string Ellipsis = "...";
    private static readonly Rem RowHeight = new Rem(1.5f);
    private static readonly Rem LabelSize = new Rem(0.875f);

    public static LightweaveNode Create(
        [DocParam("Ordered crumb labels from root to current.")]
        IReadOnlyList<string> crumbs,
        [DocParam("Invoked when an earlier crumb is clicked. When null the row renders as a non-interactive eyebrow.")]
        Action<int>? onNavigate = null,
        [DocParam("Override hover sound on non-current crumbs. Null = component default (false).")]
        bool? playHoverSound = null,
        [DocParam("Single-character separator drawn between crumbs.")]
        string separator = "/",
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        LightweaveNode node = NodeBuilder.New("Breadcrumbs", line, file);
        node.ApplyStyling("breadcrumbs", style, classes, id);
        node.PreferredHeight = RowHeight.ToPixels();
        node.MeasureWidth = () => {
            if (crumbs == null || crumbs.Count == 0) {
                return 0f;
            }
            Theme.Theme theme = RenderContext.Current.Theme;
            Font font = theme.GetFont(FontRole.Mono);
            int pixelSize = Mathf.RoundToInt(LabelSize.ToFontPx());
            GUIStyle gs = GuiStyleCache.GetOrCreate(font, pixelSize);
            float gapPx = SpacingScale.Sm.ToPixels();
            float sepWidth = gs.CalcSize(new GUIContent(separator)).x;
            float total = 0f;
            for (int i = 0; i < crumbs.Count; i++) {
                string text = (crumbs[i] ?? string.Empty).ToUpperInvariant();
                total += gs.CalcSize(new GUIContent(text)).x;
                if (i < crumbs.Count - 1) {
                    total += gapPx + sepWidth + gapPx;
                }
            }
            return Mathf.Ceil(total);
        };
        node.Paint = (rect, _) => {
            if (crumbs == null || crumbs.Count == 0) {
                return;
            }

            Theme.Theme theme = RenderContext.Current.Theme;
            Direction dir = RenderContext.Current.Direction;
            bool rtl = dir == Direction.Rtl;
            bool interactiveRow = onNavigate != null;

            Font font = theme.GetFont(FontRole.Mono);
            int pixelSize = Mathf.RoundToInt(LabelSize.ToFontPx());
            GUIStyle gs = GuiStyleCache.GetOrCreate(font, pixelSize);
            gs.alignment = TextAnchor.MiddleLeft;

            float gapPx = SpacingScale.Sm.ToPixels();
            float rowHeight = RowHeight.ToPixels();
            float rowY = rect.y + (rect.height - rowHeight) * 0.5f;

            int count = crumbs.Count;
            float[] labelWidths = new float[count];
            string[] upper = new string[count];
            for (int i = 0; i < count; i++) {
                upper[i] = (crumbs[i] ?? string.Empty).ToUpperInvariant();
                labelWidths[i] = gs.CalcSize(new GUIContent(upper[i])).x;
            }

            float sepWidth = gs.CalcSize(new GUIContent(separator)).x;
            float ellipsisWidth = gs.CalcSize(new GUIContent(Ellipsis)).x;

            bool[] visible = new bool[count];
            for (int i = 0; i < count; i++) {
                visible[i] = true;
            }

            bool showEllipsis = false;

            if (count > 1) {
                float total = TotalWidth(labelWidths, visible, count, sepWidth, gapPx, showEllipsis, ellipsisWidth);
                int removeIndex = 1;
                while (total > rect.width && removeIndex < count - 1) {
                    visible[removeIndex] = false;
                    showEllipsis = true;
                    removeIndex++;
                    total = TotalWidth(labelWidths, visible, count, sepWidth, gapPx, showEllipsis, ellipsisWidth);
                }
            }

            int lastVisibleIndex = count - 1;

            float cursor = rtl ? rect.xMax : rect.x;
            bool firstDrawn = true;
            bool ellipsisDrawn = false;

            Color separatorColor = theme.GetColor(ThemeSlot.TextMuted);
            Color currentColor = interactiveRow
                ? theme.GetColor(ThemeSlot.TextSecondary)
                : theme.GetColor(ThemeSlot.TextMuted);

            for (int i = 0; i < count; i++) {
                if (!visible[i]) {
                    if (!ellipsisDrawn && showEllipsis) {
                        if (!firstDrawn) {
                            cursor = DrawSeparator(cursor, rowY, rowHeight, sepWidth, separator, gs, separatorColor, rtl, gapPx);
                        }

                        cursor = DrawEllipsis(cursor, rowY, rowHeight, ellipsisWidth, gs, separatorColor, rtl, gapPx);
                        firstDrawn = false;
                        ellipsisDrawn = true;
                    }

                    continue;
                }

                if (!firstDrawn) {
                    cursor = DrawSeparator(cursor, rowY, rowHeight, sepWidth, separator, gs, separatorColor, rtl, gapPx);
                }

                float labelWidth = labelWidths[i];
                bool isLast = i == lastVisibleIndex;

                Rect labelRect;
                if (rtl) {
                    labelRect = new Rect(cursor - labelWidth, rowY, labelWidth, rowHeight);
                    cursor = labelRect.x - gapPx;
                }
                else {
                    labelRect = new Rect(cursor, rowY, labelWidth, rowHeight);
                    cursor = labelRect.xMax + gapPx;
                }

                DrawCrumb(labelRect, upper[i], i, isLast, interactiveRow, onNavigate, gs, theme, playHoverSound ?? false, currentColor);
                firstDrawn = false;
            }
        };
        return node;
    }

    private static float TotalWidth(
        float[] labelWidths,
        bool[] visible,
        int count,
        float separatorWidth,
        float gapPx,
        bool showEllipsis,
        float ellipsisWidth
    ) {
        float total = 0f;
        int visibleCount = 0;
        for (int i = 0; i < count; i++) {
            if (visible[i]) {
                total += labelWidths[i];
                visibleCount++;
            }
        }

        if (showEllipsis) {
            total += ellipsisWidth;
            visibleCount++;
        }

        if (visibleCount > 1) {
            int separators = visibleCount - 1;
            total += separators * (separatorWidth + gapPx * 2f);
        }

        return total;
    }

    private static float DrawSeparator(
        float cursor,
        float rowY,
        float rowHeight,
        float separatorWidth,
        string separator,
        GUIStyle style,
        Color color,
        bool rtl,
        float gapPx
    ) {
        Rect sepRect;
        float next;
        if (rtl) {
            sepRect = new Rect(cursor - separatorWidth, rowY, separatorWidth, rowHeight);
            next = sepRect.x - gapPx;
        }
        else {
            sepRect = new Rect(cursor, rowY, separatorWidth, rowHeight);
            next = sepRect.xMax + gapPx;
        }

        TextDraw.DrawWithStyle(sepRect, separator, style, color);
        return next;
    }

    private static float DrawEllipsis(
        float cursor,
        float rowY,
        float rowHeight,
        float ellipsisWidth,
        GUIStyle style,
        Color color,
        bool rtl,
        float gapPx
    ) {
        Rect ellipsisRect;
        float next;
        if (rtl) {
            ellipsisRect = new Rect(cursor - ellipsisWidth, rowY, ellipsisWidth, rowHeight);
            next = ellipsisRect.x - gapPx;
        }
        else {
            ellipsisRect = new Rect(cursor, rowY, ellipsisWidth, rowHeight);
            next = ellipsisRect.xMax + gapPx;
        }

        TextDraw.DrawWithStyle(ellipsisRect, Ellipsis, style, color);
        return next;
    }

    private static void DrawCrumb(
        Rect labelRect,
        string text,
        int index,
        bool isLast,
        bool interactiveRow,
        Action<int>? onNavigate,
        GUIStyle style,
        Theme.Theme theme,
        bool soundEnabled,
        Color currentColor
    ) {
        Event e = Event.current;
        bool interactive = interactiveRow && !isLast;
        bool hovering = interactive && labelRect.Contains(e.mousePosition);

        if (hovering) {
            PaintBox.DrawHighlight(labelRect, RadiusSpec.All(RadiusScale.Sm), true);
        }

        if (interactive) {
            LightweaveHitTracker.Track(labelRect);
            Cosmere.Lightweave.Input.InteractionFeedback.Apply(labelRect, true, soundEnabled);
        }

        Color color;
        if (isLast) {
            color = currentColor;
        }
        else if (hovering) {
            color = theme.GetColor(ThemeSlot.TextSecondary);
        }
        else {
            color = theme.GetColor(ThemeSlot.TextMuted);
        }

        TextDraw.DrawWithStyle(labelRect, text, style, color);

        if (interactive && e.type == EventType.MouseUp && e.button == 0 && labelRect.Contains(e.mousePosition) && LightweaveHitTracker.IsTopmost(labelRect)) {
            onNavigate?.Invoke(index);
            e.Use();
        }
    }

    [DocVariant("CL_Playground_Label_Default")]
    public static DocSample DocsDefault() {
        return new DocSample(() => {
            string[] path = new[] {
                (string)"CL_Playground_Breadcrumbs_Crumb_Worlds".Translate(),
                (string)"CL_Playground_Breadcrumbs_Crumb_Roshar".Translate(),
                (string)"CL_Playground_Breadcrumbs_Crumb_ShatteredPlains".Translate(),
            };
            return Breadcrumbs.Create(path);
        });
    }

    [DocVariant("CL_Playground_Label_Interactive")]
    public static DocSample DocsInteractive() {
        return new DocSample(() => {
            string[] path = new[] {
                (string)"CL_Playground_Breadcrumbs_Crumb_Worlds".Translate(),
                (string)"CL_Playground_Breadcrumbs_Crumb_Roshar".Translate(),
                (string)"CL_Playground_Breadcrumbs_Crumb_ShatteredPlains".Translate(),
            };
            return Breadcrumbs.Create(path, onNavigate: _ => { });
        });
    }

    [DocUsage]
    public static DocSample DocsUsage() {
        return new DocSample(() => {
            string[] path = new[] { "Worlds", "Roshar", "Shattered Plains" };
            return Breadcrumbs.Create(path);
        });
    }
}