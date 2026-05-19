using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Theme;
using Cosmere.Lightweave.Tokens;
using UnityEngine;
using Verse;
using LText = Cosmere.Lightweave.Typography.Typography.Text;

namespace Cosmere.Lightweave.Types;

[Doc(
    Id = "rem-spacing",
    Summary = "Sizing primitive used for every spacing, padding, and border thickness in Lightweave.",
    WhenToUse = "Pass Rem (or a SpacingScale step) anywhere a size is needed so the whole token system scales together when base units change.",
    SourcePath = "Lightweave/Types/Rem.cs",
    Category = "Foundation",
    PreferredVariantHeight = 120f,
    HideUsage = true,
    HideSource = true
)]
public static class RemDoc {
    [DocVariant("CL_Playground_rem-spacing_Conversion")]
    public static DocSample DocsConversion() {
        return new DocSample(() => {
            Style monoStyle = new Style {
                FontFamily = FontRole.Mono,
                FontSize = new Rem(0.875f),
                TextColor = ThemeSlot.TextPrimary,
            };
            string layoutBase = $"Layout base : {Spacing.BaseUnit}px = 1rem";
            string fontBase = $"Font base   : {Spacing.FontBaseUnit}px = 1rem";
            string half = $"0.5rem  -> {0.5f * Spacing.BaseUnit:0.##}px layout / {0.5f * Spacing.FontBaseUnit:0.##}px font";
            string one = $"1rem    -> {Spacing.BaseUnit}px layout / {Spacing.FontBaseUnit}px font";
            string oneAndHalf = $"1.5rem  -> {1.5f * Spacing.BaseUnit:0.##}px layout / {1.5f * Spacing.FontBaseUnit:0.##}px font";
            return Stack.Create(
                gap: new Rem(0.125f),
                children: col => {
                    col.Add(LText.Create(layoutBase, style: monoStyle));
                    col.Add(LText.Create(fontBase, style: monoStyle));
                    col.Add(LText.Create(" ", style: monoStyle));
                    col.Add(LText.Create(half, style: monoStyle));
                    col.Add(LText.Create(one, style: monoStyle));
                    col.Add(LText.Create(oneAndHalf, style: monoStyle));
                }
            );
        });
    }

    [DocVariant("CL_Playground_rem-spacing_SpacingLadder")]
    public static DocSample DocsSpacingLadder() {
        return new DocSample(() => {
            float rowHeightPx = new Rem(1.5f).ToPixels();
            float labelColumnWidthPx = new Rem(7f).ToPixels();
            Rem rowGap = new Rem(0.25f);
            (string Label, Rem Step)[] ladder = {
                ("Xxs (0.25)", SpacingScale.Xxs),
                ("Xs (0.5)", SpacingScale.Xs),
                ("Sm (0.75)", SpacingScale.Sm),
                ("Md (1)", SpacingScale.Md),
                ("Lg (1.5)", SpacingScale.Lg),
                ("Xl (2)", SpacingScale.Xl),
                ("Xxl (3)", SpacingScale.Xxl),
                ("Xxxl (4)", SpacingScale.Xxxl),
            };
            return Stack.Create(
                gap: rowGap,
                children: col => {
                    for (int i = 0; i < ladder.Length; i++) {
                        (string label, Rem step) = ladder[i];
                        float barWidthPx = step.ToPixels();
                        col.Add(HStack.Create(
                            gap: new Rem(0.5f),
                            children: row => {
                                row.Add(LText.Create(
                                    label,
                                    style: new Style {
                                        FontFamily = FontRole.Mono,
                                        FontSize = new Rem(0.8125f),
                                        TextColor = ThemeSlot.TextMuted,
                                    }
                                ), labelColumnWidthPx);
                                row.Add(Box.Create(
                                    bar => { },
                                    style: new Style {
                                        Background = BackgroundSpec.Of(ThemeSlot.SurfaceAccent),
                                        Radius = RadiusSpec.All(RadiusScale.Sm),
                                        Height = Length.Rem(0.75f),
                                    }
                                ), barWidthPx);
                            }
                        ), rowHeightPx);
                    }
                }
            );
        });
    }


    [DocVariant("CL_Playground_rem-spacing_PaddingSteps")]
    public static DocSample DocsPaddingSteps() {
        LightweaveNode Cell(string labelKey, Rem step) =>
            Box.Create(
                inner => inner.Add(LText.Create(
                    (string)labelKey.Translate(),
                    style: new Style {
                        FontFamily = FontRole.Mono,
                        FontSize = new Rem(0.8125f),
                        TextColor = ThemeSlot.TextOnAccent,
                        TextAlign = TextAlign.Center,
                    }
                )),
                style: new Style {
                    Padding = EdgeInsets.All(step),
                    Background = BackgroundSpec.Of(ThemeSlot.SurfaceAccent),
                    Radius = RadiusSpec.All(RadiusScale.Sm),
                }
            );

        return new DocSample(() =>
            HStack.Create(
                gap: SpacingScale.Sm,
                children: row => {
                    row.AddFlex(Cell("CL_Playground_rem-spacing_Step_Xs", SpacingScale.Xs));
                    row.AddFlex(Cell("CL_Playground_rem-spacing_Step_Sm", SpacingScale.Sm));
                    row.AddFlex(Cell("CL_Playground_rem-spacing_Step_Md", SpacingScale.Md));
                    row.AddFlex(Cell("CL_Playground_rem-spacing_Step_Lg", SpacingScale.Lg));
                }
            )
        );
    }

    [DocVariant("CL_Playground_rem-spacing_StackGaps")]
    public static DocSample DocsStackGaps() {
        Style chipStyle = new Style {
            Padding = new EdgeInsets(SpacingScale.Xxs, SpacingScale.Sm, SpacingScale.Xxs, SpacingScale.Sm),
            Background = BackgroundSpec.Of(ThemeSlot.SurfaceRaised),
            Border = BorderSpec.All(new Rem(1f / 16f), ThemeSlot.BorderSubtle),
            Radius = RadiusSpec.All(RadiusScale.Sm),
        };

        LightweaveNode Chip() =>
            Box.Create(
                chip => chip.Add(LText.Create(
                    "·",
                    style: new Style {
                        FontFamily = FontRole.Body,
                        FontSize = new Rem(0.75f),
                        TextColor = ThemeSlot.TextSecondary,
                        TextAlign = TextAlign.Center,
                    }
                )),
                style: chipStyle
            );

        LightweaveNode Section(string labelKey, Rem gap) =>
            Stack.Create(
                gap: SpacingScale.Xxs,
                children: section => {
                    section.Add(LText.Create(
                        (string)labelKey.Translate(),
                        style: new Style {
                            FontFamily = FontRole.Mono,
                            FontSize = new Rem(0.75f),
                            TextColor = ThemeSlot.TextMuted,
                        }
                    ));
                    section.Add(HStack.Create(
                        gap: gap,
                        children: row => {
                            row.AddHug(Chip());
                            row.AddHug(Chip());
                            row.AddHug(Chip());
                            row.AddHug(Chip());
                        }
                    ));
                }
            );

        return new DocSample(() =>
            Stack.Create(
                gap: SpacingScale.Md,
                children: col => {
                    col.Add(Section("CL_Playground_rem-spacing_Step_Xxs", SpacingScale.Xxs));
                    col.Add(Section("CL_Playground_rem-spacing_Step_Xs", SpacingScale.Xs));
                    col.Add(Section("CL_Playground_rem-spacing_Step_Sm", SpacingScale.Sm));
                    col.Add(Section("CL_Playground_rem-spacing_Step_Md", SpacingScale.Md));
                }
            )
        );
    }

    [DocVariant("CL_Playground_rem-spacing_ButtonsAreRems")]
    public static DocSample DocsButtonsAreRems() {
        return new DocSample(() =>
            HStack.Create(
                gap: SpacingScale.Sm,
                children: row => {
                    row.AddHug(Button.Create(label: Variant.Primary.Id, onClick: null, variant: Variant.Primary));
                    row.AddHug(Button.Create(label: Variant.Secondary.Id, onClick: null, variant: Variant.Secondary));
                    row.AddHug(Button.Create(label: Variant.Ghost.Id, onClick: null, variant: Variant.Ghost));
                }
            )
        );
    }

    

    

    

}
