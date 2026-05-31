using System;
using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Hooks;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;
using static Cosmere.Lightweave.Hooks.Hooks;
using Cosmere.Lightweave.Layout;
using Caption = Cosmere.Lightweave.Typography.Typography.Caption;
using Code = Cosmere.Lightweave.Typography.Typography.Code;
using Heading = Cosmere.Lightweave.Typography.Typography.Heading;
using Icon = Cosmere.Lightweave.Typography.Typography.Icon;
using Label = Cosmere.Lightweave.Typography.Typography.Label;
using RichText = Cosmere.Lightweave.Typography.Typography.RichText;
using Text = Cosmere.Lightweave.Typography.Typography.Text;

namespace Cosmere.Lightweave.Feedback;

public enum DrawerSide {
    Left,
    Right,
    Top,
    Bottom,
}

[Doc(
    Id = "drawer",
    Summary = "Edge-anchored panel that slides in from a window side.",
    WhenToUse = "Reveal secondary navigation, filters, or detail content without leaving the page.",
    SourcePath = "Lightweave/Overlay/Drawer.cs"
)]
public static class Drawer {
    private static readonly Func<float, float> EaseOutCubic = t => 1f - Mathf.Pow(1f - t, 3f);

    public static LightweaveNode Create(
        [DocParam("Whether the drawer is currently visible.")]
        bool isOpen,
        [DocParam("Which window edge the drawer slides in from.")]
        DrawerSide side,
        [DocParam("Builds the drawer body node.")]
        Func<LightweaveNode> content,
        [DocParam("Invoked when the user clicks the scrim or presses Escape.")]
        Action onDismiss,
        [DocParam("Drawer thickness in Rem; width for Left/Right, height for Top/Bottom.")]
        Rem? size = null,
        [DocParam("Small uppercase mono crumb above the title.")]
        string? crumb = null,
        [DocParam("Bold display title.")]
        string? title = null,
        [DocParam("Italic display subtitle below the title.")]
        string? subtitle = null,
        [DocParam("Optional footer slot (right-aligned actions).")]
        Func<LightweaveNode>? footer = null,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        LightweaveNode node = NodeBuilder.New($"Drawer:{side}", line, file);
        node.ApplyStyling("drawer", style, classes, id);
        node.MeasureWidth = () => {
            if (side == DrawerSide.Left || side == DrawerSide.Right) {
                return Mathf.Ceil((size ?? new Rem(20f)).ToPixels());
            }
            return 0f;
        };
        node.Measure = _ => {
            if (side == DrawerSide.Top || side == DrawerSide.Bottom) {
                return Mathf.Ceil((size ?? new Rem(12f)).ToPixels());
            }
            return 0f;
        };
        node.Paint = (_, _) => {
            float target = isOpen ? 1f : 0f;
            float progress = UseAnim.Animate(target, 0.22f, EaseOutCubic, line, file);

            if (!isOpen && progress <= 0.001f) {
                return;
            }

            Rect host = RenderContext.Current.RootRect;

            float widthPx;
            float heightPx;
            if (side == DrawerSide.Left || side == DrawerSide.Right) {
                widthPx = (size ?? new Rem(20f)).ToPixels();
                heightPx = host.height;
            }
            else {
                widthPx = host.width;
                heightPx = (size ?? new Rem(12f)).ToPixels();
            }

            float restX;
            float restY;
            float offscreenX;
            float offscreenY;
            switch (side) {
                case DrawerSide.Left:
                    restX = host.x;
                    restY = host.y;
                    offscreenX = host.x - widthPx;
                    offscreenY = host.y;
                    break;
                case DrawerSide.Right:
                    restX = host.xMax - widthPx;
                    restY = host.y;
                    offscreenX = host.xMax;
                    offscreenY = host.y;
                    break;
                case DrawerSide.Top:
                    restX = host.x;
                    restY = host.y;
                    offscreenX = host.x;
                    offscreenY = host.y - heightPx;
                    break;
                default:
                    restX = host.x;
                    restY = host.yMax - heightPx;
                    offscreenX = host.x;
                    offscreenY = host.yMax;
                    break;
            }

            float drawerX = Mathf.Lerp(offscreenX, restX, progress);
            float drawerY = Mathf.Lerp(offscreenY, restY, progress);
            Rect drawerRect = new Rect(drawerX, drawerY, widthPx, heightPx);
            float scrimAlpha = progress * 0.35f;

            bool hasHeader = !string.IsNullOrEmpty(crumb) || !string.IsNullOrEmpty(title) || !string.IsNullOrEmpty(subtitle) || onDismiss != null;
            bool hasFooter = footer != null;

            RenderContext.Current.PendingOverlays.Enqueue(() => {
                Rect screenRect = host;

                using (TintScope.Replace(Color.white)) {
                    Color scrimBase = RenderContext.Current.Theme.GetColor(ThemeSlot.ScrimDefault);
                    BackgroundSpec scrimBg = BackgroundSpec.Of(new Color(scrimBase.r, scrimBase.g, scrimBase.b, scrimAlpha));
                    PaintBox.Draw(screenRect, scrimBg, null, null);

                    RadiusSpec drawerRadius = ResolveRadius(side);

                    Rect shadowRect = new Rect(
                        drawerRect.x + 3f,
                        drawerRect.y + 3f,
                        drawerRect.width,
                        drawerRect.height
                    );
                    BackgroundSpec shadowBg = BackgroundSpec.Of(ThemeSlot.SurfaceShadow);
                    PaintBox.Draw(shadowRect, shadowBg, null, drawerRadius);

                    BackdropBlur.Draw(drawerRect, 10f);
                    PaintBox.Draw(drawerRect, BackgroundSpec.Of(ThemeSlot.Glass3), null, drawerRadius);
                    Color goldTop = new Color(0.831f, 0.659f, 0.341f, 0.10f);
                    Color goldBottom = new Color(0.831f, 0.659f, 0.341f, 0.0f);
                    PaintBox.Draw(
                        drawerRect,
                        new BackgroundSpec.Gradient(GradientTextureCache.Vertical(goldTop, goldBottom)),
                        null,
                        drawerRadius
                    );

                    LightweaveNode chrome = BuildChrome(crumb, title, subtitle, footer, content, hasHeader, hasFooter, onDismiss);
                    LightweaveRoot.PaintSubtree(chrome, drawerRect);

                    BorderSpec? drawerBorder = ResolveBorder(side);
                    PaintBox.Draw(drawerRect, null, drawerBorder, drawerRadius);
                }

                Event e = Event.current;
                if (e.type == EventType.MouseDown &&
                    !drawerRect.Contains(e.mousePosition) &&
                    screenRect.Contains(e.mousePosition)) {
                    onDismiss?.Invoke();
                    e.Use();
                }
                else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape) {
                    onDismiss?.Invoke();
                    e.Use();
                }
            }
            );
        };
        return node;
    }

    private static LightweaveNode BuildChrome(
        string? crumb,
        string? title,
        string? subtitle,
        Func<LightweaveNode>? footer,
        Func<LightweaveNode> content,
        bool hasHeader,
        bool hasFooter,
        Action? onDismiss
    ) {
        LightweaveNode? header = hasHeader ? BuildHeader(crumb, title, subtitle, onDismiss) : null;
        LightweaveNode body = BuildBody(content);
        LightweaveNode? footerNode = hasFooter ? BuildFooter(footer!) : null;

        LightweaveNode root = NodeBuilder.New("DrawerChrome", 0, nameof(Drawer));
        if (header != null) {
            root.Children.Add(header);
        }
        root.Children.Add(body);
        if (footerNode != null) {
            root.Children.Add(footerNode);
        }

        root.Paint = (rect, _) => {
            float headerH = header != null
                ? (header.Measure?.Invoke(rect.width) ?? header.PreferredHeight ?? 0f)
                : 0f;
            float footerH = footerNode != null
                ? (footerNode.Measure?.Invoke(rect.width) ?? footerNode.PreferredHeight ?? 0f)
                : 0f;
            float bodyH = Mathf.Max(0f, rect.height - headerH - footerH);

            float y = rect.y;
            if (header != null) {
                Rect hr = new Rect(rect.x, y, rect.width, headerH);
                header.MeasuredRect = hr;
                LightweaveRoot.PaintSubtree(header, hr);
                y += headerH;
            }

            Rect br = new Rect(rect.x, y, rect.width, bodyH);
            body.MeasuredRect = br;
            LightweaveRoot.PaintSubtree(body, br);
            y += bodyH;

            if (footerNode != null) {
                Rect fr = new Rect(rect.x, y, rect.width, footerH);
                footerNode.MeasuredRect = fr;
                LightweaveRoot.PaintSubtree(footerNode, fr);
            }
        };
        return root;
    }

    private static LightweaveNode BuildHeader(
        string? crumb,
        string? title,
        string? subtitle,
        Action? onDismiss
    ) {
        LightweaveNode titleBlock = Stack.Create(
            new Rem(0.25f),
            s => {
                if (!string.IsNullOrEmpty(crumb)) {
                    s.Add(
                        Caption.Create(
                            crumb!.ToUpper(),
                            style: new Style {
                                FontFamily = FontRole.Mono,
                                FontSize = new Rem(0.75f),
                                TextColor = ThemeSlot.TextMuted,
                            }
                        )
                    );
                }
                if (!string.IsNullOrEmpty(title)) {
                    s.Add(
                        Heading.Create(
                            2,
                            title!,
                            style: new Style {
                                FontFamily = FontRole.Heading,
                                FontSize = new Rem(1.5f),
                                TextColor = ThemeSlot.TextPrimary,
                            }
                        )
                    );
                }
                if (!string.IsNullOrEmpty(subtitle)) {
                    s.Add(
                        Text.Create(
                            subtitle!,
                            style: new Style {
                                FontFamily = FontRole.BodyBold,
                                FontSize = new Rem(0.75f),
                                FontWeight = FontStyle.Italic,
                                TextColor = ThemeSlot.TextSecondary,
                            }
                        )
                    );
                }
            }
        );

        LightweaveNode closeGlyph = Text.Create(
            "✕",
            style: new Style {
                FontFamily = FontRole.Body,
                FontSize = new Rem(1f),
                TextColor = ThemeSlot.TextSecondary,
                TextAlign = TextAlign.Center,
            }
        );
        LightweaveNode closeBtn = IconButton.Create(
            closeGlyph,
            () => onDismiss?.Invoke(),
            Variant.Secondary,
            iconSize: new Rem(0.875f)
        );

        EdgeInsets pad = new EdgeInsets(
            Top: new Rem(1.25f),
            Right: new Rem(1.375f),
            Bottom: new Rem(1f),
            Left: new Rem(1.375f)
        );

        LightweaveNode n = NodeBuilder.New("DrawerHeader", 0, nameof(Drawer));
        n.Children.Add(titleBlock);
        n.Children.Add(closeBtn);

        n.Measure = w => {
            (float l, float t, float r, float b) = pad.Resolve(Direction.Ltr);
            float closeW = closeBtn.MeasureWidth?.Invoke() ?? 0f;
            float closeGap = SpacingScale.Md.ToPixels();
            float titleW = Mathf.Max(0f, w - l - r - closeW - closeGap);
            float titleH = titleBlock.Measure?.Invoke(titleW) ?? 0f;
            float closeH = closeBtn.PreferredHeight ?? 0f;
            float bottomBorder = new Rem(1f / 16f).ToPixels();
            return Mathf.Max(titleH, closeH) + t + b + bottomBorder;
        };

        n.Paint = (rect, _) => {
            PaintBox.Draw(
                rect,
                null,
                new BorderSpec(Bottom: new Rem(1f / 16f), Color: ThemeSlot.BorderSubtle),
                null
            );

            (float l, float t, float r, float b) = pad.Resolve(RenderContext.Current.Direction);
            float closeW = closeBtn.MeasureWidth?.Invoke() ?? 0f;
            float closeH = closeBtn.PreferredHeight ?? 0f;
            float closeGap = SpacingScale.Md.ToPixels();
            float titleW = Mathf.Max(0f, rect.width - l - r - closeW - closeGap);
            float titleH = titleBlock.Measure?.Invoke(titleW) ?? 0f;
            float bottomBorder = new Rem(1f / 16f).ToPixels();
            float innerH = Mathf.Max(0f, rect.height - t - b - bottomBorder);

            Rect titleRect = new Rect(rect.x + l, rect.y + t, titleW, titleH);
            titleBlock.MeasuredRect = titleRect;
            LightweaveRoot.PaintSubtree(titleBlock, titleRect);

            Rect closeRect = new Rect(
                rect.xMax - r - closeW,
                rect.y + t,
                closeW,
                closeH
            );
            closeBtn.MeasuredRect = closeRect;
            LightweaveRoot.PaintSubtree(closeBtn, closeRect);
        };
        return n;
    }

    private static LightweaveNode BuildBody(Func<LightweaveNode> content) {
        LightweaveNode inner = content();
        EdgeInsets pad = new EdgeInsets(
            Top: new Rem(1.125f),
            Right: new Rem(1.375f),
            Bottom: new Rem(1.125f),
            Left: new Rem(1.375f)
        );

        LightweaveNode n = NodeBuilder.New("DrawerBody", 0, nameof(Drawer));
        n.Children.Add(inner);
        n.Paint = (rect, _) => {
            (float l, float t, float r, float b) = pad.Resolve(RenderContext.Current.Direction);
            Rect innerRect = new Rect(
                rect.x + l,
                rect.y + t,
                Mathf.Max(0f, rect.width - l - r),
                Mathf.Max(0f, rect.height - t - b)
            );
            inner.MeasuredRect = innerRect;
            LightweaveRoot.PaintSubtree(inner, innerRect);
        };
        return n;
    }

    private static LightweaveNode BuildFooter(Func<LightweaveNode> footer) {
        LightweaveNode inner = footer();
        EdgeInsets pad = new EdgeInsets(
            Top: new Rem(0.875f),
            Right: new Rem(1.125f),
            Bottom: new Rem(0.875f),
            Left: new Rem(1.125f)
        );

        LightweaveNode n = NodeBuilder.New("DrawerFooter", 0, nameof(Drawer));
        n.Children.Add(inner);

        n.Measure = w => {
            (float l, float t, float r, float b) = pad.Resolve(Direction.Ltr);
            float innerW = Mathf.Max(0f, w - l - r);
            float innerH = inner.Measure?.Invoke(innerW) ?? inner.PreferredHeight ?? 0f;
            float topBorder = new Rem(1f / 16f).ToPixels();
            return innerH + t + b + topBorder;
        };

        n.Paint = (rect, _) => {
            PaintBox.Draw(
                rect,
                BackgroundSpec.Of(new Color(0f, 0f, 0f, 0.4f)),
                new BorderSpec(Top: new Rem(1f / 16f), Color: ThemeSlot.BorderSubtle),
                null
            );

            (float l, float t, float r, float b) = pad.Resolve(RenderContext.Current.Direction);
            float topBorder = new Rem(1f / 16f).ToPixels();
            float innerW = Mathf.Max(0f, rect.width - l - r);
            float innerH = inner.Measure?.Invoke(innerW) ?? inner.PreferredHeight ?? 0f;
            float availH = Mathf.Max(0f, rect.height - t - b - topBorder);

            float natW = inner.MeasureWidth?.Invoke() ?? innerW;
            float drawW = Mathf.Min(natW, innerW);
            Rect innerRect = new Rect(
                rect.xMax - r - drawW,
                rect.y + topBorder + t,
                drawW,
                Mathf.Min(innerH, availH)
            );
            inner.MeasuredRect = innerRect;
            LightweaveRoot.PaintSubtree(inner, innerRect);
        };
        return n;
    }

    private static LightweaveNode BuildHostDemo() {
        StateHandle<bool> open = UseState(false);

        LightweaveNode trigger = Button.Create(
            (string)"CL_Playground_Drawer_TriggerOpen".Translate(),
            () => open.Set(!open.Value),
            Variant.Secondary
        );

        LightweaveNode drawer = Create(
            open.Value,
            DrawerSide.Right,
            () => Text.Create(
                (string)"CL_Playground_Drawer_ContentBody".Translate(),
                wrap: true,
                style: new Style {
                    FontFamily = FontRole.Body,
                    FontSize = new Rem(0.875f),
                    TextColor = ThemeSlot.TextPrimary,
                }
            ),
            () => open.Set(false),
            crumb: (string)"CL_Playground_Drawer_Crumb".Translate(),
            title: (string)"CL_Playground_Drawer_ContentTitle".Translate(),
            subtitle: (string)"CL_Playground_Drawer_Subtitle".Translate(),
            footer: () => HStack.Create(
                SpacingScale.Sm,
                r => {
                    r.AddHug(
                        Button.Create(
                            (string)"CL_Playground_Drawer_Cancel".Translate(),
                            () => open.Set(false),
                            Variant.Ghost
                        )
                    );
                    r.AddHug(
                        Button.Create(
                            (string)"CL_Playground_Drawer_Confirm".Translate(),
                            () => open.Set(false),
                            Variant.Primary
                        )
                    );
                }
            )
        );

        LightweaveNode composed = NodeBuilder.New("DrawerHost", 0, nameof(Drawer));
        composed.Children.Add(trigger);
        composed.Children.Add(drawer);
        composed.Measure = w => trigger.Measure?.Invoke(w) ?? trigger.PreferredHeight ?? 32f;
        composed.Paint = (rect, _) => {
            trigger.MeasuredRect = rect;
            LightweaveRoot.PaintSubtree(trigger, rect);
            drawer.MeasuredRect = rect;
            LightweaveRoot.PaintSubtree(drawer, rect);
        };
        return composed;
    }

    [DocVariant("CL_Playground_Label_Default")]
    public static DocSample DocsDefault() {
        return new DocSample(() => BuildHostDemo(), useFullSource: true);
    }

    [DocUsage]
    public static DocSample DocsUsage() {
        return new DocSample(() => BuildHostDemo(), useFullSource: true);
    }

    private static BorderSpec ResolveBorder(DrawerSide side) {
        Rem thickness = new Rem(1f / 16f);
        return new BorderSpec(thickness, thickness, thickness, thickness, Color: ThemeSlot.BorderDefault);
    }

    private static RadiusSpec ResolveRadius(DrawerSide side) {
        switch (side) {
            case DrawerSide.Left:
                return RadiusSpec.Right(RadiusScale.Lg);
            case DrawerSide.Right:
                return RadiusSpec.Left(RadiusScale.Lg);
            case DrawerSide.Top:
                return RadiusSpec.Bottom(RadiusScale.Lg);
            default:
                return RadiusSpec.Top(RadiusScale.Lg);
        }
    }
}