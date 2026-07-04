using System;
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
    // Blocks are scoped to the owning window's render root. A block registered while painting window
    // A (e.g. a modal covering its host) must only suppress base content also painted in window A -
    // never a different window B stacked on top (an on-top LogViewer reading window A's full-screen
    // block would wrongly refuse its own wheel/hover). Readers query with their own ctx.RootId.
    private readonly struct Block {
        public readonly Guid RootId;
        public readonly Rect Rect;

        public Block(Guid rootId, Rect rect) {
            RootId = rootId;
            Rect = rect;
        }
    }

    private static int currentFrame = -1;
    private static List<Block> prev = new List<Block>();
    private static List<Block> cur = new List<Block>();

    private static void EnsureFrame() {
        int frame = Time.frameCount;
        if (frame != currentFrame) {
            currentFrame = frame;
            (prev, cur) = (cur, prev);
            cur.Clear();
        }
    }

    public static void Register(Guid rootId, Rect screenRect) {
        EnsureFrame();
        cur.Add(new Block(rootId, screenRect));
    }

    public static bool IsBlocked(Guid rootId, Vector2 screenPos) {
        EnsureFrame();
        // Read both buffers. `prev` covers an overlay painted AFTER the base content it shadows (the
        // common case: a modal/popover registers too late for same-frame base input, so last frame's
        // registration closes the gap). `cur` covers a blocker painted EARLIER this frame than the
        // content it shadows (e.g. an open Dropdown whose menu sits over a sibling control painted
        // later in the same tree) - without it, that lower control would click through on the very
        // frame the row is selected.
        for (int i = 0; i < prev.Count; i++) {
            if (prev[i].RootId == rootId && prev[i].Rect.Contains(screenPos)) {
                return true;
            }
        }
        for (int i = 0; i < cur.Count; i++) {
            if (cur[i].RootId == rootId && cur[i].Rect.Contains(screenPos)) {
                return true;
            }
        }
        return false;
    }
}
