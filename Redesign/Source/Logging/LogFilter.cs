using System.Collections.Generic;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Tokens;
using CryptikLemur.RimLogging;
using CryptikLemur.RimLogging.Filtering;
using Verse;
using LogEntry = CryptikLemur.RimLogging.LogEntry;

namespace Cosmere.Lightweave.Redesign.Logging;

internal static class LogFilter {
    public static List<LogChannel> BuildChannels(IReadOnlyList<LogEntry> snapshot, LogViewerState state) {
        ChannelClassifier.EnsureBuilt();

        Dictionary<string, NodeAccum> nodes = new Dictionary<string, NodeAccum>(System.StringComparer.Ordinal);

        for (int i = 0; i < snapshot.Count; i++) {
            LogEntry entry = snapshot[i];
            string channel = string.IsNullOrEmpty(entry.Channel) ? "(root)" : entry.Channel;
            string[] path = ChannelClassifier.PathFor(channel);
            bool isError = entry.Level >= LogLevel.Error;

            for (int d = 1; d <= path.Length; d++) {
                string id = string.Join("/", path, 0, d);
                if (!nodes.TryGetValue(id, out NodeAccum acc)) {
                    acc = new NodeAccum {
                        Id = id,
                        Name = path[d - 1],
                        Depth = d - 1,
                        Count = 0,
                        HasError = false,
                    };
                }
                acc.Count++;
                if (isError) {
                    acc.HasError = true;
                }
                nodes[id] = acc;
            }
        }

        string channelFilter = state.ChannelFilter;
        bool hasFilter = !string.IsNullOrEmpty(channelFilter);
        string filterLower = hasFilter ? channelFilter.ToLowerInvariant() : string.Empty;
        HashSet<string>? keepIds = null;
        if (hasFilter) {
            keepIds = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (KeyValuePair<string, NodeAccum> kvp in nodes) {
                if (kvp.Key.ToLowerInvariant().IndexOf(filterLower, System.StringComparison.Ordinal) < 0) {
                    continue;
                }
                string[] parts = kvp.Key.Split('/');
                for (int d = 1; d <= parts.Length; d++) {
                    keepIds.Add(string.Join("/", parts, 0, d));
                }
            }
        }

        HashSet<string> hasChildrenSet = new HashSet<string>(System.StringComparer.Ordinal);
        foreach (string id in nodes.Keys) {
            int sep = id.LastIndexOf('/');
            if (sep > 0) {
                hasChildrenSet.Add(id.Substring(0, sep));
            }
        }

        List<string> sortedIds = new List<string>(nodes.Keys);
        sortedIds.Sort(static (a, b) => {
            int ra = TopRank(a);
            int rb = TopRank(b);
            if (ra != rb) {
                return ra - rb;
            }
            return string.Compare(a, b, System.StringComparison.OrdinalIgnoreCase);
        });

        List<LogChannel> result = new List<LogChannel>(sortedIds.Count + 1) {
            new LogChannel(
                LogViewerState.AllChannels,
                (string)"CL_LogViewer_AllChannels".Translate(),
                snapshot.Count,
                0,
                false,
                false,
                true
            ),
        };

        string modLabel = (string)"CL_LogViewer_Group_Mod".Translate();
        string vanillaLabel = (string)"CL_LogViewer_Group_Vanilla".Translate();

        HashSet<string> visible = new HashSet<string>(System.StringComparer.Ordinal);
        for (int i = 0; i < sortedIds.Count; i++) {
            string id = sortedIds[i];
            if (hasFilter && keepIds != null && !keepIds.Contains(id)) {
                continue;
            }
            NodeAccum acc = nodes[id];

            int sep = id.LastIndexOf('/');
            string? parentId = sep > 0 ? id.Substring(0, sep) : null;

            bool ancestorsVisible;
            if (parentId == null) {
                ancestorsVisible = true;
            } else if (hasFilter) {
                ancestorsVisible = true;
            } else {
                int parentDepth = acc.Depth - 1;
                ancestorsVisible = visible.Contains(parentId) && state.IsChannelExpanded(parentId, parentDepth);
            }

            if (!ancestorsVisible) {
                continue;
            }

            visible.Add(id);

            bool hasChildren = hasChildrenSet.Contains(id);
            bool expanded = state.IsChannelExpanded(id, acc.Depth);

            string displayName = acc.Name;
            if (acc.Depth == 0) {
                if (acc.Name == ChannelClassifier.ModGroupId) {
                    displayName = modLabel;
                } else if (acc.Name == ChannelClassifier.VanillaGroupId) {
                    displayName = vanillaLabel;
                }
            }
            result.Add(new LogChannel(id, displayName, acc.Count, acc.Depth, acc.HasError, hasChildren, expanded));
        }

        return result;
    }

    public static List<LogEntry> Apply(IReadOnlyList<LogEntry> snapshot, LogViewerState state) {
        FilterExpression? dsl = null;
        bool useDsl = false;
        if (!string.IsNullOrEmpty(state.DslSource) && state.DslError == null) {
            useDsl = FilterExpression.TryParse(state.DslSource, out dsl, out _);
        }

        bool allChannels = state.ActiveChannel == LogViewerState.AllChannels;
        string activePrefix = allChannels ? string.Empty : state.ActiveChannel + "/";

        List<LogEntry> result = new List<LogEntry>(snapshot.Count);
        for (int i = 0; i < snapshot.Count; i++) {
            LogEntry entry = snapshot[i];

            if (!allChannels) {
                string channel = string.IsNullOrEmpty(entry.Channel) ? "(root)" : entry.Channel;
                string pathKey = ChannelClassifier.JoinPath(ChannelClassifier.PathFor(channel));
                if (pathKey != state.ActiveChannel && !pathKey.StartsWith(activePrefix, System.StringComparison.Ordinal)) {
                    continue;
                }
            }

            int levelIndex = (int)entry.Level;
            if (levelIndex >= 0 && levelIndex < state.Levels.Length && !state.Levels[levelIndex]) {
                continue;
            }

            if (useDsl && dsl != null && !dsl.Match(entry)) {
                continue;
            }

            result.Add(entry);
        }

        return result;
    }

    public static ThemeSlot LevelSlot(LogLevel level) {
        switch (level) {
            case LogLevel.Trace:
                return ThemeSlot.TextMuted;
            case LogLevel.Debug:
                return ThemeSlot.StatusInfo;
            case LogLevel.Info:
                return ThemeSlot.StatusSuccess;
            case LogLevel.Warn:
                return ThemeSlot.StatusWarning;
            case LogLevel.Error:
            case LogLevel.Fatal:
                return ThemeSlot.StatusDanger;
            default:
                return ThemeSlot.TextSecondary;
        }
    }

    public static ChipTone LevelTone(LogLevel level) {
        switch (level) {
            case LogLevel.Trace:
                return ChipTone.Trace;
            case LogLevel.Debug:
                return ChipTone.Debug;
            case LogLevel.Info:
                return ChipTone.Info;
            case LogLevel.Warn:
                return ChipTone.Warn;
            default:
                return ChipTone.Error;
        }
    }

    private static int TopRank(string id) {
        if (id.StartsWith(ChannelClassifier.ModGroupId, System.StringComparison.Ordinal)) {
            int len = ChannelClassifier.ModGroupId.Length;
            if (id.Length == len || id[len] == '/') {
                return 1;
            }
        }
        if (id.StartsWith(ChannelClassifier.VanillaGroupId, System.StringComparison.Ordinal)) {
            int len = ChannelClassifier.VanillaGroupId.Length;
            if (id.Length == len || id[len] == '/') {
                return 2;
            }
        }
        return 1;
    }

    private struct NodeAccum {
        public string Id;
        public string Name;
        public int Depth;
        public int Count;
        public bool HasError;
    }
}
