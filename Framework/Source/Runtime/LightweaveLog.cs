using System.Runtime.CompilerServices;
using CryptikLemur.RimLogging;

namespace Cosmere.Lightweave.Runtime;

public static class LightweaveLog {
    public const string DefaultChannel = "Lightweave";
    public const string DiagnosticsChannel = "Lightweave.Diagnostics";

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> ChannelCache =
        new System.Collections.Concurrent.ConcurrentDictionary<string, string>();

    public static string DeriveChannel(string file) {
        if (string.IsNullOrEmpty(file)) {
            return DefaultChannel;
        }
        return ChannelCache.GetOrAdd(file, ComputeChannel);
    }

    private static string ComputeChannel(string file) {
        string normalized = file.Replace('\\', '/');
        int srcIdx = normalized.LastIndexOf("/Source/", System.StringComparison.Ordinal);
        if (srcIdx < 0) {
            return DefaultChannel;
        }
        int redesignIdx = normalized.LastIndexOf("/Redesign/", System.StringComparison.Ordinal);
        if (redesignIdx >= 0 && redesignIdx < srcIdx) {
            return "Lightweave.Redesign";
        }
        string after = normalized.Substring(srcIdx + "/Source/".Length);
        int slash = after.IndexOf('/');
        string sub = slash < 0 ? "" : after.Substring(0, slash);
        return string.IsNullOrEmpty(sub) ? DefaultChannel : DefaultChannel + "." + sub;
    }

    public static void Trace(string text, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        => Log.Trace(channel: DeriveChannel(file), template: text, line: line, file: file);

    public static void Trace(string channel, string text, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        => Log.Trace(channel: channel, template: text, line: line, file: file);

    public static void Debug(string text, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        => Log.Debug(channel: DeriveChannel(file), template: text, line: line, file: file);

    public static void Debug(string channel, string text, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        => Log.Debug(channel: channel, template: text, line: line, file: file);

    public static void Message(string text, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        => Log.Info(channel: DeriveChannel(file), template: text, line: line, file: file);

    public static void Message(string channel, string text, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        => Log.Info(channel: channel, template: text, line: line, file: file);

    public static void Info(string text, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        => Log.Info(channel: DeriveChannel(file), template: text, line: line, file: file);

    public static void Info(string channel, string text, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        => Log.Info(channel: channel, template: text, line: line, file: file);

    public static void Warning(string text, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        => Log.Warn(channel: DeriveChannel(file), template: text, line: line, file: file);

    public static void Warning(string channel, string text, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        => Log.Warn(channel: channel, template: text, line: line, file: file);

    public static void Error(string text, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        => Log.Error(channel: DeriveChannel(file), template: text, line: line, file: file);

    public static void Error(string channel, string text, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        => Log.Error(channel: channel, template: text, line: line, file: file);
}
