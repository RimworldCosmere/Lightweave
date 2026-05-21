using System;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using RimWorld;
using UnityEngine;
using Verse;
using Caption = Cosmere.Lightweave.Typography.Typography.Caption;
using Heading = Cosmere.Lightweave.Typography.Typography.Heading;

namespace Cosmere.Lightweave.Settings;

public static class LightweaveRedesignSettingsForm {
    private static readonly Guid RootId = Guid.NewGuid();

    public static void Render(Rect inRect) {
        LightweaveRoot.Render(inRect, RootId, Build);
    }

    private static LightweaveNode Build() {
        LightweaveRedesignSettings settings = LightweaveRedesignMod.Settings;

        return Stack.Create(
            new Rem(1.25f),
            stack => {
                stack.Add(Heading.Create(2, "CL_RedesignSettings_Title".Translate()));
                stack.Add(Caption.Create("CL_RedesignSettings_Subtitle".Translate()));

                stack.Add(BuildMainMenuSection(settings));
            }
        );
    }

    private static LightweaveNode BuildMainMenuSection(LightweaveRedesignSettings settings) {
        return Stack.Create(
            new Rem(0.5f),
            section => {
                section.Add(Heading.Create(3, "CL_Settings_MainMenu_Heading".Translate()));
                section.Add(Checkbox.Create(
                    label: "CL_Settings_MainMenu_Redesign".Translate(),
                    value: settings.RedesignMainMenu,
                    onChange: v => {
                        settings.RedesignMainMenu = v;
                        LightweaveRedesignMod.Save();
                        PromptRestartIfBootDiff(settings);
                    },
                    tooltipKey: "CL_Settings_MainMenu_Redesign_Tip"
                ));
                section.Add(Checkbox.Create(
                    label: "CL_Settings_MainMenu_ParseSaves".Translate(),
                    value: settings.ParseSaveMetadata,
                    onChange: v => {
                        settings.ParseSaveMetadata = v;
                        LightweaveRedesignMod.Save();
                    },
                    disabled: !settings.RedesignMainMenu,
                    tooltipKey: "CL_Settings_MainMenu_ParseSaves_Tip"
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
