using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Runtime;

namespace Cosmere.Lightweave.Redesign;

public static class RedesignLog {
    public const string Channel = "Lightweave.Redesign";

    public static void Trace(string text, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        => LightweaveLog.Trace(Channel, text, line, file);

    public static void Debug(string text, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        => LightweaveLog.Debug(Channel, text, line, file);

    public static void Message(string text, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        => LightweaveLog.Message(Channel, text, line, file);

    public static void Info(string text, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        => LightweaveLog.Info(Channel, text, line, file);

    public static void Warning(string text, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        => LightweaveLog.Warning(Channel, text, line, file);

    public static void Error(string text, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        => LightweaveLog.Error(Channel, text, line, file);
}
