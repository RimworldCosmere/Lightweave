using Concord;
using Cosmere.Lightweave.Redesign.Settings;
using System.Collections.Generic;
using Cosmere.Lightweave.Fonts;
using Cosmere.Lightweave.Settings;
using RimWorld;
using UnityEngine;
using Verse;

namespace Cosmere.Lightweave.Redesign.Patch;

[Patch]
public abstract class Dialog_OptionsFontSizePatch : Dialog_Options {
    private static readonly int[] Presets = { 85, 100, 115, 125 };

    [Inject(At.Head, "DoUIOptions")]
    public void Prefix(Listing_Standard listing) {
        LightweaveRedesignSettings? redesign = LightweaveRedesignMod.Settings;
        if (redesign != null && redesign.RedesignMainMenu) {
            return;
        }
        DrawFontSizeRow(listing);
    }

    private static void DrawFontSizeRow(Listing_Standard listing) {
        if (listing == null) {
            return;
        }

        LightweaveSettings settings = LightweaveMod.Settings;
        if (settings == null) {
            return;
        }

        string label = "CL_FontSize".Translate();
        string current = settings.FontScalePercent + "%";
        if (!listing.ButtonTextLabeledPct(label, current, 0.6f, TextAnchor.MiddleLeft, null, null, null)) {
            return;
        }

        List<FloatMenuOption> options = new List<FloatMenuOption>(Presets.Length);
        for (int i = 0; i < Presets.Length; i++) {
            int captured = Presets[i];
            options.Add(new FloatMenuOption(captured + "%", () => {
                settings.FontScalePercent = captured;
                LightweaveMod.Save();
                GameFontOverride.Apply();
            }));
        }
        Find.WindowStack.Add(new FloatMenu(options));
    }
}
