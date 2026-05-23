using System.Collections.Generic;
using CryptikLemur.RimLogging;

namespace Cosmere.Lightweave.Redesign.Logging;

internal sealed class LogViewerState {
    public bool ChannelsOpen = true;
    public string ActiveChannel = AllChannels;
    public string ChannelFilter = "";

    public bool[] Levels = { false, false, true, true, true, true };

    public string DslSource = "";
    public string? DslError;

    public LogEntry? Selected;

    public bool Uploading;

    public readonly Dictionary<string, bool> ExpandedChannels = new Dictionary<string, bool>(System.StringComparer.Ordinal);

    public const string AllChannels = "*all*";

    public bool IsChannelExpanded(string id, int depth) {
        if (ExpandedChannels.TryGetValue(id, out bool value)) {
            return value;
        }
        return depth < 2;
    }

    public void ToggleChannel(string id, int depth) {
        bool current = IsChannelExpanded(id, depth);
        ExpandedChannels[id] = !current;
    }
}
