using Cosmere.Lightweave.Runtime;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Cosmere.Lightweave.Patch;

[HarmonyPatch(typeof(UIRoot_Entry), nameof(UIRoot_Entry.UIRootOnGUI))]
public static class UIRoot_Entry_DragSkipPatch {
    public static bool Prefix() {
        if (!UIRootDragSkip.ShouldSkip()) {
            return true;
        }
        UIRootDragSkip.RunMinimalDispatch();
        return false;
    }
}

[HarmonyPatch(typeof(UIRoot_Play), nameof(UIRoot_Play.UIRootOnGUI))]
public static class UIRoot_Play_DragSkipPatch {
    public static bool Prefix() {
        if (!UIRootDragSkip.ShouldSkip()) {
            return true;
        }
        UIRootDragSkip.RunMinimalDispatch();
        return false;
    }
}

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
