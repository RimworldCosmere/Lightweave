using System.Collections.Generic;
using Cosmere.Lightweave.Redesign.NewColony;
using Xunit;

namespace Cosmere.Lightweave.Tests;

public class IdeoComplexityTests {
    [Fact]
    public void Score_NoMemes_IsZero() {
        Assert.Equal(0, IdeoComplexity.Score(0, 0));
    }

    [Fact]
    public void Score_MemesAndPrecepts_SumWeighted() {
        // 2 memes * 2 + 3 precepts * 1 = 7
        Assert.Equal(7, IdeoComplexity.Score(2, 3));
    }

    [Fact]
    public void Score_ClampsToMax() {
        Assert.Equal(IdeoComplexity.MaxScore, IdeoComplexity.Score(20, 20));
    }

    [Fact]
    public void Score_NegativeInputs_TreatedAsZero() {
        Assert.Equal(0, IdeoComplexity.Score(-5, -5));
    }

    [Fact]
    public void Score_FromMemeList_CountsMemesOnly() {
        List<string> memes = ["A", "B", "C"];
        Assert.Equal(6, IdeoComplexity.Score(memes));
    }

    [Fact]
    public void Score_FromNullList_IsZero() {
        Assert.Equal(0, IdeoComplexity.Score((IReadOnlyList<string>?)null));
    }

    [Theory]
    [InlineData(0, IdeoImpact.Low)]
    [InlineData(3, IdeoImpact.Low)]
    [InlineData(4, IdeoImpact.Medium)]
    [InlineData(6, IdeoImpact.Medium)]
    [InlineData(7, IdeoImpact.High)]
    [InlineData(10, IdeoImpact.High)]
    public void Label_MatchesMockThresholds(int score, IdeoImpact expected) {
        Assert.Equal(expected, IdeoComplexity.Label(score));
    }
}
