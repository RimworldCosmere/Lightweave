using System.Collections.Generic;
using UnityEngine;

namespace Cosmere.Lightweave.Rendering;

public static class PatternTextureCache {
    private static readonly Dictionary<CheckerKey, Texture2D> CheckerCache = new Dictionary<CheckerKey, Texture2D>();

    public static Texture2D Checker(Color a, Color b, int cellPx) {
        int cell = Mathf.Max(1, cellPx);
        CheckerKey key = new CheckerKey(a, b, cell);
        if (CheckerCache.TryGetValue(key, out Texture2D existing) && existing != null) {
            return existing;
        }

        int size = cell * 2;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false, false) {
            name = $"LightweaveChecker_{cell}",
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Point,
            hideFlags = HideFlags.HideAndDontSave,
        };
        Color[] px = new Color[size * size];
        for (int y = 0; y < size; y++) {
            int cy = y / cell;
            for (int x = 0; x < size; x++) {
                int cx = x / cell;
                px[y * size + x] = ((cx + cy) & 1) == 0 ? a : b;
            }
        }
        tex.SetPixels(px);
        tex.Apply(updateMipmaps: false, makeNoLongerReadable: true);
        CheckerCache[key] = tex;
        return tex;
    }

    private readonly struct CheckerKey {
        private readonly Color a;
        private readonly Color b;
        private readonly int cell;

        public CheckerKey(Color a, Color b, int cell) {
            this.a = a;
            this.b = b;
            this.cell = cell;
        }

        public override bool Equals(object obj) {
            if (obj is CheckerKey other) {
                return a == other.a && b == other.b && cell == other.cell;
            }
            return false;
        }

        public override int GetHashCode() {
            unchecked {
                int h = cell;
                h = (h * 397) ^ a.GetHashCode();
                h = (h * 397) ^ b.GetHashCode();
                return h;
            }
        }
    }
}
