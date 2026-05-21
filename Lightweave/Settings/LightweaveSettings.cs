using Verse;

namespace Cosmere.Lightweave.Settings;

public class LightweaveSettings : ModSettings {
    public int FontScalePercent = 100;
    public bool ReduceMotion;
    public string SelectedThemeId = "default";
    public bool ShowPerformanceMetrics;

    public float FontScale => FontScalePercent / 100f;

    public override void ExposeData() {
        Scribe_Values.Look(ref FontScalePercent, "fontScalePercent", 100);
        Scribe_Values.Look(ref ReduceMotion, "reduceMotion");
        Scribe_Values.Look(ref SelectedThemeId, "selectedThemeId", "default");
        Scribe_Values.Look(ref ShowPerformanceMetrics, "showPerformanceMetrics");
        base.ExposeData();
    }
}
