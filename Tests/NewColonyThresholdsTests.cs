using Cosmere.Lightweave.Redesign.NewColony;
using Xunit;

namespace Cosmere.Lightweave.Tests;

public class NewColonyThresholdsTests {
    [Theory]
    [InlineData(0f, "Rare")]
    [InlineData(0.05f, "Rare")]
    [InlineData(0.119f, "Rare")]
    [InlineData(0.12f, "Light")]
    [InlineData(0.24f, "Light")]
    [InlineData(0.25f, "Balanced")]
    [InlineData(0.44f, "Balanced")]
    [InlineData(0.45f, "Frequent")]
    [InlineData(0.69f, "Frequent")]
    [InlineData(0.70f, "Severe")]
    [InlineData(0.89f, "Severe")]
    [InlineData(0.90f, "Overwhelming")]
    [InlineData(1f, "Overwhelming")]
    public void AnomalyIntensityKey_maps_fraction_to_band(float fraction, string expected) {
        Assert.Equal(expected, NewColonyThresholds.AnomalyIntensityKey(fraction));
    }

    [Theory]
    [InlineData(0f, 0)]
    [InlineData(0.5f, 50)]
    [InlineData(1f, 100)]
    [InlineData(1.3f, 130)]
    [InlineData(1.7f, 170)]
    [InlineData(2.5f, 250)]
    public void DifficultyPct_rounds_scale_to_percent(float threatScale, int expected) {
        Assert.Equal(expected, NewColonyThresholds.DifficultyPct(threatScale));
    }

    [Theory]
    [InlineData(0, DifficultyTier.Peaceful)]
    [InlineData(1, DifficultyTier.Low)]
    [InlineData(50, DifficultyTier.Low)]
    [InlineData(99, DifficultyTier.Low)]
    [InlineData(100, DifficultyTier.Intended)]
    [InlineData(120, DifficultyTier.Intended)]
    [InlineData(129, DifficultyTier.Intended)]
    [InlineData(130, DifficultyTier.Danger)]
    [InlineData(200, DifficultyTier.Danger)]
    public void DifficultyTierOf_classifies_at_thresholds(int pct, DifficultyTier expected) {
        Assert.Equal(expected, NewColonyThresholds.DifficultyTierOf(pct));
    }

    [Fact]
    public void LeadParagraph_drops_rimworld_section_markup_after_blank_line() {
        // Vanilla DifficultyDef.description shape: lead sentence, blank line, then a
        // (*SectionTitle)...(/SectionTitle) block Lightweave's Text cannot parse. The
        // card should render only the clean lead sentence (matches the mock).
        string description =
            "Build a community with a taste of danger. Threats appear, but they're weakened."
            + "\n\n(*SectionTitle)Recommended for:(/SectionTitle)\n - Players who are new to this kind of game.";

        Assert.Equal(
            "Build a community with a taste of danger. Threats appear, but they're weakened.",
            NewColonyThresholds.LeadParagraph(description)
        );
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("Single paragraph, no markup.", "Single paragraph, no markup.")]
    [InlineData("  trimmed lead  \n\nrest", "trimmed lead")]
    public void LeadParagraph_handles_edge_cases(string? input, string expected) {
        Assert.Equal(expected, NewColonyThresholds.LeadParagraph(input));
    }
}
