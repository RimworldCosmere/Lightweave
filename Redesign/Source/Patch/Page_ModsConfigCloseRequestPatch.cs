using Concord;
using Cosmere.Lightweave.Redesign.Settings;
using Cosmere.Lightweave.Redesign.ModsConfig;
using RimWorld;
using Verse;

namespace Cosmere.Lightweave.Redesign.Patch;

[Patch]
public abstract class Page_ModsConfigCloseRequestPatch : Page_ModsConfig {
    [Inject(At.Head, nameof(OnCloseRequest))]
    public void Prefix(ControlHandle<bool> ch) {
        LightweaveRedesignSettings? settings = LightweaveRedesignMod.Settings;
        if (settings == null || !settings.RedesignMainMenu) {
            return;
        }

        Page_ModsConfig self = this;
        if (ModsConfigState.GetSaveChanges(self) || ModsConfigState.GetDiscardChanges(self)) {
            ch.ReturnValue = true;
            ch.Cancel();
            return;
        }

        if (!ModsConfigState.HasUnsavedChanges(self)) {
            ModsConfigState.SetDiscardChanges(self, true);
            ch.ReturnValue = true;
            ch.Cancel();
            return;
        }

        Find.WindowStack.Add(new Dialog_ModsConfigConfirmClose(
            onSave: () => {
                ModsConfigState.SetSaveChanges(self, true);
                self.Close();
            },
            onDiscard: () => {
                ModsConfigState.SetDiscardChanges(self, true);
                self.Close();
            }
        ));
        ch.ReturnValue = false;
        ch.Cancel();
    }
}
