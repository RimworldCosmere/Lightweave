using Cosmere.Lightweave.Redesign.NewColony;
using Xunit;

namespace Cosmere.Lightweave.Tests;

// Guards the Dialog_FactionRelations window-size contract: the card must hug the goodwill matrix
// exactly (no dead band to the right or below, which is what the old +80/+120 fudge produced), and
// the Slim scroll-rail gutter must be reserved on the width ONLY when the matrix is taller than the
// capped viewport - so a scrolling matrix never also triggers a horizontal scrollbar, and a matrix
// that fits stays flush to the right edge.
public class FactionRelationsLayoutTests {
    private const float LabelWidth = 200f;
    private const float CellWidth = 44f;
    private const float CellHeight = 34f;
    private const float HeaderHeight = 84f;
    private const float BodyPadding = 48f;
    private const float CardChrome = 4f;
    private const float ScrollGutter = 12f;
    private const float Tolerance = 0.01f;

    private static FactionRelationsSize Measure(int factionCount, float screenWidth, float screenHeight) {
        return FactionRelationsLayout.Measure(
            factionCount,
            LabelWidth,
            CellWidth,
            CellHeight,
            HeaderHeight,
            BodyPadding,
            CardChrome,
            ScrollGutter,
            screenWidth,
            screenHeight);
    }

    [Fact]
    public void FitsWithinScreen_SizesToContentWithNoGutter() {
        FactionRelationsSize size = Measure(factionCount: 4, screenWidth: 1920f, screenHeight: 1080f);

        // matrixWidth = 200 + 4*44 = 376; width = chrome 4 + padding 48 + 376, no gutter.
        Assert.Equal(428f, size.Width, Tolerance);
        // contentHeight = header 84 + padding 48 + (4+1)*34 = 302, well under the 972 cap.
        Assert.Equal(302f, size.Height, Tolerance);
    }

    [Fact]
    public void ExceedsScreenHeight_ReservesGutterAndCapsHeight() {
        // Same 10 factions, two screen heights. Tall screen fits; short screen forces a vertical
        // scroll, which must add exactly one gutter to the width and cap the height at 90% of screen.
        FactionRelationsSize fits = Measure(factionCount: 10, screenWidth: 1920f, screenHeight: 1080f);
        FactionRelationsSize scrolls = Measure(factionCount: 10, screenWidth: 1920f, screenHeight: 300f);

        // matrixWidth = 200 + 10*44 = 640; fits width = 4 + 48 + 640 = 692, no gutter.
        Assert.Equal(692f, fits.Width, Tolerance);
        Assert.Equal(692f + ScrollGutter, scrolls.Width, Tolerance);

        // contentHeight = 84 + 48 + (10+1)*34 = 506. Fits under 972; capped to 300*0.9 when scrolling.
        Assert.Equal(506f, fits.Height, Tolerance);
        Assert.Equal(300f * FactionRelationsLayout.ScreenFraction, scrolls.Height, Tolerance);
    }

    [Fact]
    public void ExceedsScreenWidth_CapsToNinetyPercent() {
        FactionRelationsSize size = Measure(factionCount: 100, screenWidth: 1920f, screenHeight: 1080f);

        // matrixWidth = 200 + 100*44 = 4600 blows past the screen, so width caps at 90% of 1920.
        Assert.Equal(1920f * FactionRelationsLayout.ScreenFraction, size.Width, Tolerance);
        // 101 rows tall also exceeds the height cap.
        Assert.Equal(1080f * FactionRelationsLayout.ScreenFraction, size.Height, Tolerance);
    }
}
