using System.Collections.Generic;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Tokens;
using Cryptiklemur.RimLogging;
using Cryptiklemur.RimLogging.Filtering;

namespace Cosmere.Lightweave.Logging;

internal static class LogFilter {
    public static List<LogChannel> BuildChannels(IReadOnlyList<LogEntry> snapshot, string channelFilter) {
        Dictionary<string, int> counts = new Dictionary<string, int>();
        Dictionary<string, bool> errors = new Dictionary<string, bool>();
        List<string> order = new List<string>();

        for (int i = 0; i < snapshot.Count; i++) {
            LogEntry entry = snapshot[i];
            string channel = string.IsNullOrEmpty(entry.Channel) ? "(root)" : entry.Channel;
            if (!counts.ContainsKey(channel)) {
                counts[channel] = 0;
                errors[channel] = false;
                order.Add(channel);
            }
            counts[channel] = counts[channel] + 1;
            if (entry.Level >= LogLevel.Error) {
                errors[channel] = true;
            }
        }

        order.Sort(System.StringComparer.Ordinal);

        List<LogChannel> result = new List<LogChannel>(order.Count + 1) {
            new LogChannel(LogViewerState.AllChannels, "CL_LogViewer_AllChannels", snapshot.Count, 0, false),
        };

        bool hasFilter = !string.IsNullOrEmpty(channelFilter);
        string filterLower = hasFilter ? channelFilter.ToLowerInvariant() : "";

        for (int i = 0; i < order.Count; i++) {
            string channel = order[i];
            if (hasFilter && channel.ToLowerInvariant().IndexOf(filterLower, System.StringComparison.Ordinal) < 0) {
                continue;
            }
            int depth = 0;
            for (int c = 0; c < channel.Length; c++) {
                if (channel[c] == '.') {
                    depth++;
                }
            }
            result.Add(new LogChannel(channel, channel, counts[channel], depth, errors[channel]));
        }

        return result;
    }

    public static List<LogEntry> Apply(IReadOnlyList<LogEntry> snapshot, LogViewerState state) {
        FilterExpression? dsl = null;
        bool useDsl = false;
        if (state.DslMode && !string.IsNullOrEmpty(state.DslSource) && state.DslError == null) {
            useDsl = FilterExpression.TryParse(state.DslSource, out dsl, out _);
        }

        string searchLower = string.IsNullOrEmpty(state.Search) ? "" : state.Search.ToLowerInvariant();
        bool hasSearch = searchLower.Length > 0;
        bool allChannels = state.ActiveChannel == LogViewerState.AllChannels;

        List<LogEntry> result = new List<LogEntry>(snapshot.Count);
        for (int i = 0; i < snapshot.Count; i++) {
            LogEntry entry = snapshot[i];

            if (!allChannels) {
                string channel = string.IsNullOrEmpty(entry.Channel) ? "(root)" : entry.Channel;
                if (channel != state.ActiveChannel) {
                    continue;
                }
            }

            if (useDsl && dsl != null) {
                if (!dsl.Match(entry)) {
                    continue;
                }
            }
            else {
                int levelIndex = (int)entry.Level;
                if (levelIndex >= 0 && levelIndex < state.Levels.Length && !state.Levels[levelIndex]) {
                    continue;
                }
            }

            if (hasSearch) {
                string message = entry.RenderedMessage ?? "";
                string source = entry.Source.IsCallerProvided ? entry.Source.File ?? "" : "";
                if (message.ToLowerInvariant().IndexOf(searchLower, System.StringComparison.Ordinal) < 0
                    && source.ToLowerInvariant().IndexOf(searchLower, System.StringComparison.Ordinal) < 0
                    && entry.Channel.ToLowerInvariant().IndexOf(searchLower, System.StringComparison.Ordinal) < 0) {
                    continue;
                }
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
}
