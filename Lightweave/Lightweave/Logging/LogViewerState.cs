using Cryptiklemur.RimLogging;

namespace Cosmere.Lightweave.Logging;

internal sealed class LogViewerState {
    public bool ChannelsOpen = true;
    public string ActiveChannel = AllChannels;
    public string ChannelFilter = "";
    public string Search = "";

    public bool[] Levels = { false, false, true, true, true, true };

    public bool DslMode;
    public string DslSource = "";
    public string? DslError;

    public LogEntry? Selected;

    public bool Uploading;

    public const string AllChannels = "*all*";
}
