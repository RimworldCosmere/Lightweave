using System;
using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using static Cosmere.Lightweave.Typography.Typography;

namespace Cosmere.Lightweave.Feedback;

[Doc(
    Id = "diff-banner",
    Summary = "Inline tinted banner reporting how many values differ from defaults, with an optional reset action.",
    WhenToUse = "Surface drift status (e.g. settings out of sync with defaults) in a detail header strip.",
    SourcePath = "Lightweave/Feedback/DiffBanner.cs"
)]
public static class DiffBanner {
    public static LightweaveNode Create(
        [DocParam("Banner copy. Caller is responsible for translation and pluralization.")]
        string label,
        [DocParam("Optional callback for the trailing reset button. Pair with resetLabel.")]
        Action? onReset = null,
        [DocParam("Localized label for the reset button. Required when onReset is set.")]
        string? resetLabel = null,
        [DocParam("Inline style override.", TypeOverride = "Style?", DefaultOverride = "null")]
        Style? style = null,
        [DocParam("Additional class names merged after the base 'diff-banner' class.", TypeOverride = "string[]?", DefaultOverride = "null")]
        string[]? classes = null,
        [DocParam("Stable id for state-style lookup.", TypeOverride = "string?", DefaultOverride = "null")]
        string? id = null,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        LightweaveNode node = NodeBuilder.New("DiffBanner", line, file);
        node.ApplyStyling("diff-banner", style, classes, id);

        LightweaveNode content = HStack.Create(SpacingScale.Sm, h => {
            h.AddHug(PulseDot.Create(color: ThemeSlot.SurfaceAccent, size: new Rem(0.5f)));
            h.AddFlex(Text.Create(
                label,
                wrap: false,
                style: new Style {
                    FontFamily = FontRole.Mono,
                    FontSize = new Rem(0.75f),
                    TextColor = ThemeSlot.TextSecondary,
                }
            ));
            if (onReset != null && !string.IsNullOrEmpty(resetLabel)) {
                h.AddHug(Button.Create(
                    label: resetLabel!,
                    onClick: onReset,
                    variant: Variant.Ghost
                ));
            }
        });

        LightweaveNode wrapper = Box.Create(
            children: c => c.Add(content),
            style: new Style {
                Width = Length.Stretch,
                Background = BackgroundSpec.Of(ThemeSlot.AccentSoft),
                Padding = new EdgeInsets(Top: SpacingScale.Xs, Bottom: SpacingScale.Xs, Left: SpacingScale.Md, Right: SpacingScale.Xs),
                Border = new BorderSpec(Bottom: new Rem(1f / 16f), Color: ThemeSlot.BorderSubtle),
            }
        );

        node.Children.Add(wrapper);
        node.MeasureWidth = () => wrapper.MeasureWidth?.Invoke() ?? 0f;
        node.Measure = availableWidth => wrapper.Measure?.Invoke(availableWidth) ?? wrapper.PreferredHeight ?? 0f;
        node.Paint = (rect, paintChildren) => {
            wrapper.MeasuredRect = rect;
            paintChildren();
        };

        return node;
    }

    [DocUsage]
    public static DocSample DocsUsage() {
        return new DocSample(() => Create("3 settings differ from defaults"));
    }

    [DocVariant("Default")]
    public static DocSample DocsDefault() {
        return new DocSample(() => Create("3 settings differ from defaults"));
    }

    [DocVariant("WithReset")]
    public static DocSample DocsWithReset() {
        return new DocSample(() => Create(
            label: "3 settings differ from defaults",
            onReset: () => {},
            resetLabel: "RESET"
        ));
    }

    [DocState("OneItem")]
    public static DocSample DocsOneItem() {
        return new DocSample(() => Create("1 setting differs from default"));
    }

    [DocState("Many")]
    public static DocSample DocsMany() {
        return new DocSample(() => Create(
            label: "12 settings differ from defaults",
            onReset: () => {},
            resetLabel: "RESET ALL"
        ));
    }
}
