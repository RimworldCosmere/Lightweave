using System;
using System.IO;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using RimWorld;
using UnityEngine;
using Verse;

namespace Cosmere.Lightweave.Redesign.Options.Tabs;

public static class DeveloperTab {
    public static LightweaveNode Build() {
        return Stack.Create(SpacingScale.Lg, s => {
            s.Add(SettingRow.Section("CL_Options_Section_Developer",
                SettingRow.Create(
                    "CL_Options_TestMapSizes".Translate(),
                    Switch.Create("", Prefs.TestMapSizes, v => Prefs.TestMapSizes = v, variant: Variant.Secondary)
                ),
                SettingRow.Create(
                    "CL_Options_LogVerbose".Translate(),
                    Switch.Create("", Prefs.LogVerbose, v => Prefs.LogVerbose = v, variant: Variant.Secondary)
                ),
                SettingRow.Create(
                    "CL_Options_ResetModsConfigOnCrash".Translate(),
                    Switch.Create("", Prefs.ResetModsConfigOnCrash, v => Prefs.ResetModsConfigOnCrash = v, variant: Variant.Secondary)
                ),
                SettingRow.Create(
                    "CL_Options_DisableQuickStart".Translate(),
                    Switch.Create("", Prefs.DisableQuickStartCryptoSickness, v => Prefs.DisableQuickStartCryptoSickness = v, variant: Variant.Secondary)
                ),
                SettingRow.Create(
                    "CL_Options_StartDevPalette".Translate(),
                    Switch.Create("", Prefs.StartDevPaletteOn, v => Prefs.StartDevPaletteOn = v, variant: Variant.Secondary)
                ),
                SettingRow.Create(
                    "CL_Options_OpenLogOnWarnings".Translate(),
                    Switch.Create("", Prefs.OpenLogOnWarnings, v => Prefs.OpenLogOnWarnings = v, variant: Variant.Secondary)
                ),
                SettingRow.Create(
                    "CL_Options_CloseLogOnEsc".Translate(),
                    Switch.Create("", Prefs.CloseLogWindowOnEscape, v => Prefs.CloseLogWindowOnEscape = v, variant: Variant.Secondary)
                ),
                SettingRow.Create(
                    "CL_Options_DisableDevMode".Translate(),
                    Button.Create(
                        label: "CL_Options_DisableDevMode_Action".Translate(),
                        onClick: () => Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                            "ConfirmPermanentlyDisableDevMode".Translate(),
                            DevModePermanentlyDisabledUtility.Disable,
                            destructive: true
                        )),
                        variant: Variant.Danger,
                        disabled: DevModePermanentlyDisabledUtility.Disabled
                    )
                )
            ));
        });
    }

    

    
}
