using System.Collections.Generic;
using Cosmere.Lightweave.Redesign.NewColony;
using Xunit;

namespace Cosmere.Lightweave.Tests;

public class IdeoPreceptRulesTests {
    [Fact]
    public void NoRequiredMemes_AlwaysAllowed() {
        Assert.True(IdeoPreceptRules.IsAllowed(null, new HashSet<string>()));
        Assert.True(IdeoPreceptRules.IsAllowed(new List<string>(), new HashSet<string> { "Cannibal" }));
    }

    [Fact]
    public void RequiredMeme_PresentMemeMatches_Allowed() {
        Assert.True(IdeoPreceptRules.IsAllowed(
            new List<string> { "Cannibal", "Raider" },
            new HashSet<string> { "Raider" }));
    }

    [Fact]
    public void RequiredMeme_NonePresent_Disallowed() {
        Assert.False(IdeoPreceptRules.IsAllowed(
            new List<string> { "Cannibal" },
            new HashSet<string> { "Pain" }));
    }

    [Fact]
    public void RequiredMeme_NoMemesAtAll_Disallowed() {
        Assert.False(IdeoPreceptRules.IsAllowed(new List<string> { "Cannibal" }, null));
        Assert.False(IdeoPreceptRules.IsAllowed(new List<string> { "Cannibal" }, new HashSet<string>()));
    }

    [Fact]
    public void RequiredMeme_AnyOneMatches_Allowed() {
        // Mirrors vanilla HasRequiredMemes: ANY (not ALL) required meme present suffices.
        Assert.True(IdeoPreceptRules.IsAllowed(
            new List<string> { "A", "B", "C" },
            new HashSet<string> { "C", "D" }));
    }
}
