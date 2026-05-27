using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;

namespace Cosmere.Lightweave.Playground;

public sealed record PlaygroundCategory(
    string Id,
    string LabelKey,
    string DescriptionKey,
    IReadOnlyList<string> PrimitiveIds
);

public static class PlaygroundRail {
    private const float CategoryRowHeight = 38f;
    private const float PrimitiveRowHeight = 28f;
    private static readonly float HighlightBarWidth = Spacing.StripeWidth.ToPixels();
    private const float RowPaddingX = 10f;
    private const float CategoryGap = 2f;
    private const float PrimitiveGap = 1f;
    private const float PrimitiveIndent = 18f;
    private const float HoverThresholdSeconds = 2f;
    private const float SubnavBlockPaddingY = 4f;

    public static LightweaveNode Create(
        IReadOnlyList<PlaygroundCategory> categories,
        Hooks.Hooks.StateHandle<string> selectedPrimitiveId,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        // Scope internal hook slots by the outer caller's file+line so each rail instance
        // owns its own pinned/hover state instead of sharing with every other Rail on screen.
        string scopeFile = file + "#PlaygroundRail";
        Hooks.Hooks.StateHandle<string?> pinnedCategoryId = Hooks.Hooks.UseState<string?>(null, line, scopeFile);
        Hooks.Hooks.RefHandle<Dictionary<string, float>> hoverStartsRef =
            Hooks.Hooks.UseRef(new Dictionary<string, float>(), line + 1, scopeFile);
        Hooks.Hooks.RefHandle<string?> lastHoverQualifiedRef = Hooks.Hooks.UseRef<string?>(null, line + 2, scopeFile);

        LightweaveNode node = NodeBuilder.New("PlaygroundRail", line, file);
        node.ApplyStyling("playground-rail", style, classes, id);

        node.Measure = _ => {
            float padY = SpacingScale.Sm.ToPixels();
            string? activeCategoryId = FindCategoryFor(categories, selectedPrimitiveId.Value);
            string? expandedId = pinnedCategoryId.Value ?? lastHoverQualifiedRef.Current ?? activeCategoryId;

            float h = padY;
            for (int i = 0; i < categories.Count; i++) {
                h += CategoryRowHeight + CategoryGap;
                if (categories[i].Id != expandedId) {
                    continue;
                }

                int n = categories[i].PrimitiveIds.Count;
                h += SubnavBlockPaddingY * 2f;
                h += n * (PrimitiveRowHeight + PrimitiveGap) - PrimitiveGap;
            }

            return h;
        };

        node.Paint = (rect, _) => {
            Theme.Theme theme = RenderContext.Current.Theme;
            Direction dir = RenderContext.Current.Direction;
            bool rtl = dir == Direction.Rtl;
            Event e = Event.current;
            Vector2 mouse = e.mousePosition;
            float now = Time.realtimeSinceStartup;

            float padX = SpacingScale.Sm.ToPixels();
            float padY = SpacingScale.Sm.ToPixels();
            float contentX = rect.x + padX;
            float contentWidth = rect.width - padX * 2f;

            string? activeCategoryId = FindCategoryFor(categories, selectedPrimitiveId.Value);

            // Decide expansion using stable cross-frame state so layout doesn't chicken-and-egg
            // with hover detection. Click-to-pin updates pinnedCategoryId immediately and
            // takes effect next frame; hover-to-expand relies on the previous frame's
            // hover-qualified id (imperceptible after a 2s hold).
            string? expandedId = pinnedCategoryId.Value ?? lastHoverQualifiedRef.Current ?? activeCategoryId;

            // Single-pass layout: expanded category's sub-nav pushes subsequent rows down.
            Rect[] catRects = new Rect[categories.Count];
            List<string>?[] sortedPerCat = new List<string>?[categories.Count];
            float[] primStartYPerCat = new float[categories.Count];
            Rect expandedSubnavRect = Rect.zero;

            float cursorY = rect.y + padY;
            for (int i = 0; i < categories.Count; i++) {
                catRects[i] = new Rect(contentX, cursorY, contentWidth, CategoryRowHeight);
                cursorY += CategoryRowHeight + CategoryGap;

                if (categories[i].Id != expandedId) {
                    continue;
                }

                List<string> sortedIds = new List<string>(categories[i].PrimitiveIds);
                sortedIds.Sort(string.CompareOrdinal);
                sortedPerCat[i] = sortedIds;

                float subnavTop = cursorY;
                cursorY += SubnavBlockPaddingY;
                primStartYPerCat[i] = cursorY;
                cursorY += sortedIds.Count * (PrimitiveRowHeight + PrimitiveGap);
                cursorY += SubnavBlockPaddingY - PrimitiveGap;
                expandedSubnavRect = new Rect(contentX, subnavTop, contentWidth, cursorY - subnavTop);
            }

            // Hover detection on freshly-laid-out rects. Treat sub-nav region as part of
            // the expanded category's hover zone so moving the pointer down onto primitives
            // doesn't reset the hover timer and collapse the block.
            bool mouseOverExpandedSubnav = expandedId != null
                && expandedSubnavRect.width > 0f
                && expandedSubnavRect.Contains(mouse);

            Dictionary<string, float> hoverStarts = hoverStartsRef.Current;
            string? hoverQualifiedId = null;
            for (int i = 0; i < categories.Count; i++) {
                PlaygroundCategory cat = categories[i];
                bool overRow = catRects[i].Contains(mouse);
                bool overSubnav = mouseOverExpandedSubnav && cat.Id == expandedId;
                bool hovering = overRow || overSubnav;

                if (hovering) {
                    if (!hoverStarts.TryGetValue(cat.Id, out float t) || t <= 0f) {
                        hoverStarts[cat.Id] = now;
                    }
                    else if (now - t >= HoverThresholdSeconds) {
                        hoverQualifiedId = cat.Id;
                    }
                }
                else if (hoverStarts.ContainsKey(cat.Id)) {
                    hoverStarts[cat.Id] = 0f;
                }
            }

            for (int i = 0; i < categories.Count; i++) {
                PlaygroundCategory cat = categories[i];
                Rect rowRect = catRects[i];
                bool expanded = cat.Id == expandedId;
                bool pinned = cat.Id == pinnedCategoryId.Value;
                bool overRow = rowRect.Contains(mouse);
                bool activeContainsSelection = cat.Id == activeCategoryId;

                DrawCategoryRow(rowRect, cat, theme, rtl, expanded, pinned, overRow, activeContainsSelection);

                if (Widgets.ButtonInvisible(rowRect, doMouseoverSound: false)) {
                    pinnedCategoryId.Set(pinned ? null : cat.Id);
                }

                TooltipHandler.TipRegion(rowRect, (string)cat.DescriptionKey.Translate());

                if (!expanded || sortedPerCat[i] == null) {
                    continue;
                }

                List<string> sortedIds = sortedPerCat[i]!;
                float primY = primStartYPerCat[i];
                for (int j = 0; j < sortedIds.Count; j++) {
                    string primId = sortedIds[j];
                    Rect primRect = new Rect(
                        contentX + (rtl ? 0f : PrimitiveIndent),
                        primY,
                        contentWidth - PrimitiveIndent,
                        PrimitiveRowHeight
                    );
                    bool isSelected = primId == selectedPrimitiveId.Value;
                    bool primHover = primRect.Contains(mouse);

                    DrawPrimitiveRow(primRect, primId, theme, rtl, isSelected, primHover);

                    if (Widgets.ButtonInvisible(primRect, doMouseoverSound: false)) {
                        selectedPrimitiveId.Set(primId);
                    }

                    primY += PrimitiveRowHeight + PrimitiveGap;
                }
            }

            lastHoverQualifiedRef.Current = hoverQualifiedId;
        };

        return node;
    }

    private static void DrawCategoryRow(
        Rect rowRect,
        PlaygroundCategory cat,
        Theme.Theme theme,
        bool rtl,
        bool expanded,
        bool pinned,
        bool hovering,
        bool activeContainsSelection
    ) {
        if (pinned || activeContainsSelection) {
            PaintBox.Fill(rowRect, theme.GetColor(ThemeSlot.ActiveTint));

            Color focusBar = theme.GetColor(ThemeSlot.BorderFocus);
            float barX = rtl ? rowRect.xMax - HighlightBarWidth : rowRect.x;
            Rect bar = new Rect(barX, rowRect.y, HighlightBarWidth, rowRect.height);
            PaintBox.Fill(bar, focusBar);
        }
        else if (hovering) {
            PaintBox.DrawHighlight(rowRect, RadiusSpec.All(RadiusScale.Sm), true);
        }

        string chevron = expanded ? "-" : "+";
        float labelInsetStart = rtl ? SpacingScale.Sm.ToPixels() : HighlightBarWidth + RowPaddingX;
        float labelInsetEnd = rtl ? HighlightBarWidth + RowPaddingX : SpacingScale.Sm.ToPixels();
        Rect labelRect = new Rect(
            rowRect.x + labelInsetStart,
            rowRect.y,
            rowRect.width - labelInsetStart - labelInsetEnd,
            rowRect.height
        );

        FontRole role = expanded || activeContainsSelection ? FontRole.BodyBold : FontRole.Body;
        ThemeSlot textSlot = activeContainsSelection ? ThemeSlot.TextPrimary : ThemeSlot.TextSecondary;

        string labelText = (string)cat.LabelKey.Translate();
        TextAnchor labelAnchor = rtl ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
        TextDraw.Draw(labelRect, labelText, role, new Rem(0.875f), labelAnchor, textSlot);

        Rect chevronRect = new Rect(
            rtl ? rowRect.x + RowPaddingX : rowRect.xMax - RowPaddingX - 12f,
            rowRect.y,
            12f,
            rowRect.height
        );
        TextAnchor chevronAnchor = rtl ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
        TextDraw.Draw(chevronRect, chevron, role, new Rem(0.875f), chevronAnchor, textSlot);
    }

    private static void DrawPrimitiveRow(
        Rect rowRect,
        string primId,
        Theme.Theme theme,
        bool rtl,
        bool isSelected,
        bool hovering
    ) {
        if (isSelected) {
            PaintBox.Fill(rowRect, theme.GetColor(ThemeSlot.ActiveTint));

            Color focusBar = theme.GetColor(ThemeSlot.BorderFocus);
            float barX = rtl ? rowRect.xMax - HighlightBarWidth : rowRect.x;
            Rect bar = new Rect(barX, rowRect.y, HighlightBarWidth, rowRect.height);
            PaintBox.Fill(bar, focusBar);
        }
        else if (hovering) {
            PaintBox.DrawHighlight(rowRect, RadiusSpec.All(RadiusScale.Sm), true);
        }

        float labelInsetStart = rtl ? RowPaddingX : HighlightBarWidth + RowPaddingX;
        float labelInsetEnd = rtl ? HighlightBarWidth + RowPaddingX : RowPaddingX;
        Rect labelRect = new Rect(
            rowRect.x + labelInsetStart,
            rowRect.y,
            rowRect.width - labelInsetStart - labelInsetEnd,
            rowRect.height
        );

        FontRole role = isSelected ? FontRole.BodyBold : FontRole.Body;
        ThemeSlot textSlot = isSelected ? ThemeSlot.TextPrimary : ThemeSlot.TextSecondary;

        string labelKey = "CL_Playground_" + primId + "_Title";
        string labelText = (string)labelKey.Translate();
        TextAnchor labelAnchor = rtl ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
        TextDraw.Draw(labelRect, labelText, role, new Rem(0.8125f), labelAnchor, textSlot);

        TooltipHandler.TipRegion(rowRect, labelText);
    }

    private static string? FindCategoryFor(IReadOnlyList<PlaygroundCategory> categories, string primitiveId) {
        for (int i = 0; i < categories.Count; i++) {
            IReadOnlyList<string> ids = categories[i].PrimitiveIds;
            for (int j = 0; j < ids.Count; j++) {
                if (ids[j] == primitiveId) {
                    return categories[i].Id;
                }
            }
        }

        return null;
    }
}
