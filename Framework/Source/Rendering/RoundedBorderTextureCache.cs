using System.Collections.Generic;
using UnityEngine;

namespace Cosmere.Lightweave.Rendering;

/// Bakes per-corner rounded-ring alpha masks so a hollow rounded border can be
/// drawn over any fill, including translucent ones. Unity's
/// <c>GUI.DrawTexture</c> border path renders a square rect once a border width
/// is set (it ignores <c>borderRadiuses</c> in that mode), so rounded-theme
/// borders read as sharp corners. <see cref="PaintBox"/> instead paints the four
/// straight edges as solid strips and the four corner arcs from these masks.
public static class RoundedBorderTextureCache {
    public enum Corner {
        TopLeft,
        TopRight,
        BottomRight,
        BottomLeft,
    }

    private static readonly Dictionary<(int R, int B, Corner C), Texture2D> Cache =
        new Dictionary<(int, int, Corner), Texture2D>();

    public static Texture2D QuarterRing(int radius, int borderWidth, Corner corner) {
        int r = Mathf.Max(1, radius);
        int b = Mathf.Clamp(borderWidth, 1, r);
        (int R, int B, Corner C) key = (r, b, corner);
        if (Cache.TryGetValue(key, out Texture2D existing) && existing != null) {
            return existing;
        }

        // Bake at a supersampled resolution so the arc is expressed with more
        // than the handful of pixels a small corner radius (e.g. RadiusScale.Sm
        // ~= 5px) would otherwise allow. The texture is drawn into an r x r quad
        // with bilinear filtering, which downsamples the high-res arc into a
        // smooth edge that matches Unity's GPU-rasterized rounded fill. Without
        // this the corner reads as a coarse square step against a contrasting
        // border color.
        const int ss = 4;
        int dim = r * ss;

        // Arc centre in screen-local pixels (origin top-left of the corner quad).
        // The open side of the quarter arc must face the rect interior, so each
        // orientation parks the centre at the interior-facing corner of the quad.
        float ccx;
        float ccy;
        switch (corner) {
            case Corner.TopLeft:
                ccx = dim;
                ccy = dim;
                break;
            case Corner.TopRight:
                ccx = 0f;
                ccy = dim;
                break;
            case Corner.BottomRight:
                ccx = 0f;
                ccy = 0f;
                break;
            default:
                ccx = dim;
                ccy = 0f;
                break;
        }

        Texture2D tex = NewTex($"LightweaveRoundedRing_{r}_{b}_{corner}", dim, dim);
        Color[] px = new Color[dim * dim];
        float outerR = r * ss;
        float innerR = (r - b) * ss;
        float feather = ss * 0.5f;
        for (int ty = 0; ty < dim; ty++) {
            // GUI.DrawTexture shows the texture upright, so texture row ty maps to
            // screen-local row (dim - 1 - ty) measured from the quad's top edge.
            float sy = dim - ty - 0.5f;
            int row = ty * dim;
            for (int tx = 0; tx < dim; tx++) {
                float sx = tx + 0.5f;
                float dx = sx - ccx;
                float dy = sy - ccy;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float outer = Mathf.Clamp01((outerR - d) / feather + 0.5f);
                float inner = Mathf.Clamp01((d - innerR) / feather + 0.5f);
                px[row + tx] = new Color(1f, 1f, 1f, outer * inner);
            }
        }

        Finalize(tex, px);
        Cache[key] = tex;
        return tex;
    }

    /// Bakes a filled quarter-disc (solid rounded corner) sharing the exact arc
    /// geometry of <see cref="QuarterRing"/>. <see cref="PaintBox"/> fills a
    /// rounded silhouette from these corners plus straight bands so the fill's
    /// arc is pixel-identical to the border ring drawn on top — Unity's GPU
    /// rounded-rect path instead leaves a faint stray pixel just outside the arc
    /// at some corners, which reads as a dark speck under a dark border color.
    public static Texture2D QuarterDisc(int radius, Corner corner) {
        int r = Mathf.Max(1, radius);
        (int R, int B, Corner C) key = (r, 0, corner);
        if (Cache.TryGetValue(key, out Texture2D existing) && existing != null) {
            return existing;
        }

        const int ss = 4;
        int dim = r * ss;

        float ccx;
        float ccy;
        switch (corner) {
            case Corner.TopLeft:
                ccx = dim;
                ccy = dim;
                break;
            case Corner.TopRight:
                ccx = 0f;
                ccy = dim;
                break;
            case Corner.BottomRight:
                ccx = 0f;
                ccy = 0f;
                break;
            default:
                ccx = dim;
                ccy = 0f;
                break;
        }

        Texture2D tex = NewTex($"LightweaveRoundedDisc_{r}_{corner}", dim, dim);
        Color[] px = new Color[dim * dim];
        float outerR = r * ss;
        float feather = ss * 0.5f;
        for (int ty = 0; ty < dim; ty++) {
            float sy = dim - ty - 0.5f;
            int row = ty * dim;
            for (int tx = 0; tx < dim; tx++) {
                float sx = tx + 0.5f;
                float dx = sx - ccx;
                float dy = sy - ccy;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float coverage = Mathf.Clamp01((outerR - d) / feather + 0.5f);
                px[row + tx] = new Color(1f, 1f, 1f, coverage);
            }
        }

        Finalize(tex, px);
        Cache[key] = tex;
        return tex;
    }

    private static Texture2D NewTex(string name, int w, int h) {
        return new Texture2D(w, h, TextureFormat.RGBA32, false, false) {
            name = name,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave,
        };
    }

    private static void Finalize(Texture2D tex, Color[] px) {
        tex.SetPixels(px);
        tex.Apply(updateMipmaps: false, makeNoLongerReadable: true);
    }
}
