using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Cosmere.Lightweave.Rendering;

/// <summary>
///     Memoizes <see cref="GUIStyle.CalcSize" /> / <see cref="GUIStyle.CalcHeight" /> results.
///     <para>
///         Layout re-runs every Unity event (Layout, MouseMove, MouseDrag - the latter at OS mouse
///         rate, 500+/sec). Each pass re-measures every visible text leaf, and Unity's text metrics
///         are expensive AND allocate a fresh <see cref="GUIContent" /> per call. During a scroll-bar
///         drag over a tall content list that is hundreds of identical measures per second, which
///         shows up as GC stutter ("scrolling is laggy"). The measurement of a given
///         (style, text, width, wrap, richText) tuple is invariant for the style's lifetime, so it is
///         safe to cache: a state change rebuilds the node tree (new closures, fresh measures) and a
///         non-state change (scroll offset) reuses the cached tree, where every measure repeats.
///     </para>
///     <para>
///         The style reference is part of the key (not just its font metrics) because callers mutate
///         <see cref="GUIStyle.wordWrap" /> / <see cref="GUIStyle.richText" /> on the shared cached
///         style right before measuring; those flags change the result and are captured explicitly.
///     </para>
/// </summary>
public static class TextMeasureCache {
    private readonly struct Key : IEquatable<Key> {
        private readonly GUIStyle style;
        private readonly string text;
        private readonly int widthBucket;
        private readonly bool wordWrap;
        private readonly bool richText;

        public Key(GUIStyle style, string text, int widthBucket) {
            this.style = style;
            this.text = text;
            this.widthBucket = widthBucket;
            wordWrap = style.wordWrap;
            richText = style.richText;
        }

        public bool Equals(Key other) {
            return ReferenceEquals(style, other.style)
                && widthBucket == other.widthBucket
                && wordWrap == other.wordWrap
                && richText == other.richText
                && string.Equals(text, other.text, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj) {
            return obj is Key other && Equals(other);
        }

        public override int GetHashCode() {
            unchecked {
                int hash = RuntimeHelpers.GetHashCode(style);
                hash = (hash * 397) ^ (text?.GetHashCode() ?? 0);
                hash = (hash * 397) ^ widthBucket;
                hash = (hash * 397) ^ (wordWrap ? 1 : 0);
                hash = (hash * 397) ^ (richText ? 1 : 0);
                return hash;
            }
        }
    }

    private const int Cap = 2048;
    private const int UnboundedWidth = int.MinValue;

    private static readonly Dictionary<Key, Vector2> sizeCache = new Dictionary<Key, Vector2>();
    private static readonly Dictionary<Key, float> heightCache = new Dictionary<Key, float>();

    [ThreadStatic] private static GUIContent? sharedContent;

    /// <summary>Cached <see cref="GUIStyle.CalcSize" />. Width is single-line so it is not part of the key.</summary>
    public static Vector2 Size(GUIStyle style, string text) {
        Key key = new Key(style, text, UnboundedWidth);
        if (sizeCache.TryGetValue(key, out Vector2 cached)) {
            return cached;
        }

        Vector2 size = style.CalcSize(Content(text));
        if (sizeCache.Count >= Cap) {
            sizeCache.Clear();
        }

        sizeCache[key] = size;
        return size;
    }

    /// <summary>Cached <see cref="GUIStyle.CalcHeight" />. Width is bucketed to whole pixels for the key.</summary>
    public static float Height(GUIStyle style, string text, float width) {
        int widthBucket = float.IsInfinity(width) || width >= float.MaxValue
            ? UnboundedWidth
            : Mathf.RoundToInt(width);
        Key key = new Key(style, text, widthBucket);
        if (heightCache.TryGetValue(key, out float cached)) {
            return cached;
        }

        float height = style.CalcHeight(Content(text), width);
        if (heightCache.Count >= Cap) {
            heightCache.Clear();
        }

        heightCache[key] = height;
        return height;
    }

    private static GUIContent Content(string text) {
        sharedContent ??= new GUIContent();
        sharedContent.text = text;
        return sharedContent;
    }
}
