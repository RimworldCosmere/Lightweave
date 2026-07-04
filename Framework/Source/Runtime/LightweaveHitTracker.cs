using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cosmere.Lightweave.Runtime;

/// Per-frame registry of interactive rects for the currently-rendering Lightweave root.
/// Populated during Paint by any primitive that responds to input; consumed by
/// <see cref="LightweaveWindow"/> to suppress the Move cursor over controls and by
/// interactive primitives to decide whether they are the top-most target under the mouse.
internal static class LightweaveHitTracker {
    private static readonly List<Rect> rects = new List<Rect>();
    private static readonly Dictionary<Guid, List<Rect>> prevByRoot = new Dictionary<Guid, List<Rect>>();

    public static int Count => rects.Count;

    public static void Clear() {
        rects.Clear();
    }

    public static void Commit(Guid rootId) {
        if (!prevByRoot.TryGetValue(rootId, out List<Rect> snapshot)) {
            snapshot = new List<Rect>();
            prevByRoot[rootId] = snapshot;
        }

        snapshot.Clear();
        snapshot.AddRange(rects);
    }

    public static void ReleaseRoot(Guid rootId) {
        prevByRoot.Remove(rootId);
    }

    public static void Track(Rect rect) {
        rects.Add(GUIUtility.GUIToScreenRect(rect));
    }

    public static bool IsOver(Vector2 screenPoint) {
        for (int i = 0; i < rects.Count; i++) {
            if (rects[i].Contains(screenPoint)) {
                return true;
            }
        }

        return false;
    }

    public static bool TryGetHit(Vector2 screenPoint, out Rect hit) {
        for (int i = 0; i < rects.Count; i++) {
            if (rects[i].Contains(screenPoint)) {
                hit = rects[i];
                return true;
            }
        }

        hit = default;
        return false;
    }

    /// True when the window-local <paramref name="windowRect"/> is the top-most interactive
    /// rect under the current mouse position. Two layers of occlusion are considered:
    /// cross-root (a higher overlay such as a popover/drawer/toast covering the point shadows
    /// base content beneath it, via <see cref="HoverBlockRegistry"/>; skipped when we are the
    /// overlay content, i.e. <c>OverlayContentDepth &gt; 0</c>, so the overlay's own controls
    /// still fire), then intra-root paint order (paint order == z-order) from the current
    /// root's previous completed pass. Falls back to true when the point isn't covered by any
    /// recorded rect (first frame, layout shifted) so clicks are never silently dropped.
    public static bool IsTopmost(Rect windowRect) {
        Event e = Event.current;
        if (e == null) {
            return true;
        }

        RenderContext ctx = RenderContext.Current;
        Vector2 screenPoint = GUIUtility.GUIToScreenPoint(e.mousePosition);

        if (ctx != null && ctx.OverlayContentDepth == 0 && HoverBlockRegistry.IsBlocked(ctx.RootId, screenPoint)) {
            return false;
        }

        if (ctx == null || !prevByRoot.TryGetValue(ctx.RootId, out List<Rect> prev) || prev.Count == 0) {
            return true;
        }

        Rect screenRect = GUIUtility.GUIToScreenRect(windowRect);

        if (!screenRect.Contains(screenPoint)) {
            return true;
        }

        int topIndex = -1;
        for (int i = 0; i < prev.Count; i++) {
            if (prev[i].Contains(screenPoint)) {
                topIndex = i;
            }
        }

        if (topIndex < 0) {
            return true;
        }

        return RectsApproxEqual(prev[topIndex], screenRect);
    }

    private static bool RectsApproxEqual(Rect a, Rect b) {
        const float eps = 1.0f;
        return Mathf.Abs(a.x - b.x) < eps
            && Mathf.Abs(a.y - b.y) < eps
            && Mathf.Abs(a.width - b.width) < eps
            && Mathf.Abs(a.height - b.height) < eps;
    }
}
