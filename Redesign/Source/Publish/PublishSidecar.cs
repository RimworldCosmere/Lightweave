using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace Cosmere.Lightweave.Redesign.Publish;

/// <summary>
/// Per-mod publish overrides persisted as <c>_LightweavePublish.xml</c> in the mod root.
/// Holds Workshop metadata overrides plus per-path include/exclude decisions layered on
/// top of the <c>.steamignore</c> baseline. Parsing (<see cref="FromXml"/>/<see cref="ToXml"/>)
/// and merge (<see cref="ResolveInclusion"/>) are pure; <see cref="Load"/>/<see cref="Save"/>
/// are the only members that touch disk.
/// </summary>
public sealed class PublishSidecar {
    public const string SidecarFileName = "_LightweavePublish.xml";
    public const string PublishedFileIdRelPath = "About/PublishedFileId.txt";

    public string? TitleOverride { get; set; }

    public string? PreviewOverride { get; set; }

    public List<string> Tags { get; } = new List<string>();

    /// <summary>Per-path include decisions. Key = forward-slash mod-relative path, value = included.</summary>
    public Dictionary<string, bool> Overrides { get; } = new Dictionary<string, bool>();

    public string? LastChangelog { get; set; }

    /// <summary>
    /// Resolves whether a mod-relative file path is included in the upload.
    /// Special cases first, then the most-specific sidecar override (a folder override
    /// propagates to its descendants), then the <c>.steamignore</c> baseline.
    /// </summary>
    public bool ResolveInclusion(string relPath, SteamIgnoreMatcher baseline) {
        string normalized = relPath.Replace('\\', '/').TrimStart('/');

        if (normalized == SidecarFileName) {
            return false;
        }

        if (normalized == PublishedFileIdRelPath) {
            return true;
        }

        if (TryResolveOverride(normalized, out bool overridden)) {
            return overridden;
        }

        return !baseline.IsIgnored(normalized);
    }

    private bool TryResolveOverride(string normalized, out bool included) {
        included = false;
        string? bestKey = null;
        foreach (KeyValuePair<string, bool> entry in this.Overrides) {
            string key = entry.Key.Replace('\\', '/').TrimStart('/');
            bool isAncestorOrSelf = normalized == key || normalized.StartsWith(key + "/");
            if (!isAncestorOrSelf) {
                continue;
            }

            if (bestKey == null || key.Length > bestKey.Length) {
                bestKey = key;
                included = entry.Value;
            }
        }

        return bestKey != null;
    }

    public static PublishSidecar FromXml(string xml) {
        PublishSidecar sidecar = new PublishSidecar();
        if (string.IsNullOrWhiteSpace(xml)) {
            return sidecar;
        }

        XDocument doc = XDocument.Parse(xml);
        XElement? root = doc.Root;
        if (root == null) {
            return sidecar;
        }

        sidecar.TitleOverride = NullIfEmpty(root.Element("titleOverride")?.Value);
        sidecar.PreviewOverride = NullIfEmpty(root.Element("previewOverride")?.Value);
        sidecar.LastChangelog = NullIfEmpty(root.Element("lastChangelog")?.Value);

        XElement? tags = root.Element("tags");
        if (tags != null) {
            foreach (XElement li in tags.Elements("li")) {
                string value = li.Value.Trim();
                if (value.Length > 0) {
                    sidecar.Tags.Add(value);
                }
            }
        }

        XElement? overrides = root.Element("overrides");
        if (overrides != null) {
            foreach (XElement li in overrides.Elements("li")) {
                string? path = li.Element("path")?.Value.Trim();
                if (string.IsNullOrEmpty(path)) {
                    continue;
                }

                bool include = li.Element("include")?.Value.Trim().ToLowerInvariant() == "true";
                sidecar.Overrides[path!] = include;
            }
        }

        return sidecar;
    }

    public string ToXml() {
        XElement root = new XElement("LightweavePublish");
        if (!string.IsNullOrEmpty(TitleOverride)) {
            root.Add(new XElement("titleOverride", TitleOverride));
        }

        if (!string.IsNullOrEmpty(PreviewOverride)) {
            root.Add(new XElement("previewOverride", PreviewOverride));
        }

        if (Tags.Count > 0) {
            XElement tags = new XElement("tags");
            for (int i = 0; i < Tags.Count; i++) {
                tags.Add(new XElement("li", Tags[i]));
            }

            root.Add(tags);
        }

        if (Overrides.Count > 0) {
            XElement overrides = new XElement("overrides");
            foreach (KeyValuePair<string, bool> entry in Overrides) {
                overrides.Add(new XElement(
                    "li",
                    new XElement("path", entry.Key),
                    new XElement("include", entry.Value ? "true" : "false")
                ));
            }

            root.Add(overrides);
        }

        if (!string.IsNullOrEmpty(LastChangelog)) {
            root.Add(new XElement("lastChangelog", LastChangelog));
        }

        return new XDocument(new XDeclaration("1.0", "utf-8", null), root).ToString();
    }

    public static PublishSidecar Load(string modRoot) {
        string path = Path.Combine(modRoot, SidecarFileName);
        if (!File.Exists(path)) {
            return new PublishSidecar();
        }

        return FromXml(File.ReadAllText(path));
    }

    public void Save(string modRoot) {
        string path = Path.Combine(modRoot, SidecarFileName);
        File.WriteAllText(path, ToXml());
    }

    private static string? NullIfEmpty(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        return value!.Trim();
    }
}
