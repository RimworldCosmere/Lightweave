using System.Collections.Generic;
using Concord;
using Cosmere.Lightweave.Redesign.MainMenu;
using Verse;

namespace Cosmere.Lightweave.Redesign.Patch;

[Patch(typeof(OptionListingUtility))]
public static class MainMenuLinkCapturePatch {
    [Inject(At.Head, nameof(OptionListingUtility.DrawOptionListing))]
    public static void Prefix(ControlHandle<float> ch, List<ListableOption> optList) {
        if (!MainMenuLinkHarvester.Capturing) {
            return;
        }

        if (optList != null) {
            for (int i = 0; i < optList.Count; i++) {
                if (optList[i] is ListableOption_WebLink) {
                    MainMenuLinkHarvester.Captured = optList;
                    break;
                }
            }
        }

        ch.ReturnValue = 0f;
        ch.Cancel();
    }
}
