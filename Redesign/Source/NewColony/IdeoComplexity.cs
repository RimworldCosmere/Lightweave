using System.Collections.Generic;

namespace Cosmere.Lightweave.Redesign.NewColony;

public enum IdeoImpact {
    Low,
    Medium,
    High,
}

// Pure scoring for the editor's "Overall impact N · Label" footer and the meme-section pill.
// The mock (nc-ideology.jsx) sums a per-meme impact cost (low/med/high); RimWorld's MemeDef has
// no impact field, so we approximate with tunable per-meme / per-precept weights clamped to 0..10.
// Label thresholds mirror the mock: <=3 Low, <=6 Medium, else High. No Verse/Unity dependency.
public static class IdeoComplexity {
    public const int MemeCost = 2;
    public const int PreceptCost = 1;
    public const int MaxScore = 10;

    public static int Score(int memeCount, int preceptCount) {
        if (memeCount < 0) {
            memeCount = 0;
        }

        if (preceptCount < 0) {
            preceptCount = 0;
        }

        int raw = memeCount * MemeCost + preceptCount * PreceptCost;
        if (raw < 0) {
            return 0;
        }

        return raw > MaxScore ? MaxScore : raw;
    }

    public static int Score(IReadOnlyList<string>? memeDefNames) {
        return Score(memeDefNames?.Count ?? 0, 0);
    }

    public static IdeoImpact Label(int score) {
        if (score <= 3) {
            return IdeoImpact.Low;
        }

        return score <= 6 ? IdeoImpact.Medium : IdeoImpact.High;
    }
}
