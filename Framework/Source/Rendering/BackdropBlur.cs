using System.Reflection;
using UnityEngine;

namespace Cosmere.Lightweave.Rendering;

public static class BackdropBlur {
    public static void Draw(Rect rect, float blurSizePx = 12f, Color? tint = null, float cornerRadiusPx = 0f) {
        if (Event.current.type != EventType.Repaint) {
            return;
        }

        RenderTexture? blurred = BlurPipeline.EnsureFrameBlur();
        if (blurred == null) {
            return;
        }

        Rect snapped = RectSnap.Snap(rect);
        Rect clipped = ClipToVisibleRect(snapped);
        if (clipped.width <= 0f || clipped.height <= 0f) {
            return;
        }

        Color color = tint ?? Color.white;
        BlurPipeline.Composite(snapped, clipped, blurred, color, cornerRadiusPx);
    }

    private static Rect ClipToVisibleRect(Rect rect) {
        if (VisibleRectProp == null) {
            return rect;
        }

        object? value;
        try {
            value = VisibleRectProp.GetValue(null);
        }
        catch {
            return rect;
        }

        if (value is not Rect visible) {
            return rect;
        }

        float x0 = Mathf.Max(rect.xMin, visible.xMin);
        float y0 = Mathf.Max(rect.yMin, visible.yMin);
        float x1 = Mathf.Min(rect.xMax, visible.xMax);
        float y1 = Mathf.Min(rect.yMax, visible.yMax);
        return new Rect(x0, y0, x1 - x0, y1 - y0);
    }

    private static readonly PropertyInfo? VisibleRectProp =
        typeof(GUI).Assembly
            .GetType("UnityEngine.GUIClip")
            ?.GetProperty("visibleRect", BindingFlags.Static | BindingFlags.NonPublic);
}
