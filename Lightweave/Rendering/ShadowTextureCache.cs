using System.Collections.Generic;
using UnityEngine;

namespace Cosmere.Lightweave.Rendering;

public static class ShadowTextureCache {
    private static readonly Dictionary<(int W, int H, int Blur), Texture2D> DropCache =
        new Dictionary<(int, int, int), Texture2D>();
    private static readonly Dictionary<int, Texture2D> InsetCache = new Dictionary<int, Texture2D>();

    public static Texture2D Drop(int boxWidth, int boxHeight, float blurPx) {
        int blur = Mathf.Max(1, Mathf.RoundToInt(blurPx));
        int bw = Mathf.Max(1, boxWidth);
        int bh = Mathf.Max(1, boxHeight);
        (int W, int H, int Blur) key = (bw, bh, blur);
        if (DropCache.TryGetValue(key, out Texture2D existing) && existing != null) {
            return existing;
        }

        int width = bw + 2 * blur;
        int height = bh + 2 * blur;
        Texture2D tex = NewTex($"LightweaveDropShadow_{bw}x{bh}_{blur}", width, height);

        float sigma = blur / 3f;
        float sigmaSqrt2 = sigma * Mathf.Sqrt(2f);
        float boxLeft = blur;
        float boxRight = blur + bw;
        float boxTop = blur;
        float boxBottom = blur + bh;

        float[] profileX = new float[width];
        for (int x = 0; x < width; x++) {
            float cx = x + 0.5f;
            profileX[x] = 0.5f * (Erf((cx - boxLeft) / sigmaSqrt2) - Erf((cx - boxRight) / sigmaSqrt2));
        }

        float[] profileY = new float[height];
        for (int y = 0; y < height; y++) {
            float cy = y + 0.5f;
            profileY[y] = 0.5f * (Erf((cy - boxTop) / sigmaSqrt2) - Erf((cy - boxBottom) / sigmaSqrt2));
        }

        Color[] px = new Color[width * height];
        for (int y = 0; y < height; y++) {
            float py = profileY[y];
            int row = y * width;
            for (int x = 0; x < width; x++) {
                float a = Mathf.Clamp01(profileX[x] * py);
                px[row + x] = new Color(1f, 1f, 1f, a);
            }
        }

        Finalize(tex, px);
        DropCache[key] = tex;
        return tex;
    }

    public static Texture2D InsetEdge(int heightPx) {
        int key = Mathf.Max(1, heightPx);
        if (InsetCache.TryGetValue(key, out Texture2D existing) && existing != null) {
            return existing;
        }
        int size = Mathf.Max(2, key * 2);
        Texture2D tex = NewTex($"LightweaveInsetEdge_{key}", 1, size);
        Color[] px = new Color[size];
        for (int y = 0; y < size; y++) {
            float t = 1f - y / (float)(size - 1);
            px[y] = new Color(1f, 1f, 1f, t);
        }
        Finalize(tex, px);
        InsetCache[key] = tex;
        return tex;
    }

    private static float Erf(float x) {
        float sign = x < 0f ? -1f : 1f;
        float ax = Mathf.Abs(x);
        float t = 1f / (1f + 0.3275911f * ax);
        float y = 1f - (((((1.061405429f * t - 1.453152027f) * t) + 1.421413741f) * t - 0.284496736f) * t + 0.254829592f) * t * Mathf.Exp(-ax * ax);
        return sign * y;
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
