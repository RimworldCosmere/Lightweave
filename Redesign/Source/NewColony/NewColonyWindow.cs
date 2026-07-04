using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;

namespace Cosmere.Lightweave.Redesign.NewColony;

internal sealed class NewColonyWindow : LightweaveWindow {
    private bool lastGenerating;

    protected override float WidthFraction => 1f;
    protected override float HeightFraction => 1f;
    protected override float MaxCardWidth => 99999f;
    protected override float MaxCardHeight => 99999f;
    protected override bool EdgeResizable => false;
    protected override RadiusSpec? CardRadius => RadiusSpec.All(RadiusScale.None);
    protected override EdgeInsets? CardPadding => EdgeInsets.All(new Rem(0f));
    protected override bool DrawAccentGradient => false;
    // Theme-toned backdrop over the dimmed main-menu splash: MenuVignette is paper on the light
    // themes (cosmere/roshar) so the header text reads, and dark on the dark themes. Its <1 alpha
    // lets the splash show through faintly as the backdrop photo. Body/footer get their own panels.
    protected override BackgroundSpec? CardBackground => BackgroundSpec.Of(ThemeSlot.MenuVignette);

    public override void DoWindowContents(Rect inRect) {
        NewColonyLauncher.PumpPendingGen();
        if (NewColonyLauncher.Generating != lastGenerating) {
            lastGenerating = NewColonyLauncher.Generating;
            LightweaveRoot.Invalidate(RootId);
        }
        base.DoWindowContents(inRect);
    }

    public override void PostClose() {
        base.PostClose();
        // The ambience preview is a frame-maintained sustainer; stop it on any close path so it does not
        // keep playing after the window is gone.
        AmbiencePreview.Stop();
        // Runs for every close path (Escape, click-away, headline/commit buttons), unlike the
        // Root's close callback which only the buttons invoke. Without this, an Escape-close
        // leaves NewColonyLauncher.OwnsWorld true, so NewColonyMenuPersistencePatch keeps
        // Current.Game alive and suppresses the main-menu draw. Committed closes skip teardown
        // because the game is about to start.
        if (!NewColonyLauncher.Committed) {
            NewColonyLauncher.Teardown();
        }
    }


    public override void WindowUpdate() {
        base.WindowUpdate();
        // Per-frame game-loop hook (unlike the render path, which only fires on input events). The
        // ambience preview sustainer needs maintaining every frame or it self-ends.
        AmbiencePreview.Tick();
    }

    protected override LightweaveNode Body() {
        return NewColonyRoot.Build(() => Close());
    }
}
