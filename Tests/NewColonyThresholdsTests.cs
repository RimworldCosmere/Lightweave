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

    [Theory]
    [InlineData(0, "GMT+0")]
    [InlineData(5, "GMT+5")]
    [InlineData(12, "GMT+12")]
    [InlineData(-3, "GMT-3")]
    [InlineData(-11, "GMT-11")]
    public void TimeZoneLabel_signs_offset_relative_to_gmt(int zone, string expected) {
        Assert.Equal(expected, NewColonyThresholds.TimeZoneLabel(zone));
    }

    [Theory]
    [InlineData(0f, "—")]
    [InlineData(-1f, "—")]
    [InlineData(60f, "1.0/yr")]
    [InlineData(120f, "0.5/yr")]
    [InlineData(30f, "2.0/yr")]
    public void DiseaseFrequencyLabel_converts_mtb_days_to_yearly_rate(float mtbDays, string expected) {
        Assert.Equal(expected, NewColonyThresholds.DiseaseFrequencyLabel(mtbDays));
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(-100, "-100")]
    [InlineData(-1, "-1")]
    [InlineData(1, "+1")]
    [InlineData(75, "+75")]
    [InlineData(100, "+100")]
    public void GoodwillText_prefixes_positive_values_only(int goodwill, string expected) {
        Assert.Equal(expected, NewColonyThresholds.GoodwillText(goodwill));
    }

    [Fact]
    public void CanAddFaction_allows_when_under_both_caps() {
        Assert.Equal(FactionAddRule.Allowed,
            NewColonyThresholds.CanAddFaction(defHidden: false, nonHiddenCount: 5, countOfDef: 1, maxForType: 3));
    }

    [Fact]
    public void CanAddFaction_blocks_on_total_cap_for_visible_factions() {
        Assert.Equal(FactionAddRule.TotalCapReached,
            NewColonyThresholds.CanAddFaction(defHidden: false, nonHiddenCount: 12, countOfDef: 0, maxForType: 5));
    }

    [Fact]
    public void CanAddFaction_lets_hidden_factions_bypass_total_cap() {
        Assert.Equal(FactionAddRule.Allowed,
            NewColonyThresholds.CanAddFaction(defHidden: true, nonHiddenCount: 12, countOfDef: 0, maxForType: 5));
    }

    [Fact]
    public void CanAddFaction_blocks_on_type_cap_even_when_under_total_cap() {
        Assert.Equal(FactionAddRule.TypeCapReached,
            NewColonyThresholds.CanAddFaction(defHidden: false, nonHiddenCount: 3, countOfDef: 2, maxForType: 2));
    }

    [Fact]
    public void CanAddFaction_checks_total_cap_before_type_cap() {
        // Both caps are exceeded; the total-cap rule wins, matching vanilla's check order.
        Assert.Equal(FactionAddRule.TotalCapReached,
            NewColonyThresholds.CanAddFaction(defHidden: false, nonHiddenCount: 12, countOfDef: 5, maxForType: 2));
    }

    [Fact]
    public void CanAddFaction_respects_custom_total_cap() {
        Assert.Equal(FactionAddRule.TotalCapReached,
            NewColonyThresholds.CanAddFaction(defHidden: false, nonHiddenCount: 3, countOfDef: 0, maxForType: 5, totalCap: 3));
    }

    [Fact]
    public void CountForm_capped_at_max_reads_max() {
        Assert.Equal(FactionCountForm.Max, NewColonyThresholds.CountForm(capped: true, count: 1, max: 1));
    }

    [Fact]
    public void CountForm_capped_over_max_still_reads_max() {
        Assert.Equal(FactionCountForm.Max, NewColonyThresholds.CountForm(capped: true, count: 3, max: 2));
    }

    [Fact]
    public void CountForm_capped_below_max_reads_ratio() {
        Assert.Equal(FactionCountForm.Ratio, NewColonyThresholds.CountForm(capped: true, count: 1, max: 3));
    }

    [Fact]
    public void CountForm_capped_at_zero_below_max_reads_ratio() {
        Assert.Equal(FactionCountForm.Ratio, NewColonyThresholds.CountForm(capped: true, count: 0, max: 3));
    }

    [Fact]
    public void CountForm_uncapped_with_count_reads_multiple() {
        Assert.Equal(FactionCountForm.Multiple, NewColonyThresholds.CountForm(capped: false, count: 2, max: 0));
    }

    [Fact]
    public void CountForm_uncapped_at_zero_reads_none() {
        Assert.Equal(FactionCountForm.None, NewColonyThresholds.CountForm(capped: false, count: 0, max: 0));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(9998)]
    public void IsConfigurableCap_small_positive_bound_is_capped(int max) {
        Assert.True(NewColonyThresholds.IsConfigurableCap(max));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9999)]
    [InlineData(10000)]
    public void IsConfigurableCap_sentinel_or_unset_is_uncapped(int max) {
        Assert.False(NewColonyThresholds.IsConfigurableCap(max));
    }

    [Fact]
    public void ResolveSeed_prefers_explicit_seed_over_last() {
        Assert.Equal("typed", NewColonyThresholds.ResolveSeed("typed", "last"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ResolveSeed_reuses_last_when_no_explicit(string? explicitSeed) {
        Assert.Equal("last", NewColonyThresholds.ResolveSeed(explicitSeed, "last"));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    public void ResolveSeed_null_when_nothing_to_reuse(string? explicitSeed, string? lastSeed) {
        Assert.Null(NewColonyThresholds.ResolveSeed(explicitSeed, lastSeed));
    }
}
