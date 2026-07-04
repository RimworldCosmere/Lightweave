using Concord;
using Cosmere.Lightweave.Redesign.NewColony;
using Verse;

namespace Cosmere.Lightweave.Redesign.Patch;

// While the New Colony window owns a generated world, keep Current.Game alive WITHOUT
// putting the global world renderer into Planet mode. Vanilla UIRoot_Entry.DoMainMenu nulls
// Current.Game every frame the menu runs (guarded only by ShouldDoMainMenu), so a freshly
// generated world would be wiped before the embedded WorldPreview could draw it.
//
// Forcing ShouldDoMainMenu false here protects the game and skips the (window-covered)
// main-menu draw, while wantedMode stays None so the WorldCamera never auto-renders the
// planet full-screen behind the window — the globe lives only in the preview's RenderTexture.
// On close, NewColonyLauncher clears OwnsWorld and the normal menu loop resumes.
[Patch]
public abstract class NewColonyMenuPersistencePatch : UIRoot_Entry {
    [Inject(At.Head, "get_ShouldDoMainMenu")]
    public void Prefix(ControlHandle<bool> ch) {
        if (!NewColonyLauncher.OwnsWorld) {
            return;
        }

        ch.ReturnValue = false;
        ch.Cancel();
    }
}
