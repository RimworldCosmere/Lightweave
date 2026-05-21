using System;
using Cosmere.Lightweave.LoadColony;
using Cosmere.Lightweave.ModsConfig;
using Cosmere.Lightweave.Options;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Settings;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Cosmere.Lightweave.Patch;

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
        }

        return true;
    }
}
