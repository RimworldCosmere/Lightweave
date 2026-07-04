using System;
using Cosmere.Lightweave.Redesign.NewColony;
using Xunit;

namespace Cosmere.Lightweave.Tests;

public class IdeoDetailSectionsTests {
    [Theory]
    [InlineData(IdeoDetailSection.Memes, IdeoEditorTab.Memes)]
    [InlineData(IdeoDetailSection.Precepts, IdeoEditorTab.Precepts)]
    [InlineData(IdeoDetailSection.Rituals, IdeoEditorTab.Rituals)]
    [InlineData(IdeoDetailSection.Roles, IdeoEditorTab.Roles)]
    [InlineData(IdeoDetailSection.Relics, IdeoEditorTab.Relics)]
    [InlineData(IdeoDetailSection.Buildings, IdeoEditorTab.Buildings)]
    [InlineData(IdeoDetailSection.Animals, IdeoEditorTab.Animals)]
    public void BeliefSections_MapToTheirTab(IdeoDetailSection section, IdeoEditorTab expected) {
        Assert.True(IdeoDetailSections.IsBelief(section));
        Assert.Equal(expected, IdeoDetailSections.BeliefTab(section));
    }

    [Theory]
    [InlineData(IdeoDetailSection.Xenotypes)]
    [InlineData(IdeoDetailSection.Apparel)]
    [InlineData(IdeoDetailSection.Appearance)]
    public void PreferenceSections_AreNotBelief(IdeoDetailSection section) {
        Assert.False(IdeoDetailSections.IsBelief(section));
        Assert.Null(IdeoDetailSections.BeliefTab(section));
    }

    [Fact]
    public void BeliefTab_RoundTripsThroughFromBeliefTab() {
        foreach (IdeoEditorTab tab in Enum.GetValues(typeof(IdeoEditorTab))) {
            IdeoDetailSection section = IdeoDetailSections.FromBeliefTab(tab);
            Assert.Equal(tab, IdeoDetailSections.BeliefTab(section));
        }
    }

    [Fact]
    public void IsBelief_IsTotal_OverEverySection() {
        // Exactly the seven belief members report true; the three preference members report false.
        int beliefCount = 0;
        foreach (IdeoDetailSection section in Enum.GetValues(typeof(IdeoDetailSection))) {
            if (IdeoDetailSections.IsBelief(section)) {
                beliefCount++;
            }
        }

        Assert.Equal(7, beliefCount);
    }

    [Fact]
    public void Id_RoundTripsThroughParse() {
        foreach (IdeoDetailSection section in Enum.GetValues(typeof(IdeoDetailSection))) {
            Assert.Equal(section, IdeoDetailSections.Parse(IdeoDetailSections.Id(section)));
        }
    }

    [Fact]
    public void Parse_UnknownId_FallsBackToMemes() {
        Assert.Equal(IdeoDetailSection.Memes, IdeoDetailSections.Parse("not-a-section"));
    }
}
