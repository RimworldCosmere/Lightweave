using System;
using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Feedback;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;
using static Cosmere.Lightweave.Typography.Typography;
using Cosmere.Lightweave.Data;

namespace Cosmere.Lightweave.Input;

[Doc(
    Id = "iconbutton",
    Summary = "Compact square button hosting a single icon.",
    WhenToUse = "Trigger an action where space is tight or the icon is unambiguous.",
    SourcePath = "Lightweave/Input/IconButton.cs"
)]
public static class IconButton {
    public static LightweaveNode Create(
        LightweaveNode icon,
        Action? onClick,
        Variant variant = default,
        Rem? iconSize = null,
        bool disabled = false,
        bool active = false,
        string? tooltipKey = null,
        TooltipSide tooltipSide = TooltipSide.Bottom,
        bool? playHoverSound = null,
        KeyCode? hotKey = null,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        if (variant.Id == null) {
            variant = Variant.Ghost;
        }

        LightweaveNode node = NodeBuilder.New("IconButton", line, file);
        node.ApplyStyling("icon-button", style, classes, id);
        node.Children.Add(icon);

        float iconPx = (iconSize ?? new Rem(1.25f)).ToPixels();
        float padPx = SpacingScale.Xs.ToPixels();
        float squareSize = iconPx + padPx * 2f;
        node.PreferredHeight = squareSize;
        node.MeasureWidth = () => squareSize;

        node.Paint = (rect, paintChildren) => {
            float size = Mathf.Min(squareSize, Mathf.Min(rect.width, rect.height));
            Rect square = new Rect(
                rect.x + (rect.width - size) / 2f,
                rect.y + (rect.height - size) / 2f,
                size,
                size
            );

            InteractionState state = InteractionState.Resolve(square, null, disabled);

            ThemeSlot? bgSlot = active ? ThemeSlot.ActiveTint : VariantPalette.Background(variant, state);
            ThemeSlot? borderSlot = active ? ThemeSlot.SurfaceAccent : VariantPalette.Border(variant, state);

            BackgroundSpec? bg = bgSlot.HasValue ? BackgroundSpec.Of(bgSlot.Value) : null;
            BorderSpec? border = borderSlot.HasValue
                ? BorderSpec.All(new Rem(1f / 16f), borderSlot.Value)
                : null;
            RadiusSpec radius = RadiusSpec.All(RadiusScale.Sm);

            PaintBox.Draw(square, bg, border, radius);

            float overlay = VariantPalette.OverlayAlpha(state);
            if (overlay > 0f) {
                Color overlayColor = InteractionFeedback.OverlayColor(RenderContext.Current.Theme, state, overlay);
                PaintBox.Draw(square, BackgroundSpec.Of(overlayColor), null, radius);
            }

            float innerPad = Mathf.Min(padPx, (size - iconPx) / 2f);
            Rect padRect = new Rect(
                square.x + innerPad,
                square.y + innerPad,
                size - innerPad * 2f,
                size - innerPad * 2f
            );
            icon.MeasuredRect = padRect;

            paintChildren();

            InteractionFeedback.Apply(square, !disabled, playHoverSound ?? true);

            Event e = Event.current;
            if (!disabled &&
                onClick != null &&
                e.type == EventType.MouseUp &&
                e.button == 0 &&
                square.Contains(e.mousePosition) &&
                LightweaveHitTracker.IsTopmost(square)) {
                onClick.Invoke();
                e.Use();
            }

            // Hotkey: fire onClick on the bound key, but only when no text field is focused
            // (keyboardControl == 0) so typing the same letter into a field never triggers it.
            if (!disabled &&
                onClick != null &&
                hotKey.HasValue &&
                e.type == EventType.KeyDown &&
                e.keyCode == hotKey.Value &&
                GUIUtility.keyboardControl == 0) {
                onClick.Invoke();
                e.Use();
            }
        };

        if (!string.IsNullOrEmpty(tooltipKey)) {
            // tooltipKey is unique per logical button; pass it as the tooltip id so sibling
            // IconButtons routed through one helper (same call site) don't share a hoverTimer slot.
            return Tooltip.Create(node, (string)tooltipKey!.Translate(), side: tooltipSide, id: id ?? tooltipKey, line: line, file: file);
        }

        return node;
    }

    

    [DocVariant("CL_Playground_Label_Ghost")]
    public static DocSample DocsGhost() {
        return new DocSample(() => Create(Icon.Create(TexButton.Reveal, new Rem(1f), style: new Style { TextColor = ThemeSlot.TextPrimary }), () => { }));
    }

    [DocVariant("CL_Playground_Label_Primary")]
    public static DocSample DocsPrimary() {
        return new DocSample(() => Create(Icon.Create(TexButton.NewItem, new Rem(1f), style: new Style { TextColor = ThemeSlot.TextPrimary }), () => { }, Variant.Primary));
    }

    [DocVariant("CL_Playground_Label_Secondary")]
    public static DocSample DocsSecondary() {
        return new DocSample(() => Create(Icon.Create(TexButton.Search, new Rem(1f), style: new Style { TextColor = ThemeSlot.TextPrimary }), () => { }, Variant.Secondary));
    }

    [DocState("CL_Playground_Label_Default", HideCode = true)]
    public static DocSample DocsDefault() {
        return new DocSample(() => Create(Icon.Create(TexButton.Info, new Rem(1f), style: new Style { TextColor = ThemeSlot.TextPrimary }), () => { }));
    }

    [DocState("CL_Playground_Label_Hover", HideCode = true)]
    public static DocSample DocsHover() {
        return new DocSample(() => Create(Icon.Create(TexButton.Rename, new Rem(1f), style: new Style { TextColor = ThemeSlot.TextPrimary }), () => { }));
    }

    [DocState("CL_Playground_Label_Active", HideCode = true)]
    public static DocSample DocsActive() {
        return new DocSample(() => Create(Icon.Create(TexButton.Info, new Rem(1f), style: new Style { TextColor = ThemeSlot.SurfaceAccent }), () => { }, active: true));
    }

    [DocState("CL_Playground_Label_Disabled", HideCode = true)]
    public static DocSample DocsDisabled() {
        return new DocSample(() => Create(Icon.Create(TexButton.CloseXSmall, new Rem(1f), style: new Style { TextColor = ThemeSlot.TextPrimary }), () => { }, disabled: true));
    }

    [DocUsage]
    public static DocSample DocsUsage() {
        return new DocSample(() => Create(Icon.Create(TexButton.Plus, new Rem(1f), style: new Style { TextColor = ThemeSlot.TextPrimary }), () => { }));
    }
}
