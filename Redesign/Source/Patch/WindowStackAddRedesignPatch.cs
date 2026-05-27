using Cosmere.Lightweave.Redesign.Settings;
using System;
using Cosmere.Lightweave.Redesign.LoadColony;
using Cosmere.Lightweave.Redesign.ModOptions;
using Cosmere.Lightweave.Redesign.ModsConfig;
using Cosmere.Lightweave.Redesign.Options;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Settings;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Cosmere.Lightweave.Redesign.Patch;

[HarmonyPatch(typeof(WindowStack), nameof(WindowStack.Add), [typeof(Window)])]
public static class WindowStackAddRedesignPatch {
    public static bool Prefix(WindowStack __instance, Window window) {
        LightweaveRedesignSettings? settings = LightweaveRedesignMod.Settings;
        if (settings == null || !settings.RedesignMainMenu) {
            return true;
        }

        if (window is LightweaveWindow) {
            return true;
        }

        switch (window) {
            case Dialog_SaveFileList_Load loadDialog:
                __instance.Add(new LoadColonyWindow(loadDialog));
                return false;

            case Dialog_Options options:
                __instance.Add(new OptionsWindow(options));
                return false;

            case Page_ModsConfig page when page.next == null && page.prev == null:
                __instance.Add(new ModsConfigWindow(page));
                return false;

            case Dialog_ModSettings modSettings:
                Mod? mod = ResolveDialogMod(modSettings);
                if (mod is ILightweaveSettings) {
                    __instance.Add(new LightweaveModSettingsWindow(mod));
                    return false;
                }
                return true;
        }

        return true;
    }

    private static System.Reflection.FieldInfo? ModField = HarmonyLib.AccessTools.Field(typeof(Dialog_ModSettings), "mod");

    private static Mod? ResolveDialogMod(Dialog_ModSettings dialog) {
        if (ModField == null) {
            return null;
        }
        return ModField.GetValue(dialog) as Mod;
    }
}
