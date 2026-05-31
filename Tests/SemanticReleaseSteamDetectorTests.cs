using System.IO;
using Cosmere.Lightweave.Redesign.Publish;
using Xunit;

namespace Cosmere.Lightweave.Tests;

public class SemanticReleaseSteamDetectorTests : System.IDisposable {
    private readonly string root;

    public SemanticReleaseSteamDetectorTests() {
        this.root = Path.Combine(Path.GetTempPath(), "lw-srs-" + Path.GetRandomFileName());
        Directory.CreateDirectory(this.root);
    }

    public void Dispose() {
        if (Directory.Exists(this.root)) {
            Directory.Delete(this.root, true);
        }
    }

    private void Write(string fileName, string content) {
        File.WriteAllText(Path.Combine(this.root, fileName), content);
    }

    [Fact]
    public void Detect_is_true_when_release_config_mjs_declares_the_plugin() {
        Write("release.config.mjs", "const plugins = ['semantic-release-steam'];\nexport default { plugins };");

        Assert.True(SemanticReleaseSteamDetector.Detect(this.root));
    }

    [Fact]
    public void Detect_is_true_for_a_dotreleaserc_json_variant() {
        Write(".releaserc.json", "{ \"plugins\": [\"semantic-release-steam\"] }");

        Assert.True(SemanticReleaseSteamDetector.Detect(this.root));
    }

    [Fact]
    public void Detect_is_false_when_no_release_config_exists() {
        Write("About.xml", "<ModMetaData />");

        Assert.False(SemanticReleaseSteamDetector.Detect(this.root));
    }

    [Fact]
    public void Detect_is_false_when_release_config_omits_the_steam_plugin() {
        Write("release.config.mjs", "const plugins = ['@semantic-release/github'];\nexport default { plugins };");

        Assert.False(SemanticReleaseSteamDetector.Detect(this.root));
    }

    [Fact]
    public void Detect_is_false_for_a_missing_root() {
        Assert.False(SemanticReleaseSteamDetector.Detect(Path.Combine(this.root, "does-not-exist")));
    }

    [Fact]
    public void Detect_walks_up_to_a_release_config_at_the_solution_root() {
        File.WriteAllText(Path.Combine(this.root, "Lightweave.sln"), "");
        Write("release.config.mjs", "const plugins = ['semantic-release-steam'];\nexport default { plugins };");
        string modRoot = Path.Combine(this.root, "Framework");
        Directory.CreateDirectory(modRoot);

        Assert.True(SemanticReleaseSteamDetector.Detect(modRoot));
    }

    [Fact]
    public void Detect_stops_at_the_solution_root_and_ignores_configs_above_it() {
        Write("release.config.mjs", "const plugins = ['semantic-release-steam'];\nexport default { plugins };");
        string repoRoot = Path.Combine(this.root, "repo");
        string modRoot = Path.Combine(repoRoot, "Framework");
        Directory.CreateDirectory(modRoot);
        File.WriteAllText(Path.Combine(repoRoot, "Lightweave.sln"), "");

        Assert.False(SemanticReleaseSteamDetector.Detect(modRoot));
    }

    [Fact]
    public void Detect_resolves_a_symlinked_mod_root_to_the_real_repo() {
        System.PlatformID platform = System.Environment.OSVersion.Platform;
        if (platform != System.PlatformID.Unix && platform != System.PlatformID.MacOSX) {
            return; // mods are deployed as symlinks on Unix dev setups; realpath resolution is Unix-only
        }

        string repoRoot = Path.Combine(this.root, "repo");
        string modDir = Path.Combine(repoRoot, "Framework");
        Directory.CreateDirectory(modDir);
        File.WriteAllText(Path.Combine(repoRoot, "Lightweave.sln"), "");
        File.WriteAllText(Path.Combine(repoRoot, "release.config.mjs"), "const plugins = ['semantic-release-steam'];");

        string modsDir = Path.Combine(this.root, "Mods");
        Directory.CreateDirectory(modsDir);
        string link = Path.Combine(modsDir, "Lightweave");
        Directory.CreateSymbolicLink(link, modDir);

        Assert.True(SemanticReleaseSteamDetector.Detect(link));
    }
}
