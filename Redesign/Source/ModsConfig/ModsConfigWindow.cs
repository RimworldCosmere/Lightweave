using System;
using Cosmere.Lightweave.Runtime;
using RimWorld;
using Verse;

namespace Cosmere.Lightweave.Redesign.ModsConfig;

internal sealed class ModsConfigWindow : LightweaveWindow {
    private readonly Page_ModsConfig inner;
    private bool innerPreOpened;

    public ModsConfigWindow() : this(new Page_ModsConfig()) { }

    public ModsConfigWindow(Page_ModsConfig existing) {
        inner = existing;
    }

    public override void PreOpen() {
        base.PreOpen();
        if (!innerPreOpened) {
            inner.PreOpen();
            innerPreOpened = true;
        }
    }

    public override void OnCancelKeyPressed() {
        ModsConfigState.SetDiscardChanges(inner, true);
        base.OnCancelKeyPressed();
    }

    public override void PostClose() {
        inner.PostClose();
        base.PostClose();
    }

    protected override LightweaveNode Body() {
        return ModsConfigRoot.Build(inner, () => Close());
    }
}
