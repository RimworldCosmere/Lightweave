using HarmonyLib;
using RimWorld;
using Verse;

namespace Cosmere.Lightweave.Patch;

[HarmonyPatch(typeof(UIRoot_Entry), nameof(UIRoot_Entry.UIRootOnGUI))]
public static class UIRoot_Entry_PerformanceOverlayPatch {
    public static void Postfix() {
        PerformanceOverlay.Draw();
    }
}

[HarmonyPatch(typeof(UIRoot_Play), nameof(UIRoot_Play.UIRootOnGUI))]
public static class UIRoot_Play_PerformanceOverlayPatch {
    public static void Postfix() {
        PerformanceOverlay.Draw();
    }
}
