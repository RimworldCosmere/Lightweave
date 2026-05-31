using System;

namespace Cosmere.Lightweave.Redesign.NewColony;

public enum DifficultyTier {
    Peaceful,
    Low,
    Intended,
    Danger,
}

public static class NewColonyThresholds {
    public static string AnomalyIntensityKey(float fraction) {
        if (fraction < 0.12f) {
            return "Rare";
        }
        if (fraction < 0.25f) {
            return "Light";
        }
        if (fraction < 0.45f) {
            return "Balanced";
        }
        if (fraction < 0.70f) {
            return "Frequent";
        }
        if (fraction < 0.90f) {
            return "Severe";
        }
        return "Overwhelming";
    }

    public static int DifficultyPct(float threatScale) {
        return (int)Math.Round(threatScale * 100.0, MidpointRounding.AwayFromZero);
    }

    public static DifficultyTier DifficultyTierOf(int pct) {
        if (pct >= 130) {
            return DifficultyTier.Danger;
        }
        if (pct >= 100) {
            return DifficultyTier.Intended;
        }
        if (pct == 0) {
            return DifficultyTier.Peaceful;
        }
        return DifficultyTier.Low;
    }

    public static string LeadParagraph(string? description) {
        if (description is not { Length: > 0 }) {
            return string.Empty;
        }

        int breakIndex = description.IndexOf("\n\n", StringComparison.Ordinal);
        string lead = breakIndex >= 0 ? description.Substring(0, breakIndex) : description;
        return lead.Trim();
    }
}
