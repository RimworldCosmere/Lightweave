using System.Collections.Generic;
using Cosmere.Lightweave.Playground;
using HarmonyLib;
using Verse;

namespace Cosmere.Lightweave.Patch;

[HarmonyPatch(typeof(OptionListingUtility), nameof(OptionListingUtility.DrawOptionListing))]
public static class PlaygroundMenuLinkPatch {
    public static void Prefix(List<ListableOption> optList) {
        if (!Prefs.DevMode || optList == null) {
            return;
        }

        bool isWebLinkColumn = false;
        for (int i = 0; i < optList.Count; i++) {
            if (optList[i] is PlaygroundMenuOption) {
                return;
            }

            if (optList[i] is ListableOption_WebLink) {
                isWebLinkColumn = true;
            }
        }

        if (!isWebLinkColumn) {
            return;
        }

        optList.Add(new PlaygroundMenuOption((string)"CL_DevButton_Playground".Translate(), OpenPlayground));
    }

    private static void OpenPlayground() {
        if (Find.WindowStack.IsOpen<LightweavePlayground>()) {
            return;
        }

        Find.WindowStack.Add(new LightweavePlayground());
    }
}
