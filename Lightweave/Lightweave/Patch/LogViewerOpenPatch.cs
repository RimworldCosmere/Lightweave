using Cosmere.Lightweave.Logging;
using HarmonyLib;
using Verse;

namespace Cosmere.Lightweave.Patch;

[HarmonyPatch(typeof(Log), nameof(Log.TryOpenLogWindow))]
internal static class LogViewerOpenPatch {
    private static bool Prefix() {
        LightweaveLogSink? sink = LogViewerBoot.Sink;
        WindowStack? windowStack = Find.WindowStack;
        if (sink == null || windowStack == null) {
            return true;
        }
        windowStack.Add(new LogViewerWindow(sink));
        return false;
    }
}
