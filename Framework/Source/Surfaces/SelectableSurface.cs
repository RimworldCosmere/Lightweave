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
using Text = Cosmere.Lightweave.Typography.Typography.Text;

namespace Cosmere.Lightweave.Surfaces;

public static class SelectableSurface {
    private static readonly EdgeInsets TilePad = EdgeInsets.All(SpacingScale.Md);
    private static readonly Rem ListRowPadX = new Rem(18f / 16f);
    private static readonly EdgeInsets ListRowPad =
        new EdgeInsets(SpacingScale.Md, ListRowPadX, SpacingScale.Md, ListRowPadX);
    private static readonly Rem SelectedBorderWidth = new Rem(2f / 16f);
    private static readonly Rem RestBorderWidth = new Rem(1f / 16f);
    private static readonly BackgroundSpec ListRowGlass = BackgroundSpec.Of(ThemeSlot.Glass1);
    private static readonly Rem CaretRem = new Rem(14f / 16f);
    private static readonly Rem CaretInsetRem = SpacingScale.Md;
    private static readonly Rem AccentBarWidthRem = new Rem(3f / 16f);
    private static readonly IconRef CaretIcon = Phosphor.CaretRight;
    private const float SelectedFillAlpha = 0.1f;

    public static LightweaveNode Create(
        LightweaveNode? child = null,
        bool selected = false,
        Action? onSelect = null,
        bool disabled = false,
        bool? playHoverSound = null,
        [DocParam("Visual treatment. Tile is a rounded card; ListRow is a square glass row with a leading accent bar and trailing caret when selected.")]
        SelectableSurfaceVariant variant = SelectableSurfaceVariant.Tile,
        [DocParam("Accent for the selected border, ListRow accent bar, and ListRow caret. Defaults to ThemeSlot.SurfaceAccent.")]
        ColorRef? accent = null,
        [DocParam("ListRow only: draw a trailing caret when selected. Set false for rows that carry their own leading indicator.")]
        bool trailingCaret = true,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        LightweaveNode node = NodeBuilder.New("SelectableSurface", line, file);
        node.ApplyStyling("selectable-surface", style, classes, id);

        if (child != null) {
            node.Children.Add(child);
        }

        EdgeInsets defaultPad = variant == SelectableSurfaceVariant.ListRow ? ListRowPad : TilePad;

        (float left, float top, float right, float bottom) ResolvePaddingPixels() {
            Style s = node.GetResolvedStyle();
            EdgeInsets pad = s.Padding ?? defaultPad;
            return pad.Resolve(RenderContext.Current.Direction);
        }

        node.MeasureWidth = () => {
            (float left, float top, float right, float bottom) = ResolvePaddingPixels();
            float childWidth = child?.MeasureWidth?.Invoke() ?? 0f;
            return childWidth + left + right;
        };

        node.Measure = availableWidth => {
            (float left, float top, float right, float bottom) = ResolvePaddingPixels();
            float innerWidth = Mathf.Max(0f, availableWidth - left - right);
            float childHeight = child?.Measure?.Invoke(innerWidth) ?? child?.PreferredHeight ?? 0f;
            return childHeight + top + bottom;
        };

        node.Layout = rect => {
            node.MeasuredRect = rect;

            if (child != null) {
                EdgeInsets pad = node.GetResolvedStyle().Padding ?? defaultPad;
                child.MeasuredRect = pad.Shrink(rect, RenderContext.Current.Direction);
            }

            InteractionFeedback.Apply(rect, !disabled, playHoverSound ?? true);

            if (!disabled
                && onSelect != null
                && Widgets.ButtonInvisible(rect, doMouseoverSound: false)
                && LightweaveHitTracker.IsTopmost(rect)) {
                onSelect.Invoke();
            }
        };

        node.Draw = rect => {
            Theme.Theme theme = RenderContext.Current.Theme;
            Style s = node.GetResolvedStyle();
            InteractionState state = InteractionState.Resolve(rect, null, disabled);

            if (variant == SelectableSurfaceVariant.ListRow) {
                DrawListRow(rect, theme, s, state, selected, accent, trailingCaret);
            }
            else {
                DrawTile(rect, theme, s, state, selected, accent);
            }
        };

        return node;
    }

    private static void DrawTile(
        Rect rect,
        Theme.Theme theme,
        Style s,
        InteractionState state,
        bool selected,
        ColorRef? accent
    ) {
        ThemeSlot borderSlot = ResolveBorderSlot(state, selected);
        Rem borderWidth = selected ? SelectedBorderWidth : RestBorderWidth;
        BorderSpec border = s.Border ?? (
            selected && accent != null
                ? BorderSpec.All(borderWidth, accent)
                : BorderSpec.All(borderWidth, borderSlot)
        );

        ThemeSlot? backgroundSlot = ResolveBackgroundSlot(state, selected);
        BackgroundSpec? background = s.Background
            ?? (backgroundSlot.HasValue ? BackgroundSpec.Of(backgroundSlot.Value) : null);

        RadiusSpec radius = s.Radius ?? RadiusSpec.All(RadiusScale.Md);

        PaintBox.Draw(rect, background, border, radius);

        if (!selected && !state.Disabled && (state.Hovered || state.Pressed)) {
            Color hover = theme.GetColor(state.Pressed ? ThemeSlot.ActiveTint : ThemeSlot.HoverTint);
            PaintBox.Draw(rect, BackgroundSpec.Of(hover), null, radius);
        }
    }

    private static void DrawListRow(
        Rect rect,
        Theme.Theme theme,
        Style s,
        InteractionState state,
        bool selected,
        ColorRef? accent,
        bool trailingCaret
    ) {
        RadiusSpec radius = s.Radius ?? RadiusSpec.None;
        Color accentColor = ResolveAccent(accent, theme);

        BorderSpec border = s.Border ?? (
            selected
                ? BorderSpec.All(RestBorderWidth, accent ?? (ColorRef)ThemeSlot.SurfaceAccent)
                : BorderSpec.All(RestBorderWidth, ResolveBorderSlot(state, false))
        );

        BackgroundSpec background = s.Background ?? ListRowGlass;
        PaintBox.Draw(rect, background, border, radius);

        if (state.Disabled) {
            return;
        }

        if (selected) {
            Color soft = accentColor;
            soft.a = SelectedFillAlpha;
            PaintBox.Fill(rect, soft);

            Rect barRect = new Rect(rect.x, rect.y, AccentBarWidthRem.ToPixels(), rect.height);
            PaintBox.Fill(barRect, accentColor);

            if (trailingCaret) {
                float caretSize = CaretRem.ToPixels();
                float inset = CaretInsetRem.ToPixels();
                Rect caretRect = new Rect(
                    rect.xMax - inset - caretSize,
                    rect.center.y - caretSize / 2f,
                    caretSize,
                    caretSize
                );
                TextDraw.Draw(
                    caretRect,
                    CaretIcon.Glyph,
                    FontRole.Body,
                    CaretRem,
                    TextAnchor.MiddleCenter,
                    accentColor,
                    fontOverride: CaretIcon.ResolveFont()
                );
            }
        }
        else if (state.Hovered || state.Pressed) {
            Color hover = theme.GetColor(state.Pressed ? ThemeSlot.ActiveTint : ThemeSlot.HoverTint);
            PaintBox.Fill(rect, hover);
        }
    }

    private static Color ResolveAccent(ColorRef? accent, Theme.Theme theme) {
        return accent switch {
            ColorRef.Literal lit => lit.Value,
            ColorRef.Token tok => theme.GetColor(tok.Slot),
            _ => theme.GetColor(ThemeSlot.SurfaceAccent),
        };
    }

    private static ThemeSlot ResolveBorderSlot(InteractionState state, bool selected) {
        if (selected) {
            return ThemeSlot.SurfaceAccent;
        }

        if (state.Disabled) {
            return ThemeSlot.BorderOff;
        }

        if (state.Hovered || state.Pressed) {
            return ThemeSlot.BorderHover;
        }

        return ThemeSlot.BorderDefault;
    }

    private static ThemeSlot? ResolveBackgroundSlot(InteractionState state, bool selected) {
        if (state.Disabled) {
            return ThemeSlot.SurfaceDisabled;
        }

        if (selected) {
            return ThemeSlot.SurfaceRaised;
        }

        return null;
    }

    [DocUsage]
    public static DocSample DocsUsage() {
        return new DocSample(() => Create(
            Text.Create("Selectable row"),
            onSelect: () => { }
        ));
    }

    [DocVariant("CL_Playground_Label_Row")]
    public static DocSample DocsRow() {
        return new DocSample(() => Create(
            Text.Create("List row"),
            onSelect: () => { }
        ));
    }

    [DocVariant("CL_Playground_Label_Tile")]
    public static DocSample DocsTile() {
        return new DocSample(() => Create(
            Text.Create("Tile"),
            onSelect: () => { },
            style: new Style { Padding = EdgeInsets.All(SpacingScale.Lg), Width = Length.Rem(140f / 16f) }
        ));
    }

    [DocVariant("CL_Playground_Label_ListRow")]
    public static DocSample DocsListRow() {
        return new DocSample(() => Create(
            Text.Create("List row"),
            selected: true,
            onSelect: () => { },
            variant: SelectableSurfaceVariant.ListRow,
            style: new Style { Width = Length.Stretch }
        ));
    }

    [DocState("CL_Playground_Label_Default")]
    public static DocSample DocsDefault() {
        return new DocSample(() => Create(
            Text.Create("Default"),
            onSelect: () => { }
        ));
    }

    [DocState("CL_Playground_Label_Selected")]
    public static DocSample DocsSelected() {
        return new DocSample(() => Create(
            Text.Create("Selected"),
            selected: true,
            onSelect: () => { }
        ));
    }

    [DocState("CL_Playground_Label_Hover")]
    public static DocSample DocsHover() {
        return new DocSample(() => Create(
            Text.Create("Hover"),
            onSelect: () => { }
        ));
    }

    [DocState("CL_Playground_Label_Disabled")]
    public static DocSample DocsDisabled() {
        return new DocSample(() => Create(
            Text.Create("Disabled"),
            disabled: true,
            onSelect: () => { }
        ));
    }
}
