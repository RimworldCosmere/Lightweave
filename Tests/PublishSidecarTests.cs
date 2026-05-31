using Cosmere.Lightweave.Redesign.Publish;
using Xunit;

namespace Cosmere.Lightweave.Tests;

public class PublishSidecarTests {
    [Fact]
    public void Baseline_exclusion_is_honored_when_no_override_exists() {
        SteamIgnoreMatcher baseline = new SteamIgnoreMatcher(["*.log"]);
        PublishSidecar sidecar = new PublishSidecar();

        Assert.False(sidecar.ResolveInclusion("debug.log", baseline));
        Assert.True(sidecar.ResolveInclusion("About/About.xml", baseline));
    }

    [Fact]
    public void Sidecar_override_re_includes_a_baseline_excluded_file() {
        SteamIgnoreMatcher baseline = new SteamIgnoreMatcher(["*.log"]);
        PublishSidecar sidecar = new PublishSidecar();
        sidecar.Overrides["debug.log"] = true;

        Assert.True(sidecar.ResolveInclusion("debug.log", baseline));
    }

    [Fact]
    public void Folder_override_propagates_to_descendants() {
        PublishSidecar sidecar = new PublishSidecar();
        sidecar.Overrides["Source"] = false;

        Assert.False(sidecar.ResolveInclusion("Source/Foo.cs", SteamIgnoreMatcher.Empty));
        Assert.False(sidecar.ResolveInclusion("Source/Nested/Bar.cs", SteamIgnoreMatcher.Empty));
    }

    [Fact]
    public void More_specific_override_wins_over_an_ancestor_override() {
        PublishSidecar sidecar = new PublishSidecar();
        sidecar.Overrides["Source"] = false;
        sidecar.Overrides["Source/Keep"] = true;

        Assert.False(sidecar.ResolveInclusion("Source/Foo.cs", SteamIgnoreMatcher.Empty));
        Assert.True(sidecar.ResolveInclusion("Source/Keep/Bar.cs", SteamIgnoreMatcher.Empty));
    }

    [Fact]
    public void Sidecar_file_itself_is_always_excluded() {
        PublishSidecar sidecar = new PublishSidecar();

        Assert.False(sidecar.ResolveInclusion(PublishSidecar.SidecarFileName, SteamIgnoreMatcher.Empty));
    }

    [Fact]
    public void Published_file_id_is_always_included_even_when_baseline_would_exclude_it() {
        SteamIgnoreMatcher baseline = new SteamIgnoreMatcher(["*.txt"]);
        PublishSidecar sidecar = new PublishSidecar();

        Assert.True(sidecar.ResolveInclusion(PublishSidecar.PublishedFileIdRelPath, baseline));
    }

    [Fact]
    public void Special_cases_outrank_a_contradicting_override() {
        PublishSidecar sidecar = new PublishSidecar();
        sidecar.Overrides[PublishSidecar.SidecarFileName] = true;
        sidecar.Overrides[PublishSidecar.PublishedFileIdRelPath] = false;

        Assert.False(sidecar.ResolveInclusion(PublishSidecar.SidecarFileName, SteamIgnoreMatcher.Empty));
        Assert.True(sidecar.ResolveInclusion(PublishSidecar.PublishedFileIdRelPath, SteamIgnoreMatcher.Empty));
    }
}
