using System;

namespace Cosmere.Lightweave.Redesign.NewColony;

public enum DifficultyTier {
    Peaceful,
    Low,
    Intended,
    Danger,
}

public enum FactionAddRule {
    Allowed,
    TotalCapReached,
    TypeCapReached,
}

public enum FactionCountForm {
    None,
    Multiple,
    Ratio,
    Max,
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


    // Some mods/defs wrap a tooltip section heading in a (*SectionTitle)...(/SectionTitle)
    // pseudo-markup convention. RimWorld does not parse these tags (they render raw), so split the
    // description into ordered segments - plain body text and section titles - which the caller
    // renders with proper styling (accent heading vs muted body) instead of showing the raw tags.
    public readonly struct TipSegment {
        public readonly bool IsTitle;
        public readonly string Text;

        public TipSegment(bool isTitle, string text) {
            IsTitle = isTitle;
            Text = text;
        }
    }

    private static readonly System.Text.RegularExpressions.Regex TipOpenTagRegex =
        new System.Text.RegularExpressions.Regex(
            @"\(\*[^)]*\)",
            System.Text.RegularExpressions.RegexOptions.Compiled);

    private static readonly System.Text.RegularExpressions.Regex TipCloseTagRegex =
        new System.Text.RegularExpressions.Regex(
            @"\(/[^)]*\)",
            System.Text.RegularExpressions.RegexOptions.Compiled);

    private static readonly System.Text.RegularExpressions.Regex TipAnyTagRegex =
        new System.Text.RegularExpressions.Regex(
            @"\(\*[^)]*\)|\(/[^)]*\)",
            System.Text.RegularExpressions.RegexOptions.Compiled);

    public static System.Collections.Generic.List<TipSegment> ParseTipSegments(string? description) {
        System.Collections.Generic.List<TipSegment> segments = new System.Collections.Generic.List<TipSegment>();
        if (description is not { Length: > 0 }) {
            return segments;
        }

        int cursor = 0;
        while (cursor < description.Length) {
            System.Text.RegularExpressions.Match open = TipOpenTagRegex.Match(description, cursor);
            if (!open.Success) {
                AddBodySegment(segments, description.Substring(cursor));
                break;
            }

            if (open.Index > cursor) {
                AddBodySegment(segments, description.Substring(cursor, open.Index - cursor));
            }

            int afterOpen = open.Index + open.Length;
            System.Text.RegularExpressions.Match close = TipCloseTagRegex.Match(description, afterOpen);
            if (!close.Success) {
                // Orphan open tag: drop the tag, keep the remaining text as body.
                AddBodySegment(segments, description.Substring(afterOpen));
                break;
            }

            string title = description.Substring(afterOpen, close.Index - afterOpen).Trim();
            if (title.Length > 0) {
                segments.Add(new TipSegment(true, title));
            }

            cursor = close.Index + close.Length;
        }

        return segments;
    }

    private static void AddBodySegment(System.Collections.Generic.List<TipSegment> segments, string raw) {
        string body = TipAnyTagRegex.Replace(raw, string.Empty).Trim();
        if (body.Length > 0) {
            segments.Add(new TipSegment(false, body));
        }
    }

    public static string TimeZoneLabel(int zone) {
        return zone >= 0 ? "GMT+" + zone : "GMT" + zone;
    }

    public static string DiseaseFrequencyLabel(float mtbDays) {
        if (mtbDays <= 0f) {
            return "—";
        }
        return (60f / mtbDays).ToString("F1") + "/yr";
    }

    // Pure mirror of WorldFactionsUIUtility.CanAddFaction's gating: a hard cap of `totalCap`
    // non-hidden factions, plus a per-def cap from maxConfigurableAtWorldCreation. Hidden factions
    // (Mechanoid Hive, Insect Geneline) bypass the total cap. Returns which rule blocks the add.
    public static FactionAddRule CanAddFaction(
        bool defHidden,
        int nonHiddenCount,
        int countOfDef,
        int maxForType,
        int totalCap = 12
    ) {
        if (!defHidden && nonHiddenCount >= totalCap) {
            return FactionAddRule.TotalCapReached;
        }
        if (countOfDef >= maxForType) {
            return FactionAddRule.TypeCapReached;
        }
        return FactionAddRule.Allowed;
    }

    // Trailing count-tag form for a faction kind in the add-picker, mirroring the mock
    // (nc-factions.jsx): a capped kind at its limit reads "max {N}", a capped kind below
    // its limit reads "{count} / {max}", an uncapped kind reads "×{count}" when any are
    // present, and nothing otherwise.
    public static FactionCountForm CountForm(bool capped, int count, int max) {
        if (capped) {
            return count >= max ? FactionCountForm.Max : FactionCountForm.Ratio;
        }
        return count > 0 ? FactionCountForm.Multiple : FactionCountForm.None;
    }

    // Vanilla leaves maxConfigurableAtWorldCreation at a large sentinel (9999) for kinds with no
    // real per-type limit; only a small positive bound is a configurable cap the picker should
    // surface as a "{count} / {max}" ratio. Uncapped kinds read "×{count}" instead, so the add
    // dropdown never shows a meaningless "1 / 9999".
    public const int UncappedSentinel = 9999;

    public static bool IsConfigurableCap(int maxForType) {
        return maxForType > 0 && maxForType < UncappedSentinel;
    }

    // Seed selection for world generation: an explicitly entered seed (typed into the seed field or
    // set by Randomize) wins; otherwise the last generated seed is reused so every Regenerate
    // reproduces the same planet. Null means "no seed yet" — the caller rolls one random seed and
    // persists it, after which the reuse branch keeps it stable.
    public static string? ResolveSeed(string? explicitSeed, string? lastSeed) {
        if (!string.IsNullOrEmpty(explicitSeed)) {
            return explicitSeed;
        }
        if (!string.IsNullOrEmpty(lastSeed)) {
            return lastSeed;
        }
        return null;
    }

    public static string GoodwillText(int goodwill) {
        return goodwill > 0 ? "+" + goodwill : goodwill.ToString();
    }
}
