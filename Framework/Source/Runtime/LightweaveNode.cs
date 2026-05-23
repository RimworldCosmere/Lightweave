using System;
using UnityEngine;

namespace Cosmere.Lightweave.Runtime;

public sealed class LightweaveNode {
    public int CallSiteId;
    public int BuildParentPathHash;
    public List<LightweaveNode> Children = new List<LightweaveNode>();
    public Rect ContentRect;
    public string DebugName = string.Empty;
    public object? ExplicitKey;
    public bool IsFooter;
    public bool IsCardContent;
    public Func<float, float>? Measure;
    public Func<float>? MeasureWidth;
    public Func<Rect, List<Rect>>? MeasureChildren;
    public Rect MeasuredRect;
    public Action<Rect, Action>? Paint;
    public Action<Rect>? Layout;
    public Action<Rect>? Draw;
    public float? PreferredHeight;

    private Style? _style;
    public Style? Style {
        get => _style;
        set {
            _style = value;
            _resolvedStyleEpoch = -1;
        }
    }

    public string? Id;

    private string[]? _classes;
    public string[]? Classes {
        get => _classes;
        set {
            _classes = value;
            _resolvedStyleEpoch = -1;
        }
    }

    internal Style _resolvedStyleCache;
    internal int _resolvedStyleEpoch = -1;
}