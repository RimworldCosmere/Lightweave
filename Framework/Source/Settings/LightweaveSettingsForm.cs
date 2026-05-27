using System;
using System.Collections.Generic;
using System.Linq;
using Cosmere.Lightweave.Feedback;
using Cosmere.Lightweave.Fonts;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Navigation;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Theme;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;
using Caption = Cosmere.Lightweave.Typography.Typography.Caption;
using Heading = Cosmere.Lightweave.Typography.Typography.Heading;
using Text = Cosmere.Lightweave.Typography.Typography.Text;

namespace Cosmere.Lightweave.Settings;

public static class LightweaveSettingsForm {
    private static readonly Guid RootId = Guid.NewGuid();

    public static void Render(Rect inRect) {
        LightweaveRoot.Render(inRect, RootId, Build);
    }

    public static LightweaveNode Build() {
        LightweaveSettings settings = LightweaveMod.Settings;
        Hooks.Hooks.StateHandle<SettingsCategory> activeTab = Hooks.Hooks.UseState(SettingsCategory.Display);
        int diffs = CountDiffs(settings);

        return Stack.Create(
            SpacingScale.None,
            stack => {
                if (diffs > 0) {
                    string diffLabel = diffs == 1
                        ? (string)"CL_Settings_Diff_One".Translate()
                        : (string)"CL_Settings_Diff_Many".Translate(diffs.Named("COUNT"));
                    stack.Add(DiffBanner.Create(label: diffLabel));
                }

                stack.Add(Tabs.Create<SettingsCategory>(
                    value: activeTab.Value,
                    items: new[] {
                        SettingsCategory.Display,
                        SettingsCategory.Typography,
                        SettingsCategory.Motion,
                        SettingsCategory.Misc,
                    },
                    labelFn: cat => FormatTabLabel(cat, settings),
                    onChange: cat => activeTab.Set(cat),
                    bodyFn: cat => cat switch {
                        SettingsCategory.Display => BuildThemeSection(settings),
                        SettingsCategory.Typography => BuildFontSizeSection(settings),
                        SettingsCategory.Motion => BuildAccessibilitySection(settings),
                        SettingsCategory.Misc => BuildDiagnosticsSection(settings),
                        _ => Stack.Create(SpacingScale.None, _ => {}),
                    }
                ));
            }
        );
    }

    private static string FormatTabLabel(SettingsCategory cat, LightweaveSettings settings) {
        string baseLabel = cat switch {
            SettingsCategory.Display => (string)"CL_Settings_Tab_Display".Translate(),
            SettingsCategory.Typography => (string)"CL_Settings_Tab_Typography".Translate(),
            SettingsCategory.Motion => (string)"CL_Settings_Tab_Motion".Translate(),
            SettingsCategory.Misc => (string)"CL_Settings_Tab_Misc".Translate(),
            _ => string.Empty,
        };
        return baseLabel.ToUpperInvariant();
    }

    private static int CountDiffsForCategory(SettingsCategory cat, LightweaveSettings s) {
        return cat switch {
            SettingsCategory.Display => IsThemeEdited(s) ? 1 : 0,
            SettingsCategory.Typography => s.FontScalePercent != 100 ? 1 : 0,
            SettingsCategory.Motion => s.ReduceMotion ? 1 : 0,
            SettingsCategory.Misc => (s.ShowPerformanceMetrics ? 1 : 0) + (s.RenderDiagnostics ? 1 : 0),
            _ => 0,
        };
    }

    private static bool IsThemeEdited(LightweaveSettings s) {
        return !string.Equals(s.SelectedThemeId, "default", StringComparison.Ordinal);
    }

    private enum SettingsCategory {
        Display,
        Typography,
        Motion,
        Misc,
    }

    private static int CountDiffs(LightweaveSettings s) {
        int n = 0;
        if (s.FontScalePercent != 100) n++;
        if (s.ReduceMotion) n++;
        if (!string.Equals(s.SelectedThemeId, "default", StringComparison.Ordinal)) n++;
        if (s.ShowPerformanceMetrics) n++;
        if (s.RenderDiagnostics) n++;
        return n;
    }

    public static void ResetAll(LightweaveSettings s) {
        s.FontScalePercent = 100;
        s.ReduceMotion = false;
        s.SelectedThemeId = "default";
        s.ShowPerformanceMetrics = false;
        s.RenderDiagnostics = false;
        GameFontOverride.Apply();
        DiagnosticsLevelController.Sync(false);
    }

    private static string FormatDefaultTag(string value) {
        return ((string)"CL_Settings_Default_Tag".Translate()).Replace("{VALUE}", value);
    }

    private static LightweaveNode BuildFontSizeSection(LightweaveSettings settings) {
        bool edited = settings.FontScalePercent != 100;
        return SettingsSection.Create(
            title: (string)"CL_Settings_Section_Typography".Translate(),
            description: (string)"CL_Settings_Section_Typography_Caption".Translate(),
            rows: r => {
                r.Add(SettingRow.Create(
                    label: (string)"CL_Settings_FontSize_Heading".Translate(),
                    description: (string)"CL_Settings_FontSize_Help".Translate(),
                    defaultValue: FormatDefaultTag("100%"),
                    edited: edited,
                    editedLabel: (string)"CL_Settings_Edited".Translate(),
                    control: Slider.Create(
                        value: settings.FontScalePercent,
                        onChange: v => {
                            int snapped = Mathf.RoundToInt(v);
                            if (snapped == settings.FontScalePercent) {
                                return;
                            }
                            settings.FontScalePercent = snapped;
                            LightweaveMod.Save();
                            GameFontOverride.Apply();
                        },
                        min: 75f,
                        max: 150f,
                        step: 5f,
                        marks: new[] { 85f, 100f, 115f, 125f },
                        format: v => Mathf.RoundToInt(v) + "%"
                    )
                ));
            }
        );
    }

    private static LightweaveNode BuildThemeSection(LightweaveSettings settings) {
        IReadOnlyList<ThemeDescriptor> themes = ThemeRegistry.All;
        ThemeDescriptor current = themes.FirstOrDefault(t => string.Equals(t.Id, settings.SelectedThemeId, StringComparison.Ordinal))
                                  ?? themes.FirstOrDefault();
        ThemeDescriptor defaultTheme = themes.FirstOrDefault(t => string.Equals(t.Id, "default", StringComparison.Ordinal))
                                       ?? themes.FirstOrDefault();
        string defaultLabel = defaultTheme != null ? ((string)defaultTheme.LabelKey.Translate()).ToLowerInvariant() : string.Empty;

        return SettingsSection.Create(
            title: (string)"CL_Settings_Section_Display".Translate(),
            description: (string)"CL_Settings_Section_Display_Caption".Translate(),
            rows: r => {
                if (current == null || themes.Count == 0) {
                    r.Add(Text.Create((string)"CL_Settings_Theme_None".Translate()));
                    return;
                }
                r.Add(SettingRow.Create(
                    label: (string)"CL_Settings_Theme_Heading".Translate(),
                    description: (string)"CL_Settings_Theme_Help".Translate(),
                    defaultValue: FormatDefaultTag(defaultLabel),
                    edited: IsThemeEdited(settings),
                    editedLabel: (string)"CL_Settings_Edited".Translate(),
                    control: Dropdown.Create<ThemeDescriptor>(
                        value: current,
                        options: themes,
                        labelFn: d => ((string)d.LabelKey.Translate()).ToLowerInvariant(),
                        onChange: d => {
                            settings.SelectedThemeId = d.Id;
                            LightweaveMod.Save();
                        },
                        inputVariant: Variant.Secondary,
                        buttonStyle: Variant.Secondary
                    )
                ));
            }
        );
    }

    private static LightweaveNode BuildAccessibilitySection(LightweaveSettings settings) {
        string offLabel = (string)"CL_Settings_Default_Off".Translate();
        return SettingsSection.Create(
            title: (string)"CL_Settings_Section_Motion".Translate(),
            description: (string)"CL_Settings_Section_Motion_Caption".Translate(),
            rows: r => {
                r.Add(SettingRow.Create(
                    label: (string)"CL_Settings_ReduceMotion".Translate(),
                    description: (string)"CL_Settings_ReduceMotion_Tip".Translate(),
                    defaultValue: FormatDefaultTag(offLabel),
                    edited: settings.ReduceMotion,
                    editedLabel: (string)"CL_Settings_Edited".Translate(),
                    control: Checkbox.Create(
                        label: (string)"CL_Settings_ReduceMotion".Translate(),
                        value: settings.ReduceMotion,
                        onChange: v => {
                            settings.ReduceMotion = v;
                            LightweaveMod.Save();
                        },
                        tooltipKey: "CL_Settings_ReduceMotion_Tip"
                    )
                ));
            }
        );
    }

    private static LightweaveNode BuildDiagnosticsSection(LightweaveSettings settings) {
        string offLabel = (string)"CL_Settings_Default_Off".Translate();
        string defaultTag = FormatDefaultTag(offLabel);
        string editedLabel = (string)"CL_Settings_Edited".Translate();
        return SettingsSection.Create(
            title: (string)"CL_Settings_Section_Misc".Translate(),
            description: (string)"CL_Settings_Section_Misc_Caption".Translate(),
            rows: r => {
                r.Add(SettingRow.Create(
                    label: (string)"CL_Settings_PerfOverlay".Translate(),
                    description: (string)"CL_Settings_PerfOverlay_Tip".Translate(),
                    defaultValue: defaultTag,
                    edited: settings.ShowPerformanceMetrics,
                    editedLabel: editedLabel,
                    control: Checkbox.Create(
                        label: (string)"CL_Settings_PerfOverlay".Translate(),
                        value: settings.ShowPerformanceMetrics,
                        onChange: v => {
                            settings.ShowPerformanceMetrics = v;
                            LightweaveMod.Save();
                        },
                        tooltipKey: "CL_Settings_PerfOverlay_Tip"
                    )
                ));
                r.Add(SettingRow.Create(
                    label: (string)"CL_Settings_RenderDiagnostics".Translate(),
                    description: (string)"CL_Settings_RenderDiagnostics_Tip".Translate(),
                    defaultValue: defaultTag,
                    edited: settings.RenderDiagnostics,
                    editedLabel: editedLabel,
                    control: Checkbox.Create(
                        label: (string)"CL_Settings_RenderDiagnostics".Translate(),
                        value: settings.RenderDiagnostics,
                        onChange: v => {
                            settings.RenderDiagnostics = v;
                            LightweaveMod.Save();
                            DiagnosticsLevelController.Sync(v);
                        },
                        tooltipKey: "CL_Settings_RenderDiagnostics_Tip"
                    )
                ));
            }
        );
    }
}

