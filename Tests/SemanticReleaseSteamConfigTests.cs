using Cosmere.Lightweave.Redesign.Publish;
using Xunit;

namespace Cosmere.Lightweave.Tests;

public class SemanticReleaseSteamConfigTests {
    [Fact]
    public void DeclaresSteamPlugin_is_true_when_the_plugin_is_listed() {
        string config = "const plugins = [\n  '@semantic-release/commit-analyzer',\n  'semantic-release-steam',\n];";

        Assert.True(SemanticReleaseSteamConfig.DeclaresSteamPlugin(config));
    }

    [Fact]
    public void DeclaresSteamPlugin_is_case_insensitive() {
        Assert.True(SemanticReleaseSteamConfig.DeclaresSteamPlugin("SEMANTIC-RELEASE-STEAM"));
    }

    [Fact]
    public void DeclaresSteamPlugin_is_false_for_unrelated_config() {
        string config = "const plugins = [\n  '@semantic-release/github',\n  '@semantic-release/npm',\n];";

        Assert.False(SemanticReleaseSteamConfig.DeclaresSteamPlugin(config));
    }

    [Fact]
    public void DeclaresSteamPlugin_is_false_for_empty_or_null() {
        Assert.False(SemanticReleaseSteamConfig.DeclaresSteamPlugin(""));
        Assert.False(SemanticReleaseSteamConfig.DeclaresSteamPlugin(null!));
    }
}
