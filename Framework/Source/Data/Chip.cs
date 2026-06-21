using System;
using System.Globalization;
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
using static Cosmere.Lightweave.Hooks.Hooks;

namespace Cosmere.Lightweave.Data;

[Doc(
    Id = "chip",
    Summary = "Square mono pill. A static tag by default; opt into interactive to make it a toggle.",
    WhenToUse = "Render a small status/category tag (static), or set interactive for a toggle that filters rows by facet or severity. The single pill primitive — there is no separate Badge/Pill/Tag.",
    SourcePath = "Lightweave/Feedback/Chip.cs"
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
        [DocParam("Semantic colour drawn from the theme's status slots.")]
        ChipVariant variant = ChipVariant.None,
        [DocParam("Whether the chip responds to hover and click. Off by default — a static tag.")]
        bool interactive = false,
        [DocParam("Pressed/active state. Null or false renders off; true renders the filled active look. Defaults off.")]
        bool? state = null,
        [DocParam("Fires with the new pressed value on click. Supply for an interactive chip to report toggles.")]
        Action<bool>? onToggle = null,
        [DocParam("Optional right-side count rendered after a hairline divider.")]
        int? count = null,
        [DocParam("Optional inline icon node rendered before the label.")]
        LightweaveNode? icon = null,
        [DocParam("Greys the chip and blocks clicks.")]
        bool disabled = false,
        [DocParam("Overrides whether the leading dot renders. Defaults on unless a count is shown.")]
        bool? showDot = null,
        [DocParam("Render label + count in the bold mono weight. Defaults true; flip off for quieter inline tags.")]
        bool bold = true,
        [DocParam("Upper-case the label. Defaults true (tag look); flip off for sentence-case content pills.")]
        bool upper = true,
        [DocParam("Overall scale. Large bumps height, padding, icon and font ~1.4x for prominent content tags.")]
        ChipSize size = ChipSize.Default,
        [DocParam("Optional explicit accent colour slot. Overrides the variant's status colour for brand/category tags (e.g. per-DLC). When set, the chip renders the coloured tag look regardless of variant.")]
        ThemeSlot? accent = null,
        Style? style = null,
        string[]? classes = null,
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        bool large = size == ChipSize.Large;
        Rem height = large ? new Rem(2.1f) : Height;
        Rem padX = large ? new Rem(1.05f) : PadX;
        Rem gap = large ? new Rem(0.7f) : Gap;
        Rem dotSize = large ? new Rem(0.525f) : DotSize;
        Rem iconSize = large ? new Rem(1.05f) : IconSize;
        Rem labelFont = large ? new Rem(14.7f / 16f) : LabelFont;
        Rem countFont = large ? new Rem(13.3f / 16f) : CountFont;

        LightweaveNode node = NodeBuilder.New($"Chip:{label}", line, file);
        node.ApplyStyling("chip", style, classes, id);
        node.PreferredHeight = height.ToPixels();

        string displayLabel = string.IsNullOrEmpty(label)
            ? string.Empty
            : upper ? label.ToUpperInvariant() : label;
        bool on = state ?? false;
        bool isInteractive = interactive && !disabled;
        bool hasIcon = icon != null;
        if (hasIcon) {
            node.Children.Add(icon!);
        }

        bool hasCount = count.HasValue;
        bool dot = showDot ?? !hasCount;
        string countText = hasCount ? count!.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;
        FontRole monoRole = bold ? FontRole.MonoBold : FontRole.Mono;
        InteractionState frameState = default;

        node.MeasureWidth = () => {
            float padXpx = padX.ToPixels();
            float gapPx = gap.ToPixels();
            float labelPx = labelFont.ToFontPx();
            float w = padXpx * 2f;
            if (dot) {
                w += dotSize.ToPixels() + gapPx;
            }

            if (hasIcon) {
                w += iconSize.ToPixels() + gapPx;
            }

            w += TextDraw.MeasureTracked(displayLabel, monoRole, labelFont, LabelTrackingEm * labelPx);

            if (hasCount) {
                float countPx = countFont.ToFontPx();
                w += gapPx + HairlineWidth.ToPixels() + gapPx
                     + TextDraw.MeasureTracked(countText, monoRole, countFont, CountTrackingEm * countPx);
            }

            return Mathf.Ceil(w);
        };

        node.Measure = _ => height.ToPixels();

        node.Layout = rect => {
            frameState = isInteractive ? InteractionState.Resolve(rect, null, false) : default;

            if (hasIcon) {
                float padXpx = padX.ToPixels();
                float gapPx = gap.ToPixels();
                float iconPx = iconSize.ToPixels();
                float x = rect.x + padXpx;
                if (dot) {
                    x += dotSize.ToPixels() + gapPx;
                }

                icon!.MeasuredRect = new Rect(x, rect.center.y - iconPx * 0.5f, iconPx, iconPx);
            }

            if (!isInteractive) {
                return;
            }

            Event e = Event.current;
            if (e.type == EventType.MouseUp && e.button == 0 && rect.Contains(e.mousePosition) && LightweaveHitTracker.IsTopmost(rect)) {
                onToggle?.Invoke(!on);
                e.Use();
            }
        };

        node.Draw = rect => {
            Theme.Theme theme = RenderContext.Current.Theme;
            bool hovered = frameState.Hovered;
            bool noneVariant = variant == ChipVariant.None && !accent.HasValue;

            ThemeSlot accentSlot = accent ?? AccentSlot(variant);
            Color accentColor = theme.GetColor(accentSlot);

            Color borderColor;
            Color textColor;
            Color? fillColor = null;
            Color dotColor = accentColor;
            float dotAlpha;

            if (on) {
                textColor = accentColor;
                borderColor = noneVariant ? accentColor : WithAlpha(accentColor, 0.5f);
                fillColor = WithAlpha(accentColor, noneVariant ? 0.12f : 0.09f);
                dotAlpha = 1f;
            }
            else {
                borderColor = theme.GetColor(hovered ? ThemeSlot.BorderHover : ThemeSlot.BorderOff);
                textColor = theme.GetColor(hovered ? ThemeSlot.TextPrimary : ThemeSlot.TextSecondary);
                dotAlpha = hovered ? 0.55f : 0.35f;
            }

            if (frameState.Focused && !disabled) {
                borderColor = theme.GetColor(ThemeSlot.BorderFocus);
            }

            float opacity = disabled ? 0.4f : 1f;
            borderColor = WithAlpha(borderColor, opacity);
            textColor = WithAlpha(textColor, opacity);
            dotColor = WithAlpha(dotColor, dotAlpha * opacity);
            BackgroundSpec? fill = fillColor.HasValue ? BackgroundSpec.Of(WithAlpha(fillColor.Value, opacity)) : null;

            PaintBox.Draw(rect, fill, BorderSpec.All(HairlineWidth, borderColor), RadiusSpec.All(RadiusScale.Pill));

            float padXpx = padX.ToPixels();
            float gapPx = gap.ToPixels();
            float x = rect.x + padXpx;

            if (dot) {
                float dotPx = dotSize.ToPixels();
                Rect dotRect = new Rect(x, rect.center.y - dotPx * 0.5f, dotPx, dotPx);
                PaintBox.Draw(dotRect, BackgroundSpec.Of(dotColor), null, RadiusSpec.All(RadiusScale.Full));
                x += dotPx + gapPx;
            }

            if (hasIcon) {
                x += iconSize.ToPixels() + gapPx;
            }

            float labelPx = labelFont.ToFontPx();
            float labelW = TextDraw.MeasureTracked(displayLabel, monoRole, labelFont, LabelTrackingEm * labelPx);
            Rect labelRect = new Rect(x, rect.y, labelW, rect.height);
            TextDraw.DrawTracked(labelRect, displayLabel, monoRole, labelFont, TextAnchor.MiddleLeft, textColor, LabelTrackingEm * labelPx);
            x += labelW;

            if (hasCount) {
                x += gapPx;
                float hairline = HairlineWidth.ToPixels();
                float ruleInset = new Rem(0.25f).ToPixels();
                Rect ruleRect = new Rect(x, rect.y + ruleInset, hairline, rect.height - ruleInset * 2f);
                PaintBox.Draw(ruleRect, BackgroundSpec.Of(WithAlpha(textColor, 0.5f)), null, null);
                x += hairline + gapPx;

                float countPx = countFont.ToFontPx();
                float countW = TextDraw.MeasureTracked(countText, monoRole, countFont, CountTrackingEm * countPx);
                Rect countRect = new Rect(x, rect.y, countW, rect.height);
                TextDraw.DrawTracked(countRect, countText, monoRole, countFont, TextAnchor.MiddleLeft, WithAlpha(textColor, on ? 1f : 0.7f), CountTrackingEm * countPx);
            }

            if (isInteractive) {
                InteractionFeedback.Apply(rect, true, true);
            }
        };

        return node;
    }

    private static ThemeSlot AccentSlot(ChipVariant variant) {
        switch (variant) {
            case ChipVariant.Trace: return ThemeSlot.TextSecondary;
            case ChipVariant.Debug: return ThemeSlot.StatusInfo;
            case ChipVariant.Info: return ThemeSlot.StatusSuccess;
            case ChipVariant.Warn: return ThemeSlot.StatusWarning;
            case ChipVariant.Error: return ThemeSlot.StatusDanger;
            default: return ThemeSlot.SurfaceAccent;
        }
    }

    private static Color WithAlpha(Color color, float alpha) {
        return new Color(color.r, color.g, color.b, color.a * alpha);
    }

    private static LightweaveNode VariantRow(bool error, bool warn, bool info, bool debug, bool trace) {
        return HStack.Create(
            gap: SpacingScale.Xs,
            children: b => {
                b.AddHug(StatefulChip("CL_Playground_Chip_Trace", ChipVariant.Trace, trace));
                b.AddHug(StatefulChip("CL_Playground_Chip_Debug", ChipVariant.Debug, debug));
                b.AddHug(StatefulChip("CL_Playground_Chip_Info", ChipVariant.Info, info));
                b.AddHug(StatefulChip("CL_Playground_Chip_Warn", ChipVariant.Warn, warn));
                b.AddHug(StatefulChip("CL_Playground_Chip_Error", ChipVariant.Error, error));
            }
        );
    }

    private static LightweaveNode StatefulChip(string labelKey, ChipVariant variant, bool initialOn, int? count = null) {
        StateHandle<bool> on = UseState(initialOn, file: $"Chip#{labelKey}");
        return Create(
            (string)labelKey.Translate(),
            variant: variant,
            interactive: true,
            state: on.Value,
            onToggle: v => on.Set(v),
            count: count
        );
    }

    [DocVariant("CL_Playground_Label_Default")]
    public static DocSample DocsDefault() {
        return new DocSample(() => HStack.Create(
            gap: SpacingScale.Xs,
            children: b => {
                b.AddHug(Create((string)"CL_Playground_Chip_None".Translate(), variant: ChipVariant.None, state: true));
                b.AddHug(Create((string)"CL_Playground_Chip_Trace".Translate(), variant: ChipVariant.Trace, state: true));
                b.AddHug(Create((string)"CL_Playground_Chip_Debug".Translate(), variant: ChipVariant.Debug, state: true));
                b.AddHug(Create((string)"CL_Playground_Chip_Info".Translate(), variant: ChipVariant.Info, state: true));
                b.AddHug(Create((string)"CL_Playground_Chip_Warn".Translate(), variant: ChipVariant.Warn, state: true));
                b.AddHug(Create((string)"CL_Playground_Chip_Error".Translate(), variant: ChipVariant.Error, state: true));
            }
        ));
    }

    [DocVariant("CL_Playground_Label_Variant")]
    public static DocSample DocsVariant() {
        return new DocSample(() => VariantRow(error: true, warn: true, info: true, debug: false, trace: false));
    }

    [DocVariant("CL_Playground_Label_Filter")]
    public static DocSample DocsFilter() {
        return new DocSample(() => HStack.Create(
            gap: SpacingScale.Xs,
            children: b => {
                b.AddHug(StatefulChip("CL_Playground_Chip_Vanilla", ChipVariant.None, true, 412));
                b.AddHug(StatefulChip("CL_Playground_Chip_Workshop", ChipVariant.None, false, 1206));
                b.AddHug(StatefulChip("CL_Playground_Chip_Dlc", ChipVariant.None, false, 68));
            }
        ));
    }

    [DocVariant("CL_Playground_Label_Status")]
    public static DocSample DocsStatus() {
        return new DocSample(() => HStack.Create(
            gap: SpacingScale.Xs,
            children: b => {
                b.AddHug(Create((string)"CL_Playground_Chip_Error".Translate(), variant: ChipVariant.Error, state: true, showDot: false));
                b.AddHug(Create((string)"CL_Playground_Chip_Warn".Translate(), variant: ChipVariant.Warn, state: true, showDot: false));
                b.AddHug(Create((string)"CL_Playground_Chip_Info".Translate(), variant: ChipVariant.Info, state: true, showDot: false));
                b.AddHug(Create((string)"CL_Playground_Chip_Archived".Translate(), variant: ChipVariant.None, showDot: false));
            }
        ));
    }


    [DocVariant("CL_Playground_Label_Content")]
    public static DocSample DocsContent() {
        return new DocSample(() => HStack.Create(
            gap: SpacingScale.Xs,
            children: b => {
                b.AddHug(Create((string)"CL_Playground_Chip_MineOnly".Translate(), upper: false, bold: false, showDot: false));
                b.AddHug(Create((string)"CL_Playground_Chip_Workshop".Translate(), upper: false, bold: false, showDot: false));
                b.AddHug(Create((string)"CL_Playground_Chip_Archived".Translate(), upper: false, bold: false, showDot: false));
            }
        ));
    }

    [DocUsage]
    public static DocSample DocsUsage() {
        return new DocSample(() => Create(
            (string)"CL_Playground_Chip_Warn".Translate(),
            variant: ChipVariant.Warn,
            state: true
        ));
    }

    [DocVariant("CL_Playground_Label_Large")]
    public static DocSample DocsSize() {
        return new DocSample(() => HStack.Create(
            gap: SpacingScale.Xs,
            children: b => {
                b.AddHug(Create((string)"CL_Playground_Chip_MineOnly".Translate(), size: ChipSize.Large, upper: false, bold: false, showDot: false));
                b.AddHug(Create((string)"CL_Playground_Chip_Workshop".Translate(), size: ChipSize.Large, count: 800, upper: false, bold: false, showDot: false));
            }
        ));
    }

    [DocVariant("CL_Playground_Label_Accent")]
    public static DocSample DocsAccent() {
        return new DocSample(() => HStack.Create(
            gap: SpacingScale.Xs,
            children: b => {
                b.AddHug(Create((string)"CL_Playground_Chip_Royalty".Translate(), accent: ThemeSlot.DlcRoyalty, state: true, upper: false, bold: false, showDot: false));
                b.AddHug(Create((string)"CL_Playground_Chip_Biotech".Translate(), accent: ThemeSlot.DlcBiotech, state: true, upper: false, bold: false, showDot: false));
                b.AddHug(Create((string)"CL_Playground_Chip_Anomaly".Translate(), accent: ThemeSlot.DlcAnomaly, state: true, upper: false, bold: false, showDot: false));
            }
        ));
    }
}
