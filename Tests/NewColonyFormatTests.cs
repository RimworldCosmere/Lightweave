using System.Collections.Generic;
using Cosmere.Lightweave.Redesign.NewColony;
using Xunit;

namespace Cosmere.Lightweave.Tests;

public class NewColonyFormatTests {
    [Fact]
    public void ParseTipSegments_TitleOnly_YieldsOneTitle() {
        List<NewColonyThresholds.TipSegment> segs =
            NewColonyThresholds.ParseTipSegments("(*SectionTitle)Section Title Information Here(/SectionTitle)");

        Assert.Single(segs);
        Assert.True(segs[0].IsTitle);
        Assert.Equal("Section Title Information Here", segs[0].Text);
    }

    [Fact]
    public void ParseTipSegments_LeadThenSection_YieldsBodyThenTitle() {
        string input = "Strive to survive on a rough, tough planet.\n\n(*SectionTitle)Section Title Information Here(/SectionTitle)";
        List<NewColonyThresholds.TipSegment> segs = NewColonyThresholds.ParseTipSegments(input);

        Assert.Equal(2, segs.Count);
        Assert.False(segs[0].IsTitle);
        Assert.Equal("Strive to survive on a rough, tough planet.", segs[0].Text);
        Assert.True(segs[1].IsTitle);
        Assert.Equal("Section Title Information Here", segs[1].Text);
    }

    [Fact]
    public void ParseTipSegments_TitleThenBody_KeepsOrderAndStyling() {
        string input = "(*SectionTitle)Heading(/SectionTitle)\nThe body that follows.";
        List<NewColonyThresholds.TipSegment> segs = NewColonyThresholds.ParseTipSegments(input);

        Assert.Equal(2, segs.Count);
        Assert.True(segs[0].IsTitle);
        Assert.Equal("Heading", segs[0].Text);
        Assert.False(segs[1].IsTitle);
        Assert.Equal("The body that follows.", segs[1].Text);
    }

    [Fact]
    public void ParseTipSegments_OrphanOpenTag_DroppedKeepsBody() {
        List<NewColonyThresholds.TipSegment> segs = NewColonyThresholds.ParseTipSegments("(*SectionTitle)Body text");

        Assert.Single(segs);
        Assert.False(segs[0].IsTitle);
        Assert.Equal("Body text", segs[0].Text);
    }

    [Fact]
    public void ParseTipSegments_PlainText_YieldsSingleBody() {
        List<NewColonyThresholds.TipSegment> segs =
            NewColonyThresholds.ParseTipSegments("A relaxed experience (no raids) for new players.");

        Assert.Single(segs);
        Assert.False(segs[0].IsTitle);
        Assert.Equal("A relaxed experience (no raids) for new players.", segs[0].Text);
    }

    [Fact]
    public void ParseTipSegments_NullOrEmpty_YieldsNoSegments() {
        Assert.Empty(NewColonyThresholds.ParseTipSegments(null));
        Assert.Empty(NewColonyThresholds.ParseTipSegments(""));
    }
}
