using Verse;

namespace Cosmere.Lightweave.Settings;

public class LightweaveRedesignSettings : ModSettings {
    public bool RedesignMainMenu = true;
    public bool ParseSaveMetadata = true;
    public bool TranslationToastDismissed;
    public bool DevBuildToastDismissed;

    public override void ExposeData() {
        Scribe_Values.Look(ref RedesignMainMenu, "redesignMainMenu", true);
        Scribe_Values.Look(ref ParseSaveMetadata, "parseSaveMetadata", true);
        Scribe_Values.Look(ref TranslationToastDismissed, "translationToastDismissed");
        Scribe_Values.Look(ref DevBuildToastDismissed, "devBuildToastDismissed");
        base.ExposeData();
    }
}
