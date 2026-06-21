using System.Collections.Generic;
using Cosmere.Lightweave.Redesign.NewColony;
using Xunit;

namespace Cosmere.Lightweave.Tests;

public class IdeologyDecisionsTests {
    [Fact]
    public void StructureOnly_ReturnsStructure() {
        Assert.Equal("Ideological", IdeologyDecisions.ComposeSelectionSubtitle("Ideological", null));
    }

    [Fact]
    public void StructureOnly_EmptyMemeList_ReturnsStructure() {
        Assert.Equal("Ideological", IdeologyDecisions.ComposeSelectionSubtitle("Ideological", new List<string>()));
    }

    [Fact]
    public void StructureAndMemes_JoinsWithSeparators() {
        Assert.Equal(
            "Ideological · Guilty, Pain is virtue",
            IdeologyDecisions.ComposeSelectionSubtitle("Ideological", new List<string> { "Guilty", "Pain is virtue" }));
    }

    [Fact]
    public void NullStructure_WithMemes_ReturnsMemesOnly() {
        Assert.Equal("Guilty", IdeologyDecisions.ComposeSelectionSubtitle(null, new List<string> { "Guilty" }));
    }

    [Fact]
    public void EmptyStructure_WithMemes_ReturnsMemesOnly() {
        Assert.Equal("Cannibal", IdeologyDecisions.ComposeSelectionSubtitle("", new List<string> { "Cannibal" }));
    }

    [Fact]
    public void NothingSelected_ReturnsEmpty() {
        Assert.Equal(string.Empty, IdeologyDecisions.ComposeSelectionSubtitle(null, null));
    }

    [Fact]
    public void StructureWhitespace_IsTrimmed() {
        Assert.Equal("Animist", IdeologyDecisions.ComposeSelectionSubtitle("  Animist  ", null));
    }
}
