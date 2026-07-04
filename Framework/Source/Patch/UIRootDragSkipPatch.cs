using Cosmere.Lightweave.Runtime;
using UnityEngine;
using Verse;

namespace Cosmere.Lightweave.Patch;

internal static class UIRootDragSkip {
    public static bool ShouldSkip() {
        if (!ActiveDragRegistry.IsActive) {
            return false;
        }

        EventType et = Event.current.type;
        if (et == EventType.MouseDrag || et == EventType.Layout) {
            return true;
        }

        return false;
    }

    public static void RunMinimalDispatch() {
        Text.StartOfOnGUI();
        Find.WindowStack.WindowStackOnGUI();
    }
}
