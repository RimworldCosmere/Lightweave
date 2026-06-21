using System.Collections.Generic;

namespace Cosmere.Lightweave.Redesign.NewColony;

// Pure mirror of RimWorld.IdeoFoundation.HasRequiredMemes: a precept is allowed when it lists no
// required memes, or when the ideo has ANY one of them. Used by both the precept picker (to
// annotate/disable tiles) over the live draft ideo. DefName strings only - no Verse/Unity
// dependency, unit-testable under net9.0.
public static class IdeoPreceptRules {
    public static bool IsAllowed(
        IReadOnlyList<string>? requiredMemeDefNames,
        IReadOnlyCollection<string>? presentMemeDefNames
    ) {
        if (requiredMemeDefNames == null || requiredMemeDefNames.Count == 0) {
            return true;
        }

        if (presentMemeDefNames == null || presentMemeDefNames.Count == 0) {
            return false;
        }

        for (int i = 0; i < requiredMemeDefNames.Count; i++) {
            if (presentMemeDefNames.Contains(requiredMemeDefNames[i])) {
                return true;
            }
        }

        return false;
    }
}
