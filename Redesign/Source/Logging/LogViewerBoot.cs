using System;
using Cosmere.Lightweave.Runtime;
using LudeonTK;
using Verse;

namespace Cosmere.Lightweave.Redesign.Logging;

[StaticConstructorOnStartup]
internal static class LogViewerBoot {
    public static LightweaveLogSink? Sink { get; private set; }

    static LogViewerBoot() {
        try {
            LightweaveLogSink sink = new LightweaveLogSink();
            CryptikLemur.RimLogging.Logging.RegisterSink(sink);
            Sink = sink;
            LightweaveLog.Message("Log viewer sink registered");
        }
        catch (Exception ex) {
            LightweaveLog.Error("Failed to register log viewer sink: " + ex);
        }
    }

    public static void OpenVanilla() {
        WindowStack? windowStack = Find.WindowStack;
        if (windowStack == null) {
            return;
        }
        if (windowStack.WindowOfType<EditWindow_Log>() == null) {
            windowStack.Add(new EditWindow_Log());
        }
    }
}
