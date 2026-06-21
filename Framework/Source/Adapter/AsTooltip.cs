using System;
using System.Collections.Generic;
using Cosmere.Lightweave.Data;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;

namespace Cosmere.Lightweave.Adapter;

/// <summary>
///     Adapter that renders a rich Lightweave widget tree as a hover tooltip over a given <see cref="Rect" />.
///     The tooltip is enqueued as a pending overlay and painted after the main Lightweave pass completes.
///     <para>
///         <b>Requirement:</b> must be called from within a Lightweave Paint body where
///         <see cref="RenderContext.CurrentOrNull" /> is non-null. If called outside a Lightweave pass
///         (i.e. <c>RenderContext.CurrentOrNull == null</c>), this method no-ops and no tooltip appears.
///         In that case, callers should use vanilla <see cref="TooltipHandler.TipRegion" /> instead.
///     </para>
///     <para>
///         Bypassed affordances vs vanilla <see cref="TooltipHandler.TipRegion" />:
///         <list type="bullet">
///             <item>
///                 Hover delay is honored - the tooltip appears only after the pointer dwells over
///                 <paramref name="rect" /> for the requested delay (default 0.45s), matching vanilla feel.
///             </item>
///             <item>No <c>TipSignal</c> hash caching - the <paramref name="build" /> closure is invoked fresh each frame once shown.</item>
///             <item>
///                 No built-in string translation or formatting - the Lightweave tree is responsible for all text
///                 rendering.
///             </item>
///             <item>
///                 No integration with vanilla tooltip stacking priority - this overlay always renders on top of the
///                 current Lightweave pass.
///             </item>
///         </list>
///     </para>
/// </summary>
public static class AsTooltip {
    private struct HoverEntry {
        public float Start;
        public int Frame;
    }

    private static readonly Dictionary<int, HoverEntry> Hovers = new Dictionary<int, HoverEntry>();

    /// <summary>
    ///     Attaches a rich Lightweave tooltip to <paramref name="rect" />.
    ///     No-ops if called outside a Lightweave pass or if the pointer is not over <paramref name="rect" />.
    ///     The tooltip hugs its content and only appears after the pointer dwells for <paramref name="delay" /> seconds.
    /// </summary>
    /// <param name="rect">The screen rect the tooltip is anchored to.</param>
    /// <param name="build">
    ///     Factory invoked each frame the pointer is over <paramref name="rect" /> (after the delay) to produce the
    ///     tooltip content node. The node is measured to size the tooltip to its content.
    /// </param>
    /// <param name="maxSize">Upper bound on tooltip size; content wraps/clamps within it. Defaults to 20rem x 24rem.</param>
    /// <param name="delay">Hover dwell time in seconds before the tooltip shows. Defaults to 0.45s.</param>
    /// <param name="key">
    ///     Stable identity used to track hover dwell across frames. Pass a content-derived key (e.g. an id hash) when the
    ///     <paramref name="rect" /> can move (scrolling); falls back to the rect hash when zero.
    /// </param>
    public static void Attach(
        Rect rect,
        Func<LightweaveNode> build,
        Vector2? maxSize = null,
        float delay = 0.45f,
        int key = 0,
        TooltipSide? side = null
    ) {
        RenderContext? ctx = RenderContext.CurrentOrNull;
        if (ctx == null) {
            return;
        }

        if (!Mouse.IsOver(rect)) {
            return;
        }

        // Suppress tooltips for content sitting behind an open overlay (a modal scrim, a popover).
        // Overlays register a HoverBlockRegistry region; base content (OverlayContentDepth == 0) under
        // that region must not raise a tooltip, mirroring LightweaveHitTracker.IsTopmost's gate. The
        // overlay's own content paints at depth > 0, so its tooltips still fire.
        Event? currentEvent = Event.current;
        if (ctx.OverlayContentDepth == 0 && currentEvent != null
            && HoverBlockRegistry.IsBlocked(GUIUtility.GUIToScreenPoint(currentEvent.mousePosition))) {
            return;
        }

        int hoverKey = key != 0 ? key : rect.GetHashCode();
        int frame = Time.frameCount;
        float now = Time.realtimeSinceStartup;

        if (!Hovers.TryGetValue(hoverKey, out HoverEntry entry) || entry.Frame < frame - 1) {
            entry.Start = now;
        }

        entry.Frame = frame;
        Hovers[hoverKey] = entry;

        if (now - entry.Start < delay) {
            return;
        }

        Vector2 mouseScreen = GUIUtility.GUIToScreenPoint(Event.current.mousePosition) + new Vector2(16f, 16f);
        Vector2 max = maxSize ?? new Vector2(new Rem(20f).ToPixels(), new Rem(24f).ToPixels());

        ctx.PendingOverlays.Enqueue(() => {
            float pad = new Rem(0.5f).ToPixels();

            LightweaveNode node = build();
            float naturalW = node.MeasureWidth?.Invoke() ?? max.x;
            float innerW = Mathf.Clamp(naturalW, 0f, max.x - pad * 2f);
            float naturalH = node.Measure?.Invoke(innerW) ?? node.PreferredHeight ?? 0f;
            float innerH = Mathf.Clamp(naturalH, 0f, max.y - pad * 2f);

            float w = innerW + pad * 2f;
            float h = innerH + pad * 2f;

            Rect screenRect;
            if (side is TooltipSide.Top or TooltipSide.TopStart or TooltipSide.TopEnd) {
                // Anchor centered above the source rect (used by controls pinned to the screen
                // bottom, where the default mouse-follow tooltip would clamp on top of the control).
                Vector2 topCenter = GUIUtility.GUIToScreenPoint(new Vector2(rect.center.x, rect.y));
                float gapPx = new Rem(0.375f).ToPixels();
                screenRect = new Rect(topCenter.x - w * 0.5f, topCenter.y - h - gapPx, w, h);
            }
            else {
                screenRect = new Rect(mouseScreen.x, mouseScreen.y, w, h);
            }

            if (screenRect.xMax > Screen.width) {
                screenRect.x = Screen.width - w;
            }

            if (screenRect.yMax > Screen.height) {
                screenRect.y = Screen.height - h;
            }

            if (screenRect.x < 0f) {
                screenRect.x = 0f;
            }

            if (screenRect.y < 0f) {
                screenRect.y = 0f;
            }

            Vector2 local = GUIUtility.ScreenToGUIPoint(new Vector2(screenRect.x, screenRect.y));
            Rect tooltipRect = new Rect(local.x, local.y, w, h);

            PaintBox.DrawShadow(tooltipRect, ShadowSpec.Of(ThemeSlot.ShadowTooltip));
            PaintBox.Draw(
                tooltipRect,
                BackgroundSpec.Of(ThemeSlot.SurfaceTooltip),
                BorderSpec.All(new Rem(1f / 16f), ThemeSlot.BorderTooltip),
                RadiusSpec.All(RadiusScale.Sm)
            );

            Rect inner = new Rect(
                tooltipRect.x + pad,
                tooltipRect.y + pad,
                tooltipRect.width - pad * 2f,
                tooltipRect.height - pad * 2f
            );
            LightweaveRoot.PaintSubtree(node, inner);
        }
        );
    }
}