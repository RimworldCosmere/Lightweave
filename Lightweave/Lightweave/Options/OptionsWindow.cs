using System;
using Cosmere.Lightweave.Runtime;
using RimWorld;
using Verse;

namespace Cosmere.Lightweave.Options;

internal sealed class OptionsWindow : LightweaveWindow {
    private readonly Dialog_Options inner;

    public OptionsWindow() : this(new Dialog_Options()) { }

    public OptionsWindow(Dialog_Options existing) {
        inner = existing;
    }

    protected override LightweaveNode Body() {
        return OptionsRoot.Build(inner, () => Close());
    }

    public override void PostClose() {
        Prefs.Save();
        base.PostClose();
    }
}
