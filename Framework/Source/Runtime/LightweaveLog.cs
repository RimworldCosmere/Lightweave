using System.Runtime.CompilerServices;
using CryptikLemur.RimLogging;

namespace Cosmere.Lightweave.Runtime;

public static class LightweaveLog {
    public const string DefaultChannel = "Lightweave";
    public const string DiagnosticsChannel = "Lightweave.Diagnostics";

    public static void Trace(string text, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        => Log.Trace(channel: DefaultChannel, template: text, line: line, file: file);

    public static void Trace(string channel, string text, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        => Log.Trace(channel: channel, template: text, line: line, file: file);

    public static void Debug(string text, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        => Log.Debug(channel: DefaultChannel, template: text, line: line, file: file);

    public static void Debug(string channel, string text, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        => Log.Debug(channel: channel, template: text, line: line, file: file);

    public static void Message(string text, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        => Log.Info(channel: DefaultChannel, template: text, line: line, file: file);

    public static void Message(string channel, string text, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        => Log.Info(channel: channel, template: text, line: line, file: file);

    public static void Info(string text, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        => Log.Info(channel: DefaultChannel, template: text, line: line, file: file);

    public static void Info(string channel, string text, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        => Log.Info(channel: channel, template: text, line: line, file: file);

    public static void Warning(string text, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        => Log.Warn(channel: DefaultChannel, template: text, line: line, file: file);

    public static void Warning(string channel, string text, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        => Log.Warn(channel: channel, template: text, line: line, file: file);

    public static void Error(string text, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        => Log.Error(channel: DefaultChannel, template: text, line: line, file: file);

    public static void Error(string channel, string text, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        => Log.Error(channel: channel, template: text, line: line, file: file);
}
