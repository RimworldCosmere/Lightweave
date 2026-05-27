using System;
using Cosmere.Lightweave.Feedback;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Navigation;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using RimWorld;
using UnityEngine;
using Verse;

namespace Cosmere.Lightweave.Redesign.Settings;

public static class LightweaveRedesignSettingsForm {
    private static readonly Guid RootId = Guid.NewGuid();

    public static void Render(Rect inRect) {
        LightweaveRoot.Render(inRect, RootId, Build);
    }

    public static LightweaveNode Build() {
        LightweaveRedesignSettings settings = LightweaveRedesignMod.Settings;
        Hooks.Hooks.StateHandle<RedesignCategory> activeTab = Hooks.Hooks.UseState(RedesignCategory.MainMenu);
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

                stack.Add(Tabs.Create<RedesignCategory>(
                    value: activeTab.Value,
                    items: new[] { RedesignCategory.MainMenu },
                    labelFn: cat => FormatTabLabel(cat),
                    onChange: cat => activeTab.Set(cat),
                    bodyFn: cat => cat switch {
                        RedesignCategory.MainMenu => BuildMainMenuSection(settings),
                        _ => Stack.Create(SpacingScale.None, _ => {}),
                    }
                ));
            }
        );
    }

    private enum RedesignCategory {
        MainMenu,
    }

    private static string FormatTabLabel(RedesignCategory cat) {
        string baseLabel = cat switch {
            RedesignCategory.MainMenu => (string)"CL_RedesignSettings_Tab_MainMenu".Translate(),
            _ => string.Empty,
        };
        return baseLabel.ToUpperInvariant();
    }

    public static void ResetAll(LightweaveRedesignSettings s) {
        s.RedesignMainMenu = true;
        s.ParseSaveMetadata = true;
    }

    private static int CountDiffs(LightweaveRedesignSettings s) {
        int n = 0;
        if (!s.RedesignMainMenu) n++;
        if (!s.ParseSaveMetadata) n++;
        return n;
    }

    private static string FormatDefaultTag(string value) {
        return ((string)"CL_Settings_Default_Tag".Translate()).Replace("{VALUE}", value);
    }

    private static LightweaveNode BuildMainMenuSection(LightweaveRedesignSettings settings) {
        string onLabel = (string)"CL_RedesignSettings_Default_On".Translate();
        string defaultTag = FormatDefaultTag(onLabel);
        string editedLabel = (string)"CL_Settings_Edited".Translate();
        return SettingsSection.Create(
            title: (string)"CL_RedesignSettings_Section_MainMenu".Translate(),
            description: (string)"CL_RedesignSettings_Section_MainMenu_Caption".Translate(),
            rows: r => {
                r.Add(SettingRow.Create(
                    label: (string)"CL_Settings_MainMenu_Redesign".Translate(),
                    description: (string)"CL_Settings_MainMenu_Redesign_Tip".Translate(),
                    defaultValue: defaultTag,
                    edited: !settings.RedesignMainMenu,
                    editedLabel: editedLabel,
                    control: Checkbox.Create(
                        label: (string)"CL_Settings_MainMenu_Redesign".Translate(),
                        value: settings.RedesignMainMenu,
                        onChange: v => {
                            settings.RedesignMainMenu = v;
                            LightweaveRedesignMod.Save();
                            PromptRestartIfBootDiff(settings);
                        },
                        tooltipKey: "CL_Settings_MainMenu_Redesign_Tip"
                    )
                ));
                r.Add(SettingRow.Create(
                    label: (string)"CL_Settings_MainMenu_ParseSaves".Translate(),
                    description: (string)"CL_Settings_MainMenu_ParseSaves_Tip".Translate(),
                    defaultValue: defaultTag,
                    edited: !settings.ParseSaveMetadata,
                    editedLabel: editedLabel,
                    control: Checkbox.Create(
                        label: (string)"CL_Settings_MainMenu_ParseSaves".Translate(),
                        value: settings.ParseSaveMetadata,
                        onChange: v => {
                            settings.ParseSaveMetadata = v;
                            LightweaveRedesignMod.Save();
                        },
                        disabled: !settings.RedesignMainMenu,
                        tooltipKey: "CL_Settings_MainMenu_ParseSaves_Tip"
                    )
                ));
            }
        );
    }

    private static void PromptRestartIfBootDiff(LightweaveRedesignSettings settings) {
        if (settings.RedesignMainMenu == LightweaveRedesignMod.BootRedesignMainMenu) {
            return;
        }
        Find.WindowStack.Add(new Dialog_MessageBox(
            "CL_Settings_Restart_Body".Translate(),
            "CL_Settings_Restart_Confirm".Translate(),
            () => GenCommandLine.Restart(),
            "CL_Settings_Restart_Later".Translate(),
            null,
            "CL_Settings_Restart_Title".Translate()
        ));
    }
}
