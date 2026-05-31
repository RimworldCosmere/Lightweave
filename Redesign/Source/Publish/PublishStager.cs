using System.Collections.Generic;
using System.IO;
using Verse;

namespace Cosmere.Lightweave.Redesign.Publish;

/// <summary>
/// Copies the included subset of a mod's files into a temp staging directory that the
/// uploader points Steam at, so excluded files never reach the Workshop. Tracks active
/// package ids because <c>Workshop.OnItemSubmitted</c> fires without arguments.
/// </summary>
public static class PublishStager {
    private static readonly HashSet<string> ActiveIds = new HashSet<string>();

    public static IReadOnlyCollection<string> ActivePackageIds => ActiveIds;

    public static string StagingRoot =>
        Path.Combine(GenFilePaths.ConfigFolderPath, "LightweavePublish", "Temp");

    public static string StagingDirFor(string packageId) =>
        Path.Combine(StagingRoot, SanitizePackageId(packageId));

    /// <summary>
    /// Enumerates every file under <paramref name="modRoot"/> as forward-slash,
    /// mod-relative paths. The selection layer decides which are uploaded.
    /// </summary>
    /// <summary>
    /// Enumerates the publishable files under <paramref name="modRoot"/> as forward-slash,
    /// mod-relative paths, pruning any directory excluded by <paramref name="baseline"/> so
    /// the walk never descends into <c>.git</c>/<c>bin</c>/<c>obj</c>/<c>node_modules</c> of
    /// a dev mod symlinked to a source repo. The selection layer decides which of the
    /// remaining files are uploaded.
    /// </summary>
    public static List<string> EnumerateRelativeFiles(string modRoot, SteamIgnoreMatcher baseline) {
        return PublishFileScanner.Scan(modRoot, baseline);
    }

    /// <summary>
    /// Clears any prior staging dir for the mod, copies the included files into a fresh
    /// one preserving structure, registers the package as active, and returns the dir.
    /// </summary>
    public static DirectoryInfo Stage(ModMetaData mod, IReadOnlyList<string> includedRelPaths) {
        string packageId = mod.PackageId;
        string stagingDir = StagingDirFor(packageId);
        Cleanup(packageId);
        Directory.CreateDirectory(stagingDir);

        string sourceRoot = mod.RootDir.FullName;
        for (int i = 0; i < includedRelPaths.Count; i++) {
            string relPath = includedRelPaths[i].Replace('/', Path.DirectorySeparatorChar);
            string source = Path.Combine(sourceRoot, relPath);
            if (!File.Exists(source)) {
                continue;
            }

            string destination = Path.Combine(stagingDir, relPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(source, destination, true);
        }

        ActiveIds.Add(packageId);
        return new DirectoryInfo(stagingDir);
    }

    public static void Cleanup(string packageId) {
        ActiveIds.Remove(packageId);
        string stagingDir = StagingDirFor(packageId);
        if (Directory.Exists(stagingDir)) {
            Directory.Delete(stagingDir, true);
        }
    }

    public static void CleanupAll() {
        string[] active = new string[ActiveIds.Count];
        ActiveIds.CopyTo(active);
        for (int i = 0; i < active.Length; i++) {
            Cleanup(active[i]);
        }
    }

    private static string SanitizePackageId(string packageId) {
        char[] invalid = Path.GetInvalidFileNameChars();
        char[] chars = packageId.ToCharArray();
        for (int i = 0; i < chars.Length; i++) {
            for (int j = 0; j < invalid.Length; j++) {
                if (chars[i] == invalid[j]) {
                    chars[i] = '_';
                    break;
                }
            }
        }

        return new string(chars);
    }
}
