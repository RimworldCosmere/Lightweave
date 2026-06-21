using System;
using System.Collections.Generic;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Navigation;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;

namespace Cosmere.Lightweave.Runtime;

public sealed class LightweaveScrollStatus {
    public float Height;
    public Vector2 Position;
    public bool VerticalVisible;
    public bool Dragging;
    public float DragAnchor;
    public int ArrowHeld;
    public float NextArrowRepeatAt;
    public float LastContentHeight;
    public float LastViewportHeight;
    public float LastScrollAtRealtime;
    public float LastMouseEnterAtRealtime;
    public bool MouseInsideViewport;
    public float LastObservedPositionY;
    public readonly List<ScrollAreaSection> ResolvedSectionsBuffer = new List<ScrollAreaSection>();
}

public struct LightweaveScrollView : IDisposable {
    private const float SlimRailWidth = 4f;
    private const float TicksHitWidth = 12f;
    private const float TicksHairlineRightInset = 4f;
    private const float TickMarkerLength = 8f;
    private const float TickMarkerHoverLength = 10f;
    private const float TickHitHeight = 12f;
    private const float ProgressRailWidth = 2f;
    private const float EdgeFadeHeight = 14f;
    private const float BareMaskHeight = 26f;
    private const float MinThumbHeight = 24f;

    private const float RevealAfterScrollDuration = 0.5f;
    private const float RevealAfterMouseEnterDuration = 0.8f;
    private const float RevealFadeOutDuration = 0.2f;

    private static float RailLeftGap => new Rem(0.5f).ToPixels();
    private static float RailRightInset => new Rem(0.125f).ToPixels();

    [ThreadStatic] private static float _pendingWheelDeltaY;
    [ThreadStatic] private static bool _pendingWheel;
    [ThreadStatic] private static int _pendingWheelFrame;

    // Captured once per frame at the top of LightweaveRoot.Render, before any scroll view is painted.
    // Capturing here (rather than in each scroll view's ctor) is what lets a scroll painted during the
    // overlay flush (modal/popover body) still receive the wheel: by flush time the live event's
    // rawType is already Used, so a per-ctor rawType==ScrollWheel check would never fire for it. The
    // delta is stashed; each scroll view's Dispose applies it if it's the topmost overflowing scroll
    // under the cursor this frame. Cleared at the end of every Render so it never leaks across windows.
    public static void CaptureWheel(float deltaY) {
        _pendingWheelDeltaY = deltaY;
        _pendingWheel = true;
        _pendingWheelFrame = Time.frameCount;
    }

    public static void ClearWheel() {
        _pendingWheel = false;
        _pendingWheelDeltaY = 0f;
    }

    public static float WidthFor(ScrollAreaVariant variant) {
        return variant switch {
            ScrollAreaVariant.Slim => SlimRailWidth,
            ScrollAreaVariant.Auto => SlimRailWidth,
            ScrollAreaVariant.Bare => 0f,
            ScrollAreaVariant.Ticks => TicksHitWidth,
            ScrollAreaVariant.Progress => ProgressRailWidth,
            _ => SlimRailWidth,
        };
    }

    public static float GutterPixels(bool verticalVisible, ScrollAreaVariant variant) {
        if (!verticalVisible || variant == ScrollAreaVariant.Bare) {
            return 0f;
        }

        return WidthFor(variant) + RailLeftGap;
    }

    public static float GutterPixels(bool verticalVisible) {
        return GutterPixels(verticalVisible, ScrollAreaVariant.Slim);
    }

    public const float ContentRightPadding = 10f;

    private readonly Rect outRect;
    private readonly float viewHeight;
    private readonly float contentHeight;
    private readonly LightweaveScrollStatus status;
    private readonly ScrollAreaVariant variant;
    private readonly ScrollAreaTone tone;
    private readonly bool edge;
    private readonly IReadOnlyList<ScrollAreaSection>? sections;
    private readonly LightweaveNode? contentRoot;

    private readonly Rect rect;

    public LightweaveScrollView(
        Rect outRect,
        LightweaveScrollStatus status,
        ScrollAreaVariant variant = ScrollAreaVariant.Slim,
        ScrollAreaTone tone = ScrollAreaTone.Accent,
        bool edge = false,
        IReadOnlyList<ScrollAreaSection>? sections = null,
        LightweaveNode? contentRoot = null
    ) {
        this.outRect = outRect;
        this.status = status;
        this.variant = variant;
        this.tone = tone;
        this.edge = edge;
        this.sections = sections;
        this.contentRoot = contentRoot;

        float declared = Math.Max(status.Height, outRect.height);
        bool overflows = declared - 0.1f >= outRect.height;
        status.VerticalVisible = overflows;

        float gutter = GutterPixels(status.VerticalVisible, variant);
        contentHeight = declared;
        viewHeight = outRect.height;
        rect = new Rect(0f, 0f, outRect.width - gutter, declared);

        status.LastContentHeight = declared;
        status.LastViewportHeight = outRect.height;
        status.Height = 0f;

        // The scroll wheel is captured once per frame at the top of LightweaveRoot.Render (before any
        // paint or overlay flush) via CaptureWheel(), which Use()s the event immediately so Unity's
        // Widgets.BeginScrollView below never auto-scrolls. We do NOT consume here: a scroll painted
        // inside an overlay (modal/popover) is constructed during the overlay flush, by which point the
        // event's rawType is already Used - so a ctor-time rawType==ScrollWheel check could never fire
        // for it. Dispose applies the stashed wheel to whichever scroll is topmost under the cursor.
        Widgets.BeginScrollView(outRect, ref status.Position, rect, false);
    }

    // A scroll view sitting behind an open overlay (a modal scrim, a popover) must not consume the
    // wheel - otherwise it Uses() the event before the overlay's own scroll view, painted later in
    // the PendingOverlays pass, ever sees it (the classic "wheel scrolls the hidden page" bug). The
    // overlay's own content paints at OverlayContentDepth > 0, so it always passes. Base content
    // (depth 0) defers when the cursor is over a registered hover-block region.
    private static bool WheelIsTopmost(Vector2 guiMousePos) {
        RenderContext? ctx = RenderContext.CurrentOrNull;
        if (ctx == null || ctx.OverlayContentDepth > 0) {
            return true;
        }

        return !HoverBlockRegistry.IsBlocked(GUIUtility.GUIToScreenPoint(guiMousePos));
    }

    public float Height => contentHeight;

    public bool CanCull(Rect itemRect) {
        return itemRect.yMax < status.Position.y
            || itemRect.y > status.Position.y + viewHeight;
    }

    public void Dispose() {
        Widgets.EndScrollView();

        // Gate on the stashed frame, NOT Event.current.rawType: CaptureWheel (at Render-top) already
        // Use()d the event, flipping rawType to Used for the rest of this event, so an
        // rawType==ScrollWheel check here would reject every scroll. _pendingWheelFrame scopes the apply to the
        // event that stashed it; mousePosition is preserved across Use() so contains/topmost are live.
        if (_pendingWheel
            && _pendingWheelFrame == Time.frameCount
            && status.VerticalVisible
            && outRect.Contains(Event.current.mousePosition)
            && WheelIsTopmost(Event.current.mousePosition)) {
            float scrollRange = Math.Max(0f, contentHeight - outRect.height);
            float delta = _pendingWheelDeltaY * 20f;
            float prevY = status.Position.y;
            float next = Mathf.Clamp(prevY + delta, 0f, scrollRange);
            status.Position = new Vector2(status.Position.x, next);
            if (Math.Abs(next - prevY) > 0.01f) {
                status.LastScrollAtRealtime = Time.realtimeSinceStartup;
            }
            _pendingWheel = false;
            _pendingWheelDeltaY = 0f;
        }

        if (!status.VerticalVisible) {
            return;
        }

        UpdateMouseHoverState();

        if (variant == ScrollAreaVariant.Bare) {
            PaintBareMasks();
            return;
        }

        if (edge) {
            PaintEdgeFades();
        }

        float alpha = ComputeRevealAlpha();
        if (alpha <= 0.001f) {
            return;
        }

        switch (variant) {
            case ScrollAreaVariant.Progress:
                PaintProgressRail(alpha);
                break;
            case ScrollAreaVariant.Ticks:
                PaintTicksRail(alpha);
                break;
            case ScrollAreaVariant.Slim:
            case ScrollAreaVariant.Auto:
            default:
                PaintSlimRail(alpha);
                break;
        }
    }

    private void UpdateMouseHoverState() {
        if (Event.current.type != EventType.Repaint) {
            return;
        }

        bool inside = outRect.Contains(Event.current.mousePosition);
        if (inside && !status.MouseInsideViewport) {
            status.LastMouseEnterAtRealtime = Time.realtimeSinceStartup;
        }

        status.MouseInsideViewport = inside;
    }

    private float ComputeRevealAlpha() {
        if (variant != ScrollAreaVariant.Auto) {
            return 1f;
        }

        if (status.Dragging || status.ArrowHeld != 0) {
            return 1f;
        }

        float width = WidthFor(variant);
        Rect scrollbarZone = new Rect(
            outRect.xMax - width - RailRightInset,
            outRect.y,
            width,
            outRect.height
        );
        if (scrollbarZone.Contains(Event.current.mousePosition)) {
            return 1f;
        }

        float now = Time.realtimeSinceStartup;
        float scrollAge = now - status.LastScrollAtRealtime;
        float hoverAge = now - status.LastMouseEnterAtRealtime;

        float scrollAlpha = AlphaFromAge(scrollAge, RevealAfterScrollDuration);
        float hoverAlpha = AlphaFromAge(hoverAge, RevealAfterMouseEnterDuration);
        return Mathf.Max(scrollAlpha, hoverAlpha);
    }

    private static float AlphaFromAge(float age, float visibleFor) {
        if (age < 0f) {
            return 0f;
        }

        if (age < visibleFor) {
            return 1f;
        }

        if (age < visibleFor + RevealFadeOutDuration) {
            return 1f - (age - visibleFor) / RevealFadeOutDuration;
        }

        return 0f;
    }

    private ThemeSlot ThumbSlot() {
        return tone == ScrollAreaTone.Neutral ? ThemeSlot.TextMuted : ThemeSlot.SurfaceAccent;
    }

    private void PaintSlimRail(float alpha) {
        Theme.Theme theme = RenderContext.Current.Theme;
        Event e = Event.current;

        float width = SlimRailWidth;
        float trackX = outRect.xMax - width - RailRightInset;
        float trackY = outRect.y + new Rem(0.125f).ToPixels();
        float trackHeight = Mathf.Max(0f, outRect.height - new Rem(0.25f).ToPixels());
        Rect trackRect = new Rect(trackX, trackY, width, trackHeight);

        float scrollRange = Mathf.Max(0f, contentHeight - viewHeight);
        float thumbHeight = trackRect.height > 0f
            ? Mathf.Max(MinThumbHeight, trackRect.height * Mathf.Clamp01(viewHeight / contentHeight))
            : 0f;
        thumbHeight = Mathf.Min(thumbHeight, trackRect.height);
        float trackRange = Mathf.Max(0f, trackRect.height - thumbHeight);
        float scrollNorm = scrollRange > 0f ? Mathf.Clamp01(status.Position.y / scrollRange) : 0f;
        Rect thumbRect = new Rect(
            trackRect.x,
            trackRect.y + trackRange * scrollNorm,
            trackRect.width,
            thumbHeight
        );

        bool hovering = thumbRect.Contains(e.mousePosition);
        bool active = status.Dragging;

        Color saved = GUI.color;

        Color trackColor = theme.GetColor(ThemeSlot.ShelfTint);
        trackColor.a *= 0.7f * alpha;
        GUI.color = trackColor;
        GUI.DrawTexture(trackRect, Texture2D.whiteTexture);

        Color thumbColor = theme.GetColor(ThumbSlot());
        if (!active && !hovering) {
            thumbColor.a *= 0.9f;
        }
        thumbColor.a *= alpha;

        GUI.color = thumbColor;
        GUI.DrawTexture(thumbRect, Texture2D.whiteTexture);

        GUI.color = saved;

        HandleThumbDrag(e, trackRect, thumbRect, scrollRange, trackRange);
    }

    private void PaintProgressRail(float alpha) {
        Theme.Theme theme = RenderContext.Current.Theme;

        float width = ProgressRailWidth;
        float trackX = outRect.xMax - width - RailRightInset;
        float trackY = outRect.y;
        float trackHeight = outRect.height;

        float scrollRange = Mathf.Max(0f, contentHeight - viewHeight);
        float scrollNorm = scrollRange > 0f ? Mathf.Clamp01(status.Position.y / scrollRange) : 0f;
        float fillHeight = trackHeight * scrollNorm;
        Rect fillRect = new Rect(trackX, trackY, width, fillHeight);

        Color saved = GUI.color;
        Color fillColor = theme.GetColor(ThumbSlot());
        fillColor.a *= alpha;
        GUI.color = fillColor;
        GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
        GUI.color = saved;
    }

    private void PaintTicksRail(float alpha) {
        Theme.Theme theme = RenderContext.Current.Theme;
        Event e = Event.current;

        float hitWidth = TicksHitWidth;
        float hitX = outRect.xMax - hitWidth - RailRightInset;

        float hairlineX = outRect.xMax - TicksHairlineRightInset - RailRightInset;
        Rect hairlineRect = new Rect(hairlineX, outRect.y, 1f, outRect.height);

        Color saved = GUI.color;
        Color hairlineColor = theme.GetColor(ThemeSlot.BorderDefault);
        hairlineColor.a *= alpha;
        GUI.color = hairlineColor;
        GUI.DrawTexture(hairlineRect, Texture2D.whiteTexture);

        float scrollRange = Mathf.Max(0f, contentHeight - viewHeight);
        float thumbWidth = SlimRailWidth;
        float thumbX = outRect.xMax - thumbWidth - RailRightInset;
        float thumbTrackHeight = Mathf.Max(0f, outRect.height);
        float thumbHeight = thumbTrackHeight > 0f
            ? Mathf.Max(MinThumbHeight, thumbTrackHeight * Mathf.Clamp01(viewHeight / contentHeight))
            : 0f;
        thumbHeight = Mathf.Min(thumbHeight, thumbTrackHeight);
        float thumbRange = Mathf.Max(0f, thumbTrackHeight - thumbHeight);
        float scrollNorm = scrollRange > 0f ? Mathf.Clamp01(status.Position.y / scrollRange) : 0f;
        Rect thumbRect = new Rect(
            thumbX,
            outRect.y + thumbRange * scrollNorm,
            thumbWidth,
            thumbHeight
        );

        Color thumbColor = theme.GetColor(ThumbSlot());
        thumbColor.a *= alpha;
        GUI.color = thumbColor;
        GUI.DrawTexture(thumbRect, Texture2D.whiteTexture);

        GUI.color = saved;

        PaintAndHandleTicks(e, alpha, scrollRange);
        HandleThumbDrag(e, new Rect(hitX, outRect.y, hitWidth, outRect.height), thumbRect, scrollRange, thumbRange);
    }

    private void PaintAndHandleTicks(Event e, float alpha, float scrollRange) {
        IReadOnlyList<ScrollAreaSection> active = EffectiveSections();
        if (active.Count == 0) {
            return;
        }

        Theme.Theme theme = RenderContext.Current.Theme;
        Color saved = GUI.color;

        float scrollNorm = scrollRange > 0f ? Mathf.Clamp01(status.Position.y / scrollRange) : 0f;
        int activeIndex = ResolveActiveSection(active, scrollNorm);

        int hoverIndex = -1;
        for (int i = 0; i < active.Count; i++) {
            ScrollAreaSection s = active[i];
            float y = outRect.y + outRect.height * Mathf.Clamp01(s.Pct);
            Rect tickHit = new Rect(outRect.xMax - TicksHitWidth - RailRightInset, y - TickHitHeight * 0.5f, TicksHitWidth, TickHitHeight);
            if (tickHit.Contains(e.mousePosition)) {
                hoverIndex = i;
            }
        }

        for (int i = 0; i < active.Count; i++) {
            ScrollAreaSection s = active[i];
            float y = outRect.y + outRect.height * Mathf.Clamp01(s.Pct);
            bool isHover = i == hoverIndex;
            bool isActive = i == activeIndex;
            float markerLength = isHover || isActive ? TickMarkerHoverLength : TickMarkerLength;
            float markerX = outRect.xMax - RailRightInset - TicksHairlineRightInset - markerLength + 1f;
            Rect markerRect = new Rect(markerX, y - 0.5f, markerLength, 1f);

            ThemeSlot markerSlot = isHover || isActive ? ThumbSlot() : ThemeSlot.TextMuted;
            Color markerColor = theme.GetColor(markerSlot);
            markerColor.a *= alpha;
            GUI.color = markerColor;
            GUI.DrawTexture(markerRect, Texture2D.whiteTexture);
        }

        if (hoverIndex >= 0) {
            DrawTickLabel(active[hoverIndex], alpha);
        }

        GUI.color = saved;

        if (e.type == EventType.MouseDown && e.button == 0 && hoverIndex >= 0) {
            ScrollAreaSection s = active[hoverIndex];
            float targetY = scrollRange * Mathf.Clamp01(s.Pct);
            status.Position = new Vector2(status.Position.x, targetY);
            status.LastScrollAtRealtime = Time.realtimeSinceStartup;
            e.Use();
        }
    }

    private IReadOnlyList<ScrollAreaSection> EffectiveSections() {
        if (sections != null) {
            return sections;
        }
        List<ScrollAreaSection> buffer = status.ResolvedSectionsBuffer;
        buffer.Clear();
        if (contentRoot != null && contentHeight > 0f) {
            CollectSectionAnchors(contentRoot, buffer, contentHeight);
        }
        return buffer;
    }

    private static void CollectSectionAnchors(LightweaveNode node, List<ScrollAreaSection> into, float contentHeight) {
        if (node.Tag is SectionAnchorMeta meta) {
            float pct = Mathf.Clamp01(node.MeasuredRect.y / contentHeight);
            into.Add(new ScrollAreaSection(meta.Id, meta.Label, pct));
        }
        for (int i = 0; i < node.Children.Count; i++) {
            CollectSectionAnchors(node.Children[i], into, contentHeight);
        }
    }

    private static int ResolveActiveSection(IReadOnlyList<ScrollAreaSection> active, float scrollNorm) {
        if (active.Count == 0) {
            return -1;
        }

        int activeIndex = 0;
        for (int i = 0; i < active.Count; i++) {
            if (active[i].Pct <= scrollNorm + 0.01f) {
                activeIndex = i;
            }
        }
        return activeIndex;
    }

    private void DrawTickLabel(ScrollAreaSection section, float alpha) {
        Theme.Theme theme = RenderContext.Current.Theme;
        float fontPx = 9.5f;
        float padX = 8f;
        float padY = 3f;

        float anchorX = outRect.xMax - RailRightInset - TicksHairlineRightInset - TickMarkerHoverLength - 6f;
        float anchorY = outRect.y + outRect.height * Mathf.Clamp01(section.Pct);

        string label = section.Label ?? string.Empty;
        GUIStyle measure = GuiStyleCache.GetOrCreate(theme.GetFont(FontRole.BodyBold), Mathf.RoundToInt(fontPx));
        Vector2 size = measure.CalcSize(new GUIContent(label));
        float labelW = size.x + padX * 2f;
        float labelH = size.y + padY * 2f;
        Rect labelRect = new Rect(anchorX - labelW, anchorY - labelH * 0.5f, labelW, labelH);

        Color bg = theme.GetColor(ThemeSlot.Glass3);
        bg.a *= alpha;
        PaintBox.Draw(labelRect, BackgroundSpec.Of(bg), BorderSpec.All(new Rem(1f / 16f), ThemeSlot.BorderSubtle), RadiusSpec.None);

        Color textColor = theme.GetColor(ThemeSlot.TextSecondary);
        textColor.a *= alpha;
        TextDraw.Draw(labelRect, label, FontRole.BodyBold, new Rem(fontPx / 16f), TextAnchor.MiddleCenter, textColor);
    }

    private void HandleThumbDrag(Event e, Rect trackRect, Rect thumbRect, float scrollRange, float trackRange) {
        if (e.type == EventType.MouseDown && e.button == 0) {
            if (thumbRect.Contains(e.mousePosition)) {
                status.Dragging = true;
                status.DragAnchor = e.mousePosition.y - thumbRect.y;
                status.LastScrollAtRealtime = Time.realtimeSinceStartup;
                ActiveDragRegistry.Acquire(RenderContext.Current.RootId);
                e.Use();
                return;
            }

            if (trackRect.Contains(e.mousePosition)) {
                bool above = e.mousePosition.y < thumbRect.y;
                float delta = viewHeight * 0.9f * (above ? -1f : 1f);
                ScrollBy(delta, scrollRange);
                status.LastScrollAtRealtime = Time.realtimeSinceStartup;
                e.Use();
                return;
            }
        }

        if (!status.Dragging) {
            return;
        }

        if (e.type == EventType.MouseDrag) {
            float desiredThumbY = e.mousePosition.y - status.DragAnchor;
            float clampedY = Mathf.Clamp(desiredThumbY, trackRect.y, trackRect.y + trackRange);
            float norm = trackRange > 0f ? (clampedY - trackRect.y) / trackRange : 0f;
            status.Position = new Vector2(status.Position.x, norm * scrollRange);
            status.LastScrollAtRealtime = Time.realtimeSinceStartup;
            e.Use();
            return;
        }

        if (e.type == EventType.MouseUp || e.rawType == EventType.MouseUp) {
            status.Dragging = false;
            ActiveDragRegistry.Release();
            if (e.type == EventType.MouseUp) {
                e.Use();
            }
        }
    }

    private void PaintEdgeFades() {
        if (Event.current.type != EventType.Repaint) {
            return;
        }

        Theme.Theme theme = RenderContext.Current.Theme;
        Color fadeColor = theme.GetColor(ThemeSlot.Glass3);
        Color transparent = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);

        float scrollRange = Mathf.Max(0f, contentHeight - viewHeight);
        bool atTop = status.Position.y < 1f;
        bool atBottom = scrollRange < 1f || status.Position.y >= scrollRange - 1f;

        if (!atTop) {
            Texture2D topTex = GradientTextureCache.Vertical(fadeColor, transparent);
            Rect topRect = new Rect(outRect.x, outRect.y, outRect.width, EdgeFadeHeight);
            GUI.DrawTexture(topRect, topTex, ScaleMode.StretchToFill, true);
        }

        if (!atBottom) {
            Texture2D bottomTex = GradientTextureCache.Vertical(transparent, fadeColor);
            Rect bottomRect = new Rect(outRect.x, outRect.yMax - EdgeFadeHeight, outRect.width, EdgeFadeHeight);
            GUI.DrawTexture(bottomRect, bottomTex, ScaleMode.StretchToFill, true);
        }
    }

    private void PaintBareMasks() {
        if (Event.current.type != EventType.Repaint) {
            return;
        }

        Theme.Theme theme = RenderContext.Current.Theme;
        Color maskColor = theme.GetColor(ThemeSlot.SurfacePrimary);
        Color transparent = new Color(maskColor.r, maskColor.g, maskColor.b, 0f);

        float scrollRange = Mathf.Max(0f, contentHeight - viewHeight);
        bool atTop = status.Position.y < 1f;
        bool atBottom = scrollRange < 1f || status.Position.y >= scrollRange - 1f;

        if (!atTop) {
            Texture2D topTex = GradientTextureCache.Vertical(maskColor, transparent);
            Rect topRect = new Rect(outRect.x, outRect.y, outRect.width, BareMaskHeight);
            GUI.DrawTexture(topRect, topTex, ScaleMode.StretchToFill, true);
        }

        if (!atBottom) {
            Texture2D bottomTex = GradientTextureCache.Vertical(transparent, maskColor);
            Rect bottomRect = new Rect(outRect.x, outRect.yMax - BareMaskHeight, outRect.width, BareMaskHeight);
            GUI.DrawTexture(bottomRect, bottomTex, ScaleMode.StretchToFill, true);
        }
    }

    private void ScrollBy(float delta, float scrollRange) {
        float next = Mathf.Clamp(status.Position.y + delta, 0f, scrollRange);
        status.Position = new Vector2(status.Position.x, next);
    }
}


