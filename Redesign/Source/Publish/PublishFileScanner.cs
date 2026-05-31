using System.Collections.Generic;
using System.IO;

namespace Cosmere.Lightweave.Redesign.Publish;

/// <summary>
/// Walks a mod root into forward-slash, mod-relative file paths, pruning any directory the
/// baseline <see cref="SteamIgnoreMatcher"/> excludes. Pruning follows gitignore semantics:
/// an ignored directory is never descended into, so its contents are neither listed nor
/// re-includable. This keeps the file list to the publishable set instead of walking the
/// entire <c>.git</c>/<c>bin</c>/<c>obj</c>/<c>node_modules</c> tree of a dev mod symlinked
/// to a source repo (which is tens of thousands of files and stalls the UI thread when the
/// publish dialog builds its file tree).
/// </summary>
public static class PublishFileScanner {
    public static List<string> Scan(string modRoot, SteamIgnoreMatcher baseline) {
        List<string> result = [];
        if (!Directory.Exists(modRoot)) {
            return result;
        }

        string root = Path.GetFullPath(modRoot);
        Walk(root, root, baseline, result);
        result.Sort(System.StringComparer.OrdinalIgnoreCase);
        return result;
    }

    private static void Walk(string root, string dir, SteamIgnoreMatcher baseline, List<string> result) {
        string[] files = Directory.GetFiles(dir);
        for (int i = 0; i < files.Length; i++) {
            result.Add(Relative(root, files[i]));
        }

        string[] subdirs = Directory.GetDirectories(dir);
        for (int i = 0; i < subdirs.Length; i++) {
            string relDir = Relative(root, subdirs[i]);
            if (baseline.IsDirectoryIgnored(relDir)) {
                continue;
            }

            Walk(root, subdirs[i], baseline, result);
        }
    }

    private static string Relative(string root, string fullPath) {
        string relative = fullPath.Substring(root.Length).TrimStart('/', '\\');
        return relative.Replace('\\', '/');
    }
}
