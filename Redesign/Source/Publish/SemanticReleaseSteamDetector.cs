using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Cosmere.Lightweave.Redesign.Publish;

/// <summary>
/// Decides whether a mod root is configured for semantic-release-steam by scanning the standard
/// semantic-release config filenames for a <c>semantic-release-steam</c> plugin declaration.
/// Results are cached per mod root — a mod's release config does not change during a session, and
/// this runs inside the (cached) UI tree build, so the file IO must not repeat on every rebuild.
/// </summary>
public static class SemanticReleaseSteamDetector {
    private static readonly string[] ConfigFileNames = [
        "release.config.mjs",
        "release.config.js",
        "release.config.cjs",
        ".releaserc.mjs",
        ".releaserc.js",
        ".releaserc.cjs",
        ".releaserc.json",
        ".releaserc.yaml",
        ".releaserc.yml",
        ".releaserc",
    ];

    private static readonly Dictionary<string, bool> Cache =
        new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

    public static bool IsConfigured(string modRoot) {
        if (string.IsNullOrEmpty(modRoot)) {
            return false;
        }

        if (Cache.TryGetValue(modRoot, out bool cached)) {
            return cached;
        }

        bool result = Detect(modRoot);
        Cache[modRoot] = result;
        return result;
    }

    internal static bool Detect(string modRoot) {
        if (string.IsNullOrEmpty(modRoot)) {
            return false;
        }

        DirectoryInfo? dir = new DirectoryInfo(ResolveRealPath(modRoot));
        if (!dir.Exists) {
            return false;
        }

        while (dir != null) {
            if (DirectoryDeclaresSteamPlugin(dir.FullName)) {
                return true;
            }

            if (ContainsSolution(dir.FullName)) {
                return false;
            }

            dir = dir.Parent;
        }

        return false;
    }

    private static bool DirectoryDeclaresSteamPlugin(string dir) {
        for (int i = 0; i < ConfigFileNames.Length; i++) {
            string path = Path.Combine(dir, ConfigFileNames[i]);
            if (!File.Exists(path)) {
                continue;
            }

            if (SemanticReleaseSteamConfig.DeclaresSteamPlugin(File.ReadAllText(path))) {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsSolution(string dir) {
        return Directory.GetFiles(dir, "*.sln").Length > 0;
    }

    /// <summary>
    /// Resolves <paramref name="path"/> through any symlinks to its real location. Mods are
    /// commonly deployed as a symlink from the game's Mods folder into the source repo; in that
    /// case <see cref="DirectoryInfo.Parent"/> walks the link's lexical parent (the Mods folder),
    /// not the target's, so the upward scan never reaches the repo root. On Unix we resolve via
    /// libc <c>realpath</c>; everywhere else the lexical path is used unchanged.
    /// </summary>
    private static string ResolveRealPath(string path) {
        try {
            PlatformID platform = Environment.OSVersion.Platform;
            if (platform != PlatformID.Unix && platform != PlatformID.MacOSX) {
                return path;
            }

            IntPtr resolved = realpath(path, IntPtr.Zero);
            if (resolved == IntPtr.Zero) {
                return path;
            }

            try {
                return Marshal.PtrToStringAnsi(resolved) ?? path;
            }
            finally {
                free(resolved);
            }
        }
        catch (DllNotFoundException) {
            return path;
        }
        catch (EntryPointNotFoundException) {
            return path;
        }
    }

    [DllImport("libc", SetLastError = true)]
    private static extern IntPtr realpath(string path, IntPtr resolvedName);

    [DllImport("libc")]
    private static extern void free(IntPtr ptr);
}
