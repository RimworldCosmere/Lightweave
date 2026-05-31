using Cosmere.Lightweave.Redesign.Publish;
using Xunit;

namespace Cosmere.Lightweave.Tests;

public class SteamIgnoreMatcherTests {
    [Fact]
    public void Empty_matcher_ignores_nothing() {
        SteamIgnoreMatcher matcher = SteamIgnoreMatcher.Empty;

        Assert.False(matcher.IsIgnored("About/About.xml"));
        Assert.False(matcher.IsIgnored("Source/Foo.cs"));
    }

    [Fact]
    public void Blank_and_comment_lines_are_skipped() {
        SteamIgnoreMatcher matcher = new SteamIgnoreMatcher(["# a comment", "", "   ", "*.log"]);

        Assert.True(matcher.IsIgnored("debug.log"));
        Assert.False(matcher.IsIgnored("debug.txt"));
    }

    [Fact]
    public void Star_matches_within_a_single_segment_at_any_depth() {
        SteamIgnoreMatcher matcher = new SteamIgnoreMatcher(["*.dll"]);

        Assert.True(matcher.IsIgnored("Foo.dll"));
        Assert.True(matcher.IsIgnored("Assemblies/Foo.dll"));
        Assert.False(matcher.IsIgnored("Foo.dllx"));
        Assert.False(matcher.IsIgnored("Foo.txt"));
    }

    [Fact]
    public void Question_mark_matches_exactly_one_character() {
        SteamIgnoreMatcher matcher = new SteamIgnoreMatcher(["v?.txt"]);

        Assert.True(matcher.IsIgnored("v1.txt"));
        Assert.False(matcher.IsIgnored("v12.txt"));
        Assert.False(matcher.IsIgnored("v.txt"));
    }

    [Fact]
    public void Bare_name_matches_at_any_depth_including_as_a_directory() {
        SteamIgnoreMatcher matcher = new SteamIgnoreMatcher(["obj"]);

        Assert.True(matcher.IsIgnored("obj"));
        Assert.True(matcher.IsIgnored("obj/Debug/x.dll"));
        Assert.True(matcher.IsIgnored("Source/obj/x.dll"));
        Assert.False(matcher.IsIgnored("Source/object.cs"));
    }

    [Fact]
    public void Leading_slash_anchors_to_the_root() {
        SteamIgnoreMatcher matcher = new SteamIgnoreMatcher(["/.git"]);

        Assert.True(matcher.IsIgnored(".git/config"));
        Assert.False(matcher.IsIgnored("Sub/.git/config"));
    }

    [Fact]
    public void Trailing_slash_matches_directories_only() {
        SteamIgnoreMatcher matcher = new SteamIgnoreMatcher(["node_modules/"]);

        Assert.True(matcher.IsIgnored("node_modules/pkg/index.js"));
        Assert.False(matcher.IsIgnored("node_modules"));
    }

    [Fact]
    public void Trailing_double_star_matches_everything_under_a_directory() {
        SteamIgnoreMatcher matcher = new SteamIgnoreMatcher(["docs/**"]);

        Assert.True(matcher.IsIgnored("docs/a/b.md"));
        Assert.True(matcher.IsIgnored("docs/index.md"));
        Assert.False(matcher.IsIgnored("docs"));
        Assert.False(matcher.IsIgnored("other/docs/x.md"));
    }

    [Fact]
    public void Leading_double_star_matches_a_name_at_any_depth() {
        SteamIgnoreMatcher matcher = new SteamIgnoreMatcher(["**/temp"]);

        Assert.True(matcher.IsIgnored("temp"));
        Assert.True(matcher.IsIgnored("a/b/temp"));
        Assert.False(matcher.IsIgnored("temporary"));
    }

    [Fact]
    public void Negation_re_includes_a_previously_ignored_file() {
        SteamIgnoreMatcher matcher = new SteamIgnoreMatcher(["*.dll", "!keep.dll"]);

        Assert.True(matcher.IsIgnored("Foo.dll"));
        Assert.False(matcher.IsIgnored("keep.dll"));
    }

    [Fact]
    public void Last_matching_rule_wins() {
        SteamIgnoreMatcher matcher = new SteamIgnoreMatcher(["!keep.dll", "*.dll"]);

        Assert.True(matcher.IsIgnored("keep.dll"));
    }

    [Fact]
    public void Per_mod_rules_override_global_rules_when_appended_last() {
        // ForModRoot concatenates global .steamignore lines first, then the per-mod
        // .steamignore lines, so the per-mod file wins on conflict via last-match-wins.
        string[] global = ["*.dll"];
        string[] perMod = ["!keep.dll"];
        SteamIgnoreMatcher matcher = new SteamIgnoreMatcher([..global, ..perMod]);

        Assert.True(matcher.IsIgnored("Foo.dll"));
        Assert.False(matcher.IsIgnored("keep.dll"));
    }

    [Fact]
    public void IsDirectoryIgnored_matches_a_bare_name_directory_at_any_depth() {
        SteamIgnoreMatcher matcher = new SteamIgnoreMatcher(["obj"]);

        Assert.True(matcher.IsDirectoryIgnored("obj"));
        Assert.True(matcher.IsDirectoryIgnored("Source/obj"));
        Assert.False(matcher.IsDirectoryIgnored("Source/object"));
    }

    [Fact]
    public void IsDirectoryIgnored_matches_a_trailing_slash_rule_on_the_bare_directory() {
        // A file path under node_modules is ignored, but IsIgnored on the bare directory is
        // false (the trailing-slash rule only matches when there is a child). Directory
        // pruning must still treat the directory itself as ignored so the walk skips it.
        SteamIgnoreMatcher matcher = new SteamIgnoreMatcher(["node_modules/"]);

        Assert.False(matcher.IsIgnored("node_modules"));
        Assert.True(matcher.IsDirectoryIgnored("node_modules"));
        Assert.True(matcher.IsDirectoryIgnored("packages/node_modules"));
    }

    [Fact]
    public void IsDirectoryIgnored_respects_anchored_rules() {
        SteamIgnoreMatcher matcher = new SteamIgnoreMatcher(["/.git"]);

        Assert.True(matcher.IsDirectoryIgnored(".git"));
        Assert.False(matcher.IsDirectoryIgnored("Sub/.git"));
    }

    [Fact]
    public void IsDirectoryIgnored_returns_false_for_an_unmatched_directory() {
        SteamIgnoreMatcher matcher = new SteamIgnoreMatcher([".git", "bin", "obj"]);

        Assert.False(matcher.IsDirectoryIgnored("Textures"));
        Assert.False(matcher.IsDirectoryIgnored("Source/Publish"));
    }

    [Fact]
    public void Backslashes_are_normalized_to_forward_slashes() {
        SteamIgnoreMatcher matcher = new SteamIgnoreMatcher(["Source/"]);

        Assert.True(matcher.IsIgnored("Source\\Foo.cs"));
    }
}
