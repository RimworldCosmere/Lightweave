using RimWorld;
using Verse;
using Verse.Sound;

namespace Cosmere.Lightweave.Redesign.NewColony;

// The ongoing-ritual ambience is a Sustainer, which must be Maintain()'d every frame or it self-ends
// after a single unmaintained frame. Lightweave only re-renders on input events, so maintaining it from
// a render/Paint callback dies the instant the cursor stops moving. This static holder owns the active
// preview sustainer and is pumped from NewColonyWindow.WindowUpdate (a real per-frame game-loop hook),
// so the preview keeps playing until the player toggles it off or the window closes.
public static class AmbiencePreview {
    private static Sustainer? active;

    public static bool IsPlaying => active != null && !active.Ended;

    // Called every frame from NewColonyWindow.WindowUpdate. Keeps the sustainer alive and clears the
    // reference once it has ended (so IsPlaying flips back and the glyph returns to Play).
    public static void Tick() {
        if (active == null) {
            return;
        }
        if (active.Ended) {
            active = null;
            return;
        }
        active.Maintain();
    }

    // Toggle the preview: stop if playing, otherwise start the ideo's ongoing-ritual sound.
    public static void Toggle(Ideo ideo) {
        if (active != null) {
            Stop();
            return;
        }
        active = IdeoDraftMutations.StartAmbiencePreview(ideo);
    }

    public static void Stop() {
        if (active != null) {
            active.End();
            active = null;
        }
    }
}
