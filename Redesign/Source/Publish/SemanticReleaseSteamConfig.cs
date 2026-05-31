using System;

namespace Cosmere.Lightweave.Redesign.Publish;

/// <summary>
/// Pure detection of whether a semantic-release config declares the <c>semantic-release-steam</c>
/// plugin. Content-only so it is unit-testable without filesystem access;
/// <see cref="SemanticReleaseSteamDetector"/> wraps it with the file lookup. When a mod is wired
/// for semantic-release-steam, CI owns Workshop publishing, so the in-game Publish button is
/// disabled to avoid uploading out-of-band with the release pipeline.
/// </summary>
public static class SemanticReleaseSteamConfig {
    private const string PluginName = "semantic-release-steam";

    public static bool DeclaresSteamPlugin(string configContent) {
        if (string.IsNullOrEmpty(configContent)) {
            return false;
        }

        return configContent.IndexOf(PluginName, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
