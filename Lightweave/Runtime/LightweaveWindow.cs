using System;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Theme;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Cosmere.Lightweave.Runtime;

public abstract class LightweaveWindow : Verse.Window {
    [Flags]
    private enum ResizeEdge {
        None = 0,
        Top = 1,
        Bottom = 2,
        Left = 4,
        Right = 8,
    }

    protected bool drawOwnCloseX;
    private ResizeEdge activeResize;
    private Vector2 resizeAnchorScreen;
    private Rect resizeStartRect;
    private Texture2D? currentCursor;
    private bool wasMouseDown;
    private bool isWindowDragging;
    private float lastDragClickTime = -1f;
    private Vector2 lastDragClickPos;
    private bool isMaximized;
    private Rect prerestoreRect;
    private bool positionRestored;

    protected LightweaveWindow() {
        doWindowBackground = false;
        drawShadow = false;
        resizeable = false;
        draggable = false;
        closeOnCancel = true;
        closeOnAccept = false;
        closeOnClickedOutside = false;
        forcePause = true;
        absorbInputAroundWindow = true;
        layer = Verse.WindowLayer.Super;
        doCloseX = false;
        doCloseButton = false;
    }

    protected Guid RootId { get; } = Guid.NewGuid();

    protected internal virtual Theme.Theme? ThemeOverride => null;

    protected internal virtual Direction? DirectionOverride => null;

    [DocOverride("Wrap the shell in a Vignette.", TypeOverride = "bool", DefaultOverride = "true")]
    protected internal virtual bool DrawVignette => true;

    [DocOverride("Vignette falloff shape.", TypeOverride = "VignetteShape", DefaultOverride = "Radial")]
    protected internal virtual VignetteShape VignetteShape => Cosmere.Lightweave.Rendering.VignetteShape.Radial;

    [DocOverride("Vignette alpha multiplier (0-1).", TypeOverride = "float", DefaultOverride = "0.6")]
    protected internal virtual float VignetteIntensity => 0.6f;

    [DocOverride("Vignette coverage multiplier. >1 = darker/wider, <1 = lighter/narrower.", TypeOverride = "float", DefaultOverride = "1.4")]
    protected internal virtual float VignetteScale => 1.4f;

    [DocOverride("Vignette color. Defaults to ThemeSlot.OverlayDim.", TypeOverride = "ColorRef?", DefaultOverride = "null")]
    protected internal virtual ColorRef? VignetteColor => null;

    [DocOverride("Full-screen scrim drawn behind the card to block click-through.", TypeOverride = "bool", DefaultOverride = "true")]
    protected internal virtual bool DrawScrim => true;

    [DocOverride("Scrim fill color. Defaults to theme ScrimDefault with alpha 0.55.", TypeOverride = "Color?", DefaultOverride = "null")]
    protected internal virtual Color? ScrimColor => null;

    [DocOverride("Vertical accent gradient layered inside the card.", TypeOverride = "bool", DefaultOverride = "true")]
    protected virtual bool DrawAccentGradient => true;

    [DocOverride("Top color of the accent gradient. Defaults to rgba(0.831, 0.659, 0.341, 0.10).", TypeOverride = "Color?", DefaultOverride = "null")]
    protected virtual Color? GradientTopColor => null;

    [DocOverride("Bottom color of the accent gradient. Defaults to fully-transparent gold.", TypeOverride = "Color?", DefaultOverride = "null")]
    protected virtual Color? GradientBottomColor => null;

    [DocOverride("Card background. Defaults to BackgroundSpec.Blur(rgba(0,0,0,0.85), 10px).", TypeOverride = "BackgroundSpec?", DefaultOverride = "null")]
    protected virtual BackgroundSpec? CardBackground => null;

    [DocOverride("Card border. Defaults to 1/16rem BorderDefault on all sides.", TypeOverride = "BorderSpec?", DefaultOverride = "null")]
    protected virtual BorderSpec? CardBorder => null;

    [DocOverride("Card padding between border and content. Defaults to 1/16rem on all sides.", TypeOverride = "EdgeInsets?", DefaultOverride = "null")]
    protected virtual EdgeInsets? CardPadding => null;

    [DocOverride("Card corner radius. Defaults to RadiusScale.Xl on all sides.", TypeOverride = "RadiusSpec?", DefaultOverride = "null")]
    protected virtual RadiusSpec? CardRadius => null;

    [DocOverride("Absolute card width in pixels. Takes priority over WidthFraction when set.", TypeOverride = "float?", DefaultOverride = "null")]
    protected virtual float? CardWidth => null;

    [DocOverride("Absolute card height in pixels. Takes priority over HeightFraction when set.", TypeOverride = "float?", DefaultOverride = "null")]
    protected virtual float? CardHeight => null;

    [DocOverride("Fraction of screen width occupied by the card.", TypeOverride = "float", DefaultOverride = "0.66")]
    protected virtual float WidthFraction => 0.66f;

    [DocOverride("Fraction of screen height occupied by the card.", TypeOverride = "float", DefaultOverride = "0.82")]
    protected virtual float HeightFraction => 0.82f;

    [DocOverride("Hard upper bound for card width.", TypeOverride = "float", DefaultOverride = "1800")]
    protected virtual float MaxCardWidth => 1800f;

    [DocOverride("Hard upper bound for card height.", TypeOverride = "float", DefaultOverride = "1300")]
    protected virtual float MaxCardHeight => 1300f;

    protected override float Margin => 0f;

    [DocOverride("Allow the user to drag any window edge to resize.", TypeOverride = "bool", DefaultOverride = "false")]
    protected virtual bool EdgeResizable => true;

    protected virtual float EdgeResizeThickness => 8f;

    [DocOverride("Minimum allowed window dimensions when edge-resizing.", TypeOverride = "Vector2", DefaultOverride = "(360, 240)")]
    protected virtual Vector2 MinWindowSize => new Vector2(360f, 240f);

    [DocOverride("Toggle maximize when the header is double-clicked.", TypeOverride = "bool", DefaultOverride = "false")]
    protected virtual bool EnableDoubleClickMaximize => false;

    [DocOverride("Per-savegame Scribe key for persisting window position. Null disables persistence.", TypeOverride = "string?", DefaultOverride = "null")]
    protected virtual string? PersistPositionKey => null;

    [DocOverride("Top chrome slot. Return a WindowHeader (or any node) to add a title bar; return null for a chromeless window.", TypeOverride = "LightweaveNode?", DefaultOverride = "null")]
    protected virtual LightweaveNode? Header() {
        return null;
    }

    [DocOverride("Required override returning the body content node tree.", TypeOverride = "LightweaveNode")]
    protected abstract LightweaveNode Body();

    [DocOverride("Bottom chrome slot. Return a WindowFooter (or any node) for a status bar / dialog button row.", TypeOverride = "LightweaveNode?", DefaultOverride = "null")]
    protected virtual LightweaveNode? Footer() {
        return null;
    }

    public override Vector2 InitialSize {
        get {
            float w = CardWidth ?? Mathf.Min(Verse.UI.screenWidth * WidthFraction, MaxCardWidth);
            float h = CardHeight ?? Mathf.Min(Verse.UI.screenHeight * HeightFraction, MaxCardHeight);
            return new Vector2(w, h);
        }
    }


    [DocOverride("Drag-grab region resolved each frame. Default reads the rect that WindowHeader publishes.", TypeOverride = "Rect?", DefaultOverride = "WindowHeader rect")]
    protected virtual Rect? DragRegion(Rect inRect) {
        Rect? headerRect = LightweaveWindowContext.HeaderRect;
        if (headerRect.HasValue && LightweaveWindowContext.HeaderDraggable) {
            return headerRect.Value;
        }

        return null;
    }

    public override void PreOpen() {
        base.PreOpen();
        drawOwnCloseX |= doCloseX;
        doCloseX = false;
        TryRestorePersistedPosition();
        if (DrawScrim || DrawVignette) {
            Patch.LightweaveBackdropRegistry.Register(this);
        }
    }

    private void TryRestorePersistedPosition() {
        if (positionRestored) {
            return;
        }

        positionRestored = true;
        string? key = PersistPositionKey;
        if (key == null) {
            return;
        }

        LightweaveWindowPositionStore? store = LightweaveWindowPositionStore.GetOrNull();
        if (store == null) {
            return;
        }

        if (!store.TryGet(key, out Rect saved)) {
            return;
        }

        saved.x = Mathf.Clamp(saved.x, 0f, Mathf.Max(0f, Verse.UI.screenWidth - saved.width));
        saved.y = Mathf.Clamp(saved.y, 0f, Mathf.Max(0f, Verse.UI.screenHeight - saved.height));
        saved.width = Mathf.Clamp(saved.width, MinWindowSize.x, Verse.UI.screenWidth);
        saved.height = Mathf.Clamp(saved.height, MinWindowSize.y, Verse.UI.screenHeight);
        windowRect = saved;
    }

    public override void WindowOnGUI() {
        bool selfActive = isWindowDragging || activeResize != ResizeEdge.None;
        if (!selfActive && ActiveDragRegistry.IsActiveFromOther(RootId)) {
            EventType et = Event.current.type;
            if (et == EventType.MouseDrag || et == EventType.Used) {
                return;
            }
        }
        base.WindowOnGUI();
    }

    public override void DoWindowContents(Rect inRect) {
        bool selfActive = isWindowDragging || activeResize != ResizeEdge.None;
        EventType et = Event.current.type;
        bool isHotEvent = et == EventType.Layout
            || et == EventType.MouseDrag
            || et == EventType.Used;

        if (!selfActive && ActiveDragRegistry.IsActiveFromOther(RootId) && isHotEvent) {
            return;
        }

        if (EdgeResizable) {
            HandleEdgeResize(inRect);
            UpdateEdgeAbsorb(inRect);
        }

        if (selfActive && isHotEvent) {
            HandleWindowDrag(inRect);
            return;
        }

        LightweaveWindowContext.Reset();

        LightweaveRoot.Render(inRect, RootId, BuildRoot, DirectionOverride, ThemeOverride, AfterContent);

        UpdateCursor(inRect);

        HandleWindowDrag(inRect);
    }

    private LightweaveNode BuildRoot() {
        Theme.Theme theme = RenderContext.Current.Theme;

        BackgroundSpec resolvedCardBg = CardBackground ?? BackgroundSpec.Blur(new Color(0f, 0f, 0f, 0.95f), 10f);
        BorderSpec resolvedCardBorder = CardBorder ?? BorderSpec.All(new Rem(1f / 16f), ThemeSlot.BorderDefault);
        EdgeInsets resolvedCardPadding = CardPadding ?? EdgeInsets.All(new Rem(1f / 16f));
        RadiusSpec resolvedCardRadius = CardRadius ?? RadiusSpec.All(RadiusScale.Xl);
        Color resolvedGradientTop = GradientTopColor ?? new Color(0.831f, 0.659f, 0.341f, 0.10f);
        Color resolvedGradientBottom = GradientBottomColor ?? new Color(0.831f, 0.659f, 0.341f, 0.0f);

        Rem innerR = RadiusSpec.ResolveRem(RadiusScale.Xl);
        LightweaveWindowContext.RequestHeaderRadius(RadiusSpec.Top(innerR));
        LightweaveWindowContext.RequestFooterRadius(RadiusSpec.Bottom(innerR));

        LightweaveNode? header = Header();
        LightweaveNode body = Body();
        LightweaveNode? footer = Footer();

        LightweaveNode contentStack = Layout.Stack.Create(
            SpacingScale.None,
            children: s => {
                if (header != null) {
                    s.Add(header);
                }
                s.AddFlex(body);
                if (footer != null) {
                    s.Add(footer);
                }
            }
        );

        LightweaveNode card = Layout.Box.Create(
            children: c => {
                if (DrawAccentGradient) {
                    c.Add(Layout.Box.Create(style: new Style {
                        Position = Position.Absolute,
                        Top = new Rem(0f),
                        Right = new Rem(0f),
                        Bottom = new Rem(0f),
                        Left = new Rem(0f),
                        Background = new BackgroundSpec.Gradient(
                            GradientTextureCache.Vertical(resolvedGradientTop, resolvedGradientBottom)
                        ),
                    }));
                }
                c.Add(contentStack);
            },
            style: new Style {
                Position = Position.Relative,
                Background = resolvedCardBg,
                Border = resolvedCardBorder,
                Padding = resolvedCardPadding,
                Radius = resolvedCardRadius,
            }
        );

        return card;
    }

    private void AfterContent() {
        if (!drawOwnCloseX || LightweaveWindowContext.HeaderOwnsClose) {
            return;
        }

        Rect? header = LightweaveWindowContext.HeaderRect;
        DrawCloseX(header ?? new Rect(0f, 0f, 0f, 0f));
    }

    

    private void HandleWindowDrag(Rect inRect) {
        if (activeResize != ResizeEdge.None) {
            return;
        }

        Rect? dragRectOpt = DragRegion(inRect);
        if (!dragRectOpt.HasValue) {
            if (isWindowDragging) {
                isWindowDragging = false;
                ActiveDragRegistry.Release();
            }
            return;
        }

        Rect dragRect = dragRectOpt.Value;
        Event e = Event.current;
        EventType raw = e.rawType;

        Vector2 screenTL = new Vector2(
            UnityEngine.Input.mousePosition.x,
            Verse.UI.screenHeight - UnityEngine.Input.mousePosition.y
        );
        Vector2 windowLocal = screenTL - new Vector2(windowRect.x, windowRect.y);

        if (isWindowDragging && raw == EventType.MouseUp && e.button == 0) {
            isWindowDragging = false;
            ActiveDragRegistry.Release();
        }

        if (!isWindowDragging
            && raw == EventType.MouseDown
            && e.button == 0
            && dragRect.Contains(windowLocal)
            && !LightweaveHitTracker.IsOver(windowLocal)) {
            float now = Time.realtimeSinceStartup;
            bool isDoubleClick = EnableDoubleClickMaximize
                && lastDragClickTime > 0f
                && now - lastDragClickTime < 0.3f
                && (windowLocal - lastDragClickPos).sqrMagnitude < 25f;

            if (isDoubleClick) {
                ToggleMaximized();
                lastDragClickTime = -1f;
                if (e.type == EventType.MouseDown && e.button == 0) {
                    e.Use();
                }
                return;
            }

            lastDragClickTime = now;
            lastDragClickPos = windowLocal;
            isWindowDragging = true;
            ActiveDragRegistry.Acquire(RootId);
        }

        if (LightweaveHitTracker.IsOver(windowLocal)) {
            return;
        }

        GUI.DragWindow(dragRect);
    }

    private void ToggleMaximized() {
        if (isMaximized) {
            windowRect = prerestoreRect;
            isMaximized = false;
        }
        else {
            prerestoreRect = windowRect;
            windowRect = new Rect(0f, 0f, Verse.UI.screenWidth, Verse.UI.screenHeight);
            isMaximized = true;
        }
    }

    /// Dynamically toggle <see cref="Verse.Window.absorbInputAroundWindow"/> so that a click
    /// landing in the edge-buffer zone just outside the window's rect (where we still show a
    /// resize cursor) cannot leak through to the map or a window beneath us. Also held true
    /// for the duration of an active resize so stray clicks during the drag are absorbed.
    private void UpdateEdgeAbsorb(Rect inRect) {
        if (activeResize != ResizeEdge.None) {
            absorbInputAroundWindow = true;
            return;
        }

        Vector2 screenTL = new Vector2(
            UnityEngine.Input.mousePosition.x,
            Verse.UI.screenHeight - UnityEngine.Input.mousePosition.y
        );
        if (windowRect.Contains(screenTL)) {
            absorbInputAroundWindow = false;
            return;
        }

        Vector2 windowLocal = screenTL - new Vector2(windowRect.x, windowRect.y);
        absorbInputAroundWindow = DetectEdge(inRect, windowLocal) != ResizeEdge.None;
    }

    private void HandleEdgeResize(Rect inRect) {
        Event e = Event.current;
        bool mouseDownNow = UnityEngine.Input.GetMouseButton(0);

        Vector2 screenTL = new Vector2(
            UnityEngine.Input.mousePosition.x,
            Verse.UI.screenHeight - UnityEngine.Input.mousePosition.y
        );

        if (activeResize == ResizeEdge.None) {
            if (mouseDownNow && !wasMouseDown) {
                Vector2 windowLocal = screenTL - new Vector2(windowRect.x, windowRect.y);
                ResizeEdge edge = DetectEdge(inRect, windowLocal);
                if (edge != ResizeEdge.None) {
                    activeResize = edge;
                    resizeAnchorScreen = screenTL;
                    resizeStartRect = windowRect;
                    ActiveDragRegistry.Acquire(RootId);
                    if (e.type == EventType.MouseDown && e.button == 0) {
                        e.Use();
                    }
                }
            }

            wasMouseDown = mouseDownNow;
            return;
        }

        Vector2 delta = screenTL - resizeAnchorScreen;

        Rect next = resizeStartRect;
        if ((activeResize & ResizeEdge.Right) != 0) {
            next.width = Mathf.Max(MinWindowSize.x, resizeStartRect.width + delta.x);
        }

        if ((activeResize & ResizeEdge.Left) != 0) {
            float proposed = resizeStartRect.width - delta.x;
            if (proposed < MinWindowSize.x) {
                proposed = MinWindowSize.x;
                next.x = resizeStartRect.xMax - proposed;
            }
            else {
                next.x = resizeStartRect.x + delta.x;
            }

            next.width = proposed;
        }

        if ((activeResize & ResizeEdge.Bottom) != 0) {
            next.height = Mathf.Max(MinWindowSize.y, resizeStartRect.height + delta.y);
        }

        if ((activeResize & ResizeEdge.Top) != 0) {
            float proposed = resizeStartRect.height - delta.y;
            if (proposed < MinWindowSize.y) {
                proposed = MinWindowSize.y;
                next.y = resizeStartRect.yMax - proposed;
            }
            else {
                next.y = resizeStartRect.y + delta.y;
            }

            next.height = proposed;
        }

        next.x = Mathf.Max(0f, next.x);
        next.y = Mathf.Max(0f, next.y);
        next.width = Mathf.Min(Verse.UI.screenWidth - next.x, next.width);
        next.height = Mathf.Min(Verse.UI.screenHeight - next.y, next.height);

        windowRect = next;

        if (!mouseDownNow) {
            activeResize = ResizeEdge.None;
            ActiveDragRegistry.Release();
        }

        if (e.type == EventType.MouseDrag || e.type == EventType.MouseUp) {
            e.Use();
        }

        wasMouseDown = mouseDownNow;
    }

    private void UpdateCursor(Rect inRect) {
        if (activeResize != ResizeEdge.None) {
            ApplyCursor(ResizeCursorFor(activeResize));
            return;
        }

        Vector2 mouse = Event.current.mousePosition;

        ResizeEdge edge = EdgeResizable ? DetectEdge(inRect, mouse) : ResizeEdge.None;
        if (edge != ResizeEdge.None) {
            ApplyCursor(ResizeCursorFor(edge));
            return;
        }

        Rect? dragRect = DragRegion(inRect);
        if (dragRect.HasValue && dragRect.Value.Contains(mouse) && !LightweaveHitTracker.IsOver(mouse)) {
            ApplyCursor(LightweaveCursors.Move);
            return;
        }

        ApplyCursor(null);
    }

    private ResizeEdge DetectEdge(Rect inRect, Vector2 mouse) {
        float t = EdgeResizeThickness;
        bool inside =
            mouse.x >= inRect.x - t &&
            mouse.x <= inRect.xMax + t &&
            mouse.y >= inRect.y - t &&
            mouse.y <= inRect.yMax + t;
        if (!inside) {
            return ResizeEdge.None;
        }

        bool left = mouse.x >= inRect.x - t && mouse.x < inRect.x + t;
        bool right = mouse.x > inRect.xMax - t && mouse.x <= inRect.xMax + t;
        bool top = mouse.y >= inRect.y - t && mouse.y < inRect.y + t;
        bool bottom = mouse.y > inRect.yMax - t && mouse.y <= inRect.yMax + t;

        ResizeEdge edge = ResizeEdge.None;
        if (top) {
            edge |= ResizeEdge.Top;
        }

        if (bottom) {
            edge |= ResizeEdge.Bottom;
        }

        if (left) {
            edge |= ResizeEdge.Left;
        }

        if (right) {
            edge |= ResizeEdge.Right;
        }

        return edge;
    }

    private void ApplyCursor(Texture2D? desired) {
        if (desired == currentCursor) {
            return;
        }

        currentCursor = desired;
        if (desired == null) {
            CursorOverrides.RestoreDefault();
            return;
        }

        Cursor.SetCursor(desired, LightweaveCursors.Hotspot, CursorMode.Auto);
    }

    private static Texture2D? ResizeCursorFor(ResizeEdge edge) {
        if (edge == ResizeEdge.None) {
            return null;
        }

        if (edge == (ResizeEdge.Top | ResizeEdge.Left) ||
            edge == (ResizeEdge.Bottom | ResizeEdge.Right)) {
            return LightweaveCursors.DiagonalNwSe;
        }

        if (edge == (ResizeEdge.Top | ResizeEdge.Right) ||
            edge == (ResizeEdge.Bottom | ResizeEdge.Left)) {
            return LightweaveCursors.DiagonalNeSw;
        }

        if ((edge & (ResizeEdge.Left | ResizeEdge.Right)) != 0) {
            return LightweaveCursors.Horizontal;
        }

        return LightweaveCursors.Vertical;
    }

    private void DrawCloseX(Rect anchor) {
        float padding = SpacingScale.Sm.ToPixels();
        float size = new Rem(1.125f).ToPixels();
        Rect closeRect = new Rect(
            anchor.xMax - size - padding,
            anchor.y + padding,
            size,
            size
        );
        LightweaveHitTracker.Track(closeRect);

        Theme.Theme theme = ThemeOverride ?? ThemeRegistry.Active;
        Color accent = theme.GetColor(ThemeSlot.SurfaceAccent);
        accent.a = 1f;
        Color baseColor = theme.GetColor(ThemeSlot.TextPrimary);
        Color hoverColor = accent;

        if (Widgets.ButtonImage(closeRect, TexButton.CloseXSmall, baseColor, hoverColor, true, null)) {
            Close();
        }

        MouseoverSounds.DoRegion(closeRect);
    }


    public override void PostClose() {
        if (isWindowDragging) {
            isWindowDragging = false;
            ActiveDragRegistry.Release();
        }

        if (activeResize != ResizeEdge.None) {
            activeResize = ResizeEdge.None;
            ActiveDragRegistry.Release();
        }

        if (currentCursor != null) {
            CursorOverrides.RestoreDefault();
            currentCursor = null;
        }

        TryPersistPosition();

        Patch.LightweaveBackdropRegistry.Unregister(this);

        LightweaveRoot.Release(RootId);
        base.PostClose();
    }

    private void TryPersistPosition() {
        string? key = PersistPositionKey;
        if (key == null) {
            return;
        }

        LightweaveWindowPositionStore? store = LightweaveWindowPositionStore.GetOrNull();
        if (store == null) {
            return;
        }

        Rect rectToStore = isMaximized ? prerestoreRect : windowRect;
        store.Set(key, rectToStore);
    }
}
