using System;
using Cosmere.Lightweave.Runtime;
using Verse;

namespace Cosmere.Lightweave.Feedback;

internal static class GlobalOverlayHost {
    private static GlobalOverlayContainer? container;

    public static void Enqueue(Action draw, RenderContext ctx) {
        EnsureRegistered();
        container!.Enqueue(draw, ctx);
    }

    private static void EnsureRegistered() {
        if (container != null && Find.WindowStack != null && Find.WindowStack.IsOpen(container)) {
            return;
        }

        if (container == null) {
            container = new GlobalOverlayContainer();
        }

        if (Find.WindowStack != null && !Find.WindowStack.IsOpen(container)) {
            Find.WindowStack.Add(container);
        }
    }
}
