using System.Collections.Generic;
using Cosmere.Lightweave.Redesign.MainMenu;
using HarmonyLib;
using Verse;

namespace Cosmere.Lightweave.Redesign.Patch;

[HarmonyPatch(typeof(OptionListingUtility), nameof(OptionListingUtility.DrawOptionListing))]
public static class MainMenuLinkCapturePatch {
    public static bool Prefix(List<ListableOption> optList, ref float __result) {
        if (!MainMenuLinkHarvester.Capturing) {
            return true;
        }

        if (optList != null) {
            for (int i = 0; i < optList.Count; i++) {
                if (optList[i] is ListableOption_WebLink) {
                    MainMenuLinkHarvester.Captured = optList;
                    break;
                }
            }
        }

        __result = 0f;
        return false;
    }
}
