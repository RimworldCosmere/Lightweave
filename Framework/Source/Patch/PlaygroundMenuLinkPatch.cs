using System.Collections.Generic;
using Concord;
using Cosmere.Lightweave.Playground;
using Verse;

namespace Cosmere.Lightweave.Patch;

[Patch(typeof(OptionListingUtility))]
public static class PlaygroundMenuLinkPatch {
    [Inject(At.Head, nameof(OptionListingUtility.DrawOptionListing))]
    public static void Prefix(ControlHandle<float> ch, List<ListableOption> optList) {
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
