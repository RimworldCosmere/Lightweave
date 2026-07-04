using Concord;
using Cosmere.Lightweave.Redesign.Settings;
using Cosmere.Lightweave.Redesign.ModsConfig;
using Verse;

namespace Cosmere.Lightweave.Redesign.Patch;

[Patch(typeof(Verse.ModsConfig))]
public static class ModsConfigRestartPatch {
    [Inject(At.Head, nameof(Verse.ModsConfig.RestartFromChangedMods))]
    public static Control Prefix() {
        LightweaveRedesignSettings? settings = LightweaveRedesignMod.Settings;
        if (settings == null || !settings.RedesignMainMenu) {
            return Control.Continue;
        }
        Find.WindowStack.Add(new Dialog_ModsConfigRestart());
        return Control.Cancel;
    }
}
