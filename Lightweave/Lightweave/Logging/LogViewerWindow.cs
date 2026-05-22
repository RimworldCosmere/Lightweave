using System;
using System.Collections.Generic;
using Cosmere.Lightweave.Hooks;
using Cosmere.Lightweave.Icons;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Typography;
using Cosmere.Lightweave.Types;
using Cryptiklemur.RimLogging;
using Cryptiklemur.RimLogging.Filtering;
using Cryptiklemur.RimLogging.Format;
using UnityEngine;
using Verse;
using KeyValue = Cosmere.Lightweave.Data.KeyValue;
using LogEntry = Cryptiklemur.RimLogging.LogEntry;
using LwList = Cosmere.Lightweave.Data.List;
using Text = Cosmere.Lightweave.Typography.Typography.Text;

namespace Cosmere.Lightweave.Logging;

internal sealed class LogViewerWindow : LightweaveWindow {
    private static readonly LogLevel[] ChipLevels = {
        LogLevel.Trace, LogLevel.Debug, LogLevel.Info, LogLevel.Warn, LogLevel.Error,
    };

    private static readonly string[] ChipLevelKeys = {
        "CL_LogViewer_Level_Trace", "CL_LogViewer_Level_Debug", "CL_LogViewer_Level_Info",
        "CL_LogViewer_Level_Warn", "CL_LogViewer_Level_Error",
    };

    private readonly LightweaveLogSink sink;
    private readonly LogViewerState state = new LogViewerState();

    public LogViewerWindow(LightweaveLogSink sink) {
        this.sink = sink;
    }

    public LogViewerWindow() : this(LogViewerBoot.Sink ?? new LightweaveLogSink()) {
    }

    protected override float WidthFraction => 0.85f;
    protected override float HeightFraction => 0.72f;

    protected override LightweaveNode? Header() {
        Action invalidate = MakeInvalidate();
        IReadOnlyList<LogEntry> snapshot = sink.Snapshot();
        string newest = snapshot.Count > 0
            ? snapshot[snapshot.Count - 1].Timestamp.ToString("HH:mm:ss.fff")
            : "";

        return WindowHeader.Create(
            headerContent: BuildTitlebar(newest, invalidate),
            draggable: true,
            drawDivider: true
        );
    }

    protected override LightweaveNode Body() {
        Action invalidate = MakeInvalidate();
        IReadOnlyList<LogEntry> snapshot = sink.Snapshot();
        List<LogEntry> filtered = LogFilter.Apply(snapshot, state);
        List<LogChannel> channels = LogFilter.BuildChannels(snapshot, state.ChannelFilter);

        LightweaveNode listAndDetail = SplitPane.Create(
            first: BuildListColumn(filtered, invalidate),
            second: BuildDetail(invalidate),
            orientation: SplitOrientation.Horizontal,
            initialFraction: 0.68f,
            minFirst: new Rem(20f),
            minSecond: new Rem(14f),
            style: Fill
        );

        if (!state.ChannelsOpen) {
            return listAndDetail;
        }

        return SplitPane.Create(
            first: BuildChannelPane(channels, invalidate),
            second: listAndDetail,
            orientation: SplitOrientation.Horizontal,
            initialFraction: 0.20f,
            minFirst: new Rem(11.25f),
            minSecond: new Rem(24f),
            style: Fill
        );
    }

    private static Style Fill => new Style { Width = Length.Stretch, Height = Length.Stretch };

    private static Action MakeInvalidate() {
        Hooks.Hooks.StateHandle<int> tick = Hooks.Hooks.UseState(0);
        return () => tick.Set(tick.Value + 1);
    }

    private LightweaveNode BuildTitlebar(string newest, Action invalidate) {
        return HStack.Create(
            gap: new Rem(0.5f),
            children: row => {
                row.AddHug(IconButton.Create(
                    icon: Glyph.Create(state.ChannelsOpen ? Icons.Phosphor.SidebarSimple : Icons.Phosphor.Sidebar),
                    onClick: () => {
                        state.ChannelsOpen = !state.ChannelsOpen;
                        invalidate();
                    },
                    variant: state.ChannelsOpen ? Variant.Secondary : Variant.Ghost,
                    tooltipKey: state.ChannelsOpen ? "CL_LogViewer_HideChannels" : "CL_LogViewer_ShowChannels",
                    id: "logviewer-sidebar-toggle"
                ));

                row.AddHug(Text.Create(
                    (string)"CL_LogViewer_Title".Translate(),
                    style: new Style { FontFamily = RenderContext.Current.Theme.GetFont(FontRole.Heading), FontSize = new Rem(1f), TextColor = ThemeSlot.TextPrimary }
                ));

                row.AddHug(HStack.Create(
                    gap: new Rem(0.25f),
                    children: live => {
                        live.AddHug(Glyph.Create(
                            Icons.Phosphor.Broadcast,
                            style: new Style { FontSize = new Rem(0.75f), TextColor = ThemeSlot.StatusSuccess }
                        ));
                        live.AddHug(Text.Create(
                            (string)"CL_LogViewer_Tailing".Translate(),
                            style: new Style { FontSize = new Rem(0.6875f), TextColor = ThemeSlot.StatusSuccess }
                        ));
                    }
                ));

                row.AddHug(Text.Create(
                    newest,
                    style: new Style { FontFamily = RenderContext.Current.Theme.GetFont(FontRole.Mono), FontSize = new Rem(0.6875f), TextColor = ThemeSlot.TextMuted }
                ));

                row.AddFlex(Box.Create());

                row.AddHug(Button.Create(
                    (string)"CL_LogViewer_Dsl".Translate(),
                    onClick: () => {
                        state.DslMode = !state.DslMode;
                        invalidate();
                    },
                    variant: state.DslMode ? Variant.Primary : Variant.Ghost,
                    id: "logviewer-dsl-toggle"
                ));

                row.AddHug(BuildPresetDropdown(invalidate));

                row.AddHug(IconButton.Create(
                    icon: Glyph.Create(Icons.Phosphor.Terminal),
                    onClick: LogViewerBoot.OpenVanilla,
                    variant: Variant.Ghost,
                    tooltipKey: "CL_LogViewer_OpenVanilla",
                    id: "logviewer-open-vanilla"
                ));

                row.AddHug(IconButton.Create(
                    icon: Glyph.Create(Icons.Phosphor.MagnifyingGlass),
                    onClick: state.Uploading ? (Action?)null : () => LogBundleShare.Upload(sink, state, invalidate),
                    variant: Variant.Ghost,
                    disabled: state.Uploading,
                    tooltipKey: "CL_LogViewer_ShareBundle",
                    id: "logviewer-share"
                ));
            }
        );
    }

    private LightweaveNode BuildPresetDropdown(Action invalidate) {
        IReadOnlyList<string> names = LogPresetStore.Names;
        List<string> options = new List<string>(names.Count + 1) { PresetSaveSentinel };
        for (int i = 0; i < names.Count; i++) {
            options.Add(names[i]);
        }

        return Dropdown.Create(
            value: PresetSaveSentinel,
            options: options,
            labelFn: option => option == PresetSaveSentinel
                ? (string)"CL_LogViewer_Presets".Translate()
                : option,
            onChange: option => {
                if (option == PresetSaveSentinel) {
                    SaveCurrentPreset();
                }
                else if (LogPresetStore.TryGetExpression(option, out string expr)) {
                    state.DslSource = expr;
                    state.DslMode = true;
                    FilterExpression.TryParse(expr, out _, out state.DslError);
                }
                invalidate();
            },
            variant: DropdownVariant.Button,
            buttonStyle: Variant.Ghost,
            instanceKey: "logviewer-presets"
        );
    }

    private void SaveCurrentPreset() {
        if (string.IsNullOrEmpty(state.DslSource)) {
            return;
        }
        string name = (string)"CL_LogViewer_PresetName".Translate((LogPresetStore.Names.Count + 1).Named("INDEX"));
        LogPresetStore.Save(name, state.DslSource);
    }

    private LightweaveNode BuildChannelPane(List<LogChannel> channels, Action invalidate) {
        return Column.Create(
            gap: new Rem(0.25f),
            children: col => {
                col.Add(SearchField.Create(
                    value: state.ChannelFilter,
                    onChange: text => {
                        state.ChannelFilter = text;
                        invalidate();
                    },
                    placeholder: (string)"CL_LogViewer_FilterChannels".Translate(),
                    id: "logviewer-channel-filter"
                ));
                col.Add(LwList.Create(
                    items: channels,
                    rowBuilder: (channel, index) => BuildChannelRow(channel, invalidate),
                    rowHeight: new Rem(1.75f).ToPixels(),
                    virtualize: true,
                    overscan: 4,
                    style: new Style { Width = Length.Stretch, Height = Length.Stretch }
                ));
            },
            style: Fill
        );
    }

    private LightweaveNode BuildChannelRow(LogChannel channel, Action invalidate) {
        bool active = channel.Id == state.ActiveChannel;
        string label = channel.Id == LogViewerState.AllChannels
            ? (string)"CL_LogViewer_AllChannels".Translate()
            : channel.Name;

        LightweaveNode body = HStack.Create(
            gap: new Rem(0.375f),
            children: row => {
                if (channel.Depth > 0) {
                    row.Add(Box.Create(), new Rem(channel.Depth * 0.875f).ToPixels());
                }
                row.AddHug(Glyph.Create(
                    channel.Depth == 0 ? Icons.Phosphor.CaretDown : Icons.Phosphor.CaretRight,
                    style: new Style { FontSize = new Rem(0.6875f), TextColor = active ? ThemeSlot.SurfaceAccent : ThemeSlot.TextMuted }
                ));
                row.AddFlex(Text.Create(
                    label,
                    style: new Style {
                        FontSize = new Rem(0.8125f),
                        TextColor = active ? ThemeSlot.SurfaceAccent : ThemeSlot.TextSecondary,
                    }
                ));
                row.AddHug(Text.Create(
                    channel.Count.ToString("N0"),
                    style: new Style {
                        FontFamily = RenderContext.Current.Theme.GetFont(FontRole.Mono),
                        FontSize = new Rem(0.6875f),
                        TextColor = channel.HasError
                            ? ThemeSlot.StatusDanger
                            : active ? ThemeSlot.SurfaceAccent : ThemeSlot.TextMuted,
                    }
                ));
            }
        );

        Style rowStyle = active
            ? new Style {
                Background = BackgroundSpec.Of(ThemeSlot.ActiveTint),
                Border = new BorderSpec(Left: new Rem(2f / 16f), Color: ThemeSlot.SurfaceAccent),
                Padding = EdgeInsets.Horizontal(new Rem(0.5f)),
            }
            : new Style { Padding = EdgeInsets.Horizontal(new Rem(0.5f)) };

        return Button.Create(
            label: string.Empty,
            onClick: () => {
                state.ActiveChannel = channel.Id;
                invalidate();
            },
            variant: Variant.Ghost,
            ghost: true,
            body: body,
            style: rowStyle,
            id: "logviewer-channel-" + channel.Id
        );
    }

    private LightweaveNode BuildListColumn(List<LogEntry> filtered, Action invalidate) {
        return Column.Create(
            gap: new Rem(0.375f),
            children: col => {
                col.Add(BuildFilterBar(invalidate));
                col.Add(LwList.Create(
                    items: filtered,
                    rowBuilder: (entry, index) => BuildLogRow(entry, invalidate),
                    rowHeight: new Rem(1.625f).ToPixels(),
                    virtualize: true,
                    overscan: 6,
                    style: new Style { Width = Length.Stretch, Height = Length.Stretch }
                ));
            },
            style: Fill
        );
    }

    private LightweaveNode BuildFilterBar(Action invalidate) {
        return HStack.Create(
            gap: new Rem(0.375f),
            children: row => {
                row.AddFlex(SearchField.Create(
                    value: state.Search,
                    onChange: text => {
                        state.Search = text;
                        invalidate();
                    },
                    placeholder: (string)"CL_LogViewer_SearchPlaceholder".Translate(),
                    id: "logviewer-search"
                ));

                if (state.DslMode) {
                    row.AddFlex(BuildDslEditor(invalidate));
                    return;
                }

                for (int i = 0; i < ChipLevels.Length; i++) {
                    int index = i;
                    LogLevel level = ChipLevels[index];
                    row.AddHug(Chip.Create(
                        label: ((string)ChipLevelKeys[index].Translate()).ToUpperInvariant(),
                        on: state.Levels[(int)level],
                        onToggle: value => {
                            state.Levels[(int)level] = value;
                            invalidate();
                        },
                        variant: ChipVariant.Severity,
                        tone: LogFilter.LevelTone(level),
                        id: "logviewer-chip-" + level
                    ));
                }
            }
        );
    }

    private LightweaveNode BuildDslEditor(Action invalidate) {
        return Column.Create(
            gap: new Rem(0.25f),
            children: col => {
                col.Add(TextField.Create(
                    value: state.DslSource,
                    onChange: text => {
                        state.DslSource = text;
                        FilterExpression.TryParse(text, out _, out state.DslError);
                        invalidate();
                    },
                    placeholder: (string)"CL_LogViewer_DslPlaceholder".Translate(),
                    id: "logviewer-dsl-editor"
                ));
                if (state.DslError != null) {
                    col.Add(Text.Create(
                        state.DslError,
                        style: new Style { FontSize = new Rem(0.75f), TextColor = ThemeSlot.StatusDanger }
                    ));
                }
            }
        );
    }

    private LightweaveNode BuildLogRow(LogEntry entry, Action invalidate) {
        bool selected = ReferenceEquals(entry, state.Selected);
        string timestamp = entry.Timestamp.ToString("HH:mm:ss.fff");
        string levelLabel = entry.Level.ToString().ToUpperInvariant();
        ThemeSlot levelSlot = LogFilter.LevelSlot(entry.Level);
        string source = entry.Source.IsCallerProvided
            ? entry.Source.File + ":" + entry.Source.Line
            : "";

        LightweaveNode body = HStack.Create(
            gap: new Rem(0.375f),
            children: row => {
                row.Add(Text.Create(
                    timestamp,
                    style: new Style { FontFamily = RenderContext.Current.Theme.GetFont(FontRole.Mono), FontSize = new Rem(0.6875f), TextColor = ThemeSlot.TextMuted }
                ), new Rem(4.5f).ToPixels());
                row.Add(Column.Create(
                    justify: FlexJustify.Center,
                    align: FlexAlign.Start,
                    children: cell => cell.Add(Text.Create(
                        levelLabel,
                        style: new Style {
                            FontFamily = RenderContext.Current.Theme.GetFont(FontRole.Mono),
                            FontSize = new Rem(0.5625f),
                            TextColor = levelSlot,
                            TextAlign = TextAlign.Center,
                            LetterSpacing = Tracking.Of(0.22f),
                            Border = BorderSpec.All(new Rem(1f / 16f), levelSlot),
                            Padding = new EdgeInsets(Top: new Rem(0.0625f), Bottom: new Rem(0.0625f), Left: new Rem(0.3125f), Right: new Rem(0.3125f)),
                        }
                    ))
                ), new Rem(3.75f).ToPixels());
                row.Add(Text.Create(
                    source,
                    style: new Style { FontFamily = RenderContext.Current.Theme.GetFont(FontRole.Mono), FontSize = new Rem(0.625f), TextColor = ThemeSlot.SurfaceAccent }
                ), new Rem(8.75f).ToPixels());
                row.AddFlex(Text.Create(
                    entry.RenderedMessage,
                    style: new Style { FontSize = new Rem(0.8125f), TextColor = ThemeSlot.TextPrimary }
                ));
            }
        );

        Style rowStyle = selected
            ? new Style {
                Background = BackgroundSpec.Of(ThemeSlot.ActiveTint),
                Border = new BorderSpec(Left: new Rem(2f / 16f), Color: ThemeSlot.SurfaceAccent),
            }
            : default;

        return Button.Create(
            label: string.Empty,
            onClick: () => {
                state.Selected = selected ? null : entry;
                invalidate();
            },
            variant: Variant.Ghost,
            ghost: true,
            body: body,
            style: rowStyle,
            id: "logviewer-row"
        );
    }

    private LightweaveNode BuildDetail(Action invalidate) {
        LogEntry? entry = state.Selected;
        if (entry == null) {
            return Column.Create(
                gap: new Rem(0.25f),
                align: FlexAlign.Center,
                justify: FlexJustify.Center,
                children: col => {
                    col.Add(Text.Create(
                        ((string)"CL_LogViewer_NoSelection".Translate()).ToUpperInvariant(),
                        style: new Style { FontFamily = RenderContext.Current.Theme.GetFont(FontRole.Mono), FontSize = new Rem(0.6875f), TextColor = ThemeSlot.TextMuted, LetterSpacing = Tracking.Of(0.22f) }
                    ));
                    col.Add(Text.Create(
                        (string)"CL_LogViewer_NoSelectionHint".Translate(),
                        style: new Style { FontSize = new Rem(0.8125f), FontWeight = FontStyle.Italic, TextColor = ThemeSlot.TextSecondary }
                    ));
                },
                style: Fill
            );
        }

        return Column.Create(
            gap: new Rem(0.5f),
            children: col => {
                col.Add(HStack.Create(
                    children: row => {
                        row.AddFlex(Text.Create(
                            (string)"CL_LogViewer_Detail_Crumb".Translate(),
                            style: new Style { FontSize = new Rem(0.6875f), TextColor = ThemeSlot.TextMuted }
                        ));
                        row.AddHug(IconButton.Create(
                            icon: Glyph.Create(Icons.Phosphor.X),
                            onClick: () => {
                                state.Selected = null;
                                invalidate();
                            },
                            variant: Variant.Ghost,
                            tooltipKey: "CL_LogViewer_Deselect",
                            id: "logviewer-deselect"
                        ));
                    }
                ));

                col.Add(Text.Create(
                    entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    style: new Style { FontFamily = RenderContext.Current.Theme.GetFont(FontRole.Mono), FontSize = new Rem(0.6875f), TextColor = ThemeSlot.TextMuted }
                ));

                col.Add(KeyValue.Create(
                    (string)"CL_LogViewer_Detail_Level".Translate(),
                    Text.Create(
                        entry.Level.ToString().ToUpperInvariant(),
                        style: new Style { TextColor = LogFilter.LevelSlot(entry.Level) }
                    )
                ));
                col.Add(KeyValue.Create(
                    (string)"CL_LogViewer_Detail_Channel".Translate(),
                    Text.Create(
                        entry.Channel,
                        style: new Style { TextColor = ThemeSlot.SurfaceAccent }
                    )
                ));
                col.Add(KeyValue.Create(
                    (string)"CL_LogViewer_Detail_Source".Translate(),
                    Text.Create(
                        entry.Source.IsCallerProvided ? entry.Source.File + ":" + entry.Source.Line : (string)"CL_LogViewer_Detail_NoSource".Translate(),
                        style: new Style { FontFamily = RenderContext.Current.Theme.GetFont(FontRole.Mono), FontSize = new Rem(0.75f), TextColor = ThemeSlot.TextSecondary }
                    )
                ));

                col.Add(Text.Create(
                    entry.RenderedMessage,
                    wrap: true,
                    style: new Style { TextColor = ThemeSlot.TextPrimary }
                ));

                if (entry.Context != null && entry.Context.Count > 0) {
                    foreach (KeyValuePair<string, object?> pair in entry.Context) {
                        col.Add(KeyValue.Create(
                            pair.Key,
                            Text.Create(
                                pair.Value?.ToString() ?? "null",
                                style: new Style { FontSize = new Rem(0.75f), TextColor = ThemeSlot.TextSecondary }
                            )
                        ));
                    }
                }

                string? trace = entry.StackTrace ?? entry.Exception?.ToString();
                if (trace != null) {
                    col.Add(Box.Create(
                        children: kids => kids.Add(Text.Create(
                            trace,
                            wrap: true,
                            style: new Style { FontFamily = RenderContext.Current.Theme.GetFont(FontRole.Mono), FontSize = new Rem(0.6875f), TextColor = ThemeSlot.TextMuted }
                        )),
                        style: new Style { Background = BackgroundSpec.Of(ThemeSlot.SurfaceSunken), Padding = EdgeInsets.All(new Rem(0.5f)), Radius = RadiusSpec.All(RadiusScale.Sm) }
                    ));
                }

                col.Add(Button.Create(
                    (string)"CL_LogViewer_Copy".Translate(),
                    onClick: () => {
                        GUIUtility.systemCopyBuffer = DefaultFormat.Render(DefaultFormat.Default, entry, true);
                    },
                    variant: Variant.Secondary,
                    id: "logviewer-copy"
                ));
            },
            style: Fill
        );
    }

    private const string PresetSaveSentinel = "*save*";
}
