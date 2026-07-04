using System;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Settings;
using Cosmere.Lightweave.Theme;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;

namespace Cosmere.Lightweave.Runtime;

public static class LightweaveRoot {
    private static readonly Dictionary<Guid, HookStore> stores = new Dictionary<Guid, HookStore>();
    private static readonly Dictionary<Guid, BuildCache> buildCache = new Dictionary<Guid, BuildCache>();

    private struct BuildCache {
        public int FrameCount;
        public int HookVersion;
        public Rect InRect;
        public LightweaveNode Root;
    }

    public static bool DiagnosticsEnabled => LightweaveMod.Settings?.RenderDiagnostics ?? false;

    public static void Render(
        Rect inRect,
        Guid rootId,
        Func<LightweaveNode> build,
        Direction? directionOverride = null,
        Theme.Theme? themeOverride = null,
        Action? afterContent = null
    ) {
        DiagnosticsLevelController.Sync(DiagnosticsEnabled);
        long renderStart = DiagnosticsEnabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0L;
        if (DiagnosticsEnabled) {
            EventType evt = Event.current?.type ?? EventType.Used;
            LightweaveTelemetry.RenderCall(evt.ToString());
        }

        if (!stores.TryGetValue(rootId, out HookStore store)) {
            store = new HookStore();
            stores[rootId] = store;
        }

        AnimationClock.ClearFrame();
        LightweaveHitTracker.Clear();
        RenderContext ctx = new RenderContext(store) { RootId = rootId, RootRect = inRect };
        ctx.ThemeStack.Push(themeOverride ?? GetBaseTheme());
        ctx.DirectionStack.Push(directionOverride ?? DetectDirection());
        ctx.Breakpoint = Breakpoints.For(Verse.UI.screenWidth);
        ctx.PointerPos = Event.current?.mousePosition ?? Vector2.zero;
        RenderContext.Push(ctx);

        // Capture the scroll wheel once, before any paint or overlay flush, and Use() it immediately so
        // Unity's Widgets.BeginScrollView calls never auto-scroll. Each LightweaveScrollView.Dispose this
        // frame applies the stashed delta if it is the topmost overflowing scroll under the cursor. This
        // is the only point at which the event is guaranteed still live for scrolls painted during the
        // overlay flush (modal/popover bodies), whose ctors run after the event has already gone Used.
        Event? wheelEvt = Event.current;
        if (wheelEvt != null && wheelEvt.rawType == EventType.ScrollWheel && inRect.Contains(wheelEvt.mousePosition)) {
            LightweaveScrollView.CaptureWheel(wheelEvt.delta.y);
            wheelEvt.Use();
        }

        bool didBuild = false;
        bool painted = false;
        try {
            try {
                int frame = Time.frameCount;
                int version = store.Version;
                LightweaveNode root;
                long buildStart = DiagnosticsEnabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0L;
                bool animating = AnimationClock.WasActiveLastFrame(rootId);
                bool hasCached = buildCache.TryGetValue(rootId, out BuildCache cached);
                if (hasCached
                    && cached.HookVersion == version
                    && cached.InRect == inRect
                    && (!animating || cached.FrameCount == frame)) {
                    root = cached.Root;
                    if (DiagnosticsEnabled) {
                        LightweaveTelemetry.BuildHit();
                    }
                }
                else {
                    string missReason;
                    if (!hasCached) {
                        missReason = "new";
                    }
                    else if (cached.HookVersion != version) {
                        missReason = "version";
                    }
                    else if (cached.InRect != inRect) {
                        missReason = "rect";
                    }
                    else {
                        missReason = "animating";
                    }
                    root = build();
                    didBuild = true;
                    buildCache[rootId] = new BuildCache {
                        FrameCount = frame,
                        HookVersion = version,
                        InRect = inRect,
                        Root = root,
                    };
                    if (DiagnosticsEnabled) {
                        LightweaveTelemetry.BuildMiss(missReason);
                    }
                }
                if (DiagnosticsEnabled) {
                    LightweaveTelemetry.ObserveBuild(System.Diagnostics.Stopwatch.GetTimestamp() - buildStart);
                }
                root.MeasuredRect = inRect;
                root.ContentRect = inRect;
                EventType currentEvent = Event.current?.type ?? EventType.Used;
                if (currentEvent != EventType.Layout) {
                    painted = true;
                    long paintStart = DiagnosticsEnabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0L;
                    Paint(root);
                    if (DiagnosticsEnabled) {
                        LightweaveTelemetry.ObservePaint(System.Diagnostics.Stopwatch.GetTimestamp() - paintStart);
                    }
                    afterContent?.Invoke();
                    ctx.FlushHotkeys();
                    ctx.PendingOverlays.Flush();
                    CursorOverrides.ApplyForFrame();
                }
            }
            finally {
                ctx.PendingOverlays.Clear();
                if (didBuild) {
                    store.RetireUntouched();
                }
            }
        }
        finally {
            // Only snapshot the hit-test rects on passes that actually painted (i.e. tracked).
            // Layout events clear `rects` but skip Paint, so committing on them would overwrite
            // the last real snapshot with an empty list - and since Unity sends Layout right
            // before each MouseUp, every IsTopmost check would then hit its empty-prev fallback
            // (return true), letting whatever paints first claim clicks meant for controls on top.
            if (painted) {
                LightweaveHitTracker.Commit(rootId);
            }
            // Clear the captured wheel so it never leaks into the next window's Render this same frame.
            LightweaveScrollView.ClearWheel();
            RenderContext.Clear();
            if (DiagnosticsEnabled) {
                LightweaveTelemetry.ObserveTotal(System.Diagnostics.Stopwatch.GetTimestamp() - renderStart);
            }
        }
    }

    public static void Release(Guid rootId) {
        if (stores.TryGetValue(rootId, out HookStore store)) {
            store.ReleaseAll();
            stores.Remove(rootId);
        }
        buildCache.Remove(rootId);
        LightweaveHitTracker.ReleaseRoot(rootId);
    }

    public static void Invalidate(Guid rootId) {
        if (stores.TryGetValue(rootId, out HookStore store)) {
            store.Invalidate();
        }
    }

    private static Theme.Theme GetBaseTheme() {
        return ThemeRegistry.Active;
    }

    private static Direction DetectDirection() {
        string code = LanguageDatabase.activeLanguage?.folderName ?? "English";
        return code is "Arabic" or "Hebrew" or "Persian" or "Urdu" ? Direction.Rtl : Direction.Ltr;
    }

    public static void PaintSubtree(LightweaveNode node, Rect rect) {
        node.MeasuredRect = rect;
        node.ContentRect = rect;
        Paint(node);
    }


    private static LightweaveNode? currentPaintNode;

    private static readonly Action SharedPaintChildren = () => {
        LightweaveNode? node = currentPaintNode;
        if (node == null) {
            return;
        }

        for (int i = 0; i < node.Children.Count; i++) {
            Paint(node.Children[i]);
        }
    };

    private static void Paint(LightweaveNode node) {
        RenderContext? rc = RenderContext.CurrentOrNull;
        int previousParentHash = rc?.ParentPathHash ?? 0;
        if (rc != null) {
            rc.ParentPathHash = node.BuildParentPathHash;
        }

        LightweaveNode? prevPaintNode = currentPaintNode;
        currentPaintNode = node;

        Style style = default;
        bool hasStyle = node.Style.HasValue || (node.Classes != null && node.Classes.Length > 0) || node.Id != null;
        if (hasStyle && rc != null) {
            // Route through the per-node cache, not Theme.ResolveStyle directly: resolved style depends
            // only on (Classes, Style, theme epoch), none of which change across the many MouseDrag walks
            // a globe/slider drag triggers. The build cache reuses the same node instances during a drag,
            // so this caches the heaviest per-node op (class merge + string.Join) instead of re-running it
            // ~500x/sec. A real state change (slider value) bumps the hook version, rebuilds fresh nodes,
            // and resolves once - so live label updates are unaffected.
            style = node.GetResolvedStyle();
        }

        if (style.Visible == false) {
            currentPaintNode = prevPaintNode;
            if (rc != null) {
                rc.ParentPathHash = previousParentHash;
            }
            return;
        }

        static float ClampMin(float v, Length? min, float parentSize) {
            if (!min.HasValue) {
                return v;
            }
            Length lm = min.Value;
            if (lm.IsGrower) {
                return v;
            }
            float minPx = lm.ToPixels(parentSize, 0f);
            return v < minPx ? minPx : v;
        }

        static float ClampMax(float v, Length? max, float parentSize) {
            if (!max.HasValue) {
                return v;
            }
            Length lm = max.Value;
            if (lm.IsGrower) {
                return v;
            }
            float maxPx = lm.ToPixels(parentSize, float.MaxValue);
            return v > maxPx ? maxPx : v;
        }

        Position pos = style.Position ?? Position.Static;
        if (pos == Position.Absolute || pos == Position.Fixed) {
            Rect ancestor;
            if (pos == Position.Fixed) {
                ancestor = rc?.RootRect ?? node.MeasuredRect;
            }
            else if (rc != null && rc.PositioningAncestorStack.Count > 0) {
                ancestor = rc.PositioningAncestorStack.Peek();
            }
            else {
                ancestor = rc?.RootRect ?? node.MeasuredRect;
            }

            bool hasLeft = style.Left.HasValue;
            bool hasRight = style.Right.HasValue;
            bool hasTop = style.Top.HasValue;
            bool hasBottom = style.Bottom.HasValue;
            float leftPx = hasLeft ? style.Left!.Value.ToPixels() : 0f;
            float rightPx = hasRight ? style.Right!.Value.ToPixels() : 0f;
            float topPx = hasTop ? style.Top!.Value.ToPixels() : 0f;
            float bottomPx = hasBottom ? style.Bottom!.Value.ToPixels() : 0f;

            float intrinsicW = node.MeasureWidth?.Invoke() ?? ancestor.width;
            float w;
            if (style.Width.HasValue) {
                Length lw = style.Width.Value;
                w = lw.IsGrower ? ancestor.width : lw.ToPixels(ancestor.width, intrinsicW);
            }
            else if (hasLeft && hasRight) {
                w = Mathf.Max(0f, ancestor.width - leftPx - rightPx);
            }
            else {
                w = intrinsicW;
            }
            w = ClampMin(w, style.MinWidth, ancestor.width);
            w = ClampMax(w, style.MaxWidth, ancestor.width);

            float intrinsicH = node.Measure?.Invoke(w) ?? node.PreferredHeight ?? 0f;
            float h;
            if (style.Height.HasValue) {
                Length lh = style.Height.Value;
                h = lh.IsGrower ? ancestor.height : lh.ToPixels(ancestor.height, intrinsicH);
            }
            else if (hasTop && hasBottom) {
                h = Mathf.Max(0f, ancestor.height - topPx - bottomPx);
            }
            else {
                h = intrinsicH;
            }
            h = ClampMin(h, style.MinHeight, ancestor.height);
            h = ClampMax(h, style.MaxHeight, ancestor.height);

            float x;
            if (hasLeft) {
                x = ancestor.x + leftPx;
            }
            else if (hasRight) {
                x = ancestor.x + ancestor.width - rightPx - w;
            }
            else {
                x = ancestor.x;
            }

            float y;
            if (hasTop) {
                y = ancestor.y + topPx;
            }
            else if (hasBottom) {
                y = ancestor.y + ancestor.height - bottomPx - h;
            }
            else {
                y = ancestor.y;
            }

            Rect outer = new Rect(x, y, w, h);
            if (style.Margin.HasValue) {
                outer = style.Margin.Value.Shrink(outer, rc?.Direction ?? Direction.Ltr);
            }
            node.MeasuredRect = outer;
        }
        else {
            Rect r = node.MeasuredRect;
            if (style.Margin.HasValue) {
                r = style.Margin.Value.Shrink(r, rc?.Direction ?? Direction.Ltr);
            }

            if (style.Width.HasValue) {
                Length lw = style.Width.Value;
                if (!lw.IsGrower) {
                    float intrinsicW = node.MeasureWidth?.Invoke() ?? r.width;
                    r.width = lw.ToPixels(r.width, intrinsicW);
                }
            }
            if (style.Height.HasValue) {
                Length lh = style.Height.Value;
                if (!lh.IsGrower) {
                    float intrinsicH = node.Measure?.Invoke(r.width) ?? node.PreferredHeight ?? r.height;
                    r.height = lh.ToPixels(r.height, intrinsicH);
                }
            }
            float cw = ClampMin(r.width, style.MinWidth, r.width);
            cw = ClampMax(cw, style.MaxWidth, r.width);
            float ch = ClampMin(r.height, style.MinHeight, r.height);
            ch = ClampMax(ch, style.MaxHeight, r.height);
            r.width = cw;
            r.height = ch;

            if (pos == Position.Relative) {
                if (style.Left.HasValue) {
                    r.x += style.Left.Value.ToPixels();
                }
                else if (style.Right.HasValue) {
                    r.x -= style.Right.Value.ToPixels();
                }
                if (style.Top.HasValue) {
                    r.y += style.Top.Value.ToPixels();
                }
                else if (style.Bottom.HasValue) {
                    r.y -= style.Bottom.Value.ToPixels();
                }
            }

            node.MeasuredRect = r;
        }

        bool pushedAncestor = false;
        if (rc != null && pos != Position.Static && (pos == Position.Relative || pos == Position.Absolute || pos == Position.Fixed)) {
            rc.PositioningAncestorStack.Push(node.MeasuredRect);
            pushedAncestor = true;
        }

        Color savedColor = GUI.color;
        bool appliedOpacity = false;
        if (style.Opacity.HasValue) {
            GUI.color = new Color(savedColor.r, savedColor.g, savedColor.b, savedColor.a * style.Opacity.Value);
            appliedOpacity = true;
        }

        try {
            bool isRepaint = Event.current?.type == EventType.Repaint;

            if (isRepaint) {
                if (style.Shadow != null) {
                    PaintBox.DrawShadow(node.MeasuredRect, style.Shadow);
                }

                if (style.Background != null || style.Border.HasValue || style.Radius.HasValue) {
                    PaintBox.Draw(node.MeasuredRect, style.Background, style.Border, style.Radius);
                }

                if (style.Shadow != null) {
                    PaintBox.DrawInsetHighlight(node.MeasuredRect, style.Shadow);
                }
            }

            Rect innerRect = node.MeasuredRect;
            if (style.Padding.HasValue) {
                innerRect = style.Padding.Value.Shrink(innerRect, rc?.Direction ?? Direction.Ltr);
            }

            node.Layout?.Invoke(innerRect);

            if (isRepaint) {
                node.Draw?.Invoke(innerRect);
            }

            if (node.Paint != null) {
                node.Paint(innerRect, SharedPaintChildren);
            }
            else {
                for (int i = 0; i < node.Children.Count; i++) {
                    Paint(node.Children[i]);
                }
            }
        }
        finally {
            if (appliedOpacity) {
                GUI.color = savedColor;
            }
            if (pushedAncestor && rc != null) {
                rc.PositioningAncestorStack.Pop();
            }
            currentPaintNode = prevPaintNode;
            if (rc != null) {
                rc.ParentPathHash = previousParentHash;
            }
        }
    }
}