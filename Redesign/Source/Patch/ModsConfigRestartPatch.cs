using Cosmere.Lightweave.Redesign.Settings;
using Cosmere.Lightweave.Redesign.ModsConfig;
using Cosmere.Lightweave.Settings;
using HarmonyLib;
using Verse;

namespace Cosmere.Lightweave.Redesign.Patch;

[HarmonyPatch(typeof(Verse.ModsConfig), nameof(Verse.ModsConfig.RestartFromChangedMods))]
public static class ModsConfigRestartPatch {
    public static bool Prefix() {
        LightweaveRedesignSettings? settings = LightweaveRedesignMod.Settings;
        if (settings == null || !settings.RedesignMainMenu) {
            return true;
        }
        Find.WindowStack.Add(new Dialog_ModsConfigRestart());
        return false;
    }
}
