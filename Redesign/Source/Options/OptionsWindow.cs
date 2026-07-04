using System;
using Cosmere.Lightweave.Runtime;
using RimWorld;
using Verse;

namespace Cosmere.Lightweave.Redesign.Options;

internal sealed class OptionsWindow : LightweaveWindow {
    private readonly Dialog_Options inner;

    public OptionsWindow() : this(new Dialog_Options()) { }

    public OptionsWindow(Dialog_Options existing) {
        inner = existing;
    }

    // Match the Playground's full-bleed initial size rather than the default capped card.
    public override UnityEngine.Vector2 InitialSize =>
        new UnityEngine.Vector2(UI.screenWidth * 0.85f, UI.screenHeight * 0.9f);

    protected override LightweaveNode Body() {
        return OptionsRoot.Build(inner, () => Close());
    }

    public override void PostClose() {
        Prefs.Save();
        base.PostClose();
    }
}
