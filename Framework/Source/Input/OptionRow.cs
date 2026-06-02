using System;
using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Surfaces;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using RimWorld;
using UnityEngine;
using Verse.Sound;
using static Cosmere.Lightweave.Typography.Typography;

namespace Cosmere.Lightweave.Input;

[Doc(
    Id = "option-row",
    Summary = "Clickable row: optional leading node, flexing label, optional trailing node.",
    WhenToUse = "A selectable list/menu item where Button's centered content won't do — e.g. icon + name + trailing tag in a picker.",
    SourcePath = "Lightweave/Input/OptionRow.cs"
)]
public static class OptionRow {
    public static LightweaveNode Create(
        [DocParam("Row label, drawn left-aligned in the flexing middle column.")]
        string label,
        [DocParam("Click handler. Null or disabled makes the row inert.")]
        Action? onClick,
        [DocParam("Optional leading node (icon badge, glyph), hugged at the start.", TypeOverride = "LightweaveNode?", DefaultOverride = "null")]
        LightweaveNode? leading = null,
        [DocParam("Optional trailing node (tag, caret), hugged at the end.", TypeOverride = "LightweaveNode?", DefaultOverride = "null")]
        LightweaveNode? trailing = null,
        [DocParam("Dims the row to 40% and blocks interaction.")]
        bool disabled = false,
        [DocParam("Play the vanilla mouseover sound on hover. Defaults to true.", TypeOverride = "bool?", DefaultOverride = "null")]
        bool? playHoverSound = null,
        [DocParam("Gap between leading, label, and trailing. Defaults to SpacingScale.Sm.", TypeOverride = "Rem?", DefaultOverride = "null")]
        Rem? gap = null,
        [DocParam("Vertical nudge applied to the label only. Corrects the line-box-vs-glyph centering gap when the label is all-caps and a leading glyph is present.", TypeOverride = "Rem?", DefaultOverride = "null")]
        Rem? labelOffsetY = null,
        [DocParam("Style for the row surface (background/border/radius), content padding, and the label font (family/size/color/letter-spacing).", TypeOverride = "Style?", DefaultOverride = "null")]
        Style? style = null,
        [DocParam("Additional class names merged after the base 'option-row' class.", TypeOverride = "string[]?", DefaultOverride = "null")]
        string[]? classes = null,
        [DocParam("Stable id for state-style lookup.", TypeOverride = "string?", DefaultOverride = "null")]
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        LightweaveNode node = NodeBuilder.New("OptionRow:" + label, line, file);
        Style baseStyle = style ?? new Style();
        // Padding insets the inner row's content, not the node, so the node surface
        // (background/border/radius) still covers the full rect while content stays inset.
        EdgeInsets pad = baseStyle.Padding ?? new EdgeInsets(new Rem(0.5625f), new Rem(0.75f), new Rem(0.5625f), new Rem(0.75f));
        Style nodeStyle = baseStyle.Padding != null ? baseStyle with { Padding = null } : baseStyle;
        node.ApplyStyling("option-row", nodeStyle, classes, id);

        Style labelStyle = new Style {
            Width = Length.Stretch,
            FontFamily = baseStyle.FontFamily ?? FontRole.Body,
            FontSize = baseStyle.FontSize ?? new Rem(15f / 16f),
            LetterSpacing = baseStyle.LetterSpacing,
            TextColor = baseStyle.TextColor ?? ThemeSlot.TextPrimary,
        };
        if (labelOffsetY.HasValue) {
            labelStyle = labelStyle with { Position = Position.Relative, Top = labelOffsetY.Value };
        }

        LightweaveNode inner = HStack.Create(
            gap ?? SpacingScale.Sm,
            h => {
                if (leading != null) {
                    h.AddHug(leading);
                }
                h.AddFlex(Text.Create(label, style: labelStyle));
                if (trailing != null) {
                    h.AddHug(trailing);
                }
            },
            align: FlexAlign.Center,
            style: new Style {
                Width = Length.Stretch,
                Padding = pad,
            });
        node.Children.Add(inner);

        node.MeasureWidth = () => inner.MeasureWidth?.Invoke() ?? 0f;
        node.Measure = availableWidth => inner.Measure?.Invoke(availableWidth) ?? inner.PreferredHeight ?? 0f;
        node.PreferredHeight = inner.PreferredHeight;

        node.Paint = (rect, paintChildren) => {
            InteractionState st = InteractionState.Resolve(rect, null, disabled);
            bool hot = !disabled && (st.Hovered || st.Pressed);

            if (hot) {
                Style s = node.GetResolvedStyle();
                PaintBox.Draw(rect, BackgroundSpec.Of(ThemeSlot.HoverTint), null, s.Radius);
                if (playHoverSound ?? true) {
                    MouseoverSounds.DoRegion(rect);
                }
            }

            inner.MeasuredRect = rect;

            Color saved = GUI.color;
            if (disabled) {
                GUI.color = new Color(saved.r, saved.g, saved.b, saved.a * 0.4f);
            }
            paintChildren();
            if (disabled) {
                GUI.color = saved;
            }

            if (!disabled && onClick != null) {
                Event e = Event.current;
                if (e.type == EventType.MouseUp && e.button == 0 && rect.Contains(e.mousePosition) && LightweaveHitTracker.IsTopmost(rect)) {
                    onClick.Invoke();
                    SoundDefOf.Click.PlayOneShotOnCamera();
                    RenderContext.Current.Hooks.Invalidate();
                    e.Use();
                }
            }
        };

        return node;
    }

    private static LightweaveNode Badge(ThemeSlot tone) {
        return Box.Create(
            c => c.Add(Text.Create("LW", style: new Style {
                Width = Length.Stretch,
                FontFamily = FontRole.Mono,
                FontSize = new Rem(0.625f),
                TextColor = ThemeSlot.TextOnAccent,
                TextAlign = TextAlign.Center,
            })),
            style: new Style {
                Width = new Rem(1.875f),
                Height = new Rem(1.875f),
                Background = BackgroundSpec.Of(tone),
                Radius = RadiusSpec.All(RadiusScale.Sm),
            });
    }

    private static LightweaveNode CountTag(string text) {
        return Text.Create(text, style: new Style {
            FontFamily = FontRole.Mono,
            FontSize = new Rem(0.625f),
            LetterSpacing = Tracking.Of(0.12f),
            TextColor = ThemeSlot.TextMuted,
        });
    }

    [DocUsage]
    public static DocSample DocsUsage() {
        return new DocSample(() => Create(
            "Civil outlander union",
            () => { },
            leading: Badge(ThemeSlot.SurfaceAccent),
            trailing: CountTag("1 / 3")));
    }

    [DocVariant("CL_Playground_Label_Default")]
    public static DocSample DocsPlain() {
        return new DocSample(() => Create("Plain row", () => { }));
    }

    [DocVariant("CL_Playground_Label_Leading")]
    public static DocSample DocsLeading() {
        return new DocSample(() => Create("With badge", () => { }, leading: Badge(ThemeSlot.StatusSuccess)));
    }

    [DocVariant("CL_Playground_Label_Trailing")]
    public static DocSample DocsTrailing() {
        return new DocSample(() => Create("With count", () => { }, trailing: CountTag("max 1")));
    }

    [DocState("CL_Playground_Label_Default", HideCode = true)]
    public static DocSample DocsStateDefault() {
        return new DocSample(() => Create("Savage tribe", () => { }, leading: Badge(ThemeSlot.StatusDanger), trailing: CountTag("×2")));
    }

    [DocState("CL_Playground_Label_Hover", HideCode = true)]
    public static DocSample DocsStateHover() {
        return new DocSample(() => Create("Savage tribe", () => { }, leading: Badge(ThemeSlot.StatusDanger), trailing: CountTag("×2")));
    }

    [DocState("CL_Playground_Label_Disabled", HideCode = true)]
    public static DocSample DocsStateDisabled() {
        return new DocSample(() => Create("Empire", () => { }, leading: Badge(ThemeSlot.SurfaceAccent), trailing: CountTag("max 1"), disabled: true));
    }
}
