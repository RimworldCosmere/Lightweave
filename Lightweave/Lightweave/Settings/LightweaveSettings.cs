using System.Collections.Generic;
using Verse;

namespace Cosmere.Lightweave.Settings;

public class LightweaveSettings : ModSettings {
    public int FontScalePercent = 100;
    public bool ReduceMotion;
    public string SelectedThemeId = "default";
    public bool ShowPerformanceMetrics;
    public List<string> LogPresetNames = new List<string>();
    public List<string> LogPresetExpressions = new List<string>();

    public float FontScale => FontScalePercent / 100f;

    public override void ExposeData() {
        Scribe_Values.Look(ref FontScalePercent, "fontScalePercent", 100);
        Scribe_Values.Look(ref ReduceMotion, "reduceMotion");
        Scribe_Values.Look(ref SelectedThemeId, "selectedThemeId", "default");
        Scribe_Values.Look(ref ShowPerformanceMetrics, "showPerformanceMetrics");
        Scribe_Collections.Look(ref LogPresetNames, "logPresetNames", LookMode.Value);
        Scribe_Collections.Look(ref LogPresetExpressions, "logPresetExpressions", LookMode.Value);
        base.ExposeData();
        if (LogPresetNames == null) {
            LogPresetNames = new List<string>();
        }
        if (LogPresetExpressions == null) {
            LogPresetExpressions = new List<string>();
        }
    }
}
