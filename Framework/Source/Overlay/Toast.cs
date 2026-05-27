using System;
using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Icons;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;
using Verse.Sound;
using static Cosmere.Lightweave.Hooks.Hooks;
using Cosmere.Lightweave.Layout;

namespace Cosmere.Lightweave.Overlay;

public enum ToastKind {
    Info,
    Success,
    Warning,
    Danger,
}

public enum ToastPosition {
    TopLeft,
    TopCenter,
    TopRight,
    MiddleLeft,
    MiddleCenter,
    MiddleRight,
    BottomLeft,
    BottomCenter,
    BottomRight,
}

public enum ToastTarget {
    CurrentWindow,
    GameWindow,
}

public sealed record ToastMessage(
    string Id,
    string Text,
    ToastKind Kind = ToastKind.Info,
    float DurationSeconds = 4f,
    string? Title = null,
    string? Meta = null
);

[Doc(
    Id = "toast",
    Summary = "Stacked transient notifications anchored to a window corner.",
    WhenToUse = "Confirm an action, surface a non-blocking warning, or report a result.",
    SourcePath = "Lightweave/Overlay/Toast.cs"
)]
public static class Toast {
    public static LightweaveNode Create(
        [DocParam("Active toast messages to render.")]
        IReadOnlyList<ToastMessage> toasts,
        [DocParam("Invoked with a toast id when it should be removed.")]
        Action<string> onDismiss,
        [DocParam("Anchor corner or edge for the stack.")]
        ToastPosition position = ToastPosition.BottomRight,
        [DocParam("Whether the stack is positioned in the host window or the full screen.")]
        ToastTarget target = ToastTarget.GameWindow,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        Hooks.Hooks.RefHandle<Dictionary<string, float>> spawnsRef = Hooks.Hooks.UseRef<Dictionary<string, float>>(
            new Dictionary<string, float>(),
            line,
            file
        );

        List<string> staleScratch = new List<string>(8);
        List<string> expiredScratch = new List<string>(8);
        HashSet<string> presentScratch = new HashSet<string>();

        LightweaveNode node = NodeBuilder.New($"Toast:{position}", line, file);
        node.ApplyStyling("toast", style, classes, id);
        node.MeasureWidth = () => Mathf.Ceil(new Rem(20f).ToPixels());
        node.Measure = _ => 0f;
        node.Paint = (_, _) => {
            Dictionary<string, float> spawns = spawnsRef.Current;
            float now = Time.unscaledTime;

            HashSet<string> presentIds = presentScratch;
            presentIds.Clear();
            for (int i = 0; i < toasts.Count; i++) {
                ToastMessage msg = toasts[i];
                presentIds.Add(msg.Id);
                if (!spawns.ContainsKey(msg.Id)) {
                    spawns[msg.Id] = now;
                }
            }

            List<string> stale = staleScratch;
            stale.Clear();
            foreach (KeyValuePair<string, float> kvp in spawns) {
                if (!presentIds.Contains(kvp.Key)) {
                    stale.Add(kvp.Key);
                }
            }

            for (int i = 0; i < stale.Count; i++) {
                spawns.Remove(stale[i]);
            }

            List<string> expired = expiredScratch;
            expired.Clear();
            for (int i = 0; i < toasts.Count; i++) {
                ToastMessage msg = toasts[i];
                float spawnTime = spawns[msg.Id];
                if (msg.DurationSeconds > 0f && now - spawnTime > msg.DurationSeconds) {
                    expired.Add(msg.Id);
                }
            }

            for (int i = 0; i < expired.Count; i++) {
                onDismiss?.Invoke(expired[i]);
            }

            if (toasts.Count == 0) {
                return;
            }

            float widthPx = new Rem(20f).ToPixels();
            float gapPx = SpacingScale.Sm.ToPixels();
            float marginPx = new Rem(1.5f).ToPixels();
            float fadePx = 0.2f;
            float stripWidth = new Rem(0.1875f).ToPixels();
            float padXPx = new Rem(1f).ToPixels();
            float padYPx = new Rem(0.875f).ToPixels();
            float iconBoxSize = new Rem(1.25f).ToPixels();
            float closeSize = new Rem(1.125f).ToPixels();
            float bodyGapPx = SpacingScale.Xs.ToPixels();
            float metaGapPx = new Rem(0.25f).ToPixels();
            float progressHeightPx = 2f;

            Rect host = target == ToastTarget.GameWindow
                ? new Rect(0f, 0f, Screen.width, Screen.height)
                : RenderContext.Current.RootRect;

            HorizontalAnchor hAnchor = HorizontalOf(position);
            VerticalAnchor vAnchor = VerticalOf(position);

            float anchorX;
            switch (hAnchor) {
                case HorizontalAnchor.Left:
                    anchorX = host.x + marginPx;
                    break;
                case HorizontalAnchor.Right:
                    anchorX = host.xMax - widthPx - marginPx;
                    break;
                default:
                    anchorX = host.x + (host.width - widthPx) / 2f;
                    break;
            }

            int count = toasts.Count;
            float[] heights = new float[count];
            float[] alphas = new float[count];
            float[] progressFracs = new float[count];
            ToastMessage[] snapshot = new ToastMessage[count];

            Theme.Theme theme = RenderContext.Current.Theme;
            Direction dir = RenderContext.Current.Direction;
            Font titleFont = theme.GetFont(FontRole.Heading);
            int titlePixelSize = Mathf.RoundToInt(new Rem(0.9375f).ToFontPx());
            GUIStyle titleStyle = GuiStyleCache.GetOrCreate(titleFont, titlePixelSize);
            titleStyle.alignment = TextAnchor.UpperLeft;
            titleStyle.wordWrap = true;

            Font bodyFont = theme.GetFont(FontRole.Body);
            int bodyPixelSize = Mathf.RoundToInt(new Rem(0.8125f).ToFontPx());
            GUIStyle bodyStyle = GuiStyleCache.GetOrCreate(bodyFont, bodyPixelSize);
            bodyStyle.alignment = TextAnchor.UpperLeft;
            bodyStyle.wordWrap = true;

            Font metaFont = theme.GetFont(FontRole.Mono);
            int metaPixelSize = Mathf.RoundToInt(new Rem(0.625f).ToFontPx());
            GUIStyle metaStyle = GuiStyleCache.GetOrCreate(metaFont, metaPixelSize);
            metaStyle.alignment = TextAnchor.UpperLeft;
            metaStyle.wordWrap = false;

            float bodyLeft;
            float bodyRight;
            if (dir == Direction.Ltr) {
                bodyLeft = stripWidth + padXPx + iconBoxSize + bodyGapPx;
                bodyRight = padXPx + closeSize + bodyGapPx;
            }
            else {
                bodyLeft = padXPx + closeSize + bodyGapPx;
                bodyRight = stripWidth + padXPx + iconBoxSize + bodyGapPx;
            }

            float totalHeight = 0f;
            for (int i = 0; i < count; i++) {
                ToastMessage msg = toasts[i];
                snapshot[i] = msg;
                float spawnTime = spawns[msg.Id];
                float age = now - spawnTime;
                float remaining = msg.DurationSeconds - age;
                float fadeIn = fadePx > 0f ? Mathf.Clamp01(age / fadePx) : 1f;
                float fadeOut = fadePx > 0f ? Mathf.Clamp01(remaining / fadePx) : 1f;
                alphas[i] = fadeIn * fadeOut;
                progressFracs[i] = msg.DurationSeconds > 0f
                    ? Mathf.Clamp01(remaining / msg.DurationSeconds)
                    : 0f;

                float bodyWidth = Mathf.Max(0f, widthPx - bodyLeft - bodyRight);

                bool hasTitle = !string.IsNullOrEmpty(msg.Title);
                bool hasMeta = !string.IsNullOrEmpty(msg.Meta);

                float titleH = hasTitle ? titleStyle.CalcHeight(new GUIContent(msg.Title), bodyWidth) : 0f;
                float bodyH = string.IsNullOrEmpty(msg.Text) ? 0f : bodyStyle.CalcHeight(new GUIContent(msg.Text), bodyWidth);
                float metaH = hasMeta ? metaStyle.CalcHeight(new GUIContent(msg.Meta!.ToUpperInvariant()), bodyWidth) : 0f;

                float stack = 0f;
                if (hasTitle) {
                    stack += titleH;
                    if (bodyH > 0f) stack += bodyGapPx;
                }
                stack += bodyH;
                if (hasMeta) {
                    stack += metaGapPx + metaH;
                }

                float rowHeight = Mathf.Max(iconBoxSize, stack) + padYPx * 2f + progressHeightPx;
                heights[i] = rowHeight;
                totalHeight += rowHeight;
                if (i < count - 1) {
                    totalHeight += gapPx;
                }

                if (alphas[i] < 1f || progressFracs[i] > 0f) {
                    AnimationClock.RegisterActive(RenderContext.Current.RootId);
                }
            }

            float startY;
            switch (vAnchor) {
                case VerticalAnchor.Top:
                    startY = host.y + marginPx;
                    break;
                case VerticalAnchor.Bottom:
                    startY = host.yMax - marginPx - totalHeight;
                    break;
                default:
                    startY = host.y + (host.height - totalHeight) / 2f;
                    break;
            }

            float[] positionsY = new float[count];
            float cursorY = startY;
            for (int i = 0; i < count; i++) {
                positionsY[i] = cursorY;
                cursorY += heights[i] + gapPx;
            }

            Action drawToasts = () => {
                for (int i = 0; i < count; i++) {
                    ToastMessage msg = snapshot[i];
                    float alpha = alphas[i];
                    float progressFrac = progressFracs[i];
                    Rect toastRect = new Rect(anchorX, positionsY[i], widthPx, heights[i]);

                    using (TintScope.Multiply(new Color(1f, 1f, 1f, alpha))) {
                        BackgroundSpec bg = BackgroundSpec.Of(ThemeSlot.Glass2);
                        BorderSpec border = BorderSpec.All(new Rem(1f / 16f), ThemeSlot.BorderDefault);
                        PaintBox.Draw(toastRect, bg, border, null);

                        ThemeSlot stripSlot = StripSlot(msg.Kind);
                        Color stripColor = theme.GetColor(stripSlot);
                        float stripX = dir == Direction.Ltr
                            ? toastRect.x
                            : toastRect.xMax - stripWidth;
                        Rect stripRect = new Rect(stripX, toastRect.y, stripWidth, toastRect.height);
                        PaintBox.Draw(stripRect, BackgroundSpec.Of(stripColor), null, null);

                        float iconX = dir == Direction.Ltr
                            ? toastRect.x + stripWidth + padXPx
                            : toastRect.xMax - stripWidth - padXPx - iconBoxSize;
                        Rect iconRect = new Rect(iconX, toastRect.y + padYPx, iconBoxSize, iconBoxSize);

                        Color iconBorder = stripColor;
                        iconBorder.a *= 0.4f;
                        PaintBox.Draw(
                            iconRect,
                            BackgroundSpec.Of(new Color(0f, 0f, 0f, 0f)),
                            BorderSpec.All(new Rem(1f / 16f), iconBorder),
                            null
                        );

                        IconRef iconRef = IconForKind(msg.Kind);
                        TextDraw.Draw(
                            iconRect,
                            iconRef.Glyph,
                            FontRole.Body,
                            new Rem(0.75f),
                            TextAnchor.MiddleCenter,
                            stripColor,
                            fontOverride: iconRef.ResolveFont()
                        );

                        float bodyX = dir == Direction.Ltr
                            ? toastRect.x + bodyLeft
                            : toastRect.x + bodyRight;
                        float bodyWidth = Mathf.Max(0f, toastRect.width - bodyLeft - bodyRight);

                        Color titleColor = theme.GetColor(ThemeSlot.TextPrimary);
                        Color bodyColor = theme.GetColor(ThemeSlot.TextSecondary);
                        Color metaColor = theme.GetColor(ThemeSlot.TextMuted);

                        float yCursor = toastRect.y + padYPx;
                        bool hasTitle = !string.IsNullOrEmpty(msg.Title);
                        bool hasMeta = !string.IsNullOrEmpty(msg.Meta);

                        if (hasTitle) {
                            float titleH = titleStyle.CalcHeight(new GUIContent(msg.Title), bodyWidth);
                            Rect titleRect = new Rect(bodyX, yCursor, bodyWidth, titleH);
                            TextDraw.DrawWithStyle(titleRect, msg.Title, titleStyle, titleColor);
                            yCursor += titleH;
                            if (!string.IsNullOrEmpty(msg.Text)) {
                                yCursor += bodyGapPx;
                            }
                        }

                        if (!string.IsNullOrEmpty(msg.Text)) {
                            float bodyH = bodyStyle.CalcHeight(new GUIContent(msg.Text), bodyWidth);
                            Rect bodyRect = new Rect(bodyX, yCursor, bodyWidth, bodyH);
                            TextDraw.DrawWithStyle(bodyRect, msg.Text, bodyStyle, bodyColor);
                            yCursor += bodyH;
                        }

                        if (hasMeta) {
                            yCursor += metaGapPx;
                            string metaText = msg.Meta!.ToUpperInvariant();
                            float metaH = metaStyle.CalcHeight(new GUIContent(metaText), bodyWidth);
                            Rect metaRect = new Rect(bodyX, yCursor, bodyWidth, metaH);
                            TextDraw.DrawWithStyle(metaRect, metaText, metaStyle, metaColor);
                        }

                        float closeX = dir == Direction.Ltr
                            ? toastRect.xMax - padXPx - closeSize
                            : toastRect.x + stripWidth + padXPx;
                        Rect closeRect = new Rect(closeX, toastRect.y + padYPx, closeSize, closeSize);

                        bool closeHovered = Mouse.IsOver(closeRect);
                        if (closeHovered) {
                            MouseoverSounds.DoRegion(closeRect);
                        }
                        Color closeColor = theme.GetColor(closeHovered ? ThemeSlot.TextPrimary : ThemeSlot.TextMuted);
                        IconRef xRef = Icons.Phosphor.X;
                        TextDraw.Draw(
                            closeRect,
                            xRef.Glyph,
                            FontRole.Body,
                            new Rem(0.75f),
                            TextAnchor.MiddleCenter,
                            closeColor,
                            fontOverride: xRef.ResolveFont()
                        );

                        if (progressFrac > 0f && msg.DurationSeconds > 0f) {
                            Color progressTrack = stripColor;
                            progressTrack.a *= 0.15f;
                            Rect trackRect = new Rect(
                                toastRect.x + stripWidth,
                                toastRect.yMax - progressHeightPx,
                                toastRect.width - stripWidth,
                                progressHeightPx
                            );
                            PaintBox.Draw(trackRect, BackgroundSpec.Of(progressTrack), null, null);
                            Rect fillRect;
                            if (dir == Direction.Ltr) {
                                fillRect = new Rect(trackRect.x, trackRect.y, trackRect.width * progressFrac, trackRect.height);
                            }
                            else {
                                float w = trackRect.width * progressFrac;
                                fillRect = new Rect(trackRect.xMax - w, trackRect.y, w, trackRect.height);
                            }
                            PaintBox.Draw(fillRect, BackgroundSpec.Of(stripColor), null, null);
                        }

                        Event e = Event.current;
                        if (e.type == EventType.MouseUp && e.button == 0 && closeRect.Contains(e.mousePosition)) {
                            onDismiss?.Invoke(msg.Id);
                            e.Use();
                        }
                    }
                }
            };

            if (target == ToastTarget.GameWindow) {
                GlobalOverlayHost.Enqueue(drawToasts, RenderContext.Current);
            }
            else {
                RenderContext.Current.PendingOverlays.Enqueue(drawToasts);
            }
        };
        return node;
    }

    private static IconRef IconForKind(ToastKind kind) {
        switch (kind) {
            case ToastKind.Success: return Icons.Phosphor.Check;
            case ToastKind.Warning: return Icons.Phosphor.Warning;
            case ToastKind.Danger: return Icons.Phosphor.WarningOctagon;
            default: return Icons.Phosphor.Info;
        }
    }

    private static HorizontalAnchor HorizontalOf(ToastPosition p) {
        switch (p) {
            case ToastPosition.TopLeft:
            case ToastPosition.MiddleLeft:
            case ToastPosition.BottomLeft:
                return HorizontalAnchor.Left;
            case ToastPosition.TopRight:
            case ToastPosition.MiddleRight:
            case ToastPosition.BottomRight:
                return HorizontalAnchor.Right;
            default:
                return HorizontalAnchor.Center;
        }
    }

    private static VerticalAnchor VerticalOf(ToastPosition p) {
        switch (p) {
            case ToastPosition.TopLeft:
            case ToastPosition.TopCenter:
            case ToastPosition.TopRight:
                return VerticalAnchor.Top;
            case ToastPosition.BottomLeft:
            case ToastPosition.BottomCenter:
            case ToastPosition.BottomRight:
                return VerticalAnchor.Bottom;
            default:
                return VerticalAnchor.Middle;
        }
    }

    private static ThemeSlot StripSlot(ToastKind kind) {
        switch (kind) {
            case ToastKind.Success:
                return ThemeSlot.StatusSuccess;
            case ToastKind.Warning:
                return ThemeSlot.StatusWarning;
            case ToastKind.Danger:
                return ThemeSlot.StatusDanger;
            default:
                return ThemeSlot.SurfaceAccent;
        }
    }

    private enum HorizontalAnchor {
        Left,
        Center,
        Right,
    }

    private enum VerticalAnchor {
        Top,
        Middle,
        Bottom,
    }

    private static LightweaveNode BuildVariantDemo(
        IReadOnlyList<(string buttonKey, string messageKey, ToastKind kind, Variant buttonVariant)> triggers,
        ToastPosition position = ToastPosition.BottomRight,
        ToastTarget target = ToastTarget.CurrentWindow,
        float duration = 4f
    ) {
        StateHandle<List<ToastMessage>> toasts = UseState(new List<ToastMessage>());
        RefHandle<int> counter = UseRef(0);

        string TitleForKind(ToastKind k) {
            switch (k) {
                case ToastKind.Success: return (string)"CL_Playground_toast_Title_Success".Translate();
                case ToastKind.Warning: return (string)"CL_Playground_toast_Title_Warning".Translate();
                case ToastKind.Danger: return (string)"CL_Playground_toast_Title_Danger".Translate();
                default: return (string)"CL_Playground_toast_Title_Info".Translate();
            }
        }

        void PushToast(string messageKey, ToastKind kind) {
            counter.Current = counter.Current + 1;
            DateTime now = DateTime.Now;
            string metaStamp = $"#{counter.Current:D3} · {now:HH:mm:ss}";
            List<ToastMessage> next = new List<ToastMessage>(toasts.Value) {
                new ToastMessage(
                    "playground-toast-" + counter.Current,
                    (string)messageKey.Translate(),
                    kind,
                    duration,
                    Title: TitleForKind(kind),
                    Meta: metaStamp
                ),
            };
            toasts.Set(next);
        }

        void DismissToast(string id) {
            List<ToastMessage> next = new List<ToastMessage>();
            for (int i = 0; i < toasts.Value.Count; i++) {
                if (toasts.Value[i].Id != id) {
                    next.Add(toasts.Value[i]);
                }
            }

            toasts.Set(next);
        }

        void DismissAll() {
            toasts.Set(new List<ToastMessage>());
        }

        LightweaveNode toastLayer = Create(toasts.Value, DismissToast, position: position, target: target);
        toastLayer.PreferredHeight = 0f;

        return Stack.Create(
            SpacingScale.Sm,
            s => {
                s.Add(HStack.Create(
                    SpacingScale.Sm,
                    row => {
                        for (int i = 0; i < triggers.Count; i++) {
                            (string buttonKey, string messageKey, ToastKind kind, Variant buttonVariant) trigger = triggers[i];
                            row.AddHug(Button.Create(
                                (string)trigger.buttonKey.Translate(),
                                () => PushToast(trigger.messageKey, trigger.kind),
                                trigger.buttonVariant
                            ));
                        }

                        if (duration <= 0f) {
                            row.AddHug(Button.Create(
                                (string)"CL_Playground_Toast_DismissAll".Translate(),
                                DismissAll,
                                Variant.Ghost
                            ));
                        }
                    }
                ));
                s.Add(toastLayer);
            }
        );
    }

    [DocVariant("CL_Playground_Toast_Kinds")]
    public static DocSample DocsKinds() {
        return new DocSample(() => BuildVariantDemo(new[] {
            ("CL_Playground_Toast_Info", "CL_Playground_Toast_Msg_Info", ToastKind.Info, Variant.Secondary),
            ("CL_Playground_Toast_Success", "CL_Playground_Toast_Msg_Success", ToastKind.Success, Variant.Primary),
            ("CL_Playground_Toast_Warning", "CL_Playground_Toast_Msg_Warning", ToastKind.Warning, Variant.Secondary),
            ("CL_Playground_Toast_Danger", "CL_Playground_Toast_Msg_Danger", ToastKind.Danger, Variant.Danger),
        }), helpers: new[] { nameof(BuildVariantDemo) });
    }

    [DocVariant("CL_Playground_Toast_PositionTopRight")]
    public static DocSample DocsPositionTopRight() {
        return new DocSample(() => BuildVariantDemo(new[] {
            ("CL_Playground_Toast_Info", "CL_Playground_Toast_Msg_Info", ToastKind.Info, Variant.Secondary),
            ("CL_Playground_Toast_Success", "CL_Playground_Toast_Msg_Success", ToastKind.Success, Variant.Primary),
        }, position: ToastPosition.TopRight), helpers: new[] { nameof(BuildVariantDemo) });
    }

    [DocVariant("CL_Playground_Toast_PositionTopCenter")]
    public static DocSample DocsPositionTopCenter() {
        return new DocSample(() => BuildVariantDemo(new[] {
            ("CL_Playground_Toast_Warning", "CL_Playground_Toast_Msg_Warning", ToastKind.Warning, Variant.Secondary),
        }, position: ToastPosition.TopCenter), helpers: new[] { nameof(BuildVariantDemo) });
    }

    [DocVariant("CL_Playground_Toast_Persistent")]
    public static DocSample DocsPersistent() {
        return new DocSample(() => BuildVariantDemo(new[] {
            ("CL_Playground_Toast_Info", "CL_Playground_Toast_Msg_Persistent", ToastKind.Info, Variant.Primary),
        }, duration: 0f), helpers: new[] { nameof(BuildVariantDemo) });
    }

    [DocVariant("CL_Playground_Toast_TargetScreen")]
    public static DocSample DocsTargetScreen() {
        return new DocSample(() => BuildVariantDemo(new[] {
            ("CL_Playground_Toast_Danger", "CL_Playground_Toast_Msg_Danger", ToastKind.Danger, Variant.Danger),
        }, position: ToastPosition.BottomRight, target: ToastTarget.GameWindow), helpers: new[] { nameof(BuildVariantDemo) });
    }

    [DocUsage]
    public static DocSample DocsUsage() {
        return new DocSample(() => BuildVariantDemo(new[] {
            ("CL_Playground_Toast_Info", "CL_Playground_Toast_Msg_Info", ToastKind.Info, Variant.Secondary),
        }), helpers: new[] { nameof(BuildVariantDemo) });
    }
}