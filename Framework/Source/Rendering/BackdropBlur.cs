using System.Reflection;
using UnityEngine;

namespace Cosmere.Lightweave.Rendering;

public static class BackdropBlur {
    public static void Draw(Rect rect, float blurSizePx = 12f, Color? tint = null, float cornerRadiusPx = 0f) {
        Material? mat = LightweaveShaderDatabase.BlurMaterial;
        if (mat == null) {
            return;
        }

        if (Event.current.type != EventType.Repaint) {
            return;
        }

        Rect snapped = RectSnap.Snap(rect);
        Rect clipped = ClipToVisibleRect(snapped);
        if (clipped.width <= 0f || clipped.height <= 0f) {
            return;
        }

        // The rounded mask is keyed off the quad's own 0..1 texcoords against the
        // full rect size, which only holds when the quad is drawn unclipped. A
        // partially-scrolled-off surface falls back to a square blur (its rounded
        // edge is off-screen anyway), avoiding a distorted corner.
        bool fullyVisible = Mathf.Abs(clipped.x - snapped.x) < 0.5f
                            && Mathf.Abs(clipped.y - snapped.y) < 0.5f
                            && Mathf.Abs(clipped.width - snapped.width) < 0.5f
                            && Mathf.Abs(clipped.height - snapped.height) < 0.5f;
        float effectiveRadius = fullyVisible ? cornerRadiusPx : 0f;

        Color color = tint ?? Color.white;
        mat.SetFloat(BlurSizeId, blurSizePx);
        mat.SetColor(ColorId, color);
        mat.SetFloat(CornerRadiusId, effectiveRadius);
        mat.SetVector(RectSizeId, new Vector4(snapped.width, snapped.height, 0f, 0f));

        Graphics.DrawTexture(
            clipped,
            Texture2D.whiteTexture,
            new Rect(0f, 0f, 1f, 1f),
            0,
            0,
            0,
            0,
            color,
            mat
        );
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

    private static readonly int BlurSizeId = Shader.PropertyToID("_BlurSize");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int CornerRadiusId = Shader.PropertyToID("_CornerRadius");
    private static readonly int RectSizeId = Shader.PropertyToID("_RectSize");

    private static readonly PropertyInfo? VisibleRectProp =
        typeof(GUI).Assembly
            .GetType("UnityEngine.GUIClip")
            ?.GetProperty("visibleRect", BindingFlags.Static | BindingFlags.NonPublic);
}
