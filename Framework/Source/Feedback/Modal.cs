using System;
using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Hooks;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Surfaces;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;
using static Cosmere.Lightweave.Hooks.Hooks;
using Button = Cosmere.Lightweave.Input.Button;
using Text = Cosmere.Lightweave.Typography.Typography.Text;

namespace Cosmere.Lightweave.Feedback;

[Doc(
    Id = "modal",
    Summary = "Centered scrim-backed panel that floats over the current surface.",
    WhenToUse =
        "Host a focused task (an editor, a picker, a confirm) over the page without leaving it. Use Drawer instead when the panel should anchor to a window edge.",
    SourcePath = "Lightweave/Feedback/Modal.cs"
)]
public static class Modal {
    private static readonly Func<float, float> EaseOutCubic = t => 1f - Mathf.Pow(1f - t, 3f);

    public static LightweaveNode Create(
        [DocParam("Whether the modal is currently visible.")]
        bool isOpen,
        [DocParam("Builds the modal body node. Owns its own padding and chrome.")]
        Func<LightweaveNode> content,
        [DocParam("Invoked when the user clicks the scrim or presses Escape.")]
        Action onDismiss,
        [DocParam("Panel width in Rem. When omitted, defaults to 90% of the host rect capped at 64rem; an explicit value is respected up to 95% of the host.")]
        Rem? width = null,
        [DocParam("Panel height in Rem. When omitted, defaults to 88% of the host rect capped at 44rem; an explicit value is respected up to 95% of the host.")]
        Rem? height = null,
        [DocParam("Whether clicking the scrim outside the panel dismisses the modal.")]
        bool dismissOnScrimClick = true,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        LightweaveNode node = NodeBuilder.New("Modal", line, file);
        node.ApplyStyling("modal", style, classes, id);
        node.MeasureWidth = () => 0f;
        node.Measure = _ => 0f;

        // The body tree is built once and reused across paint events. Modal.Create only re-runs when
        // the cached root tree is rebuilt - which happens exactly on a HookStore version bump (a real
        // state change). Scrolling mutates a RefHandle without bumping the version, so the root stays
        // cached and this Modal node (and this closure) persists; rebuilding `content()` every paint
        // would reconstruct the entire body (e.g. the ideoligion editor's 25-row precept grid) on
        // every MouseDrag event (~500/sec during a scroll), which is the dominant scroll-lag cost.
        LightweaveNode? bodyCache = null;

        node.Paint = (_, _) => {
            float target = isOpen ? 1f : 0f;
            float progress = UseAnim.Animate(target, 0.18f, EaseOutCubic, line, file);

            if (!isOpen && progress <= 0.001f) {
                return;
            }

            Rect host = RenderContext.Current.RootRect;

            // Claim the whole host as a hover-block so every interactive widget and tooltip painted
            // behind the modal goes non-topmost (LightweaveHitTracker.IsTopmost / AsTooltip consult
            // HoverBlockRegistry when OverlayContentDepth == 0). The modal's own body paints with
            // OverlayContentDepth > 0 below, so its controls and tooltips stay live.
            HoverBlockRegistry.Register(GUIUtility.GUIToScreenRect(host));

            float maxW = host.width * 0.95f;
            float maxH = host.height * 0.95f;
            // An explicit caller size is respected and clamped only to the 95%-of-host safety bound.
            // The Rem hard-caps apply ONLY to the auto-default, so a large surface (e.g. the ideoligion
            // editor) can request a panel bigger than 64x44rem without being silently shrunk.
            float panelW = width?.ToPixels() ?? Mathf.Min(host.width * 0.9f, new Rem(64f).ToPixels());
            float panelH = height?.ToPixels() ?? Mathf.Min(host.height * 0.88f, new Rem(44f).ToPixels());
            panelW = Mathf.Min(panelW, maxW);
            panelH = Mathf.Min(panelH, maxH);

            float scale = Mathf.Lerp(0.96f, 1f, progress);
            float scaledW = panelW * scale;
            float scaledH = panelH * scale;
            Rect panelRect = new Rect(
                host.center.x - scaledW * 0.5f,
                host.center.y - scaledH * 0.5f,
                scaledW,
                scaledH
            );
            float scrimAlpha = progress * 0.45f;

            RenderContext.Current.PendingOverlays.Enqueue(() => {
                using (TintScope.Replace(Color.white)) {
                    Theme.Theme modalTheme = RenderContext.Current.Theme;

                    // Blur + darken the surface behind the panel so the modal reads as a focused
                    // foreground layer (mock .nc-overlay = blur over a warm scrim, with the page's own
                    // radial vignette showing through). Mirrors the LightweaveWindow backdrop patch
                    // (BackdropBlur + flat scrim + radial vignette) for the in-tree Modal path.
                    BackdropBlur.Draw(host, 6f * progress);

                    Color scrimBase = modalTheme.GetColor(ThemeSlot.ScrimDefault);
                    PaintBox.Draw(
                        host,
                        BackgroundSpec.Of(new Color(scrimBase.r, scrimBase.g, scrimBase.b, scrimAlpha)),
                        null,
                        null
                    );

                    // Radial vignette darkening toward the screen edges, transparent where the panel
                    // sits — painted under the panel so only the scrim ring is darkened, not the body.
                    Color vignette = modalTheme.GetColor(ThemeSlot.OverlayDim);
                    vignette.a *= progress * 0.7f;
                    PaintBox.DrawTexture(host, VignetteTextureCache.Radial(falloff: 1.2f), vignette);

                    RadiusSpec radius = RadiusSpec.All(RadiusScale.Lg);

                    Rect shadowRect = new Rect(panelRect.x + 4f, panelRect.y + 6f, panelRect.width, panelRect.height);
                    PaintBox.Draw(shadowRect, BackgroundSpec.Of(ThemeSlot.SurfaceShadow), null, radius);

                    // Opaque panel base, then the glass tint on top. A modal must fully occlude whatever
                    // is behind it; the previous translucent glass over a backdrop blur let the main-menu
                    // splash bleed through the panel as a brighter, bluish wash.
                    PaintBox.Draw(panelRect, BackgroundSpec.Of(ThemeSlot.SurfaceTranslucentDark), null, radius);
                    PaintBox.Draw(panelRect, BackgroundSpec.Of(ThemeSlot.WindowGlass), null, radius);

                    bodyCache ??= content();
                    RenderContext.Current.OverlayContentDepth++;
                    try {
                        LightweaveRoot.PaintSubtree(bodyCache, panelRect);
                    }
                    finally {
                        RenderContext.Current.OverlayContentDepth--;
                    }

                    PaintBox.Draw(panelRect, null, BorderSpec.All(new Rem(1f / 16f), ThemeSlot.BorderFaint), radius);
                }

                Event e = Event.current;
                if (dismissOnScrimClick &&
                    e.type == EventType.MouseDown &&
                    !panelRect.Contains(e.mousePosition) &&
                    host.Contains(e.mousePosition)) {
                    onDismiss?.Invoke();
                    e.Use();
                }
                else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape) {
                    onDismiss?.Invoke();
                    e.Use();
                }
            });
        };

        return node;
    }

    private static LightweaveNode BuildHostDemo() {
        StateHandle<bool> open = UseState(false);

        LightweaveNode trigger = Button.Create(
            (string)"CL_Playground_Modal_TriggerOpen".Translate(),
            () => open.Set(!open.Value),
            Variant.Secondary
        );

        LightweaveNode modal = Create(
            open.Value,
            () => Box.Create(
                children: c => c.Add(Stack.Create(SpacingScale.Md, s => {
                    s.Add(Text.Create(
                        (string)"CL_Playground_Modal_ContentTitle".Translate(),
                        style: new Style {
                            FontFamily = FontRole.Display,
                            FontSize = new Rem(1.25f),
                            TextColor = ThemeSlot.TextPrimary,
                        }
                    ));
                    s.Add(Text.Create(
                        (string)"CL_Playground_Modal_ContentBody".Translate(),
                        wrap: true,
                        style: new Style {
                            FontFamily = FontRole.Body,
                            FontSize = new Rem(0.875f),
                            TextColor = ThemeSlot.TextSecondary,
                        }
                    ));
                    s.Add(Button.Create(
                        (string)"CL_Playground_Modal_Close".Translate(),
                        () => open.Set(false),
                        Variant.Primary
                    ));
                })),
                style: new Style { Padding = EdgeInsets.All(SpacingScale.Lg) }
            ),
            () => open.Set(false),
            width: new Rem(26f),
            height: new Rem(16f)
        );

        LightweaveNode composed = NodeBuilder.New("ModalHost", 0, nameof(Modal));
        composed.Children.Add(trigger);
        composed.Children.Add(modal);
        composed.Measure = w => trigger.Measure?.Invoke(w) ?? trigger.PreferredHeight ?? 32f;
        composed.Paint = (rect, _) => {
            trigger.MeasuredRect = rect;
            LightweaveRoot.PaintSubtree(trigger, rect);
            modal.MeasuredRect = rect;
            LightweaveRoot.PaintSubtree(modal, rect);
        };
        return composed;
    }

    [DocUsage]
    public static DocSample DocsUsage() {
        return new DocSample(() => BuildHostDemo(), useFullSource: true);
    }
}
