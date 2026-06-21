using System.Collections.Generic;
using UnityEngine;

namespace Cosmere.Lightweave.Runtime;

internal static class HoverBlockRegistry {
    // Double-buffered: writers register into `cur` (this frame), readers test against `prev`
    // (last frame's registrations). An overlay (modal/popover) paints AFTER the base content it
    // covers, so it registers its block region too late for same-frame base-content input checks
    // (e.g. a scroll view behind the modal grabbing the wheel before the modal node has painted).
    // Reading the previous frame closes that ordering gap: a persistent overlay registers every
    // frame, so by its second frame `prev` reliably holds the block. The one-frame lag on
    // open/close is invisible (the modal opens from a click, tooltips are dwell-delayed).
    private static int currentFrame = -1;
    private static List<Rect> prev = new List<Rect>();
    private static List<Rect> cur = new List<Rect>();

    private static void EnsureFrame() {
        int frame = Time.frameCount;
        if (frame != currentFrame) {
            currentFrame = frame;
            (prev, cur) = (cur, prev);
            cur.Clear();
        }
    }

    public static void Register(Rect screenRect) {
        EnsureFrame();
        cur.Add(screenRect);
    }

    public static bool IsBlocked(Vector2 screenPos) {
        EnsureFrame();
        for (int i = 0; i < prev.Count; i++) {
            if (prev[i].Contains(screenPos)) {
                return true;
            }
        }
        return false;
    }
}
