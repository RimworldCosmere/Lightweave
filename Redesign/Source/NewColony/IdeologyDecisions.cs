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
}
