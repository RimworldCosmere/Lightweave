using System;
using System.Collections.Generic;
using System.Linq;
using Cosmere.Lightweave.Fonts;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Theme;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;
using Caption = Cosmere.Lightweave.Typography.Typography.Caption;
using Heading = Cosmere.Lightweave.Typography.Typography.Heading;

namespace Cosmere.Lightweave.Settings;

public static class LightweaveSettingsForm {
    private static readonly Guid RootId = Guid.NewGuid();

    public static void Render(Rect inRect) {
        LightweaveRoot.Render(inRect, RootId, Build);
    }

    public static LightweaveNode Build() {
        LightweaveSettings settings = LightweaveMod.Settings;

        return Stack.Create(
            new Rem(1.25f),
            stack => {
                stack.Add(Heading.Create(2, "CL_Settings_Title".Translate()));
                stack.Add(Caption.Create("CL_Settings_Subtitle".Translate()));

                stack.Add(BuildThemeSection(settings));
                stack.Add(Divider.Horizontal());

                stack.Add(BuildFontSizeSection(settings));
                stack.Add(Divider.Horizontal());

                stack.Add(BuildAccessibilitySection(settings));
                stack.Add(Divider.Horizontal());

                stack.Add(BuildDiagnosticsSection(settings));
            }
        );
    }

    private static LightweaveNode BuildFontSizeSection(LightweaveSettings settings) {
        return Stack.Create(
            new Rem(0.5f),
            section => {
                section.Add(Heading.Create(3, "CL_Settings_FontSize_Heading".Translate()));
                section.Add(Caption.Create("CL_Settings_FontSize_Help".Translate()));
                section.Add(Slider.Create(
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
                ));
            }
        );
    }

    private static LightweaveNode BuildThemeSection(LightweaveSettings settings) {
        IReadOnlyList<ThemeDescriptor> themes = ThemeRegistry.All;
        ThemeDescriptor current = themes.FirstOrDefault(t => string.Equals(t.Id, settings.SelectedThemeId, StringComparison.Ordinal))
            ?? themes.FirstOrDefault(t => string.Equals(t.Id, ThemeRegistry.DefaultId, StringComparison.Ordinal))
            ?? (themes.Count > 0 ? themes[0] : null!);
        return Stack.Create(
            new Rem(0.5f),
            section => {
                section.Add(Heading.Create(3, "CL_Settings_Theme_Heading".Translate()));
                section.Add(Caption.Create("CL_Settings_Theme_Help".Translate()));
                if (current == null || themes.Count == 0) {
                    section.Add(Caption.Create("CL_Settings_Theme_None".Translate()));
                    return;
                }
                section.Add(Dropdown.Create<ThemeDescriptor>(
                    value: current,
                    options: themes,
                    labelFn: d => (string)d.LabelKey.Translate(),
                    onChange: d => {
                        if (d == null || string.Equals(d.Id, settings.SelectedThemeId, StringComparison.Ordinal)) {
                            return;
                        }
                        settings.SelectedThemeId = d.Id;
                        LightweaveMod.Save();
                    },
                    inputVariant: Variant.Secondary,
                    buttonStyle: Variant.Secondary
                ));
            }
        );
    }

    private static LightweaveNode BuildAccessibilitySection(LightweaveSettings settings) {
        return Stack.Create(
            new Rem(0.5f),
            section => {
                section.Add(Heading.Create(3, "CL_Settings_Accessibility_Heading".Translate()));
                section.Add(Checkbox.Create(
                    label: "CL_Settings_ReduceMotion".Translate(),
                    value: settings.ReduceMotion,
                    onChange: v => {
                        settings.ReduceMotion = v;
                        LightweaveMod.Save();
                    },
                    tooltipKey: "CL_Settings_ReduceMotion_Tip"
                ));
            }
        );
    }

    private static LightweaveNode BuildDiagnosticsSection(LightweaveSettings settings) {
        return Stack.Create(
            new Rem(0.5f),
            section => {
                section.Add(Heading.Create(3, "CL_Settings_Diagnostics_Heading".Translate()));
                section.Add(Checkbox.Create(
                    label: "CL_Settings_PerfOverlay".Translate(),
                    value: settings.ShowPerformanceMetrics,
                    onChange: v => {
                        settings.ShowPerformanceMetrics = v;
                        LightweaveMod.Save();
                    },
                    tooltipKey: "CL_Settings_PerfOverlay_Tip"
                ));
                section.Add(Checkbox.Create(
                    label: "CL_Settings_RenderDiagnostics".Translate(),
                    value: settings.RenderDiagnostics,
                    onChange: v => {
                        settings.RenderDiagnostics = v;
                        LightweaveMod.Save();
                        DiagnosticsLevelController.Sync(v);
                    },
                    tooltipKey: "CL_Settings_RenderDiagnostics_Tip"
                ));
            }
        );
    }
}
