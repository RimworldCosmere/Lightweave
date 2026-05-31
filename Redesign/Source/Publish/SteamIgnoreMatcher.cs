using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Cosmere.Lightweave.Redesign.Publish;

/// <summary>
/// gitignore-style path matcher built from the lines of a <c>.steamignore</c> file.
/// Pure once constructed: <see cref="IsIgnored"/> never touches disk.
/// Supported subset: blank/comment lines, anchored leading <c>/</c>, trailing-<c>/</c>
/// directory match, <c>*</c>/<c>?</c>/<c>**</c> wildcards, bare-name-any-depth, and
/// <c>!</c> negation with last-match-wins (default: not ignored).
/// </summary>
public sealed class SteamIgnoreMatcher {
    private readonly List<Rule> rules;

    public SteamIgnoreMatcher(IEnumerable<string> lines) {
        this.rules = new List<Rule>();
        foreach (string raw in lines) {
            Rule? rule = ParseLine(raw);
            if (rule != null) {
                this.rules.Add(rule);
            }
        }
    }

    public static SteamIgnoreMatcher Empty => new SteamIgnoreMatcher([]);

    /// <summary>
    /// Builds a matcher from <c>&lt;modRoot&gt;/.steamignore</c>, or an empty matcher if absent.
    /// </summary>
    /// <summary>
    /// Builds a matcher from the global <c>.steamignore</c> (in the user profile and/or
    /// %APPDATA%) merged with <c>&lt;modRoot&gt;/.steamignore</c>. Global rules are applied
    /// first so the per-mod file can override them (last-match-wins). Returns an empty
    /// matcher when no file exists.
    /// </summary>
    public static SteamIgnoreMatcher ForModRoot(string modRoot) {
        List<string> lines = [];
        foreach (string globalPath in GlobalIgnorePaths()) {
            if (System.IO.File.Exists(globalPath)) {
                lines.AddRange(System.IO.File.ReadAllLines(globalPath));
            }
        }

        string modPath = System.IO.Path.Combine(modRoot, ".steamignore");
        if (System.IO.File.Exists(modPath)) {
            lines.AddRange(System.IO.File.ReadAllLines(modPath));
        }

        return lines.Count == 0 ? Empty : new SteamIgnoreMatcher(lines);
    }

    private static IEnumerable<string> GlobalIgnorePaths() {
        string home = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrEmpty(home)) {
            yield return System.IO.Path.Combine(home, ".steamignore");
        }

        string appData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
        if (!string.IsNullOrEmpty(appData)) {
            yield return System.IO.Path.Combine(appData, ".steamignore");
        }
    }

    /// <summary>
    /// Returns whether the given mod-root-relative file path is excluded by the
    /// baseline ignore rules. <paramref name="relPath"/> must use forward slashes
    /// and have no leading slash (e.g. <c>About/About.xml</c>).
    /// </summary>
    /// <summary>
    /// Returns whether the given mod-root-relative file path is excluded by the
    /// baseline ignore rules. <paramref name="relPath"/> must use forward slashes
    /// and have no leading slash (e.g. <c>About/About.xml</c>).
    /// </summary>
    public bool IsIgnored(string relPath) {
        return Matches(relPath, lastSegmentIsDir: false);
    }

    /// <summary>
    /// Returns whether the given mod-root-relative directory path is excluded, treating
    /// the final segment as a directory so directory-only rules (e.g. <c>node_modules/</c>)
    /// match it. Used to prune the file walk: an ignored directory is never descended into,
    /// matching gitignore semantics (a child of an excluded directory cannot be re-included).
    /// </summary>
    public bool IsDirectoryIgnored(string relDirPath) {
        return Matches(relDirPath, lastSegmentIsDir: true);
    }

    private bool Matches(string relPath, bool lastSegmentIsDir) {
        if (string.IsNullOrEmpty(relPath)) {
            return false;
        }

        string normalized = relPath.Replace('\\', '/').TrimStart('/');
        string[] segments = normalized.Split('/');

        bool ignored = false;
        for (int r = 0; r < this.rules.Count; r++) {
            Rule rule = this.rules[r];
            if (RuleMatchesPathOrAncestor(rule, segments, lastSegmentIsDir)) {
                ignored = !rule.Negated;
            }
        }

        return ignored;
    }

    private static bool RuleMatchesPathOrAncestor(Rule rule, string[] segments, bool lastSegmentIsDir) {
        for (int depth = 1; depth <= segments.Length; depth++) {
            bool isDir = depth < segments.Length || lastSegmentIsDir;
            if (rule.DirOnly && !isDir) {
                continue;
            }

            StringBuilder candidate = new StringBuilder();
            for (int i = 0; i < depth; i++) {
                if (i > 0) {
                    candidate.Append('/');
                }

                candidate.Append(segments[i]);
            }

            if (rule.Pattern.IsMatch(candidate.ToString())) {
                return true;
            }
        }

        return false;
    }

    private static Rule? ParseLine(string raw) {
        if (raw == null) {
            return null;
        }

        string line = raw.Trim();
        if (line.Length == 0 || line[0] == '#') {
            return null;
        }

        bool negated = false;
        if (line[0] == '!') {
            negated = true;
            line = line.Substring(1);
        }

        bool dirOnly = false;
        if (line.EndsWith("/")) {
            dirOnly = true;
            line = line.Substring(0, line.Length - 1);
        }

        if (line.Length == 0) {
            return null;
        }

        bool anchored = line.StartsWith("/") || line.Contains("/");
        string body = line.StartsWith("/") ? line.Substring(1) : line;
        string regex = BuildRegex(body, anchored);
        return new Rule(negated, dirOnly, new Regex(regex, RegexOptions.CultureInvariant));
    }

    private static string BuildRegex(string pattern, bool anchored) {
        StringBuilder sb = new StringBuilder();
        sb.Append(anchored ? "^" : "(?:^|.*/)");

        for (int i = 0; i < pattern.Length; i++) {
            char c = pattern[i];
            if (c == '*') {
                bool doubleStar = i + 1 < pattern.Length && pattern[i + 1] == '*';
                if (doubleStar) {
                    i++;
                    if (i + 1 < pattern.Length && pattern[i + 1] == '/') {
                        i++;
                        sb.Append("(?:.*/)?");
                    }
                    else {
                        sb.Append(".*");
                    }
                }
                else {
                    sb.Append("[^/]*");
                }
            }
            else if (c == '?') {
                sb.Append("[^/]");
            }
            else {
                sb.Append(Regex.Escape(c.ToString()));
            }
        }

        sb.Append("$");
        return sb.ToString();
    }

    private sealed class Rule {
        public Rule(bool negated, bool dirOnly, Regex pattern) {
            this.Negated = negated;
            this.DirOnly = dirOnly;
            this.Pattern = pattern;
        }

        public bool Negated { get; }

        public bool DirOnly { get; }

        public Regex Pattern { get; }
    }
}
