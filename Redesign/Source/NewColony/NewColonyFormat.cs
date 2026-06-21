using Cosmere.Lightweave.Tokens;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Cosmere.Lightweave.Redesign.NewColony;

public static class NewColonyFormat {
    public static readonly int[] PopulationLevels = [1, 3, 5];

    // The five seasons the vanilla Dialog_AdvancedGameConfig offers; the enum also has
    // PermanentSummer/PermanentWinter, which that dialog deliberately omits.
    public static readonly Season[] Seasons = [
        Season.Undefined, Season.Spring, Season.Summer, Season.Fall, Season.Winter,
    ];

    public static OverallRainfall Rainfall(int ordinal) {
        return (OverallRainfall)Mathf.Clamp(ordinal, 0, 6);
    }

    public static OverallTemperature Temperature(int ordinal) {
        return (OverallTemperature)Mathf.Clamp(ordinal, 0, 6);
    }

    public static OverallPopulation Population(int ordinal) {
        return (OverallPopulation)Mathf.Clamp(ordinal, 0, 6);
    }

    public static LandmarkDensity Landmark(int ordinal) {
        return (LandmarkDensity)Mathf.Clamp(ordinal, 0, 6);
    }

    public static string RainfallLabel(int ordinal) {
        return ("CL_NewColony_Rainfall_" + Mathf.Clamp(ordinal, 0, 6)).Translate();
    }

    public static string TemperatureLabel(int ordinal) {
        return ("CL_NewColony_Temperature_" + Mathf.Clamp(ordinal, 0, 6)).Translate();
    }

    public static string PopulationLabel(int ordinal) {
        return ("CL_NewColony_Population_" + Mathf.Clamp(ordinal, 0, 6)).Translate();
    }

    public static string CoverageLabel(float coverage) {
        return Mathf.RoundToInt(coverage * 100f) + "%";
    }

    public static string MapSizeLabel(int size) {
        // Maps are square (the new-game flow feeds a single mapSize into both axes), so the readout
        // shows the literal NxN dimension rather than a Small/Medium/Large tier name.
        return size + " × " + size;
    }

    public static string LandmarkLabel(int ordinal) {
        return ("CL_NewColony_Landmark_" + Mathf.Clamp(ordinal, 0, 6)).Translate();
    }

    public static string PollutionLabel(float pollution) {
        return Mathf.RoundToInt(pollution * 100f) + "%";
    }

    public static string SeasonLabel(Season season) {
        return season == Season.Undefined
            ? (string)"CL_NewColony_World_Season_Default".Translate()
            : ("CL_NewColony_World_Season_" + season).Translate();
    }

    public static string AnomalyIntensityLabel(float fraction) {
        return ("CL_NewColony_Anomaly_Intensity_" + NewColonyThresholds.AnomalyIntensityKey(fraction)).Translate();
    }

    public static int DifficultyPct(float threatScale) {
        return NewColonyThresholds.DifficultyPct(threatScale);
    }

    public static ThemeSlot DifficultyColor(int pct) {
        switch (NewColonyThresholds.DifficultyTierOf(pct)) {
            case DifficultyTier.Danger:
                return ThemeSlot.StatusDanger;
            case DifficultyTier.Intended:
                return ThemeSlot.StatusWarning;
            case DifficultyTier.Peaceful:
                return ThemeSlot.StatusSuccess;
            default:
                return ThemeSlot.TextMuted;
        }
    }

    public static string LeadParagraph(string? description) {
        return NewColonyThresholds.LeadParagraph(description);
    }


}
