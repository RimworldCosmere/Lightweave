using System;
using Concord;
using Cosmere.Lightweave.Redesign.Publish;
using Cosmere.Lightweave.Redesign.Settings;
using RimWorld;
using Verse;

namespace Cosmere.Lightweave.Redesign.Patch;

/// <summary>
/// Redirects the stock "Upload to Workshop" confirm dialog into the Lightweave publish flow.
/// Leaves vanilla's confirmation prompt intact but, when the mod is locally editable, swaps its
/// accept action to open <see cref="Dialog_PublishToWorkshop"/> instead of the all-or-nothing
/// vanilla upload. Gated on the main-menu redesign being active.
/// </summary>
[Patch]
public abstract class Dialog_ConfirmModUploadPatch : Dialog_ConfirmModUpload {
    protected Dialog_ConfirmModUploadPatch(ModMetaData mod, Action acceptAction) : base(mod, acceptAction) { }

    [Inject(At.Tail, parameterTypes: [typeof(ModMetaData), typeof(Action)])]
    public void Postfix(ModMetaData mod) {
        LightweaveRedesignSettings? settings = LightweaveRedesignMod.Settings;
        if (settings == null || !settings.RedesignMainMenu) {
            return;
        }

        if (!PublishGate.CanPublish(mod)) {
            return;
        }

        Dialog_ConfirmModUpload instance = this;
        Action redirect = () => {
            Find.WindowStack.TryRemove(instance, false);
            Find.WindowStack.Add(new Dialog_PublishToWorkshop(mod));
        };

        instance.acceptAction = redirect;
        instance.buttonAAction = redirect;
    }
}
