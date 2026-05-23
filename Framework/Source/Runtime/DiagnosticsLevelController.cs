using CryptikLemur.RimLogging;

namespace Cosmere.Lightweave.Runtime;

internal static class DiagnosticsLevelController {
    private static bool applied;
    private static LogLevel restoreLevel;

    public static void Sync(bool enabled) {
        if (enabled == applied) {
            return;
        }
        if (enabled) {
            restoreLevel = CryptikLemur.RimLogging.Logging.GlobalMinLevel;
            CryptikLemur.RimLogging.Logging.GlobalMinLevel = LogLevel.Trace;
            applied = true;
            return;
        }
        CryptikLemur.RimLogging.Logging.GlobalMinLevel = restoreLevel;
        applied = false;
    }
}
