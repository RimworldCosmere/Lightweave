using System.Collections.Generic;
using System.IO;
using Cosmere.Lightweave.Redesign.Publish;
using Xunit;

namespace Cosmere.Lightweave.Tests;

public class PublishFileScannerTests : System.IDisposable {
    private readonly string root;

    public PublishFileScannerTests() {
        this.root = Path.Combine(Path.GetTempPath(), "lw-scan-" + Path.GetRandomFileName());
        Directory.CreateDirectory(this.root);
    }

    public void Dispose() {
        if (Directory.Exists(this.root)) {
            Directory.Delete(this.root, true);
        }
    }

    private void Touch(string relPath) {
        string full = Path.Combine(this.root, relPath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, "x");
    }

    [Fact]
    public void Scan_excludes_files_inside_ignored_directories() {
        Touch("About/About.xml");
        Touch("Source/Foo.cs");
        Touch(".git/objects/pack/deadbeef");
        Touch(".git/HEAD");
        Touch("Source/obj/Debug/Foo.dll");
        Touch("node_modules/pkg/index.js");

        SteamIgnoreMatcher matcher = new SteamIgnoreMatcher([".git", "obj", "node_modules/"]);
        List<string> scanned = PublishFileScanner.Scan(this.root, matcher);

        Assert.Contains("About/About.xml", scanned);
        Assert.Contains("Source/Foo.cs", scanned);
        Assert.DoesNotContain(".git/HEAD", scanned);
        Assert.DoesNotContain(".git/objects/pack/deadbeef", scanned);
        Assert.DoesNotContain("Source/obj/Debug/Foo.dll", scanned);
        Assert.DoesNotContain("node_modules/pkg/index.js", scanned);
    }

    [Fact]
    public void Scan_still_lists_individually_ignored_files_outside_pruned_dirs() {
        // Pruning is directory-only: a root-level ignored file stays visible so the user can
        // re-include it. Only excluded directories are skipped wholesale (gitignore parity).
        Touch("Mod.sln");
        Touch("About/About.xml");

        SteamIgnoreMatcher matcher = new SteamIgnoreMatcher(["*.sln"]);
        List<string> scanned = PublishFileScanner.Scan(this.root, matcher);

        Assert.Contains("Mod.sln", scanned);
        Assert.Contains("About/About.xml", scanned);
    }

    [Fact]
    public void Scan_returns_empty_for_a_missing_root() {
        List<string> scanned = PublishFileScanner.Scan(Path.Combine(this.root, "does-not-exist"), SteamIgnoreMatcher.Empty);

        Assert.Empty(scanned);
    }

    [Fact]
    public void Scan_with_empty_matcher_returns_every_file() {
        Touch("About/About.xml");
        Touch("Source/Foo.cs");

        List<string> scanned = PublishFileScanner.Scan(this.root, SteamIgnoreMatcher.Empty);

        Assert.Equal(2, scanned.Count);
    }
}
