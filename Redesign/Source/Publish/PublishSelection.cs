using System;
using System.Collections.Generic;
using Cosmere.Lightweave.Data;

namespace Cosmere.Lightweave.Redesign.Publish;

/// <summary>
/// Pure selection helpers shared by the publish dialog and the uploader.
/// No disk or Unity access — unit-testable in isolation.
/// </summary>
public static class PublishSelection {
    /// <summary>
    /// Derives a folder row's tri-state from the inclusion of every file beneath it.
    /// <paramref name="descendantRelPaths"/> are the leaf file paths under the folder.
    /// </summary>
    public static TriState DeriveFolderState(
        IReadOnlyList<string> descendantRelPaths,
        Func<string, bool> isIncluded
    ) {
        if (descendantRelPaths.Count == 0) {
            return TriState.Unchecked;
        }

        int included = 0;
        for (int i = 0; i < descendantRelPaths.Count; i++) {
            if (isIncluded(descendantRelPaths[i])) {
                included++;
            }
        }

        if (included == 0) {
            return TriState.Unchecked;
        }

        return included == descendantRelPaths.Count ? TriState.Checked : TriState.Mixed;
    }

    /// <summary>
    /// Returns the subset of <paramref name="relPaths"/> that should be staged for upload,
    /// preserving input order.
    /// </summary>
    public static List<string> SelectFilesForUpload(
        IReadOnlyList<string> relPaths,
        Func<string, bool> isIncluded
    ) {
        List<string> selected = new List<string>();
        for (int i = 0; i < relPaths.Count; i++) {
            string relPath = relPaths[i];
            if (isIncluded(relPath)) {
                selected.Add(relPath);
            }
        }

        return selected;
    }
}
