using System;
using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;

namespace Cosmere.Lightweave.Feedback;

public enum TooltipSide {
    Top,
    TopStart,
    TopEnd,
    Right,
    RightStart,
    RightEnd,
    Bottom,
    BottomStart,
    BottomEnd,
    Left,
    LeftStart,
    LeftEnd,
}

public enum TooltipAlign {
    Start,
    Center,
    End,
}

[Doc(
    Id = "tooltip",
    Summary = "Hover-delayed contextual hint anchored to a trigger element.",
    WhenToUse = "Reveal short clarifying text on hover without claiming layout space.",
    SourcePath = "Lightweave/Feedback/Tooltip.cs"
)]
public static class Tooltip {
    private const float DefaultDelaySeconds = 0.5f;
    private const float DefaultSideOffsetPx = 4f;
    private const float DefaultMaxWidthRem = 20f;

    [DocVariant("CL_Playground_Label_Default")]
    public static DocSample DocsDefault() {
        return new DocSample(() => Tooltip.Create(
            Button.Create("Hover me", () => { }, Variant.Secondary),
            "A single-line tooltip."
        ));
    }

    [DocVariant("CL_Playground_Tooltip_Variant_AllSides")]
    public static DocSample DocsAllSides() {
        return new DocSample(() => Stack.Create(
            gap: SpacingScale.Xxl,
            children: outer => {
                outer.Add(HStack.Create(
                    gap: SpacingScale.Lg,
                    children: row => {
                        row.AddFlex(Tooltip.Create(
                            Button.Create("TopStart", () => { }, Variant.Secondary),
                            "Anchored top, start-aligned.",
                            side: TooltipSide.TopStart
                        ));
                        row.AddFlex(Tooltip.Create(
                            Button.Create("Top", () => { }, Variant.Secondary),
                            "Anchored top, centered.",
                            side: TooltipSide.Top
                        ));
                        row.AddFlex(Tooltip.Create(
                            Button.Create("TopEnd", () => { }, Variant.Secondary),
                            "Anchored top, end-aligned.",
                            side: TooltipSide.TopEnd
                        ));
                    }
                ));
                outer.Add(HStack.Create(
                    gap: SpacingScale.Lg,
                    children: row => {
                        row.AddFlex(Tooltip.Create(
                            Button.Create("Left", () => { }, Variant.Secondary),
                            "Anchored to the left.",
                            side: TooltipSide.Left
                        ));
                        row.AddFlex(Box.Create());
                        row.AddFlex(Tooltip.Create(
                            Button.Create("Right", () => { }, Variant.Secondary),
                            "Anchored to the right.",
                            side: TooltipSide.Right
                        ));
                    }
                ));
                outer.Add(HStack.Create(
                    gap: SpacingScale.Lg,
                    children: row => {
                        row.AddFlex(Tooltip.Create(
                            Button.Create("BottomStart", () => { }, Variant.Secondary),
                            "Anchored bottom, start-aligned.",
                            side: TooltipSide.BottomStart
                        ));
                        row.AddFlex(Tooltip.Create(
                            Button.Create("Bottom", () => { }, Variant.Secondary),
                            "Anchored bottom, centered.",
                            side: TooltipSide.Bottom
                        ));
                        row.AddFlex(Tooltip.Create(
                            Button.Create("BottomEnd", () => { }, Variant.Secondary),
                            "Anchored bottom, end-aligned.",
                            side: TooltipSide.BottomEnd
                        ));
                    }
                ));
            }
        ));
    }

    [DocVariant("CL_Playground_Tooltip_Variant_LongDelay")]
    public static DocSample DocsLongDelay() {
        return new DocSample(() => Tooltip.Create(
            Button.Create("Patient", () => { }, Variant.Secondary),
            "Two-second delay before this appears.",
            delayDuration: 2f
        ));
    }

    [DocVariant("CL_Playground_Tooltip_Variant_NoDelay")]
    public static DocSample DocsNoDelay() {
        return new DocSample(() => Tooltip.Create(
            Button.Create("Instant", () => { }, Variant.Secondary),
            "Appears immediately on hover.",
            delayDuration: 0f
        ));
    }

    [DocVariant("CL_Playground_Tooltip_Variant_LargeOffset")]
    public static DocSample DocsLargeOffset() {
        return new DocSample(() => Tooltip.Create(
            Button.Create("Far", () => { }, Variant.Secondary),
            "20px offset from the trigger.",
            sideOffset: 20f
        ));
    }

    public static DocSample DocsWrapping() {
        return new DocSample(() => Tooltip.Create(
            Button.Create("Long body", () => { }, Variant.Secondary),
            "This tooltip body wraps onto multiple lines because it exceeds the maximum width that the Style.MaxWidth constrains it to.",
            style: new Style { MaxWidth = new Rem(12f) }
        ));
    }

    [DocVariant("CL_Playground_Tooltip_Variant_Disabled")]
    public static DocSample DocsOnDisabled() {
        return new DocSample(() => Tooltip.Create(
            Button.Create("Disabled", () => { }, Variant.Secondary, disabled: true),
            "Disabled triggers still surface their tooltip on hover."
        ));
    }

    [DocVariant("CL_Playground_Tooltip_Variant_Live")]
    public static DocSample DocsLive() {
        return new DocSample(() => {
            Hooks.Hooks.StateHandle<int> ticks = Hooks.Hooks.UseState(0);
            return Tooltip.Create(
                Button.Create("Tick", () => ticks.Set(ticks.Value + 1), Variant.Secondary),
                () => $"Clicked {ticks.Value} time(s).",
                side: TooltipSide.Bottom
            );
        });
    }

    [DocVariant("CL_Playground_Tooltip_Variant_RichContent")]
    public static DocSample DocsRichContent() {
        return new DocSample(() => Tooltip.Create(
            Button.Create("Rich", () => { }, Variant.Secondary),
            BuildRichBody(),
            new Vector2(new Rem(14f).ToPixels(), new Rem(4.5f).ToPixels())
        ));
    }

    private static LightweaveNode BuildRichBody() {
        return Stack.Create(
            gap: SpacingScale.Xs,
            children: b => {
                b.Add(Typography.Typography.Heading.Create(4, "Stormlight"));
                b.Add(Typography.Typography.Text.Create(
                    "412 / 1000",
                    style: new Style { FontFamily = FontRole.Body, FontSize = new Rem(0.875f), TextColor = ThemeSlot.TextSecondary }
                ));
            }
        );
    }

    [DocUsage]
    public static DocSample DocsUsage() {
        return new DocSample(() => Tooltip.Create(
            Button.Create("Hover me", () => { }, Variant.Secondary),
            "Hint shown after a brief hover."
        ));
    }

    public static LightweaveNode Create(
        [DocParam("Element the tooltip is anchored to. Receives all layout space; tooltip overlays separately.")]
        LightweaveNode children,
        [DocParam("Static hint text. Wrapped automatically up to Style.MaxWidth (defaults to 20rem).")]
        string text,
        [DocParam("Anchor side and optional Start/End suffix. Suffix overrides align.")]
        TooltipSide side = TooltipSide.Bottom,
        [DocParam("Cross-axis alignment when side is cardinal (Top/Right/Bottom/Left).")]
        TooltipAlign align = TooltipAlign.Center,
        [DocParam("Hover seconds before the tooltip appears. 0 = instant.")]
        float delayDuration = DefaultDelaySeconds,
        [DocParam("Pixel gap between trigger and tooltip on the anchor axis.")]
        float sideOffset = DefaultSideOffsetPx,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        float maxWidthRem = ResolveMaxWidthRem(style?.MaxWidth);
        return CreateInternal(
            children,
            () => BuildTextNode(text),
            () => MeasureContent(BuildTextNode(text), maxWidthRem),
            side,
            align,
            delayDuration,
            sideOffset,
            style,
            classes,
            id,
            line,
            file
        );
    }

    public static LightweaveNode Create(
        [DocParam("Element the tooltip is anchored to.")]
        LightweaveNode children,
        [DocParam("Delegate returning hint text. Re-evaluated each frame the tooltip is visible.")]
        Func<string> text,
        [DocParam("Anchor side. Suffix overrides align.")]
        TooltipSide side = TooltipSide.Bottom,
        [DocParam("Cross-axis alignment for cardinal sides.")]
        TooltipAlign align = TooltipAlign.Center,
        [DocParam("Hover seconds before the tooltip appears.")]
        float delayDuration = DefaultDelaySeconds,
        [DocParam("Pixel gap between trigger and tooltip.")]
        float sideOffset = DefaultSideOffsetPx,
        [DocParam("Optional dynamic anchor rect for positioning. When set, hover still uses the children rect, but the tooltip is placed relative to this rect (e.g. a hovered point on a chart).")]
        Func<Rect>? anchor = null,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        Func<string> resolver = text;
        float maxWidthRem = ResolveMaxWidthRem(style?.MaxWidth);
        return CreateInternal(
            children,
            () => BuildTextNode(resolver()),
            () => MeasureContent(BuildTextNode(resolver()), maxWidthRem),
            side,
            align,
            delayDuration,
            sideOffset,
            style,
            classes,
            id,
            line,
            file,
            anchor
        );
    }

    public static LightweaveNode Create(
        [DocParam("Element the tooltip is anchored to.")]
        LightweaveNode children,
        [DocParam("Custom node painted inside the tooltip surface.")]
        LightweaveNode content,
        [DocParam("Pixel size of the tooltip surface.")]
        Vector2 preferredSize,
        [DocParam("Anchor side. Suffix overrides align.")]
        TooltipSide side = TooltipSide.Bottom,
        [DocParam("Cross-axis alignment for cardinal sides.")]
        TooltipAlign align = TooltipAlign.Center,
        [DocParam("Hover seconds before the tooltip appears.")]
        float delayDuration = DefaultDelaySeconds,
        [DocParam("Pixel gap between trigger and tooltip.")]
        float sideOffset = DefaultSideOffsetPx,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        return CreateInternal(
            children,
            () => content,
            () => preferredSize,
            side,
            align,
            delayDuration,
            sideOffset,
            style,
            classes,
            id,
            line,
            file
        );
    }

    public static LightweaveNode Create(
        [DocParam("Element the tooltip is anchored to.")]
        LightweaveNode children,
        [DocParam("Delegate building the tooltip body. Rebuilt each frame the tooltip is visible.")]
        Func<LightweaveNode> content,
        [DocParam("Delegate returning the tooltip surface size each frame.")]
        Func<Vector2> preferredSize,
        [DocParam("Anchor side. Suffix overrides align.")]
        TooltipSide side = TooltipSide.Bottom,
        [DocParam("Cross-axis alignment for cardinal sides.")]
        TooltipAlign align = TooltipAlign.Center,
        [DocParam("Hover seconds before the tooltip appears.")]
        float delayDuration = DefaultDelaySeconds,
        [DocParam("Pixel gap between trigger and tooltip.")]
        float sideOffset = DefaultSideOffsetPx,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        return CreateInternal(
            children,
            content,
            preferredSize,
            side,
            align,
            delayDuration,
            sideOffset,
            style,
            classes,
            id,
            line,
            file
        );
    }


    public static LightweaveNode Create(
        [DocParam("Element the tooltip is anchored to.")]
        LightweaveNode children,
        [DocParam("Delegate building the tooltip body. Rebuilt each frame the tooltip is visible. Size is auto-measured from the node's Measure callbacks.")]
        Func<LightweaveNode> content,
        [DocParam("Anchor side. Suffix overrides align.")]
        TooltipSide side = TooltipSide.Bottom,
        [DocParam("Cross-axis alignment for cardinal sides.")]
        TooltipAlign align = TooltipAlign.Center,
        [DocParam("Hover seconds before the tooltip appears.")]
        float delayDuration = DefaultDelaySeconds,
        [DocParam("Pixel gap between trigger and tooltip.")]
        float sideOffset = DefaultSideOffsetPx,
        [DocParam("Max content width in rem before wrapping. Default 20rem.")]
        float maxWidthRem = 20f,
        [DocParam("Optional dynamic anchor rect for positioning. When set, hover still uses the children rect, but the tooltip is placed relative to this rect.")]
        Func<Rect>? anchor = null,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        return CreateInternal(
            children,
            content,
            () => MeasureContent(content(), maxWidthRem),
            side,
            align,
            delayDuration,
            sideOffset,
            style,
            classes,
            id,
            line,
            file,
            anchor
        );
    }

    private static LightweaveNode CreateInternal(
        LightweaveNode children,
        Func<LightweaveNode> contentFactory,
        Func<Vector2> sizeFactory,
        TooltipSide side,
        TooltipAlign align,
        float delayDuration,
        float sideOffset,
        Style? style,
        string[]? classes,
        string? id,
        int line,
        string file,
        Func<Rect>? anchorOverride = null
    ) {
        LightweaveNode node = NodeBuilder.New("Tooltip", line, file);
        node.ApplyStyling("tooltip", style, classes, id);
        node.Children.Add(children);

        node.MeasureWidth = () => children.MeasureWidth?.Invoke() ?? 0f;
        node.Measure = availableWidth => children.Measure?.Invoke(availableWidth) ?? children.PreferredHeight ?? 0f;
        node.PreferredHeight = children.PreferredHeight;

        node.Paint = (rect, _) => {
            children.MeasuredRect = rect;
            LightweaveRoot.PaintSubtree(children, rect);

            Rect hoverRect = children.MeasuredRect;

            Hooks.Hooks.RefHandle<float> hoverTimer = Hooks.Hooks.UseRef(0f, line, file);

            bool hovered = Mouse.IsOver(hoverRect);
            Event e = Event.current;

            if (!hovered || e.type == EventType.MouseDown) {
                hoverTimer.Current = 0f;
                return;
            }

            if (e.type == EventType.Repaint) {
                hoverTimer.Current += Time.unscaledDeltaTime;
            }

            if (hoverTimer.Current < delayDuration) {
                return;
            }

            Rect anchorRect = anchorOverride?.Invoke() ?? hoverRect;
            Vector2 size = sizeFactory();
            (Rect tooltipScreenRect, TooltipSide resolvedSide, Rect anchorScreenRect) = ResolvePlacement(anchorRect, size, side, align, sideOffset);
            LightweaveNode content = contentFactory();

            RenderContext.Current.PendingOverlays.Enqueue(() => {
                Vector2 local = GUIUtility.ScreenToGUIPoint(new Vector2(tooltipScreenRect.x, tooltipScreenRect.y));
                Rect tooltipRect = new Rect(local.x, local.y, tooltipScreenRect.width, tooltipScreenRect.height);

                Vector2 anchorLocal = GUIUtility.ScreenToGUIPoint(new Vector2(anchorScreenRect.x, anchorScreenRect.y));
                Rect anchorLocalRect = new Rect(anchorLocal.x, anchorLocal.y, anchorScreenRect.width, anchorScreenRect.height);

                PaintBox.DrawShadow(tooltipRect, ShadowSpec.Of(ThemeSlot.ShadowTooltip));
                DrawArrow(tooltipRect, anchorLocalRect, resolvedSide);

                PaintBox.Draw(
                    tooltipRect,
                    BackgroundSpec.Of(ThemeSlot.SurfaceTooltip),
                    BorderSpec.All(new Rem(1f / 16f), ThemeSlot.BorderTooltip),
                    RadiusSpec.All(RadiusScale.Sm)
                );

                EraseArrowJoin(tooltipRect, anchorLocalRect, resolvedSide);

                float pad = new Rem(0.5f).ToPixels();
                Rect innerRect = new Rect(
                    tooltipRect.x + pad,
                    tooltipRect.y + pad,
                    tooltipRect.width - pad * 2f,
                    tooltipRect.height - pad * 2f
                );

                LightweaveRoot.PaintSubtree(content, innerRect);
            });
        };

        return node;
    }

    private static LightweaveNode BuildTextNode(string text) {
        return Typography.Typography.Text.Create(
            text,
            wrap: true,
            richText: true,
            style: new Style { FontFamily = FontRole.Body, FontSize = new Rem(0.8125f), TextColor = ThemeSlot.TextPrimary }
        );
    }

    

    

    private static void DrawArrow(Rect tooltipRect, Rect anchorRect, TooltipSide resolvedSide) {
        float arrowFill = new Rem(0.5f).ToPixels();
        float arrowOutline = arrowFill + 2f;
        float corner = RadiusSpec.ResolveRem(RadiusScale.Sm).ToPixels() + arrowOutline * 0.5f;

        Vector2 center;
        switch (resolvedSide) {
            case TooltipSide.Top: {
                float x = Mathf.Clamp(anchorRect.center.x, tooltipRect.x + corner, tooltipRect.xMax - corner);
                center = new Vector2(x, tooltipRect.yMax);
                break;
            }
            case TooltipSide.Bottom: {
                float x = Mathf.Clamp(anchorRect.center.x, tooltipRect.x + corner, tooltipRect.xMax - corner);
                center = new Vector2(x, tooltipRect.y);
                break;
            }
            case TooltipSide.Left: {
                float y = Mathf.Clamp(anchorRect.center.y, tooltipRect.y + corner, tooltipRect.yMax - corner);
                center = new Vector2(tooltipRect.xMax, y);
                break;
            }
            case TooltipSide.Right: {
                float y = Mathf.Clamp(anchorRect.center.y, tooltipRect.y + corner, tooltipRect.yMax - corner);
                center = new Vector2(tooltipRect.x, y);
                break;
            }
            default:
                return;
        }

        Color borderColor = RenderContext.Current.Theme.GetColor(ThemeSlot.BorderTooltip);
        Color fillColor = RenderContext.Current.Theme.GetColor(ThemeSlot.SurfaceTooltip);

        using (RotateScope.Around(45f, center)) {
            Rect outlineRect = new Rect(
                center.x - arrowOutline * 0.5f,
                center.y - arrowOutline * 0.5f,
                arrowOutline,
                arrowOutline
            );
            PaintBox.Fill(outlineRect, borderColor);

            Rect fillRect = new Rect(
                center.x - arrowFill * 0.5f,
                center.y - arrowFill * 0.5f,
                arrowFill,
                arrowFill
            );
            PaintBox.Fill(fillRect, fillColor);
        }
    }

    private static void EraseArrowJoin(Rect tooltipRect, Rect anchorRect, TooltipSide resolvedSide) {
        float arrowFill = new Rem(0.5f).ToPixels();
        float arrowOutline = arrowFill + 2f;
        float corner = RadiusSpec.ResolveRem(RadiusScale.Sm).ToPixels() + arrowOutline * 0.5f;
        float halfDiag = arrowOutline * 0.5f * Mathf.Sqrt(2f);
        float strip = 2f;

        Color fillColor = RenderContext.Current.Theme.GetColor(ThemeSlot.SurfaceTooltip);

        Rect eraseRect;
        switch (resolvedSide) {
            case TooltipSide.Top: {
                float x = Mathf.Clamp(anchorRect.center.x, tooltipRect.x + corner, tooltipRect.xMax - corner);
                eraseRect = new Rect(x - halfDiag, tooltipRect.yMax - strip + 1f, halfDiag * 2f, strip);
                break;
            }
            case TooltipSide.Bottom: {
                float x = Mathf.Clamp(anchorRect.center.x, tooltipRect.x + corner, tooltipRect.xMax - corner);
                eraseRect = new Rect(x - halfDiag, tooltipRect.y - 1f, halfDiag * 2f, strip);
                break;
            }
            case TooltipSide.Left: {
                float y = Mathf.Clamp(anchorRect.center.y, tooltipRect.y + corner, tooltipRect.yMax - corner);
                eraseRect = new Rect(tooltipRect.xMax - strip + 1f, y - halfDiag, strip, halfDiag * 2f);
                break;
            }
            case TooltipSide.Right: {
                float y = Mathf.Clamp(anchorRect.center.y, tooltipRect.y + corner, tooltipRect.yMax - corner);
                eraseRect = new Rect(tooltipRect.x - 1f, y - halfDiag, strip, halfDiag * 2f);
                break;
            }
            default:
                return;
        }

        PaintBox.Fill(eraseRect, fillColor);
    }

    

    private static Vector2 MeasureContent(LightweaveNode content, float maxWidthRem) {
        float pad = new Rem(0.5f).ToPixels() * 2f;
        float maxOuter = maxWidthRem * Spacing.BaseUnit;
        float maxInner = Mathf.Max(0f, maxOuter - pad);
        float w = content.MeasureWidth?.Invoke() ?? maxInner;
        w = Mathf.Min(w, maxInner);
        float h = content.Measure?.Invoke(w) ?? content.PreferredHeight ?? 0f;
        return new Vector2(w + pad, h + pad);
    }

    private static float ResolveMaxWidthRem(Length? maxWidth) {
        if (maxWidth.HasValue && maxWidth.Value.Mode == Length.Kind.Rem) {
            return maxWidth.Value.Value;
        }
        return DefaultMaxWidthRem;
    }

    private static (Rect Tooltip, TooltipSide ResolvedSide, Rect AnchorScreen) ResolvePlacement(
        Rect anchorGuiRect,
        Vector2 size,
        TooltipSide side,
        TooltipAlign align,
        float sideOffset
    ) {
        Vector2 topLeft = GUIUtility.GUIToScreenPoint(new Vector2(anchorGuiRect.x, anchorGuiRect.y));
        Vector2 bottomRight = GUIUtility.GUIToScreenPoint(new Vector2(anchorGuiRect.xMax, anchorGuiRect.yMax));
        Rect anchorScreen = new Rect(
            topLeft.x,
            topLeft.y,
            bottomRight.x - topLeft.x,
            bottomRight.y - topLeft.y
        );

        (TooltipSide cardinal, TooltipAlign effective) = ResolveSide(side, align);

        Rect candidate = PlaceTooltip(anchorScreen, size, cardinal, effective, sideOffset);
        if (FitsOnScreen(candidate)) {
            return (candidate, cardinal, anchorScreen);
        }

        TooltipSide opposite = OppositeSide(cardinal);
        Rect flipped = PlaceTooltip(anchorScreen, size, opposite, effective, sideOffset);
        if (FitsOnScreen(flipped)) {
            return (flipped, opposite, anchorScreen);
        }

        return (ClampToScreen(candidate), cardinal, anchorScreen);
    }

    private static (TooltipSide cardinal, TooltipAlign align) ResolveSide(TooltipSide side, TooltipAlign align) {
        return side switch {
            TooltipSide.Top => (TooltipSide.Top, align),
            TooltipSide.TopStart => (TooltipSide.Top, TooltipAlign.Start),
            TooltipSide.TopEnd => (TooltipSide.Top, TooltipAlign.End),
            TooltipSide.Right => (TooltipSide.Right, align),
            TooltipSide.RightStart => (TooltipSide.Right, TooltipAlign.Start),
            TooltipSide.RightEnd => (TooltipSide.Right, TooltipAlign.End),
            TooltipSide.Bottom => (TooltipSide.Bottom, align),
            TooltipSide.BottomStart => (TooltipSide.Bottom, TooltipAlign.Start),
            TooltipSide.BottomEnd => (TooltipSide.Bottom, TooltipAlign.End),
            TooltipSide.Left => (TooltipSide.Left, align),
            TooltipSide.LeftStart => (TooltipSide.Left, TooltipAlign.Start),
            TooltipSide.LeftEnd => (TooltipSide.Left, TooltipAlign.End),
            _ => (TooltipSide.Bottom, align),
        };
    }

    private static TooltipSide OppositeSide(TooltipSide cardinal) {
        return cardinal switch {
            TooltipSide.Top => TooltipSide.Bottom,
            TooltipSide.Bottom => TooltipSide.Top,
            TooltipSide.Left => TooltipSide.Right,
            TooltipSide.Right => TooltipSide.Left,
            _ => TooltipSide.Bottom,
        };
    }

    private static Rect PlaceTooltip(Rect anchor, Vector2 size, TooltipSide side, TooltipAlign align, float offset) {
        float x = 0f;
        float y = 0f;
        switch (side) {
            case TooltipSide.Top:
                y = anchor.y - offset - size.y;
                x = ResolveCrossAxis(anchor.x, anchor.width, size.x, align);
                break;
            case TooltipSide.Bottom:
                y = anchor.yMax + offset;
                x = ResolveCrossAxis(anchor.x, anchor.width, size.x, align);
                break;
            case TooltipSide.Left:
                x = anchor.x - offset - size.x;
                y = ResolveCrossAxis(anchor.y, anchor.height, size.y, align);
                break;
            case TooltipSide.Right:
                x = anchor.xMax + offset;
                y = ResolveCrossAxis(anchor.y, anchor.height, size.y, align);
                break;
        }

        return new Rect(x, y, size.x, size.y);
    }

    private static float ResolveCrossAxis(float anchorStart, float anchorSize, float size, TooltipAlign align) {
        return align switch {
            TooltipAlign.Start => anchorStart + anchorSize - size,
            TooltipAlign.Center => anchorStart + (anchorSize - size) / 2f,
            TooltipAlign.End => anchorStart,
            _ => anchorStart + (anchorSize - size) / 2f,
        };
    }

    private static bool FitsOnScreen(Rect r) {
        return r.x >= 0f && r.y >= 0f && r.xMax <= Screen.width && r.yMax <= Screen.height;
    }

    private static Rect ClampToScreen(Rect r) {
        float x = Mathf.Clamp(r.x, 0f, Mathf.Max(0f, Screen.width - r.width));
        float y = Mathf.Clamp(r.y, 0f, Mathf.Max(0f, Screen.height - r.height));
        return new Rect(x, y, r.width, r.height);
    }
}
