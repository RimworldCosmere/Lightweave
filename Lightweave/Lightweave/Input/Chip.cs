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
    Summary = "Clickable toggle pill that persists a pressed state.",
    WhenToUse = "Filter rows by facet or severity, or expose a sticky on/off tag. Distinct from the static Badge — a Chip is interactive.",
    SourcePath = "Lightweave/Input/Chip.cs"
)]
public static class Chip {
    private static readonly Rem Height = new Rem(1.5f);
    private static readonly Rem DotSize = new Rem(0.4375f);
    private static readonly Rem IconSize = new Rem(0.875f);
    private static readonly Rem HairlineWidth = new Rem(1f / 16f);
    private static readonly Rem LabelFont = new Rem(0.8125f);

    public static LightweaveNode Create(
        [DocParam("Visible label.")]
        string label,
        [DocParam("Pressed state.")]
        bool on,
        [DocParam("Fires with the new pressed value on click.")]
        Action<bool> onToggle,
        [DocParam("Picks which optional ornaments render and how they are styled.")]
        ChipVariant variant = ChipVariant.Default,
        [DocParam("Severity colour drawn from the theme's status slots.")]
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

        bool hasIcon = icon != null;
        if (hasIcon) {
            node.Children.Add(icon!);
        }

        bool dot = showDot ?? (variant == ChipVariant.Severity || variant == ChipVariant.Default);
        bool hasCount = variant == ChipVariant.Filter && count.HasValue;
        InteractionState frameState = default;

        node.MeasureWidth = () => {
            Theme.Theme theme = RenderContext.Current.Theme;
            float padX = SpacingScale.Sm.ToPixels();
            float gap = SpacingScale.Xs.ToPixels();
            float w = padX * 2f;
            if (dot) {
                w += DotSize.ToPixels() + gap;
            }

            if (hasIcon) {
                w += IconSize.ToPixels() + gap;
            }

            GUIStyle labelStyle = LabelStyle(theme);
            w += string.IsNullOrEmpty(label) ? 0f : labelStyle.CalcSize(new GUIContent(label)).x;

            if (hasCount) {
                GUIStyle countStyle = CountStyle(theme);
                string countText = count!.Value.ToString(CultureInfo.InvariantCulture);
                w += gap + HairlineWidth.ToPixels() + gap + countStyle.CalcSize(new GUIContent(countText)).x;
            }

            return Mathf.Ceil(w);
        };

        node.Measure = _ => Height.ToPixels();

        node.Layout = rect => {
            frameState = InteractionState.Resolve(rect, null, disabled);

            if (hasIcon) {
                float padX = SpacingScale.Sm.ToPixels();
                float gap = SpacingScale.Xs.ToPixels();
                float iconPx = IconSize.ToPixels();
                float x = rect.x + padX;
                if (dot) {
                    x += DotSize.ToPixels() + gap;
                }

                icon!.MeasuredRect = new Rect(x, rect.center.y - iconPx * 0.5f, iconPx, iconPx);
            }

            if (disabled) {
                return;
            }

            Event e = Event.current;
            if (e.type == EventType.MouseUp && e.button == 0 && rect.Contains(e.mousePosition)) {
                onToggle?.Invoke(!on);
                e.Use();
            }
        };

        node.Draw = rect => {
            Theme.Theme theme = RenderContext.Current.Theme;
            bool hovered = frameState.Hovered;

            ThemeSlot accentSlot = AccentSlot(tone);
            Color accent = theme.GetColor(accentSlot);

            ThemeSlot borderSlot;
            Color textColor;
            BackgroundSpec? fill = null;
            Color dotColor;

            if (disabled) {
                borderSlot = ThemeSlot.BorderSubtle;
                textColor = WithAlpha(theme.GetColor(ThemeSlot.TextMuted), 0.5f);
                dotColor = WithAlpha(accent, 0.25f);
            }
            else if (on) {
                borderSlot = accentSlot;
                textColor = accent;
                fill = BackgroundSpec.Of(WithAlpha(accent, 0.12f));
                dotColor = accent;
            }
            else {
                borderSlot = hovered ? ThemeSlot.BorderHover : ThemeSlot.BorderOff;
                textColor = theme.GetColor(hovered ? ThemeSlot.TextPrimary : ThemeSlot.TextMuted);
                dotColor = WithAlpha(accent, hovered ? 0.55f : 0.35f);
            }

            RadiusSpec radius = RadiusSpec.All(RadiusScale.Full);
            PaintBox.Draw(rect, fill, BorderSpec.All(HairlineWidth, borderSlot), radius);

            float padX = SpacingScale.Sm.ToPixels();
            float gap = SpacingScale.Xs.ToPixels();
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

            GUIStyle labelStyle = LabelStyle(theme);
            float labelW = string.IsNullOrEmpty(label) ? 0f : labelStyle.CalcSize(new GUIContent(label)).x;
            Rect labelRect = new Rect(x, rect.y, labelW, rect.height);
            TextDraw.DrawWithStyle(labelRect, label, labelStyle, textColor);
            x += labelW;

            if (hasCount) {
                x += gap;
                float hairline = HairlineWidth.ToPixels();
                float ruleInset = new Rem(0.25f).ToPixels();
                Rect ruleRect = new Rect(x, rect.y + ruleInset, hairline, rect.height - ruleInset * 2f);
                PaintBox.Draw(ruleRect, BackgroundSpec.Of(WithAlpha(textColor, 0.45f)), null, null);
                x += hairline + gap;

                GUIStyle countStyle = CountStyle(theme);
                string countText = count!.Value.ToString(CultureInfo.InvariantCulture);
                float countW = countStyle.CalcSize(new GUIContent(countText)).x;
                Rect countRect = new Rect(x, rect.y, countW, rect.height);
                TextDraw.DrawWithStyle(countRect, countText, countStyle, textColor);
            }

            InteractionFeedback.Apply(rect, !disabled, true);
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

    private static GUIStyle LabelStyle(Theme.Theme theme) {
        Font font = theme.GetFont(FontRole.Body);
        int px = Mathf.RoundToInt(LabelFont.ToFontPx());
        GUIStyle gs = GuiStyleCache.GetOrCreate(font, px, FontStyle.Normal);
        gs.alignment = TextAnchor.MiddleLeft;
        return gs;
    }

    private static GUIStyle CountStyle(Theme.Theme theme) {
        Font font = theme.GetFont(FontRole.Mono);
        int px = Mathf.RoundToInt(LabelFont.ToFontPx());
        GUIStyle gs = GuiStyleCache.GetOrCreate(font, px, FontStyle.Normal);
        gs.alignment = TextAnchor.MiddleLeft;
        return gs;
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
