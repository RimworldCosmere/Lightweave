using System.Collections.Generic;

namespace Cosmere.Lightweave.Redesign.NewColony;

// Pure decision helpers for the Ideology tab. No Verse/UnityEngine dependency so the
// logic is unit-testable under net9.0 (linked into Lightweave.Tests). Vanilla def lookups
// and rendering live in IdeologyTab / NewColonyLauncher; this file is only the string and
// validation logic that does not need the game runtime.
public static class IdeologyDecisions {
    // The current-selection subtitle in the rail: "{structure} · {meme1}, {meme2}".
    // Mirrors the mock's nc-ideo-current-sub composition (structure name, then memes when present).
    public static string ComposeSelectionSubtitle(string? structureName, IReadOnlyList<string>? memeNames) {
        string structure = structureName?.Trim() ?? string.Empty;

        if (memeNames == null || memeNames.Count == 0) {
            return structure;
        }

        string memes = string.Join(", ", memeNames);
        if (structure.Length == 0) {
            return memes;
        }

        return structure + " · " + memes;
    }

    // Name, adjective and member noun all regenerate from one grammar request, so any re-roll that
    // touches the grammar (the name dice, a culture re-roll, randomize-all) produces fresh values for
    // all three at once. This resolves the final value for each field: a locked field keeps the value it
    // had before the re-roll; an unlocked field takes the freshly generated value.
    public static (string Name, string Adjective, string Member) ApplyTextSymbolLocks(
        SectionLock locks,
        string keptName, string keptAdjective, string keptMember,
        string freshName, string freshAdjective, string freshMember) {
        string name = (locks & SectionLock.Name) != 0 ? keptName : freshName;
        string adjective = (locks & SectionLock.Adjective) != 0 ? keptAdjective : freshAdjective;
        string member = (locks & SectionLock.MemberNoun) != 0 ? keptMember : freshMember;
        return (name, adjective, member);
    }

    // True when all three grammar fields are locked, so a text-symbol re-roll is a no-op and the grammar
    // regeneration can be skipped entirely.
    public static bool TextSymbolsFullyLocked(SectionLock locks) {
        return (locks & SectionLock.Name) != 0
            && (locks & SectionLock.Adjective) != 0
            && (locks & SectionLock.MemberNoun) != 0;
    }


    // The structure meme is part of the meme set, so re-rolling memes also re-rolls the structure. When
    // Structure is locked, the original structure must be swapped back afterward - but only when the
    // roll actually changed it. Returns true when a restore swap is needed. T is MemeDef in production;
    // kept generic + reference-equality so this stays unit-testable without Verse types.
    public static bool ShouldRestoreStructure<T>(SectionLock locks, T? keptStructure, T? rolledStructure)
        where T : class {
        if ((locks & SectionLock.Structure) == 0) {
            return false;
        }
        if (keptStructure == null) {
            return false;
        }
        return !ReferenceEquals(keptStructure, rolledStructure);
    }

    // When the icon picker applies a new color, vanilla SetIcon recolors everything tied to the old
    // identity color (clearPrimaryFactionColor) - but only when the color actually changed. Returns
    // true when the recolor-all should fire. T is ColorDef in production; generic + reference-equality
    // keeps this unit-testable without Verse types.
    public static bool ShouldClearPrimaryFactionColor<T>(T? currentColor, T? newColor)
        where T : class {
        return !ReferenceEquals(currentColor, newColor);
    }
}
