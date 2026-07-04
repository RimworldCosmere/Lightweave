using Concord;
using RimWorld;
using Verse;

namespace Cosmere.Lightweave.Patch;

[Patch]
public abstract class UIRoot_Entry_OverlayPatch : UIRoot_Entry {
    [Inject(At.Head, nameof(UIRootOnGUI))]
    public void Prefix(ControlHandle ch) {
        PerformanceOverlay.HandleToggleHotkey();

        if (UIRootDragSkip.ShouldSkip()) {
            UIRootDragSkip.RunMinimalDispatch();
            ch.Cancel();
        }
    }

    [Inject(At.Tail, nameof(UIRootOnGUI))]
    public void Postfix(ControlHandle ch) {
        PerformanceOverlay.Draw();
    }
}

[Patch]
public abstract class UIRoot_Play_OverlayPatch : UIRoot_Play {
    [Inject(At.Head, nameof(UIRootOnGUI))]
    public void Prefix(ControlHandle ch) {
        PerformanceOverlay.HandleToggleHotkey();

        if (UIRootDragSkip.ShouldSkip()) {
            UIRootDragSkip.RunMinimalDispatch();
            ch.Cancel();
        }
    }

    [Inject(At.Tail, nameof(UIRootOnGUI))]
    public void Postfix(ControlHandle ch) {
        PerformanceOverlay.Draw();
    }
}
