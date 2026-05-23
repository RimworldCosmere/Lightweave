using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;
using static Cosmere.Lightweave.Hooks.Hooks;

namespace Cosmere.Lightweave.Input;

[Doc(
    Id = "chip",
    Summary = "Square mono pill. Interactive toggle when given onToggle, or a static status tag when not.",
    WhenToUse = "Filter rows by facet or severity (interactive), or render a small status/category tag (static). The single pill primitive — there is no separate Badge/Pill/Tag.",
    SourcePath = "Lightweave/Input/Chip.cs"
)]
public static class Chip {
    private static readonly Rem Height = new Rem(1.5f);
    private static readonly Rem PadX = new Rem(0.75f);
    private static readonly Rem Gap = new Rem(0.5f);
    private static readonly Rem DotSize = new Rem(0.375f);
    private static readonly Rem IconSize = new Rem(0.75f);
    private static readonly Rem HairlineWidth = new Rem(1f / 16f);
    private static readonly Rem LabelFont = new Rem(10.5f / 16f);
    private static readonly Rem CountFont = new Rem(9.5f / 16f);
    private const float LabelTrackingEm = 0.18f;
    private const float CountTrackingEm = 0.1f;

    public static LightweaveNode Create(
        [DocParam("Visible label. Rendered uppercase.")]
        string label,
        [DocParam("Pressed/active state.")]
        bool on,
        [DocParam("Fires with the new pressed value on click. Pass null for a static, non-interactive tag.")]
        Action<bool>? onToggle = null,
        [DocParam("Picks which optional ornaments render and how they are styled.")]
        ChipVariant variant = ChipVariant.Default,
        [DocParam("Severity/semantic colour drawn from the theme's status slots.")]
        ChipTone tone = ChipTone.None,
        [DocParam("Right-side count for the filter variant only.")]
        int? count = null,
        [DocParam("Optional inline icon node rendered before the label.")]
        LightweaveNode? icon = null,
        [DocParam("Greys the chip and blocks clicks.")]
        bool disabled = false,
        [DocParam("Overrides whether the leading dot renders. Defaults to on for Default and Severity variants.")]
        bool? showDot = null,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        LightweaveNode node = NodeBuilder.New($"Chip:{label}", line, file);
        node.ApplyStyling("chip", style, classes, id);
        node.PreferredHeight = Height.ToPixels();

        string displayLabel = string.IsNullOrEmpty(label) ? string.Empty : label.ToUpperInvariant();
        bool interactive = onToggle != null && !disabled;
        bool hasIcon = icon != null;
        if (hasIcon) {
            node.Children.Add(icon!);
        }

        bool dot = showDot ?? (variant == ChipVariant.Severity || variant == ChipVariant.Default);
        bool hasCount = variant == ChipVariant.Filter && count.HasValue;
        string countText = hasCount ? count!.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;
        InteractionState frameState = default;

        node.MeasureWidth = () => {
            float padX = PadX.ToPixels();
            float gap = Gap.ToPixels();
            float labelPx = LabelFont.ToFontPx();
            float w = padX * 2f;
            if (dot) {
                w += DotSize.ToPixels() + gap;
            }

            if (hasIcon) {
                w += IconSize.ToPixels() + gap;
            }

            w += TextDraw.MeasureTracked(displayLabel, FontRole.Mono, LabelFont, LabelTrackingEm * labelPx);

            if (hasCount) {
                float countPx = CountFont.ToFontPx();
                w += gap + HairlineWidth.ToPixels() + gap
                     + TextDraw.MeasureTracked(countText, FontRole.Mono, CountFont, CountTrackingEm * countPx);
            }

            return Mathf.Ceil(w);
        };

        node.Measure = _ => Height.ToPixels();

        node.Layout = rect => {
            frameState = interactive ? InteractionState.Resolve(rect, null, false) : default;

            if (hasIcon) {
                float padX = PadX.ToPixels();
                float gap = Gap.ToPixels();
                float iconPx = IconSize.ToPixels();
                float x = rect.x + padX;
                if (dot) {
                    x += DotSize.ToPixels() + gap;
                }

                icon!.MeasuredRect = new Rect(x, rect.center.y - iconPx * 0.5f, iconPx, iconPx);
            }

            if (!interactive) {
                return;
            }

            Event e = Event.current;
            if (e.type == EventType.MouseUp && e.button == 0 && rect.Contains(e.mousePosition)) {
                onToggle!.Invoke(!on);
                e.Use();
            }
        };

        node.Draw = rect => {
            Theme.Theme theme = RenderContext.Current.Theme;
            bool hovered = frameState.Hovered;
            bool noneTone = tone == ChipTone.None;

            ThemeSlot accentSlot = AccentSlot(tone);
            Color accent = theme.GetColor(accentSlot);

            Color borderColor;
            Color textColor;
            Color? fillColor = null;
            Color dotColor = accent;
            float dotAlpha;

            if (on) {
                textColor = accent;
                borderColor = noneTone ? accent : WithAlpha(accent, 0.5f);
                fillColor = WithAlpha(accent, noneTone ? 0.12f : 0.09f);
                dotAlpha = 1f;
            }
            else {
                borderColor = theme.GetColor(hovered ? ThemeSlot.BorderHover : ThemeSlot.BorderOff);
                textColor = theme.GetColor(hovered ? ThemeSlot.TextPrimary : ThemeSlot.TextSecondary);
                dotAlpha = hovered ? 0.55f : 0.35f;
            }

            float opacity = disabled ? 0.4f : 1f;
            borderColor = WithAlpha(borderColor, opacity);
            textColor = WithAlpha(textColor, opacity);
            dotColor = WithAlpha(dotColor, dotAlpha * opacity);
            BackgroundSpec? fill = fillColor.HasValue ? BackgroundSpec.Of(WithAlpha(fillColor.Value, opacity)) : null;

            PaintBox.Draw(rect, fill, BorderSpec.All(HairlineWidth, borderColor), null);

            float padX = PadX.ToPixels();
            float gap = Gap.ToPixels();
            float x = rect.x + padX;

            if (dot) {
                float dotPx = DotSize.ToPixels();
                Rect dotRect = new Rect(x, rect.center.y - dotPx * 0.5f, dotPx, dotPx);
                PaintBox.Draw(dotRect, BackgroundSpec.Of(dotColor), null, RadiusSpec.All(RadiusScale.Full));
                x += dotPx + gap;
            }

            if (hasIcon) {
                x += IconSize.ToPixels() + gap;
            }

            float labelPx = LabelFont.ToFontPx();
            float labelW = TextDraw.MeasureTracked(displayLabel, FontRole.Mono, LabelFont, LabelTrackingEm * labelPx);
            Rect labelRect = new Rect(x, rect.y, labelW, rect.height);
            TextDraw.DrawTracked(labelRect, displayLabel, FontRole.Mono, LabelFont, TextAnchor.MiddleLeft, textColor, LabelTrackingEm * labelPx);
            x += labelW;

            if (hasCount) {
                x += gap;
                float hairline = HairlineWidth.ToPixels();
                float ruleInset = new Rem(0.25f).ToPixels();
                Rect ruleRect = new Rect(x, rect.y + ruleInset, hairline, rect.height - ruleInset * 2f);
                PaintBox.Draw(ruleRect, BackgroundSpec.Of(WithAlpha(textColor, 0.5f)), null, null);
                x += hairline + gap;

                float countPx = CountFont.ToFontPx();
                float countW = TextDraw.MeasureTracked(countText, FontRole.Mono, CountFont, CountTrackingEm * countPx);
                Rect countRect = new Rect(x, rect.y, countW, rect.height);
                TextDraw.DrawTracked(countRect, countText, FontRole.Mono, CountFont, TextAnchor.MiddleLeft, WithAlpha(textColor, on ? 1f : 0.7f), CountTrackingEm * countPx);
            }

            if (interactive) {
                InteractionFeedback.Apply(rect, true, true);
            }
        };

        return node;
    }

    private static ThemeSlot AccentSlot(ChipTone tone) {
        switch (tone) {
            case ChipTone.Trace: return ThemeSlot.TextSecondary;
            case ChipTone.Debug: return ThemeSlot.StatusInfo;
            case ChipTone.Info: return ThemeSlot.StatusSuccess;
            case ChipTone.Warn: return ThemeSlot.StatusWarning;
            case ChipTone.Error: return ThemeSlot.StatusDanger;
            default: return ThemeSlot.SurfaceAccent;
        }
    }

    private static Color WithAlpha(Color color, float alpha) {
        return new Color(color.r, color.g, color.b, color.a * alpha);
    }

    private static LightweaveNode SeverityRow(bool error, bool warn, bool info, bool debug, bool trace) {
        return HStack.Create(
            gap: SpacingScale.Xs,
            children: b => {
                b.AddHug(StatefulChip("CL_Playground_Chip_Trace", ChipVariant.Severity, ChipTone.Trace, trace));
                b.AddHug(StatefulChip("CL_Playground_Chip_Debug", ChipVariant.Severity, ChipTone.Debug, debug));
                b.AddHug(StatefulChip("CL_Playground_Chip_Info", ChipVariant.Severity, ChipTone.Info, info));
                b.AddHug(StatefulChip("CL_Playground_Chip_Warn", ChipVariant.Severity, ChipTone.Warn, warn));
                b.AddHug(StatefulChip("CL_Playground_Chip_Error", ChipVariant.Severity, ChipTone.Error, error));
            }
        );
    }

    private static LightweaveNode StatefulChip(string labelKey, ChipVariant variant, ChipTone tone, bool initialOn, int? count = null) {
        StateHandle<bool> on = UseState(initialOn);
        return Create(
            (string)labelKey.Translate(),
            on.Value,
            v => on.Set(v),
            variant: variant,
            tone: tone,
            count: count
        );
    }

    [DocVariant("CL_Playground_Label_Default")]
    public static DocSample DocsDefault() {
        return new DocSample(() => HStack.Create(
            gap: SpacingScale.Xs,
            children: b => {
                b.AddHug(StatefulChip("CL_Playground_Chip_Pinned", ChipVariant.Default, ChipTone.None, true));
                b.AddHug(StatefulChip("CL_Playground_Chip_Archived", ChipVariant.Default, ChipTone.None, false));
                b.AddHug(StatefulChip("CL_Playground_Chip_MineOnly", ChipVariant.Default, ChipTone.None, true));
            }
        ));
    }

    [DocVariant("CL_Playground_Label_Severity")]
    public static DocSample DocsSeverity() {
        return new DocSample(() => SeverityRow(error: true, warn: true, info: true, debug: false, trace: false));
    }

    [DocVariant("CL_Playground_Label_Filter")]
    public static DocSample DocsFilter() {
        return new DocSample(() => HStack.Create(
            gap: SpacingScale.Xs,
            children: b => {
                b.AddHug(StatefulChip("CL_Playground_Chip_Vanilla", ChipVariant.Filter, ChipTone.None, true, 412));
                b.AddHug(StatefulChip("CL_Playground_Chip_Workshop", ChipVariant.Filter, ChipTone.None, false, 1206));
                b.AddHug(StatefulChip("CL_Playground_Chip_Dlc", ChipVariant.Filter, ChipTone.None, false, 68));
            }
        ));
    }

    [DocVariant("CL_Playground_Label_Status")]
    public static DocSample DocsStatus() {
        return new DocSample(() => HStack.Create(
            gap: SpacingScale.Xs,
            children: b => {
                b.AddHug(Create((string)"CL_Playground_Chip_Error".Translate(), true, tone: ChipTone.Error, showDot: false));
                b.AddHug(Create((string)"CL_Playground_Chip_Warn".Translate(), true, tone: ChipTone.Warn, showDot: false));
                b.AddHug(Create((string)"CL_Playground_Chip_Info".Translate(), true, tone: ChipTone.Info, showDot: false));
                b.AddHug(Create((string)"CL_Playground_Chip_Archived".Translate(), false, tone: ChipTone.None, showDot: false));
            }
        ));
    }

    [DocState("CL_Playground_Label_Rest")]
    public static DocSample DocsRest() {
        return new DocSample(() => Create((string)"CL_Playground_Chip_Pinned".Translate(), false, _ => { }));
    }

    [DocState("CL_Playground_Label_Hover")]
    public static DocSample DocsHover() {
        return new DocSample(() => Create((string)"CL_Playground_Chip_Pinned".Translate(), false, _ => { }));
    }

    [DocState("CL_Playground_Label_On")]
    public static DocSample DocsOn() {
        return new DocSample(() => Create((string)"CL_Playground_Chip_Pinned".Translate(), true, _ => { }));
    }

    [DocState("CL_Playground_Label_Disabled")]
    public static DocSample DocsDisabled() {
        return new DocSample(() => Create((string)"CL_Playground_Chip_Pinned".Translate(), false, _ => { }, disabled: true));
    }

    [DocUsage]
    public static DocSample DocsUsage() {
        StateHandle<bool> on = UseState(true);
        return new DocSample(() => Create(
            (string)"CL_Playground_Chip_Warn".Translate(),
            on.Value,
            v => on.Set(v),
            variant: ChipVariant.Severity,
            tone: ChipTone.Warn
        ));
    }
}
