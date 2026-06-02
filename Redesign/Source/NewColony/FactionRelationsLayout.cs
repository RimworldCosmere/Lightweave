using System;

namespace Cosmere.Lightweave.Redesign.NewColony;

// Pure sizing math for Dialog_FactionRelations, extracted so the window-size contract is unit
// testable without a RimWorld runtime. The dialog renders a fixed-size card: a title-row header
// stacked over a ScrollArea whose content is the goodwill matrix padded by Lg on every side.
//
// The window must hug the matrix exactly - no oversized card (the old +80/+120 fudge left a wide
// band of dead space and made every row's bottom divider overrun past the last column). The only
// nuance is the scroll gutter: the Slim rail reserves horizontal space only when the matrix is
// taller than the viewport, so we add that gutter to the width only when the matrix will scroll,
// keeping the common (fits) case pixel-tight.
public readonly struct FactionRelationsSize {
    public readonly float Width;
    public readonly float Height;

    public FactionRelationsSize(float width, float height) {
        Width = width;
        Height = height;
    }
}

public static class FactionRelationsLayout {
    public const float ScreenFraction = 0.9f;

    public static FactionRelationsSize Measure(
        int factionCount,
        float labelWidth,
        float cellWidth,
        float cellHeight,
        float headerHeight,
        float bodyPadding,
        float cardChrome,
        float scrollGutter,
        float screenWidth,
        float screenHeight
    ) {
        float matrixWidth = labelWidth + factionCount * cellWidth;
        // Header row + one data row per faction.
        float matrixHeight = (factionCount + 1) * cellHeight;
        float contentHeight = headerHeight + bodyPadding + matrixHeight;
        float screenCapHeight = screenHeight * ScreenFraction;

        // Reserve the gutter only when the matrix will be taller than the capped viewport, so a
        // scrolling matrix never also triggers a horizontal scrollbar while a fitting matrix stays
        // flush to the right edge.
        float gutter = contentHeight > screenCapHeight ? scrollGutter : 0f;

        float width = Math.Min(cardChrome + bodyPadding + matrixWidth + gutter, screenWidth * ScreenFraction);
        float height = Math.Min(contentHeight, screenCapHeight);
        return new FactionRelationsSize(width, height);
    }
}
