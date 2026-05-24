using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cosmere.Lightweave.Rendering;

public sealed class TrackedGlyphRun {
    public readonly Vector3[] Verts;
    public readonly Vector2[] Uvs;
    public readonly int GlyphCount;
    public readonly float TotalWidth;
    public readonly float LineHeight;
    public readonly Material? Material;
    public readonly bool HadMissing;

    public TrackedGlyphRun(Vector3[] verts, Vector2[] uvs, int glyphCount, float totalWidth, float lineHeight, Material? material, bool hadMissing) {
        Verts = verts;
        Uvs = uvs;
        GlyphCount = glyphCount;
        TotalWidth = totalWidth;
        LineHeight = lineHeight;
        Material = material;
        HadMissing = hadMissing;
    }
}

public static class TrackedTextGlyphCache {
    private const int MaxEntries = 512;

    private static readonly Dictionary<Key, TrackedGlyphRun> cache = new Dictionary<Key, TrackedGlyphRun>(512);
    private static readonly LinkedList<Key> lru = new LinkedList<Key>();
    private static readonly Dictionary<Key, LinkedListNode<Key>> lruNodes = new Dictionary<Key, LinkedListNode<Key>>();
    private static bool subscribed;

    private static void EnsureSubscribed() {
        if (subscribed) {
            return;
        }
        Font.textureRebuilt += OnFontRebuilt;
        subscribed = true;
    }

    private static void OnFontRebuilt(Font _) {
        Clear();
    }

    public static TrackedGlyphRun GetOrCreate(Font? font, int pixelSize, FontStyle fontStyle, int letterSpacingPx, string content) {
        EnsureSubscribed();

        int fontId = font != null ? font.GetInstanceID() : 0;
        Key key = new Key(fontId, pixelSize, fontStyle, letterSpacingPx, content);
        if (cache.TryGetValue(key, out TrackedGlyphRun cached)) {
            Touch(key);
            return cached;
        }

        TrackedGlyphRun built = Build(font, pixelSize, fontStyle, letterSpacingPx, content);
        if (built.HadMissing) {
            return built;
        }

        cache[key] = built;
        LinkedListNode<Key> node = lru.AddFirst(key);
        lruNodes[key] = node;
        Evict();
        return built;
    }

    private static readonly TextGenerator Generator = new TextGenerator();

    private static bool HasDegenerateUv(TextGenerator gen) {
        int vc = gen.vertexCount;
        if (vc < 4) {
            return false;
        }
        IList<UIVertex> v = gen.verts;
        int qc = vc / 4;
        for (int q = 0; q < qc; q++) {
            int b = q * 4;
            UIVertex a = v[b];
            UIVertex c = v[b + 1];
            UIVertex e = v[b + 2];
            UIVertex d = v[b + 3];
            if (Mathf.Approximately(a.position.x, c.position.x) || Mathf.Approximately(a.position.y, d.position.y)) {
                continue;
            }
            bool allSame = Mathf.Approximately(a.uv0.x, c.uv0.x)
                && Mathf.Approximately(a.uv0.x, e.uv0.x)
                && Mathf.Approximately(a.uv0.x, d.uv0.x)
                && Mathf.Approximately(a.uv0.y, c.uv0.y)
                && Mathf.Approximately(a.uv0.y, e.uv0.y)
                && Mathf.Approximately(a.uv0.y, d.uv0.y);
            if (allSame) {
                return true;
            }
        }
        return false;
    }

    private static TrackedGlyphRun Build(Font? font, int pixelSize, FontStyle fontStyle, int letterSpacingPx, string content) {
        int n = content.Length;
        if (font == null || n == 0) {
            return new TrackedGlyphRun(Array.Empty<Vector3>(), Array.Empty<Vector2>(), 0, 0f, pixelSize, null, false);
        }

        TextGenerationSettings settings = new TextGenerationSettings {
            font = font,
            color = Color.white,
            fontSize = pixelSize,
            fontStyle = fontStyle,
            lineSpacing = 1f,
            richText = false,
            scaleFactor = 1f,
            textAnchor = TextAnchor.UpperLeft,
            alignByGeometry = false,
            resizeTextForBestFit = false,
            updateBounds = false,
            horizontalOverflow = HorizontalWrapMode.Overflow,
            verticalOverflow = VerticalWrapMode.Overflow,
            generationExtents = new Vector2(100000f, 100000f),
            pivot = Vector2.zero,
            generateOutOfBounds = true,
        };

        font.RequestCharactersInTexture(content, pixelSize, fontStyle);
        Generator.Populate(content, settings);
        if (HasDegenerateUv(Generator)) {
            font.RequestCharactersInTexture(content, pixelSize, fontStyle);
            Generator.Populate(content, settings);
        }

        int vertCount = Generator.vertexCount;
        int quadCount = vertCount / 4;
        if (quadCount == 0) {
            return new TrackedGlyphRun(Array.Empty<Vector3>(), Array.Empty<Vector2>(), 0, 0f, pixelSize, font.material, false);
        }

        IList<UIVertex> genVerts = Generator.verts;

        float scale = font.fontSize > 0 ? (float)pixelSize / font.fontSize : 1f;
        float lineHeightRaw = font.lineHeight > 0 ? font.lineHeight * scale : pixelSize;
        float lineHeight = Mathf.Max(lineHeightRaw, pixelSize);

        float minX = float.MaxValue;
        float maxY = float.MinValue;
        for (int q = 0; q < quadCount; q++) {
            int b = q * 4;
            Vector3 p0 = genVerts[b].position;
            Vector3 p1 = genVerts[b + 1].position;
            Vector3 p3 = genVerts[b + 3].position;
            bool zeroArea = Mathf.Approximately(p0.x, p1.x) || Mathf.Approximately(p0.y, p3.y);
            if (zeroArea) {
                continue;
            }
            if (p0.x < minX) {
                minX = p0.x;
            }
            if (p0.y > maxY) {
                maxY = p0.y;
            }
        }
        if (minX == float.MaxValue) {
            minX = 0f;
            maxY = 0f;
        }

        Vector3[] verts = new Vector3[quadCount * 4];
        Vector2[] uvs = new Vector2[quadCount * 4];

        float maxX = 0f;
        int gi = 0;
        bool hadMissing = false;
        for (int q = 0; q < quadCount; q++) {
            int b = q * 4;
            UIVertex v0 = genVerts[b];
            UIVertex v1 = genVerts[b + 1];
            UIVertex v2 = genVerts[b + 2];
            UIVertex v3 = genVerts[b + 3];

            float shift = q * letterSpacingPx;
            float x0 = v0.position.x - minX + shift;
            float x1 = v1.position.x - minX + shift;

            bool zeroArea = Mathf.Approximately(v0.position.x, v1.position.x)
                || Mathf.Approximately(v0.position.y, v3.position.y);
            if (zeroArea) {
                if (x1 > maxX) {
                    maxX = x1;
                }
                continue;
            }

            bool trulyMissing = Mathf.Approximately(v0.uv0.x, v1.uv0.x)
                && Mathf.Approximately(v0.uv0.x, v2.uv0.x)
                && Mathf.Approximately(v0.uv0.x, v3.uv0.x)
                && Mathf.Approximately(v0.uv0.y, v1.uv0.y)
                && Mathf.Approximately(v0.uv0.y, v2.uv0.y)
                && Mathf.Approximately(v0.uv0.y, v3.uv0.y);
            if (trulyMissing) {
                hadMissing = true;
                if (x1 > maxX) {
                    maxX = x1;
                }
                continue;
            }

            float yTop = maxY - v0.position.y;
            float yBot = maxY - v3.position.y;

            int o = gi * 4;
            verts[o]     = new Vector3(x0, yTop, 0f);
            verts[o + 1] = new Vector3(x1, yTop, 0f);
            verts[o + 2] = new Vector3(x1, yBot, 0f);
            verts[o + 3] = new Vector3(x0, yBot, 0f);

            uvs[o]     = v0.uv0;
            uvs[o + 1] = v1.uv0;
            uvs[o + 2] = v2.uv0;
            uvs[o + 3] = v3.uv0;

            if (x1 > maxX) {
                maxX = x1;
            }
            gi++;
        }

        return new TrackedGlyphRun(verts, uvs, gi, Mathf.Max(0f, maxX), lineHeight, font.material, hadMissing);
    }

    public static void Clear() {
        cache.Clear();
        lru.Clear();
        lruNodes.Clear();
    }

    private static void Touch(Key key) {
        if (lruNodes.TryGetValue(key, out LinkedListNode<Key> node)) {
            lru.Remove(node);
            lru.AddFirst(node);
        }
    }

    private static void Evict() {
        while (cache.Count > MaxEntries) {
            LinkedListNode<Key>? tail = lru.Last;
            if (tail == null) {
                break;
            }

            lru.RemoveLast();
            lruNodes.Remove(tail.Value);
            cache.Remove(tail.Value);
        }
    }

    private readonly struct Key : IEquatable<Key> {
        public readonly int FontId;
        public readonly int PixelSize;
        public readonly FontStyle FontStyle;
        public readonly int LetterSpacingPx;
        public readonly string Content;

        public Key(int fontId, int pixelSize, FontStyle fontStyle, int letterSpacingPx, string content) {
            FontId = fontId;
            PixelSize = pixelSize;
            FontStyle = fontStyle;
            LetterSpacingPx = letterSpacingPx;
            Content = content;
        }

        public bool Equals(Key o) {
            return FontId == o.FontId
                && PixelSize == o.PixelSize
                && FontStyle == o.FontStyle
                && LetterSpacingPx == o.LetterSpacingPx
                && string.Equals(Content, o.Content, StringComparison.Ordinal);
        }

        public override bool Equals(object? o) {
            return o is Key k && Equals(k);
        }

        public override int GetHashCode() {
            return (FontId, PixelSize, FontStyle, LetterSpacingPx, Content).GetHashCode();
        }
    }
}

public static class TrackedTextDraw {
    public static void Draw(TrackedGlyphRun run, Rect rect, TextAnchor anchor, Color color) {
        if (run.GlyphCount <= 0 || run.Material == null) {
            return;
        }

        float ox = anchor switch {
            TextAnchor.UpperCenter or TextAnchor.MiddleCenter or TextAnchor.LowerCenter
                => rect.x + (rect.width - run.TotalWidth) * 0.5f,
            TextAnchor.UpperRight or TextAnchor.MiddleRight or TextAnchor.LowerRight
                => rect.xMax - run.TotalWidth,
            _ => rect.x,
        };
        ox = Mathf.Floor(ox);

        float oy = anchor switch {
            TextAnchor.UpperLeft or TextAnchor.UpperCenter or TextAnchor.UpperRight
                => rect.y,
            TextAnchor.LowerLeft or TextAnchor.LowerCenter or TextAnchor.LowerRight
                => rect.yMax - run.LineHeight,
            _ => rect.y + (rect.height - run.LineHeight) * 0.5f,
        };
        oy = Mathf.Floor(oy);

        Rect visible = GuiClipReflection.VisibleRect;
        float cxMin = Mathf.Max(rect.xMin, visible.xMin);
        float cxMax = Mathf.Min(rect.xMax, visible.xMax);
        float cyMin = Mathf.Max(rect.yMin, visible.yMin);
        float cyMax = Mathf.Min(rect.yMax, visible.yMax);

        if (cxMax <= cxMin || cyMax <= cyMin) {
            return;
        }

        run.Material.SetPass(0);
        GL.Begin(GL.QUADS);
        GL.Color(color);
        int count = run.GlyphCount;
        for (int i = 0; i < count; i++) {
            int v = i * 4;
            float origX0 = run.Verts[v].x + ox;
            float origX1 = run.Verts[v + 1].x + ox;
            float origY0 = run.Verts[v].y + oy;
            float origY1 = run.Verts[v + 2].y + oy;

            if (origX1 <= cxMin || origX0 >= cxMax || origY1 <= cyMin || origY0 >= cyMax) {
                continue;
            }

            float newX0 = origX0 < cxMin ? cxMin : origX0;
            float newX1 = origX1 > cxMax ? cxMax : origX1;
            float newY0 = origY0 < cyMin ? cyMin : origY0;
            float newY1 = origY1 > cyMax ? cyMax : origY1;

            float origW = origX1 - origX0;
            float origH = origY1 - origY0;
            float sx0 = origW > 0f ? (newX0 - origX0) / origW : 0f;
            float sx1 = origW > 0f ? (newX1 - origX0) / origW : 1f;
            float sy0 = origH > 0f ? (newY0 - origY0) / origH : 0f;
            float sy1 = origH > 0f ? (newY1 - origY0) / origH : 1f;

            Vector2 u0 = run.Uvs[v];
            Vector2 u1 = run.Uvs[v + 1];
            Vector2 u2 = run.Uvs[v + 2];
            Vector2 u3 = run.Uvs[v + 3];

            float tlx = u0.x + (u1.x - u0.x) * sx0;
            float tly = u0.y + (u1.y - u0.y) * sx0;
            float blx = u3.x + (u2.x - u3.x) * sx0;
            float bly = u3.y + (u2.y - u3.y) * sx0;
            float nuTLx = tlx + (blx - tlx) * sy0;
            float nuTLy = tly + (bly - tly) * sy0;
            float nuBLx = tlx + (blx - tlx) * sy1;
            float nuBLy = tly + (bly - tly) * sy1;

            float trx = u0.x + (u1.x - u0.x) * sx1;
            float try_ = u0.y + (u1.y - u0.y) * sx1;
            float brx = u3.x + (u2.x - u3.x) * sx1;
            float bry = u3.y + (u2.y - u3.y) * sx1;
            float nuTRx = trx + (brx - trx) * sy0;
            float nuTRy = try_ + (bry - try_) * sy0;
            float nuBRx = trx + (brx - trx) * sy1;
            float nuBRy = try_ + (bry - try_) * sy1;

            GL.TexCoord2(nuTLx, nuTLy); GL.Vertex3(newX0, newY0, 0f);
            GL.TexCoord2(nuTRx, nuTRy); GL.Vertex3(newX1, newY0, 0f);
            GL.TexCoord2(nuBRx, nuBRy); GL.Vertex3(newX1, newY1, 0f);
            GL.TexCoord2(nuBLx, nuBLy); GL.Vertex3(newX0, newY1, 0f);
        }
        GL.End();
    }
}





