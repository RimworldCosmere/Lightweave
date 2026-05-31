using Cosmere.Lightweave.Tokens;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Cosmere.Lightweave.Redesign.NewColony;

public static class NewColonyFormat {
    public static readonly int[] MapSizes = [200, 250, 275, 300, 350];

    public static readonly float[] Coverages = [0.3f, 0.5f, 1f];

    public static readonly int[] PopulationLevels = [1, 3, 5];

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
        return ("CL_NewColony_MapSize_" + size).Translate();
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
