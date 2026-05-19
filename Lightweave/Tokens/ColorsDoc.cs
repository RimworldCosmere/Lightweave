using System;
using Cosmere.Lightweave.Doc;
using Cosmere.Lightweave.Feedback;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Rendering;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;
using LText = Cosmere.Lightweave.Typography.Typography.Text;

namespace Cosmere.Lightweave.Tokens;

[Doc(
    Id = "colors",
    Summary = "Theme color slots and variant palettes used everywhere paint resolves a color.",
    WhenToUse = "Pass a ThemeSlot to Theme.GetColor inside a Paint callback. Never inline RGB literals in a primitive.",
    SourcePath = "Lightweave/Tokens/ThemeSlot.cs",
    Category = "Foundation",
    PreferredVariantHeight = 120f,
    HideUsage = true,
    HideSource = true
)]
public static class ColorsDoc {
    private static readonly (string Group, ThemeSlot[] Slots)[] SlotGroups = {
        ("CL_Playground_colors_Group_Surface", new[] {
            ThemeSlot.SurfacePrimary,
            ThemeSlot.SurfaceRaised,
            ThemeSlot.SurfaceSunken,
            ThemeSlot.SurfaceTranslucent,
            ThemeSlot.SurfaceTranslucentDark,
            ThemeSlot.SurfaceAccent,
            ThemeSlot.SurfaceInput,
            ThemeSlot.SurfaceDisabled,
            ThemeSlot.SurfaceShadow,
            ThemeSlot.SurfaceGhostHover,
        }),
        ("CL_Playground_colors_Group_Text", new[] {
            ThemeSlot.TextPrimary,
            ThemeSlot.TextSecondary,
            ThemeSlot.TextMuted,
            ThemeSlot.TextOnAccent,
            ThemeSlot.TextOnDanger,
            ThemeSlot.MetadataLabel,
        }),
        ("CL_Playground_colors_Group_Border", new[] {
            ThemeSlot.BorderDefault,
            ThemeSlot.BorderSubtle,
            ThemeSlot.BorderHover,
            ThemeSlot.BorderFocus,
            ThemeSlot.BorderOff,
            ThemeSlot.BorderDanger,
        }),
        ("CL_Playground_colors_Group_Status", new[] {
            ThemeSlot.StatusWarning,
            ThemeSlot.StatusDanger,
            ThemeSlot.StatusSuccess,
            ThemeSlot.AccentMuted,
        }),
        ("CL_Playground_colors_Group_Interaction", new[] {
            ThemeSlot.InteractionHover,
            ThemeSlot.InteractionPress,
            ThemeSlot.OverlayDim,
            ThemeSlot.ScrimDefault,
            ThemeSlot.MapPreviewTint,
        }),
    };

    private static readonly Variant[] AllVariants = {
        Variant.Primary,
        Variant.Secondary,
        Variant.Ghost,
        Variant.Danger,
        Variant.Frosted,
    };

    

    [DocVariant("CL_Playground_colors_Slots", HideCode = true)]
    public static DocSample DocsSlots() {
        return new DocSample(() => SlotSwatchesNode());
    }

    [DocVariant("CL_Playground_colors_Variants", HideCode = true)]
    public static DocSample DocsVariants() {
        return new DocSample(() => VariantPaletteNode());
    }


    [DocVariant("CL_Playground_colors_VariantsAsButtons")]
    public static DocSample DocsVariantsAsButtons() {
        return new DocSample(() =>
            HStack.Create(
                gap: SpacingScale.Sm,
                children: row => {
                    row.AddHug(Button.Create(label: Variant.Primary.Id, onClick: null, variant: Variant.Primary));
                    row.AddHug(Button.Create(label: Variant.Secondary.Id, onClick: null, variant: Variant.Secondary));
                    row.AddHug(Button.Create(label: Variant.Ghost.Id, onClick: null, variant: Variant.Ghost));
                    row.AddHug(Button.Create(label: Variant.Danger.Id, onClick: null, variant: Variant.Danger));
                    row.AddHug(Button.Create(label: Variant.Frosted.Id, onClick: null, variant: Variant.Frosted));
                }
            )
        );
    }

    [DocVariant("CL_Playground_colors_StatusBadges")]
    public static DocSample DocsStatusBadges() {
        return new DocSample(() =>
            HStack.Create(
                gap: SpacingScale.Sm,
                children: row => {
                    row.AddHug(Badge.Create(text: "Neutral", variant: BadgeVariant.Neutral));
                    row.AddHug(Badge.Create(text: "Accent", variant: BadgeVariant.Accent));
                    row.AddHug(Badge.Create(text: "Success", variant: BadgeVariant.Success));
                    row.AddHug(Badge.Create(text: "Warning", variant: BadgeVariant.Warning));
                    row.AddHug(Badge.Create(text: "Danger", variant: BadgeVariant.Danger));
                }
            )
        );
    }

    [DocVariant("CL_Playground_colors_SurfaceLayering")]
    public static DocSample DocsSurfaceLayering() {
        Rem padding = new Rem(0.75f);
        Style innerCardStyle = new Style {
            Padding = EdgeInsets.All(padding),
            Background = BackgroundSpec.Of(ThemeSlot.SurfaceRaised),
            Border = BorderSpec.All(new Rem(1f / 16f), ThemeSlot.BorderSubtle),
            Radius = RadiusSpec.All(RadiusScale.Sm),
        };
        Style sunkenStyle = new Style {
            Padding = EdgeInsets.All(padding),
            Background = BackgroundSpec.Of(ThemeSlot.SurfaceSunken),
            Border = BorderSpec.All(new Rem(1f / 16f), ThemeSlot.BorderSubtle),
            Radius = RadiusSpec.All(RadiusScale.Sm),
        };
        Style outerStyle = new Style {
            Padding = EdgeInsets.All(padding),
            Background = BackgroundSpec.Of(ThemeSlot.SurfacePrimary),
            Border = BorderSpec.All(new Rem(1f / 16f), ThemeSlot.BorderSubtle),
            Radius = RadiusSpec.All(RadiusScale.Sm),
        };

        return new DocSample(() =>
            Box.Create(
                primary => primary.Add(Stack.Create(
                    gap: SpacingScale.Sm,
                    children: pc => {
                        pc.Add(LText.Create(
                            (string)"CL_Playground_colors_Surface_Primary_Label".Translate(),
                            style: new Style {
                                FontFamily = FontRole.BodyBold,
                                FontSize = new Rem(0.8125f),
                                TextColor = ThemeSlot.TextSecondary,
                            }
                        ));
                        pc.Add(Box.Create(
                            raised => raised.Add(Stack.Create(
                                gap: SpacingScale.Sm,
                                children: rc => {
                                    rc.Add(LText.Create(
                                        (string)"CL_Playground_colors_Surface_Raised_Label".Translate(),
                                        style: new Style {
                                            FontFamily = FontRole.BodyBold,
                                            FontSize = new Rem(0.8125f),
                                            TextColor = ThemeSlot.TextSecondary,
                                        }
                                    ));
                                    rc.Add(Box.Create(
                                        sunken => sunken.Add(LText.Create(
                                            (string)"CL_Playground_colors_Surface_Sunken_Label".Translate(),
                                            style: new Style {
                                                FontFamily = FontRole.BodyBold,
                                                FontSize = new Rem(0.8125f),
                                                TextColor = ThemeSlot.TextMuted,
                                            }
                                        )),
                                        style: sunkenStyle
                                    ));
                                }
                            )),
                            style: innerCardStyle
                        ));
                    }
                )),
                style: outerStyle
            )
        );
    }

    [DocVariant("CL_Playground_colors_TextHierarchy")]
    public static DocSample DocsTextHierarchy() {
        return new DocSample(() =>
            Stack.Create(
                gap: SpacingScale.Xs,
                children: col => {
                    col.Add(LText.Create(
                        (string)"CL_Playground_colors_Text_Heading_Sample".Translate(),
                        style: new Style {
                            FontFamily = FontRole.Heading,
                            FontSize = new Rem(1.25f),
                            TextColor = ThemeSlot.TextPrimary,
                        }
                    ));
                    col.Add(LText.Create(
                        (string)"CL_Playground_colors_Text_Body_Sample".Translate(),
                        wrap: true,
                        style: new Style {
                            FontFamily = FontRole.Body,
                            FontSize = new Rem(0.9375f),
                            TextColor = ThemeSlot.TextSecondary,
                        }
                    ));
                    col.Add(LText.Create(
                        (string)"CL_Playground_colors_Text_Caption_Sample".Translate(),
                        wrap: true,
                        style: new Style {
                            FontFamily = FontRole.Body,
                            FontSize = new Rem(0.8125f),
                            TextColor = ThemeSlot.TextMuted,
                        }
                    ));
                    col.Add(LText.Create(
                        (string)"CL_Playground_colors_Text_Metadata_Sample".Translate(),
                        style: new Style {
                            FontFamily = FontRole.BodyBold,
                            FontSize = new Rem(0.75f),
                            TextColor = ThemeSlot.MetadataLabel,
                        }
                    ));
                }
            )
        );
    }

    [DocVariant("CL_Playground_colors_BorderStates")]
    public static DocSample DocsBorderStates() {
        LightweaveNode BorderCell(string labelKey, ThemeSlot slot) =>
            Box.Create(
                cell => cell.Add(LText.Create(
                    (string)labelKey.Translate(),
                    style: new Style {
                        FontFamily = FontRole.Mono,
                        FontSize = new Rem(0.75f),
                        TextColor = ThemeSlot.TextMuted,
                        TextAlign = TextAlign.Center,
                    }
                )),
                style: new Style {
                    Padding = EdgeInsets.All(new Rem(0.625f)),
                    Background = BackgroundSpec.Of(ThemeSlot.SurfaceRaised),
                    Border = BorderSpec.All(new Rem(2f / 16f), slot),
                    Radius = RadiusSpec.All(RadiusScale.Sm),
                }
            );

        return new DocSample(() =>
            HStack.Create(
                gap: SpacingScale.Sm,
                children: row => {
                    row.AddFlex(BorderCell("CL_Playground_colors_Border_Default_Label", ThemeSlot.BorderDefault));
                    row.AddFlex(BorderCell("CL_Playground_colors_Border_Subtle_Label", ThemeSlot.BorderSubtle));
                    row.AddFlex(BorderCell("CL_Playground_colors_Border_Focus_Label", ThemeSlot.BorderFocus));
                    row.AddFlex(BorderCell("CL_Playground_colors_Border_Hover_Label", ThemeSlot.BorderHover));
                    row.AddFlex(BorderCell("CL_Playground_colors_Border_Off_Label", ThemeSlot.BorderOff));
                    row.AddFlex(BorderCell("CL_Playground_colors_Border_Danger_Label", ThemeSlot.BorderDanger));
                }
            )
        );
    }

    

    private static LightweaveNode SlotSwatchesNode() {
        LightweaveNode node = NodeBuilder.New("ColorsSlots");

        float headingHeight = new Rem(1.25f).ToPixels();
        float swatchSize = new Rem(1.75f).ToPixels();
        float rowGap = new Rem(0.25f).ToPixels();
        float groupGap = new Rem(0.5f).ToPixels();
        float labelGap = new Rem(0.5f).ToPixels();
        float columnGap = new Rem(0.75f).ToPixels();
        float minColumnWidth = new Rem(11f).ToPixels();
        float rowHeight = swatchSize;

        LightweaveNode[][] cellsByGroup = new LightweaveNode[SlotGroups.Length][];
        for (int g = 0; g < SlotGroups.Length; g++) {
            ThemeSlot[] slots = SlotGroups[g].Slots;
            LightweaveNode[] cells = new LightweaveNode[slots.Length];
            Rect[] swatchRects = new Rect[slots.Length];
            for (int i = 0; i < slots.Length; i++) {
                int cellIndex = i;
                Rect[] capturedRects = swatchRects;
                ThemeSlot slot = slots[i];
                LightweaveNode cell = BuildSwatchCell(
                    slot,
                    swatchSize,
                    labelGap,
                    rowHeight,
                    r => capturedRects[cellIndex] = r
                );
                cells[i] = Tooltip.Create(
                    cell,
                    () => BuildSlotTooltipContent(slot, RenderContext.Current.Theme.GetColor(slot)),
                    side: TooltipSide.TopEnd,
                    anchor: () => capturedRects[cellIndex],
                    line: (int)slot,
                    file: "ColorsDoc.SlotSwatchesNode"
                );
                node.Children.Add(cells[i]);
            }
            cellsByGroup[g] = cells;
        }

        node.Measure = availableWidth => {
            int columns = ComputeColumns(availableWidth, minColumnWidth, columnGap);
            float total = 0f;
            for (int g = 0; g < SlotGroups.Length; g++) {
                int count = SlotGroups[g].Slots.Length;
                int rowsInGroup = Mathf.CeilToInt((float)count / columns);
                total += headingHeight + rowGap;
                total += rowsInGroup * rowHeight + Mathf.Max(0, rowsInGroup - 1) * rowGap;
                if (g < SlotGroups.Length - 1) {
                    total += groupGap;
                }
            }
            return total;
        };

        node.Paint = (rect, _) => {
            Theme.Theme theme = RenderContext.Current.Theme;
            int headingPx = Mathf.RoundToInt(new Rem(0.9375f).ToFontPx());
            GUIStyle headingStyle = GuiStyleCache.GetOrCreate(theme.GetFont(FontRole.BodyBold), headingPx, FontStyle.Bold);
            headingStyle.alignment = TextAnchor.MiddleLeft;

            int columns = ComputeColumns(rect.width, minColumnWidth, columnGap);
            float columnWidth = (rect.width - columnGap * (columns - 1)) / columns;

            float y = rect.y;
            for (int g = 0; g < SlotGroups.Length; g++) {
                (string headingKey, ThemeSlot[] slots) = SlotGroups[g];
                Rect headingRect = new Rect(rect.x, y, rect.width, headingHeight);
                Color savedColor = GUI.color;
                GUI.color = theme.GetColor(ThemeSlot.TextPrimary);
                GUI.Label(RectSnap.Snap(headingRect), headingKey.Translate(), headingStyle);
                GUI.color = savedColor;
                y += headingHeight + rowGap;

                LightweaveNode[] cells = cellsByGroup[g];
                for (int i = 0; i < cells.Length; i++) {
                    int col = i % columns;
                    int row = i / columns;
                    float cellX = rect.x + col * (columnWidth + columnGap);
                    float cellY = y + row * (rowHeight + rowGap);

                    Rect cellRect = new Rect(cellX, cellY, columnWidth, rowHeight);
                    LightweaveRoot.PaintSubtree(cells[i], cellRect);
                }
                int rowsInGroup = Mathf.CeilToInt((float)cells.Length / columns);
                y += rowsInGroup * rowHeight + Mathf.Max(0, rowsInGroup - 1) * rowGap;
                if (g < SlotGroups.Length - 1) {
                    y += groupGap;
                }
            }
        };

        return node;
    }

    private static LightweaveNode BuildSwatchCell(
        ThemeSlot slot,
        float swatchSize,
        float labelGap,
        float rowHeight,
        Action<Rect>? reportSwatchRect = null
    ) {
        LightweaveNode cell = NodeBuilder.New("ColorsSwatchCell");
        cell.Measure = _ => rowHeight;
        cell.MeasureWidth = () => swatchSize + labelGap + new Rem(6f).ToPixels();
        cell.Paint = (rect, _) => {
            Theme.Theme theme = RenderContext.Current.Theme;
            RadiusSpec radius = RadiusSpec.All(RadiusScale.Sm);
            BorderSpec swatchBorder = BorderSpec.All(new Rem(1f / 16f), ThemeSlot.BorderSubtle);
            Rect swatchRect = new Rect(rect.x, rect.y, swatchSize, swatchSize);
            reportSwatchRect?.Invoke(swatchRect);
            PaintBox.Draw(swatchRect, BackgroundSpec.Of(slot), swatchBorder, radius);

            int labelPx = Mathf.RoundToInt(new Rem(0.75f).ToFontPx());
            GUIStyle labelStyle = GuiStyleCache.GetOrCreate(theme.GetFont(FontRole.Mono), labelPx, FontStyle.Normal);
            labelStyle.alignment = TextAnchor.MiddleLeft;
            labelStyle.wordWrap = false;
            labelStyle.clipping = TextClipping.Clip;

            Rect labelRect = new Rect(rect.x + swatchSize + labelGap, rect.y, Mathf.Max(0f, rect.width - swatchSize - labelGap), rect.height);
            Color savedLabel = GUI.color;
            GUI.color = theme.GetColor(ThemeSlot.TextSecondary);
            GUI.Label(RectSnap.Snap(labelRect), slot.ToString(), labelStyle);
            GUI.color = savedLabel;
        };
        return cell;
    }

    private static int ComputeColumns(float availableWidth, float minColumnWidth, float columnGap) {
        if (availableWidth <= 0f || minColumnWidth <= 0f) {
            return 1;
        }
        int columns = Mathf.FloorToInt((availableWidth + columnGap) / (minColumnWidth + columnGap));
        if (columns < 1) {
            columns = 1;
        }
        if (columns > 4) {
            columns = 4;
        }
        return columns;
    }

    private static LightweaveNode VariantPaletteNode() {
        Rem gap = new Rem(0.5f);
        float rowHeightPx = new Rem(2.25f).ToPixels();
        float labelColumnWidthPx = new Rem(6f).ToPixels();

        InteractionState idle = default(InteractionState);

        string headingLine = $"{"CL_Playground_colors_Variants_Bg".Translate()}  ·  {"CL_Playground_colors_Variants_Fg".Translate()}  ·  {"CL_Playground_colors_Variants_Bd".Translate()}";

        return Stack.Create(
            gap: gap,
            children: outer => {
                outer.Add(LText.Create(
                    headingLine,
                    style: new Style {
                        FontFamily = FontRole.BodyBold,
                        FontSize = new Rem(0.875f),
                        TextColor = ThemeSlot.TextMuted,
                    }
                ));

                for (int i = 0; i < AllVariants.Length; i++) {
                    Variant v = AllVariants[i];
                    ThemeSlot? bgSlot = VariantPalette.Background(v, idle, ghost: false);
                    ThemeSlot fgSlot = VariantPalette.Foreground(v, idle, ghost: false);
                    ThemeSlot? bdSlot = VariantPalette.Border(v, idle, ghost: false);

                    LightweaveNode bgBox = BuildVariantBgCell(bgSlot, fgSlot);
                    if (bgSlot.HasValue) {
                        ThemeSlot capturedBg = bgSlot.Value;
                        bgBox = Tooltip.Create(
                            bgBox,
                            () => BuildSlotTooltipContent(capturedBg, RenderContext.Current.Theme.GetColor(capturedBg)),
                            side: TooltipSide.Top,
                            line: i * 3,
                            file: "ColorsDoc.VariantPaletteNode"
                        );
                    }

                    LightweaveNode fgBox = BuildVariantFgCell(fgSlot);
                    ThemeSlot capturedFg = fgSlot;
                    fgBox = Tooltip.Create(
                        fgBox,
                        () => BuildSlotTooltipContent(capturedFg, RenderContext.Current.Theme.GetColor(capturedFg)),
                        side: TooltipSide.Top,
                        line: i * 3 + 1,
                        file: "ColorsDoc.VariantPaletteNode"
                    );

                    LightweaveNode bdBox = BuildVariantBdCell(bdSlot);
                    if (bdSlot.HasValue) {
                        ThemeSlot capturedBd = bdSlot.Value;
                        bdBox = Tooltip.Create(
                            bdBox,
                            () => BuildSlotTooltipContent(capturedBd, RenderContext.Current.Theme.GetColor(capturedBd)),
                            side: TooltipSide.Top,
                            line: i * 3 + 2,
                            file: "ColorsDoc.VariantPaletteNode"
                        );
                    }

                    Variant capturedVariant = v;
                    outer.Add(HStack.Create(
                        gap: gap,
                        children: row => {
                            row.Add(LText.Create(
                                capturedVariant.Id,
                                style: new Style {
                                    FontFamily = FontRole.Mono,
                                    FontSize = new Rem(0.8125f),
                                    TextColor = ThemeSlot.TextPrimary,
                                }
                            ), labelColumnWidthPx);
                            row.AddFlex(bgBox);
                            row.AddFlex(fgBox);
                            row.AddFlex(bdBox);
                        }
                    ), rowHeightPx);
                }
            }
        );
    }

    private static LightweaveNode BuildVariantBgCell(ThemeSlot? bgSlot, ThemeSlot fgSlot) {
        Style cellStyle = bgSlot.HasValue
            ? new Style {
                Background = BackgroundSpec.Of(bgSlot.Value),
                Radius = RadiusSpec.All(RadiusScale.Sm),
                Padding = EdgeInsets.All(new Rem(0.5f)),
            }
            : new Style {
                Border = BorderSpec.All(new Rem(1f / 16f), ThemeSlot.BorderSubtle),
                Radius = RadiusSpec.All(RadiusScale.Sm),
                Padding = EdgeInsets.All(new Rem(0.5f)),
            };

        return Box.Create(
            children: c => c.Add(LText.Create(
                bgSlot.HasValue ? bgSlot.Value.ToString() : "none",
                style: new Style {
                    FontFamily = FontRole.BodyBold,
                    FontSize = new Rem(0.8125f),
                    TextColor = bgSlot.HasValue ? fgSlot : ThemeSlot.TextMuted,
                    TextAlign = TextAlign.Center,
                }
            )),
            style: cellStyle
        );
    }

    private static LightweaveNode BuildVariantFgCell(ThemeSlot fgSlot) {
        Style cellStyle = new Style {
            Background = BackgroundSpec.Of(ThemeSlot.SurfaceRaised),
            Border = BorderSpec.All(new Rem(1f / 16f), ThemeSlot.BorderSubtle),
            Radius = RadiusSpec.All(RadiusScale.Sm),
            Padding = EdgeInsets.All(new Rem(0.5f)),
        };

        return Box.Create(
            children: c => c.Add(LText.Create(
                fgSlot.ToString(),
                style: new Style {
                    FontFamily = FontRole.BodyBold,
                    FontSize = new Rem(0.8125f),
                    TextColor = fgSlot,
                    TextAlign = TextAlign.Center,
                }
            )),
            style: cellStyle
        );
    }

    private static LightweaveNode BuildVariantBdCell(ThemeSlot? bdSlot) {
        Style cellStyle = bdSlot.HasValue
            ? new Style {
                Background = BackgroundSpec.Of(ThemeSlot.SurfaceRaised),
                Border = BorderSpec.All(new Rem(2f / 16f), bdSlot.Value),
                Radius = RadiusSpec.All(RadiusScale.Sm),
                Padding = EdgeInsets.All(new Rem(0.5f)),
            }
            : new Style {
                Background = BackgroundSpec.Of(ThemeSlot.SurfaceRaised),
                Radius = RadiusSpec.All(RadiusScale.Sm),
                Padding = EdgeInsets.All(new Rem(0.5f)),
            };

        return Box.Create(
            children: c => c.Add(LText.Create(
                bdSlot.HasValue ? bdSlot.Value.ToString() : "none",
                style: new Style {
                    FontFamily = FontRole.BodyBold,
                    FontSize = new Rem(0.8125f),
                    TextColor = bdSlot.HasValue ? ThemeSlot.TextSecondary : ThemeSlot.TextMuted,
                    TextAlign = TextAlign.Center,
                }
            )),
            style: cellStyle
        );
    }

    private static LightweaveNode BuildSlotTooltipContent(ThemeSlot slot, Color c) {
        int r = Mathf.RoundToInt(c.r * 255f);
        int g = Mathf.RoundToInt(c.g * 255f);
        int b = Mathf.RoundToInt(c.b * 255f);
        string hex = $"#{r:X2}{g:X2}{b:X2}";
        string rgba = $"rgba({r}, {g}, {b}, {c.a:0.00})";
        return Stack.Create(
            gap: new Rem(0.375f),
            children: outer => {
                outer.Add(LText.Create(
                    slot.ToString(),
                    style: new Style {
                        FontFamily = FontRole.BodyBold,
                        FontSize = new Rem(0.9375f),
                        TextColor = ThemeSlot.TextPrimary,
                    }
                ));
                outer.Add(Stack.Create(
                    gap: new Rem(0.125f),
                    children: inner => {
                        inner.Add(LText.Create(
                            hex,
                            style: new Style {
                                FontFamily = FontRole.Mono,
                                FontSize = new Rem(0.8125f),
                                TextColor = ThemeSlot.TextSecondary,
                            }
                        ));
                        inner.Add(LText.Create(
                            rgba,
                            style: new Style {
                                FontFamily = FontRole.Mono,
                                FontSize = new Rem(0.8125f),
                                TextColor = ThemeSlot.TextSecondary,
                            }
                        ));
                    }
                ));
            }
        );
    }

    

    

    

    

    

    
}
