using System.Collections.Generic;
using UnityEngine;

namespace Cosmere.Lightweave.Rendering;

public static class GradientTextureCache {
    private const int GradientHeight = 32;
    private static readonly Dictionary<long, Texture2D> Cache = new Dictionary<long, Texture2D>();

    public static Texture2D Vertical(Color top, Color bottom) {
        long key = HashColor(top) * 397L ^ HashColor(bottom);
        if (Cache.TryGetValue(key, out Texture2D existing) && existing != null) {
            return existing;
        }

        Texture2D tex = new Texture2D(1, GradientHeight, TextureFormat.RGBA32, false, false) {
            name = "LightweaveGradient",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave,
        };
        // Interpolate in premultiplied-alpha space (matching CSS linear-gradient), then
        // un-premultiply for storage so GUI.DrawTexture's straight-alpha blend composites
        // correctly. Straight Color.Lerp between endpoints of differing alpha (e.g. a 0.55-alpha
        // dark top and a 0.08-alpha gold bottom) drives RGB toward the bright endpoint faster
        // than alpha falls, producing a muddy gold mid-band; premultiplied interpolation fades
        // cleanly. Endpoints of equal alpha (e.g. Button's primary gradient) are unaffected.
        float topR = top.r * top.a;
        float topG = top.g * top.a;
        float topB = top.b * top.a;
        float botR = bottom.r * bottom.a;
        float botG = bottom.g * bottom.a;
        float botB = bottom.b * bottom.a;
        Color[] pixels = new Color[GradientHeight];
        for (int i = 0; i < GradientHeight; i++) {
            float t = i / (float)(GradientHeight - 1);
            float a = Mathf.Lerp(top.a, bottom.a, t);
            float pr = Mathf.Lerp(topR, botR, t);
            float pg = Mathf.Lerp(topG, botG, t);
            float pb = Mathf.Lerp(topB, botB, t);
            Color c = a > 0.0001f ? new Color(pr / a, pg / a, pb / a, a) : new Color(0f, 0f, 0f, 0f);
            pixels[GradientHeight - 1 - i] = c;
        }
        tex.SetPixels(pixels);
        tex.Apply(updateMipmaps: false, makeNoLongerReadable: true);
        Cache[key] = tex;
        return tex;
    }

    private static long HashColor(Color c) {
        long r = (long)Mathf.Round(c.r * 1023f) & 0x3FF;
        long g = (long)Mathf.Round(c.g * 1023f) & 0x3FF;
        long b = (long)Mathf.Round(c.b * 1023f) & 0x3FF;
        long a = (long)Mathf.Round(c.a * 1023f) & 0x3FF;
        return (r << 30) | (g << 20) | (b << 10) | a;
    }
}
