using HarmonyLib;
using RimWorld;
using Verse;

namespace Cosmere.Lightweave.Patch;

[HarmonyPatch(typeof(UIRoot_Entry), nameof(UIRoot_Entry.UIRootOnGUI))]
public static class UIRoot_Entry_PerformanceOverlayPatch {
    // Prefix runs before base.UIRootOnGUI handles window events, so the toggle is
    // caught even when a fullscreen modal window (e.g. the New Colony window) would
    // otherwise absorb the keystroke.
    public static void Prefix() {
        PerformanceOverlay.HandleToggleHotkey();
    }

    public static void Postfix() {
        PerformanceOverlay.Draw();
    }
}

[HarmonyPatch(typeof(UIRoot_Play), nameof(UIRoot_Play.UIRootOnGUI))]
public static class UIRoot_Play_PerformanceOverlayPatch {
    public static void Prefix() {
        PerformanceOverlay.HandleToggleHotkey();
    }

    public static void Postfix() {
        PerformanceOverlay.Draw();
    }
}
