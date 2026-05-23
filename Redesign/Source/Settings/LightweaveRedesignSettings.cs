using Verse;

namespace Cosmere.Lightweave.Redesign.Settings;

public class LightweaveRedesignSettings : ModSettings {
    public bool RedesignMainMenu = true;
    public bool ParseSaveMetadata = true;

    public override void ExposeData() {
        Scribe_Values.Look(ref RedesignMainMenu, "redesignMainMenu", true);
        Scribe_Values.Look(ref ParseSaveMetadata, "parseSaveMetadata", true);
        base.ExposeData();
    }
}
