using System;
using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Adapter;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Icons;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using LwText = Cosmere.Lightweave.Typography.Typography.Text;

namespace Cosmere.Lightweave.Surfaces;

[Doc(
    Id = "add-tile",
    Summary = "Full-cell dashed 'add' tile: square corners, accent dashed border, centered plus glyph over an uppercase mono label. Fills its grid cell and washes accent-soft on hover.",
    WhenToUse = "The trailing cell of a gallery grid where the user adds another item (a meme, a style, a precept). For an inline dashed pill use the smaller add-chip pattern; for a ghost text button use Button with the ghost variant.",
    SourcePath = "Lightweave/Surfaces/AddTile.cs"
)]
public static class AddTile {
    private static readonly Rem MinHeight = new Rem(5.75f);
    private static readonly Rem IconSize = new Rem(1.375f);
    private static readonly Rem LabelSize = new Rem(0.656f);
    private static readonly Rem Gap = new Rem(0.3125f);
    private static readonly Rem PadX = new Rem(0.75f);
    private const float TrackingEm = 0.14f;

    public static LightweaveNode Create(
        [DocParam("Label, rendered uppercase in mono. Caller translates.")]
        string label,
        [DocParam("Invoked when the tile is clicked.", TypeOverride = "Action?", DefaultOverride = "null")]
        Action? onAdd = null,
        [DocParam("Leading glyph. Defaults to a plus.", TypeOverride = "IconRef?", DefaultOverride = "null")]
        IconRef? icon = null,
        [DocParam("Greys the tile and blocks clicks.")]
        bool disabled = false,
        [DocParam("Optional hover tooltip, shown as a Lightweave tooltip.", TypeOverride = "string?", DefaultOverride = "null")]
        string? tooltip = null,
        [DocParam("Inline style override.", TypeOverride = "Style?", DefaultOverride = "null")]
        Style? style = null,
        [DocParam("Additional class names merged after the base 'add-tile' class.", TypeOverride = "string[]?", DefaultOverride = "null")]
        string[]? classes = null,
        [DocParam("Stable id for state-style lookup.", TypeOverride = "string?", DefaultOverride = "null")]
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        string display = string.IsNullOrEmpty(label) ? string.Empty : label.ToUpperInvariant();
        IconRef glyph = icon ?? Icons.Phosphor.Plus;

        LightweaveNode node = NodeBuilder.New($"AddTile:{display}", line, file);
        node.ApplyStyling("add-tile", style, classes, id);
        node.PreferredHeight = MinHeight.ToPixels();

        node.Measure = _ => MinHeight.ToPixels();

        node.MeasureWidth = () => {
            float trackingPx = TrackingEm * LabelSize.ToFontPx();
            float labelW = string.IsNullOrEmpty(display) ? 0f : TextDraw.MeasureTracked(display, FontRole.Mono, LabelSize, trackingPx);
            float iconW = TextDraw.Measure(glyph.Glyph, FontRole.Body, IconSize, fontOverride: glyph.ResolveFont()).x;
            return Mathf.Ceil(Mathf.Max(labelW, iconW) + PadX.ToPixels() * 2f);
        };

        node.Layout = rect => {
            node.MeasuredRect = rect;
            InteractionFeedback.Apply(rect, !disabled, true);

            if (!string.IsNullOrEmpty(tooltip)) {
                string tip = tooltip!;
                AsTooltip.Attach(rect, () => LwText.Create(tip, wrap: true, richText: true), key: tip.GetHashCode());
            }

            Event? ev = Event.current;
            if (!disabled
                && onAdd != null
                && ev != null
                && ev.type == EventType.MouseUp
                && ev.button == 0
                && rect.Contains(ev.mousePosition)
                && LightweaveHitTracker.IsTopmost(rect)) {
                onAdd.Invoke();
                ev.Use();
            }
        };

        node.Draw = rect => {
            InteractionState state = InteractionState.Resolve(rect, null, disabled);
            float opacity = disabled ? 0.38f : 1f;

            BackgroundSpec? fill = state.Hovered && !disabled ? BackgroundSpec.Of(ThemeSlot.AccentSoft) : null;
            ThemeSlot borderSlot = disabled ? ThemeSlot.BorderDefault : ThemeSlot.AccentGlow;
            PaintBox.Draw(rect, fill, BorderSpec.AllDashed(new Rem(1f / 16f), borderSlot), RadiusSpec.None);

            ThemeSlot inkSlot = disabled ? ThemeSlot.TextMuted : ThemeSlot.SurfaceAccent;
            Theme.Theme theme = RenderContext.Current.Theme;
            Color ink = theme.GetColor(inkSlot);
            ink.a *= opacity;

            float iconPx = IconSize.ToFontPx();
            float labelPx = LabelSize.ToFontPx();
            float gapPx = Gap.ToPixels();
            bool hasLabel = !string.IsNullOrEmpty(display);
            float groupH = iconPx + (hasLabel ? gapPx + labelPx : 0f);
            float top = rect.y + (rect.height - groupH) * 0.5f;

            Rect iconRect = new Rect(rect.x, top, rect.width, iconPx);
            TextDraw.Draw(iconRect, glyph.Glyph, FontRole.Body, IconSize, TextAnchor.MiddleCenter, ink, fontOverride: glyph.ResolveFont());

            if (hasLabel) {
                Rect labelRect = new Rect(rect.x, top + iconPx + gapPx, rect.width, labelPx);
                TextDraw.DrawTracked(labelRect, display, FontRole.Mono, LabelSize, TextAnchor.MiddleCenter, ink, TrackingEm * labelPx);
            }
        };

        return node;
    }

    [DocUsage]
    public static DocSample DocsUsage() {
        return new DocSample(() => AddTile.Create("Add meme", () => { }));
    }

    [DocVariant("CL_Playground_AddTile_Default")]
    public static DocSample DocsDefault() {
        return new DocSample(() => AddTile.Create("Add meme", () => { }));
    }

    [DocVariant("CL_Playground_AddTile_Disabled")]
    public static DocSample DocsDisabled() {
        return new DocSample(() => AddTile.Create("Add style", () => { }, disabled: true));
    }
}
