using Cosmere.Lightweave.Redesign.Logging;
using HarmonyLib;
using LudeonTK;
using UnityEngine;
using Verse;

namespace Cosmere.Lightweave.Redesign.Patch;

[HarmonyPatch(typeof(DebugWindowsOpener), "ToggleLogWindow")]
internal static class DebugLogTogglePatch {
    private static bool Prefix() {
        if (Event.current != null && Event.current.shift) {
            return true;
        }
        LightweaveLogSink? sink = LogViewerBoot.Sink;
        WindowStack? windowStack = Find.WindowStack;
        if (sink == null || windowStack == null) {
            return true;
        }
        if (!windowStack.TryRemove(typeof(LogViewerWindow))) {
            windowStack.Add(new LogViewerWindow(sink));
        }
        return false;
    }
}
