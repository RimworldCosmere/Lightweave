using Cosmere.Lightweave.Redesign.NewColony;
using Xunit;

namespace Cosmere.Lightweave.Tests;

// Regression guard for the lock bug: re-rolling the name (or culture, or randomize-all) regenerates
// name/adjective/member as one grammar batch. A locked field of that trio must keep its prior value,
// even when a sibling field is the one being re-rolled. Reported in-game as "randomizing the name
// still randomizes a locked member noun".
public class TextSymbolLockTests {
    [Fact]
    public void LockedMemberNoun_SurvivesAGrammarReroll() {
        (string name, string adjective, string member) = IdeologyDecisions.ApplyTextSymbolLocks(
            SectionLock.MemberNoun,
            keptName: "Oldname", keptAdjective: "Oldadj", keptMember: "Oldmember",
            freshName: "Newname", freshAdjective: "Newadj", freshMember: "Newmember");

        Assert.Equal("Newname", name);
        Assert.Equal("Newadj", adjective);
        Assert.Equal("Oldmember", member);
    }

    [Fact]
    public void LockedName_SurvivesAGrammarReroll() {
        (string name, string adjective, string member) = IdeologyDecisions.ApplyTextSymbolLocks(
            SectionLock.Name,
            keptName: "Oldname", keptAdjective: "Oldadj", keptMember: "Oldmember",
            freshName: "Newname", freshAdjective: "Newadj", freshMember: "Newmember");

        Assert.Equal("Oldname", name);
        Assert.Equal("Newadj", adjective);
        Assert.Equal("Newmember", member);
    }

    [Fact]
    public void NoLocks_TakesEveryFreshValue() {
        (string name, string adjective, string member) = IdeologyDecisions.ApplyTextSymbolLocks(
            SectionLock.None,
            keptName: "Oldname", keptAdjective: "Oldadj", keptMember: "Oldmember",
            freshName: "Newname", freshAdjective: "Newadj", freshMember: "Newmember");

        Assert.Equal("Newname", name);
        Assert.Equal("Newadj", adjective);
        Assert.Equal("Newmember", member);
    }

    [Fact]
    public void AllThreeLocked_KeepsEveryPriorValue() {
        SectionLock all = SectionLock.Name | SectionLock.Adjective | SectionLock.MemberNoun;
        (string name, string adjective, string member) = IdeologyDecisions.ApplyTextSymbolLocks(
            all,
            keptName: "Oldname", keptAdjective: "Oldadj", keptMember: "Oldmember",
            freshName: "Newname", freshAdjective: "Newadj", freshMember: "Newmember");

        Assert.Equal("Oldname", name);
        Assert.Equal("Oldadj", adjective);
        Assert.Equal("Oldmember", member);
    }

    [Fact]
    public void UnrelatedLock_DoesNotTouchTheGrammarTrio() {
        // A lock on some other section (e.g. Deities) must not pin any grammar field.
        (string name, string adjective, string member) = IdeologyDecisions.ApplyTextSymbolLocks(
            SectionLock.Deities | SectionLock.Culture,
            keptName: "Oldname", keptAdjective: "Oldadj", keptMember: "Oldmember",
            freshName: "Newname", freshAdjective: "Newadj", freshMember: "Newmember");

        Assert.Equal("Newname", name);
        Assert.Equal("Newadj", adjective);
        Assert.Equal("Newmember", member);
    }

    [Theory]
    [InlineData(SectionLock.None, false)]
    [InlineData(SectionLock.Name, false)]
    [InlineData(SectionLock.Name | SectionLock.Adjective, false)]
    [InlineData(SectionLock.Name | SectionLock.Adjective | SectionLock.MemberNoun, true)]
    public void TextSymbolsFullyLocked_OnlyWhenAllThreeAreLocked(SectionLock locks, bool expected) {
        Assert.Equal(expected, IdeologyDecisions.TextSymbolsFullyLocked(locks));
    }

    // Structure lock regression: RandomizeMemes re-rolls the structure meme too, so a Structure lock has
    // to swap the original back afterward - but only when it actually changed and only when locked.
    // Reported in-game as "structure is still randomizing when locked".
    [Fact]
    public void StructureLocked_AndRolled_NeedsRestore() {
        object kept = new object();
        object rolled = new object();
        Assert.True(IdeologyDecisions.ShouldRestoreStructure(SectionLock.Structure, kept, rolled));
    }

    [Fact]
    public void StructureLocked_ButUnchanged_NoRestore() {
        object same = new object();
        Assert.False(IdeologyDecisions.ShouldRestoreStructure(SectionLock.Structure, same, same));
    }

    [Fact]
    public void StructureUnlocked_NeverRestores() {
        object kept = new object();
        object rolled = new object();
        Assert.False(IdeologyDecisions.ShouldRestoreStructure(SectionLock.None, kept, rolled));
        Assert.False(IdeologyDecisions.ShouldRestoreStructure(SectionLock.Memes, kept, rolled));
    }

    [Fact]
    public void StructureLocked_NullKept_NoRestore() {
        Assert.False(IdeologyDecisions.ShouldRestoreStructure<object>(SectionLock.Structure, null, new object()));
    }

    // Icon picker recolor decision: applying a new color clears the primary faction color (recolor-all)
    // only when the color actually changed. Mirrors vanilla Dialog_ChooseIdeoSymbols.TryAccept passing
    // newColorDef != ideo.colorDef. Reported in-game as "picking the icon should also recolor everything".
    [Fact]
    public void IconColorChanged_ClearsPrimaryFactionColor() {
        object oldColor = new object();
        object newColor = new object();
        Assert.True(IdeologyDecisions.ShouldClearPrimaryFactionColor(oldColor, newColor));
    }

    [Fact]
    public void IconColorUnchanged_KeepsPrimaryFactionColor() {
        object same = new object();
        Assert.False(IdeologyDecisions.ShouldClearPrimaryFactionColor(same, same));
    }
}
