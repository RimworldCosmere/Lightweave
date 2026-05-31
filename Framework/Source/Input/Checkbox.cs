using System;
using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;
using Verse.Sound;
using static Cosmere.Lightweave.Hooks.Hooks;

namespace Cosmere.Lightweave.Input;

[Doc(
    Id = "checkbox",
    Summary = "Two-state toggle with adjacent label.",
    WhenToUse = "Capture a single boolean choice in a form or row.",
    SourcePath = "Lightweave/Input/Checkbox.cs"
)]
public static class Checkbox {
    public static LightweaveNode Create(
        [DocParam("Text rendered next to the box.")]
        string label,
        [DocParam("Current checked state.")]
        bool value,
        [DocParam("Invoked with the new value when toggled.")]
        Action<bool> onChange,
        [DocParam("Disables interaction and applies disabled styling.")]
        bool disabled = false,
        [DocParam("Optional translation key shown as a tooltip on hover.")]
        string? tooltipKey = null,
        [DocParam("Optional subtitle rendered under the label in muted text.")]
        string? hint = null,
        [DocParam("Draws a dash glyph instead of a checkmark when true and value is false.")]
        bool indeterminate = false,
        Variant variant = default,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        LightweaveNode node = NodeBuilder.New($"Checkbox:{label}", line, file);
        node.ApplyStyling("checkbox", style, classes, id);

        Rem boxSizeRem = new Rem(1f);
        Rem gapRem = new Rem(0.75f);
        Rem labelLineRem = new Rem(1.25f);
        Rem hintLineRem = new Rem(1.1f);
        Rem labelHintGapRem = new Rem(0.15f);

        float boxSize = boxSizeRem.ToPixels();
        float gapPx = gapRem.ToPixels();
        float labelLine = labelLineRem.ToPixels();
        float hintLine = hintLineRem.ToPixels();
        float labelHintGap = labelHintGapRem.ToPixels();

        bool hasHint = !string.IsNullOrEmpty(hint);
        float bodyHeight = hasHint ? labelLine + labelHintGap + hintLine : labelLine;
        float rowHeight = Mathf.Max(boxSize, bodyHeight);
        node.PreferredHeight = rowHeight;

        node.MeasureWidth = () => {
            Theme.Theme theme = RenderContext.Current.Theme;
            Font labelFont = theme.GetFont(FontRole.Body);
            int labelPixelSize = Mathf.RoundToInt(new Rem(0.875f).ToFontPx());
            GUIStyle labelStyle = GuiStyleCache.GetOrCreate(labelFont, labelPixelSize);
            float labelW = string.IsNullOrEmpty(label) ? 0f : labelStyle.CalcSize(new GUIContent(label)).x;
            float hintW = 0f;
            if (hasHint) {
                Font hintFont = theme.GetFont(FontRole.Caption);
                int hintPixelSize = Mathf.RoundToInt(new Rem(0.75f).ToFontPx());
                GUIStyle hintStyle = GuiStyleCache.GetOrCreate(hintFont, hintPixelSize);
                hintW = hintStyle.CalcSize(new GUIContent(hint!)).x;
            }
            float bodyW = Mathf.Max(labelW, hintW);
            return Mathf.Ceil(boxSize + gapPx + bodyW);
        };

        node.Measure = availableWidth => rowHeight;

        node.Paint = (rect, paintChildren) => {
            Theme.Theme theme = RenderContext.Current.Theme;
            Direction dir = RenderContext.Current.Direction;
            bool rtl = dir == Direction.Rtl;

            float boxX = rtl ? rect.xMax - boxSize : rect.x;
            float boxY = rect.y + Mathf.Max(0f, (rect.height - boxSize) / 2f);
            if (hasHint) {
                boxY = rect.y + Mathf.Max(0f, (labelLine - boxSize) / 2f);
            }
            Rect boxRect = new Rect(boxX, boxY, boxSize, boxSize);

            float bodyX = rtl ? rect.x : boxX + boxSize + gapPx;
            float bodyWidth = rtl
                ? boxX - gapPx - rect.x
                : rect.xMax - bodyX;
            float bodyY = rect.y + Mathf.Max(0f, (rect.height - bodyHeight) / 2f);
            Rect labelRect = new Rect(bodyX, bodyY, Mathf.Max(0f, bodyWidth), labelLine);

            Rect hitRect = new Rect(rect.x, rect.y, rect.width, rect.height);
            LightweaveHitTracker.Track(hitRect);

            RenderContext ctx = RenderContext.Current;
            bool effectiveDisabled = disabled || ctx.ForceDisabled;
            bool mouseOver = Mouse.IsOver(hitRect);
            bool hovered = !effectiveDisabled && (mouseOver || ctx.ForceHovered || ctx.ForcePressed);
            bool focused = !effectiveDisabled && ctx.ForceFocused;
            if (!effectiveDisabled) {
                MouseoverSounds.DoRegion(hitRect);
            }
            else if (mouseOver) {
                CursorOverrides.MarkDisabledHover();
            }

            if (!string.IsNullOrEmpty(tooltipKey)) {
                TooltipHandler.TipRegion(hitRect, (string)tooltipKey.Translate());
            }

            DrawBox(boxRect, value, effectiveDisabled, hovered, indeterminate, focused);

            Font labelFont = theme.GetFont(FontRole.Body);
            int labelPixelSize = Mathf.RoundToInt(new Rem(0.875f).ToFontPx());
            GUIStyle labelStyle = GuiStyleCache.GetOrCreate(labelFont, labelPixelSize);
            labelStyle.alignment = rtl ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            Color labelColor = effectiveDisabled
                ? theme.GetColor(ThemeSlot.TextMuted)
                : theme.GetColor(ThemeSlot.TextPrimary);
            TextDraw.DrawWithStyle(labelRect, label, labelStyle, labelColor);

            if (hasHint) {
                Rect hintRect = new Rect(bodyX, labelRect.yMax + labelHintGap, Mathf.Max(0f, bodyWidth), hintLine);
                Font hintFont = theme.GetFont(FontRole.Caption);
                int hintPixelSize = Mathf.RoundToInt(new Rem(0.75f).ToFontPx());
                GUIStyle hintStyle = GuiStyleCache.GetOrCreate(hintFont, hintPixelSize);
                hintStyle.alignment = rtl ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
                Color hintColor = effectiveDisabled
                    ? theme.GetColor(ThemeSlot.TextMuted)
                    : theme.GetColor(ThemeSlot.TextSecondary);
                TextDraw.DrawWithStyle(hintRect, hint!, hintStyle, hintColor);
            }

            paintChildren();

            Event e = Event.current;
            if (!effectiveDisabled && e.type == EventType.MouseUp && e.button == 0 && hitRect.Contains(e.mousePosition) && LightweaveHitTracker.IsTopmost(hitRect)) {
                onChange?.Invoke(!value);
                RenderContext.Current.Hooks.Invalidate();
                e.Use();
            }
        };

        return node;
    }

    private static void DrawCheckmark(Rect rect, Color color) {
        float pad = rect.width * 0.18f;
        float stroke = Mathf.Max(2f, rect.width * 0.18f);
        Vector2 p1 = new Vector2(rect.x + pad, rect.y + rect.height * 0.52f);
        Vector2 p2 = new Vector2(rect.x + rect.width * 0.42f, rect.yMax - pad);
        Vector2 p3 = new Vector2(rect.xMax - pad, rect.y + pad);
        PaintBox.DrawRotatedLine(p1, p2, color, stroke);
        PaintBox.DrawRotatedLine(p2, p3, color, stroke);
    }

    private static void DrawDash(Rect rect, Color color) {
        float pad = rect.width * 0.22f;
        float stroke = Mathf.Max(1.5f, rect.width * 0.14f);
        Vector2 p1 = new Vector2(rect.x + pad, rect.y + rect.height * 0.5f);
        Vector2 p2 = new Vector2(rect.xMax - pad, rect.y + rect.height * 0.5f);
        PaintBox.DrawRotatedLine(p1, p2, color, stroke);
    }


    public static void DrawBox(Rect rect, bool value, bool disabled, bool hovered, bool indeterminate = false, bool focused = false) {
        Theme.Theme theme = RenderContext.Current.Theme;
        InteractionState boxState = new InteractionState(hovered && !disabled, false, false, disabled);
        bool filled = value || (indeterminate && !value);
        BackgroundSpec boxBg = filled
            ? BackgroundSpec.Of(disabled ? ThemeSlot.SurfaceDisabled : ThemeSlot.SurfaceAccent)
            : BackgroundSpec.Of(disabled ? ThemeSlot.SurfaceDisabled : ThemeSlot.SurfaceInput);
        BorderSpec? boxBorder = null;
        if (focused && !disabled) {
            boxBorder = BorderSpec.All(new Rem(1f / 16f), ThemeSlot.BorderFocus);
        }
        else if (!filled || hovered || disabled) {
            ThemeSlot borderSlot = InputSurface.ResolveToggleBorderSlot(boxState, filled);
            boxBorder = BorderSpec.All(new Rem(1f / 16f), borderSlot);
        }
        RadiusSpec boxRadius = RadiusSpec.All(RadiusScale.None);
        PaintBox.Draw(rect, boxBg, boxBorder, boxRadius);
        if (value) {
            DrawCheckmark(rect, theme.GetColor(ThemeSlot.TextOnAccent));
        }
        else if (indeterminate) {
            DrawDash(rect, theme.GetColor(ThemeSlot.TextOnAccent));
        }
    }

    [DocVariant("CL_Playground_Label_True")]
    public static DocSample DocsTrue() {
        StateHandle<bool> s = UseState(true);
        return new DocSample(() => Create("Enabled", s.Value, v => s.Set(v)));
    }

    [DocVariant("CL_Playground_Label_False")]
    public static DocSample DocsFalse() {
        StateHandle<bool> s = UseState(false);
        return new DocSample(() => Create("Disabled", s.Value, v => s.Set(v)));
    }

    [DocVariant("CL_Playground_Label_Hint")]
    public static DocSample DocsHint() {
        StateHandle<bool> s = UseState(true);
        return new DocSample(() => Create(
            "Permadeath",
            s.Value,
            v => s.Set(v),
            hint: "No save scumming. Death is forever."
        ));
    }

    [DocVariant("CL_Playground_Label_Indeterminate")]
    public static DocSample DocsIndeterminate() {
        StateHandle<bool> s = UseState(false);
        return new DocSample(() => Create(
            "Some sub-items enabled",
            s.Value,
            v => s.Set(v),
            indeterminate: !s.Value
        ));
    }

    private static LightweaveNode AllVariantsRow() {
        return HStack.Create(
            SpacingScale.Lg,
            row => {
                row.AddHug(Create("Checked", true, _ => { }));
                row.AddHug(Create("Unchecked", false, _ => { }));
                row.AddHug(Create("Mixed", false, _ => { }, indeterminate: true));
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

    [DocState("CL_Playground_Label_Focus", HideCode = true)]
    public static DocSample DocsFocus() {
        return new DocSample(() => AllVariantsRow());
    }

    [DocState("CL_Playground_Label_Disabled", HideCode = true)]
    public static DocSample DocsDisabled() {
        return new DocSample(() => AllVariantsRow());
    }

    [DocUsage]
    public static DocSample DocsUsage() {
        StateHandle<bool> s = UseState(true);
        return new DocSample(() => Create(
            "Pause on threat",
            s.Value,
            v => s.Set(v),
            hint: "Auto-pauses time when raiders are spotted"
        ));
    }
}