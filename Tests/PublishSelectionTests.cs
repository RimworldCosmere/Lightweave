using System;
using System.Collections.Generic;
using Cosmere.Lightweave.Data;
using Cosmere.Lightweave.Redesign.Publish;
using Xunit;

namespace Cosmere.Lightweave.Tests;

public class PublishSelectionTests {
    [Fact]
    public void DeriveFolderState_is_unchecked_when_no_descendants() {
        TriState state = PublishSelection.DeriveFolderState([], _ => true);

        Assert.Equal(TriState.Unchecked, state);
    }

    [Fact]
    public void DeriveFolderState_is_checked_when_all_descendants_included() {
        List<string> leaves = ["a.cs", "b.cs"];

        TriState state = PublishSelection.DeriveFolderState(leaves, _ => true);

        Assert.Equal(TriState.Checked, state);
    }

    [Fact]
    public void DeriveFolderState_is_unchecked_when_no_descendants_included() {
        List<string> leaves = ["a.cs", "b.cs"];

        TriState state = PublishSelection.DeriveFolderState(leaves, _ => false);

        Assert.Equal(TriState.Unchecked, state);
    }

    [Fact]
    public void DeriveFolderState_is_mixed_when_some_descendants_included() {
        List<string> leaves = ["a.cs", "b.cs"];

        TriState state = PublishSelection.DeriveFolderState(leaves, path => path == "a.cs");

        Assert.Equal(TriState.Mixed, state);
    }

    [Fact]
    public void SelectFilesForUpload_keeps_only_included_paths_in_order() {
        List<string> all = ["a.cs", "b.cs", "c.cs", "d.cs"];
        HashSet<string> included = ["a.cs", "c.cs"];

        List<string> selected = PublishSelection.SelectFilesForUpload(all, included.Contains);

        Assert.Equal(["a.cs", "c.cs"], selected);
    }

    [Fact]
    public void SelectFilesForUpload_returns_empty_when_nothing_included() {
        List<string> all = ["a.cs", "b.cs"];

        List<string> selected = PublishSelection.SelectFilesForUpload(all, _ => false);

        Assert.Empty(selected);
    }
}
