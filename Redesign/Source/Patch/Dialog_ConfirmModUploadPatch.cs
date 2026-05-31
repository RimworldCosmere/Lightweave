using System;
using Cosmere.Lightweave.Redesign.Publish;
using Cosmere.Lightweave.Redesign.Settings;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Cosmere.Lightweave.Redesign.Patch;

/// <summary>
/// Redirects the stock "Upload to Workshop" confirm dialog into the Lightweave publish flow.
/// Leaves vanilla's confirmation prompt intact but, when the mod is locally editable, swaps its
/// accept action to open <see cref="Dialog_PublishToWorkshop"/> instead of the all-or-nothing
/// vanilla upload. Gated on the main-menu redesign being active.
/// </summary>
[HarmonyPatch(typeof(Dialog_ConfirmModUpload), MethodType.Constructor, [typeof(ModMetaData), typeof(Action)])]
public static class Dialog_ConfirmModUploadPatch {
    public static void Postfix(Dialog_ConfirmModUpload __instance, ModMetaData mod) {
        LightweaveRedesignSettings? settings = LightweaveRedesignMod.Settings;
        if (settings == null || !settings.RedesignMainMenu) {
            return;
        }

        if (!PublishGate.CanPublish(mod)) {
            return;
        }

        Action redirect = () => {
            Find.WindowStack.TryRemove(__instance, false);
            Find.WindowStack.Add(new Dialog_PublishToWorkshop(mod));
        };

        __instance.acceptAction = redirect;
        __instance.buttonAAction = redirect;
    }
}
