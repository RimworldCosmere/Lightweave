using System;
using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;

namespace Cosmere.Lightweave.Input;

[Doc(
    Id = "button",
    Summary = "Clickable text button with variant styling.",
    WhenToUse = "Trigger an action with a label-driven affordance.",
    SourcePath = "Lightweave/Input/Button.cs"
)]
public static class Button {
    public static LightweaveNode Create(
        string label,
        Action? onClick,
        Variant variant = default,
        bool ghost = false,
        LightweaveNode? leading = null,
        LightweaveNode? trailing = null,
        bool disabled = false,
        bool? playHoverSound = null,
        LightweaveNode? body = null,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        LightweaveNode node = NodeBuilder.New($"Button:{label}", line, file);
        node.ApplyStyling("button", style, classes, id);

        Style resolved0 = node.GetResolvedStyle();
        float intrinsicHeightPx = resolved0.Height is { Mode: Length.Kind.Rem } h0
            ? h0.ToPixels(0f, 0f)
            : new Rem(2.875f).ToPixels();
        node.PreferredHeight = intrinsicHeightPx;

        Rem ResolveLabelFontSize(Style ls) => ls.FontSize ?? new Rem(0.875f);

        Font ResolveLabelFont(Style ls, Theme.Theme th) => ls.FontFamily switch {
            FontRef.Literal lit => lit.Value,
            FontRef.Role r => th.GetFont(r.RoleValue),
            _ => th.GetFont(FontRole.BodyBold),
        };

        float ResolveLabelTrackingPx(Style ls, Rem fontSize) {
            Tracking? t = ls.LetterSpacing;
            if (!t.HasValue || Mathf.Approximately(t.Value.Em, 0f)) {
                return 0f;
            }
            return t.Value.ToPixels(fontSize.ToFontPx());
        }

        node.MeasureWidth = () => {
            Theme.Theme theme = RenderContext.Current.Theme;
            Style s = node.GetResolvedStyle();
            Rem fontSize = ResolveLabelFontSize(s);
            Font font = ResolveLabelFont(s, theme);
            int pixelSize = Mathf.RoundToInt(fontSize.ToFontPx());
            GUIStyle gstyle = GuiStyleCache.GetOrCreate(font, pixelSize);
            float trackingPx = ResolveLabelTrackingPx(s, fontSize);
            float labelWidth;
            if (string.IsNullOrEmpty(label) || body != null) {
                labelWidth = 0f;
            }
            else if (trackingPx > 0f) {
                labelWidth = TextDraw.MeasureTracked(label, FontRole.BodyBold, fontSize, trackingPx, FontStyle.Normal, font);
            }
            else {
                labelWidth = gstyle.CalcSize(new GUIContent(label)).x;
            }
            float padXPx = new Rem(2f).ToPixels();
            float padYPx = new Rem(1f).ToPixels();
            float iconGapPx = SpacingScale.Sm.ToPixels();
            float iconSize = Mathf.Min(intrinsicHeightPx - padYPx, new Rem(1.25f).ToPixels());
            float iconAllowance = (leading != null ? iconSize + iconGapPx : 0f)
                                + (trailing != null ? iconSize + iconGapPx : 0f);
            return labelWidth + iconAllowance + padXPx * 2f;
        };

        if (leading != null) {
            node.Children.Add(leading);
        }

        if (trailing != null) {
            node.Children.Add(trailing);
        }

        if (body != null) {
            node.Children.Add(body);
        }

        node.Paint = (allocatedRect, paintChildren) => {
            Theme.Theme theme = RenderContext.Current.Theme;
            Direction dir = RenderContext.Current.Direction;
            Style s = node.GetResolvedStyle();

            bool fullWidth = s.Width is { IsGrower: true };
            bool fillHeight = s.Height is { IsGrower: true };

            float desiredHeight = intrinsicHeightPx;
            float h = fillHeight ? allocatedRect.height : Mathf.Min(desiredHeight, allocatedRect.height);
            float yOffset = fillHeight ? 0f : (allocatedRect.height - h) * 0.5f;

            float padXPx = new Rem(2f).ToPixels();
            float padYPx = new Rem(1f).ToPixels();
            float iconGapPx = SpacingScale.Sm.ToPixels();
            float iconSize = Mathf.Min(h - padYPx, new Rem(1.25f).ToPixels());

            Rem fontSize = ResolveLabelFontSize(s);
            Font font = ResolveLabelFont(s, theme);
            int pixelSize = Mathf.RoundToInt(fontSize.ToFontPx());
            GUIStyle gstyle = GuiStyleCache.GetOrCreate(font, pixelSize);
            gstyle.alignment = TextAnchor.MiddleCenter;
            float trackingPx = ResolveLabelTrackingPx(s, fontSize);

            float labelWidth;
            if (string.IsNullOrEmpty(label) || body != null) {
                labelWidth = 0f;
            }
            else if (trackingPx > 0f) {
                labelWidth = TextDraw.MeasureTracked(label, FontRole.BodyBold, fontSize, trackingPx, FontStyle.Normal, font);
            }
            else {
                labelWidth = gstyle.CalcSize(new GUIContent(label)).x;
            }
            float iconAllowance = (leading != null ? iconSize + iconGapPx : 0f)
                                + (trailing != null ? iconSize + iconGapPx : 0f);
            float naturalWidth = labelWidth + iconAllowance + padXPx * 2f;

            Rect rect;
            if (fullWidth || body != null) {
                rect = new Rect(allocatedRect.x, allocatedRect.y + yOffset, allocatedRect.width, h);
            }
            else {
                float w = Mathf.Min(naturalWidth, allocatedRect.width);
                float x = dir == Direction.Rtl ? allocatedRect.xMax - w : allocatedRect.x;
                rect = new Rect(x, allocatedRect.y + yOffset, w, h);
            }

            node.MeasuredRect = rect;

            InteractionState state = InteractionState.Resolve(rect, null, disabled);

            ThemeSlot fgSlot = VariantPalette.Foreground(variant, state, ghost);
            ThemeSlot? borderSlot = VariantPalette.Border(variant, state, ghost);
            RadiusSpec radius = RadiusSpec.All(RadiusScale.Sm);
            BorderSpec? border = borderSlot.HasValue
                ? BorderSpec.All(new Rem(1f / 16f), borderSlot.Value)
                : null;

            bool isRepaint = Event.current?.type == EventType.Repaint;

            if (isRepaint) {
                if (variant == Variant.Frosted && !ghost) {
                    bool active = state.Hovered || state.Pressed;
                    BackdropBlur.Draw(rect, active ? 8f : 6f, cornerRadiusPx: radius.ResolveVector(dir).x);
                    PaintBox.Draw(rect, BackgroundSpec.Of(ThemeSlot.Glass3), border, radius);
                }
                else {
                    ThemeSlot? bgSlot = VariantPalette.Background(variant, state, ghost);
                    BackgroundSpec? bg;
                    if (!bgSlot.HasValue) {
                        if ((variant == Variant.Ghost || variant == Variant.Quiet) && state.Hovered && !disabled) {
                            Color ghostHoverBase = theme.GetColor(ThemeSlot.SurfaceGhostHover);
                            bg = BackgroundSpec.Of(new Color(ghostHoverBase.r, ghostHoverBase.g, ghostHoverBase.b, 0.4f));
                        }
                        else {
                            bg = null;
                        }
                    }
                    else if (variant == Variant.Primary && !ghost && !state.Pressed && !disabled) {
                        Color accent = theme.GetColor(bgSlot.Value);
                        Color.RGBToHSV(accent, out float hue, out float sat, out float val);
                        if (state.Hovered) {
                            val = Mathf.Clamp01(val * 1.18f);
                            sat = Mathf.Clamp01(sat * 0.9f);
                        }
                        Color top = Color.HSVToRGB(hue, sat, val);
                        Color bottom = Color.HSVToRGB(hue, sat, val * 0.78f);
                        top.a = accent.a;
                        bottom.a = accent.a;
                        bg = new BackgroundSpec.Gradient(GradientTextureCache.Vertical(top, bottom));
                    }
                    else if (variant == Variant.Secondary && (state.Hovered || state.Pressed) && !disabled) {
                        Color secondaryTint = theme.GetColor(ThemeSlot.HoverTint);
                        if (state.Pressed) {
                            secondaryTint.a = Mathf.Clamp01(secondaryTint.a * 1.6f);
                        }
                        bg = BackgroundSpec.Of(secondaryTint);
                    }
                    else {
                        bg = BackgroundSpec.Of(bgSlot.Value);
                    }

                    PaintBox.Draw(rect, bg, border, radius);
                }

                float overlay = VariantPalette.OverlayAlpha(state);
                if (overlay > 0f) {
                    Color overlayColor = InteractionFeedback.OverlayColor(theme, state, overlay);
                    PaintBox.Draw(rect, BackgroundSpec.Of(overlayColor), null, radius);
                }
            }

            bool rtl = dir == Direction.Rtl;
            Rect labelRect = new Rect(rect.x + padXPx, rect.y, rect.width - padXPx * 2f, rect.height);

            if (leading != null) {
                float groupW = iconSize + iconGapPx + labelWidth;
                float centeredStart = rect.x + (rect.width - groupW) * 0.5f;
                float iconY = rect.y + (rect.height - iconSize) / 2f;
                if (rtl) {
                    float labelX = Mathf.Min(rect.xMax - padXPx - labelWidth, centeredStart + iconSize + iconGapPx);
                    float leadingX = labelX - iconGapPx - iconSize;
                    leading.MeasuredRect = new Rect(leadingX, iconY, iconSize, iconSize);
                    labelRect = new Rect(labelX, labelRect.y, rect.xMax - padXPx - labelX, labelRect.height);
                    gstyle.alignment = TextAnchor.MiddleLeft;
                }
                else {
                    float leadingX = Mathf.Max(rect.x + padXPx, centeredStart);
                    leading.MeasuredRect = new Rect(leadingX, iconY, iconSize, iconSize);
                    float labelX = leadingX + iconSize + iconGapPx;
                    labelRect = new Rect(labelX, labelRect.y, rect.xMax - padXPx - labelX, labelRect.height);
                    gstyle.alignment = TextAnchor.MiddleLeft;
                }
            }

            if (trailing != null) {
                if (!fullWidth && body == null && leading == null) {
                    float groupW = labelWidth + iconGapPx + iconSize;
                    float centeredStart = Mathf.Max(rect.x + padXPx, rect.x + (rect.width - groupW) * 0.5f);
                    float iconY = rect.y + (rect.height - iconSize) / 2f;
                    if (rtl) {
                        trailing.MeasuredRect = new Rect(centeredStart, iconY, iconSize, iconSize);
                        float labelX = centeredStart + iconSize + iconGapPx;
                        labelRect = new Rect(labelX, labelRect.y, labelWidth, labelRect.height);
                    }
                    else {
                        labelRect = new Rect(centeredStart, labelRect.y, labelWidth, labelRect.height);
                        trailing.MeasuredRect = new Rect(centeredStart + labelWidth + iconGapPx, iconY, iconSize, iconSize);
                    }
                    gstyle.alignment = TextAnchor.MiddleLeft;
                }
                else {
                    float trailingX = rtl
                        ? rect.x + padXPx
                        : rect.xMax - padXPx - iconSize;
                    trailing.MeasuredRect = new Rect(trailingX, rect.y + (rect.height - iconSize) / 2f, iconSize, iconSize);
                    if (rtl) {
                        labelRect = new Rect(
                            trailingX + iconSize + iconGapPx,
                            labelRect.y,
                            labelRect.xMax - (trailingX + iconSize + iconGapPx),
                            labelRect.height
                        );
                    }
                    else {
                        labelRect = new Rect(labelRect.x, labelRect.y, trailingX - padXPx - labelRect.x, labelRect.height);
                    }
                }
            }

            if (body != null) {
                body.MeasuredRect = rect;
                LightweaveRoot.PaintSubtree(body, rect);
            }
            else if (isRepaint) {
                Color fg = s.TextColor switch {
                    ColorRef.Literal lit => lit.Value,
                    ColorRef.Token tok => theme.GetColor(tok.Slot),
                    _ => theme.GetColor(fgSlot),
                };

                if (trackingPx > 0f) {
                    TextDraw.DrawTracked(labelRect, label, FontRole.BodyBold, fontSize, gstyle.alignment, fg, trackingPx, FontStyle.Normal, font);
                }
                else {
                    TextDraw.DrawWithStyle(labelRect, label, gstyle, fg);
                }
            }

            paintChildren();

            InteractionFeedback.Apply(rect, !disabled, playHoverSound ?? true);

            if (!disabled && onClick != null && Widgets.ButtonInvisible(rect, doMouseoverSound: false) && LightweaveHitTracker.IsTopmost(rect)) {
                onClick.Invoke();
            }
        };

        return node;
    }

    [DocVariant("CL_Playground_Label_Primary")]
    public static DocSample DocsPrimary() {
        return new DocSample(() => Create("Primary", () => { }));
    }

    [DocVariant("CL_Playground_Label_Secondary")]
    public static DocSample DocsSecondary() {
        return new DocSample(() => Create("Secondary", () => { }, Variant.Secondary));
    }

    [DocVariant("CL_Playground_Label_Ghost")]
    public static DocSample DocsGhost() {
        return new DocSample(() => Create("Ghost", () => { }, Variant.Ghost));
    }

    [DocVariant("CL_Playground_Label_Danger")]
    public static DocSample DocsDanger() {
        return new DocSample(() => Create("Danger", () => { }, Variant.Danger));
    }


    [DocVariant("CL_Playground_Label_Frosted")]
    public static DocSample DocsFrosted() {
        return new DocSample(() => Create("Frosted", () => { }, Variant.Frosted));
    }

    [DocVariant("CL_Playground_Label_Quiet")]
    public static DocSample DocsQuiet() {
        return new DocSample(() => Create("Quiet", () => { }, Variant.Quiet));
    }

    private static LightweaveNode AllVariantsRow() {
        return HStack.Create(
            gap: SpacingScale.Sm,
            children: row => {
                row.AddHug(Create(Variant.Primary.Id, null, Variant.Primary));
                row.AddHug(Create(Variant.Secondary.Id, null, Variant.Secondary));
                row.AddHug(Create(Variant.Ghost.Id, null, Variant.Ghost));
                row.AddHug(Create(Variant.Danger.Id, null, Variant.Danger));
                row.AddHug(Create(Variant.Frosted.Id, null, Variant.Frosted));
                row.AddHug(Create(Variant.Quiet.Id, null, Variant.Quiet));
            }
        );
    }

    [DocState("CL_Playground_Label_Default", HideCode = true)]
    public static DocSample DocsDefault() {
        return new DocSample(() => AllVariantsRow());
    }

    [DocState("CL_Playground_Label_Hover", HideCode = true)]
    public static DocSample DocsHover() {
        return new DocSample(() => AllVariantsRow());
    }

    [DocState("CL_Playground_Label_Active", HideCode = true)]
    public static DocSample DocsActive() {
        return new DocSample(() => AllVariantsRow());
    }

    [DocState("CL_Playground_Label_Disabled", HideCode = true)]
    public static DocSample DocsDisabled() {
        return new DocSample(() => AllVariantsRow());
    }

    [DocUsage]
    public static DocSample DocsUsage() {
        return new DocSample(() => Create("Confirm", () => { }));
    }
}
