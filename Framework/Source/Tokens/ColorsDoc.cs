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
using Cosmere.Lightweave.Surfaces;
using Cosmere.Lightweave.Data;
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
            ThemeSlot.ExpansionBarSurface,
            ThemeSlot.ExpansionPillSurface,
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
            ThemeSlot.BorderFaint,
            ThemeSlot.BorderHover,
            ThemeSlot.BorderFocus,
            ThemeSlot.BorderOff,
            ThemeSlot.BorderDanger,
        }),
        ("CL_Playground_colors_Group_Status", new[] {
            ThemeSlot.StatusWarning,
            ThemeSlot.StatusDanger,
            ThemeSlot.StatusSuccess,
            ThemeSlot.AccentSoft,
        }),
        ("CL_Playground_colors_Group_Interaction", new[] {
            ThemeSlot.HoverTint,
            ThemeSlot.ActiveTint,
            ThemeSlot.OverlayDim,
            ThemeSlot.MenuVignette,
            ThemeSlot.ScrimDefault,
            ThemeSlot.MapPreviewTint,
        }),
        ("CL_Playground_colors_Group_Glass", new[] {
            ThemeSlot.Glass1,
            ThemeSlot.Glass2,
            ThemeSlot.Glass3,
            ThemeSlot.WindowSurface,
            ThemeSlot.GlassFrost,
        }),
        ("CL_Playground_colors_Group_GradientTints", new[] {
            ThemeSlot.WindowHeaderTint,
            ThemeSlot.WindowFooterTint,
            ThemeSlot.ButtonPrimaryFill,
            ThemeSlot.ButtonPrimaryFillHover,
            ThemeSlot.AccentGlow,
            ThemeSlot.ShelfTint,
        }),
        ("CL_Playground_colors_Group_Shadows", new[] {
            ThemeSlot.ShadowModal,
            ThemeSlot.ShadowCard,
            ThemeSlot.ShadowPopover,
            ThemeSlot.ShadowTooltip,
            ThemeSlot.ShadowToast,
            ThemeSlot.ShadowRim,
            ThemeSlot.ShadowInsetTop,
            ThemeSlot.ShadowInsetTopStrong,
        }),
        ("CL_Playground_colors_Group_TextShadows", new[] {
            ThemeSlot.TextShadowEmboss,
            ThemeSlot.TextShadowDeep,
            ThemeSlot.TextShadowDisplay,
        }),
        ("CL_Playground_colors_Group_Grain", new[] {
            ThemeSlot.GrainTint,
        }),
    };

    private static readonly string[] SlotNames = BuildSlotNameCache();

    private static string[] BuildSlotNameCache() {
        ThemeSlot[] values = (ThemeSlot[])System.Enum.GetValues(typeof(ThemeSlot));
        int max = 0;
        for (int i = 0; i < values.Length; i++) {
            int v = (int)values[i];
            if (v > max) {
                max = v;
            }
        }
        string[] names = new string[max + 1];
        for (int i = 0; i < values.Length; i++) {
            names[(int)values[i]] = values[i].ToString();
        }
        return names;
    }

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
                    row.AddHug(Chip.Create("Neutral", state: false, variant: ChipVariant.None, showDot: false));
                    row.AddHug(Chip.Create("Accent", state: true, variant: ChipVariant.None, showDot: false));
                    row.AddHug(Chip.Create("Success", state: true, variant: ChipVariant.Info, showDot: false));
                    row.AddHug(Chip.Create("Warning", state: true, variant: ChipVariant.Warn, showDot: false));
                    row.AddHug(Chip.Create("Danger", state: true, variant: ChipVariant.Error, showDot: false));
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
                    row.AddFlex(BorderCell("CL_Playground_colors_Border_Faint_Label", ThemeSlot.BorderFaint));
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

        float headingHeight = new Rem(1.5f).ToPixels();
        float headingGap = new Rem(0.5f).ToPixels();
        float groupGap = new Rem(1.5f).ToPixels();
        float cardGap = new Rem(0.75f).ToPixels();
        float chipHeight = new Rem(5.5f).ToPixels();
        float metaPadX = new Rem(0.875f).ToPixels();
        float metaPadY = new Rem(0.625f).ToPixels();
        float metaLineGap = new Rem(0.125f).ToPixels();
        float minColumnWidth = new Rem(10.5f).ToPixels();

        int namePx = Mathf.RoundToInt(new Rem(0.8125f).ToFontPx());
        int monoPx = Mathf.RoundToInt(new Rem(0.6875f).ToFontPx());
        float nameLine = Mathf.Round(namePx * 1.4f);
        float monoLine = Mathf.Round(monoPx * 1.45f);
        float cardHeight = chipHeight + metaPadY * 2f + nameLine + monoLine + monoLine + metaLineGap * 2f;

        node.Measure = availableWidth => {
            int columns = ComputeColumns(availableWidth, minColumnWidth, cardGap);
            float total = 0f;
            for (int g = 0; g < SlotGroups.Length; g++) {
                int count = SlotGroups[g].Slots.Length;
                int rowsInGroup = Mathf.CeilToInt((float)count / columns);
                total += headingHeight + headingGap;
                total += rowsInGroup * cardHeight + Mathf.Max(0, rowsInGroup - 1) * cardGap;
                if (g < SlotGroups.Length - 1) {
                    total += groupGap;
                }
            }
            return total;
        };

        node.Paint = (rect, _) => {
            Theme.Theme theme = RenderContext.Current.Theme;

            int headingPx = Mathf.RoundToInt(new Rem(1.0f).ToFontPx());
            GUIStyle headingStyle = GuiStyleCache.GetOrCreate(theme.GetFont(FontRole.BodyBold), headingPx, FontStyle.Normal);
            headingStyle.alignment = TextAnchor.LowerLeft;

            GUIStyle nameStyle = GuiStyleCache.GetOrCreate(theme.GetFont(FontRole.Body), namePx, FontStyle.Normal);
            nameStyle.alignment = TextAnchor.MiddleLeft;
            nameStyle.wordWrap = false;
            nameStyle.clipping = TextClipping.Clip;

            GUIStyle tokenStyle = GuiStyleCache.GetOrCreate(theme.GetFont(FontRole.Mono), monoPx, FontStyle.Normal);
            tokenStyle.alignment = TextAnchor.MiddleLeft;
            tokenStyle.wordWrap = false;
            tokenStyle.clipping = TextClipping.Clip;

            GUIStyle valStyle = GuiStyleCache.GetOrCreate(theme.GetFont(FontRole.Mono), monoPx, FontStyle.Normal);
            valStyle.alignment = TextAnchor.MiddleLeft;
            valStyle.wordWrap = false;
            valStyle.clipping = TextClipping.Clip;

            Color textPrimary = theme.GetColor(ThemeSlot.TextPrimary);
            Color textMuted = theme.GetColor(ThemeSlot.TextMuted);
            Color accentColor = theme.GetColor(ThemeSlot.SurfaceAccent);
            Color checkerDark = theme.GetColor(ThemeSlot.SurfaceSunken);
            Color checkerLight = theme.GetColor(ThemeSlot.SurfaceRaised);
            Color dividerColor = theme.GetColor(ThemeSlot.BorderSubtle);
            const int checkerCell = 12;
            Texture2D checkerTex = PatternTextureCache.Checker(checkerLight, checkerDark, checkerCell);

            BackgroundSpec cardBg = BackgroundSpec.Of(ThemeSlot.Glass1);
            BorderSpec cardBorder = BorderSpec.All(new Rem(1f / 16f), ThemeSlot.BorderSubtle);
            RadiusSpec cardRadius = RadiusSpec.All(RadiusScale.Sm);

            int columns = ComputeColumns(rect.width, minColumnWidth, cardGap);
            float columnWidth = (rect.width - cardGap * (columns - 1)) / columns;

            float y = rect.y;
            Color savedColor = GUI.color;

            for (int g = 0; g < SlotGroups.Length; g++) {
                (string headingKey, ThemeSlot[] slots) = SlotGroups[g];

                Rect headingRect = new Rect(rect.x, y, rect.width, headingHeight);
                GUI.color = textPrimary;
                GUI.Label(RectSnap.Snap(headingRect), headingKey.Translate(), headingStyle);
                GUI.color = savedColor;
                y += headingHeight + headingGap;

                for (int i = 0; i < slots.Length; i++) {
                    ThemeSlot slot = slots[i];
                    int col = i % columns;
                    int row = i / columns;
                    float cardX = rect.x + col * (columnWidth + cardGap);
                    float cardY = y + row * (cardHeight + cardGap);
                    Rect cardRect = new Rect(cardX, cardY, columnWidth, cardHeight);

                    PaintBox.Draw(cardRect, cardBg, cardBorder, cardRadius);

                    Rect chipRect = new Rect(cardRect.x + 1f, cardRect.y + 1f, cardRect.width - 2f, chipHeight);
                    float tileSize = checkerCell * 2f;
                    GUI.DrawTextureWithTexCoords(
                        chipRect,
                        checkerTex,
                        new Rect(0f, 0f, chipRect.width / tileSize, chipRect.height / tileSize)
                    );
                    Color slotColor = theme.GetColor(slot);
                    Widgets.DrawBoxSolid(chipRect, slotColor);

                    Rect divider = new Rect(cardRect.x + 1f, chipRect.yMax, cardRect.width - 2f, 1f);
                    Widgets.DrawBoxSolid(divider, dividerColor);

                    int r8 = Mathf.Clamp(Mathf.RoundToInt(slotColor.r * 255f), 0, 255);
                    int g8 = Mathf.Clamp(Mathf.RoundToInt(slotColor.g * 255f), 0, 255);
                    int b8 = Mathf.Clamp(Mathf.RoundToInt(slotColor.b * 255f), 0, 255);
                    string slotName = SlotNames[(int)slot];
                    string hexLine = $"#{r8:X2}{g8:X2}{b8:X2}";
                    string rgbaLine = $"rgba({r8}, {g8}, {b8}, {slotColor.a:0.00})";

                    float metaTop = chipRect.yMax + 1f + metaPadY;
                    float metaLeft = cardRect.x + metaPadX;
                    float metaWidth = Mathf.Max(0f, cardRect.width - metaPadX * 2f);

                    Rect nameRect = new Rect(metaLeft, metaTop, metaWidth, nameLine);
                    GUI.color = textPrimary;
                    GUI.Label(RectSnap.Snap(nameRect), slotName, nameStyle);

                    Rect hexRect = new Rect(metaLeft, nameRect.yMax + metaLineGap, metaWidth, monoLine);
                    GUI.color = accentColor;
                    GUI.Label(RectSnap.Snap(hexRect), hexLine, tokenStyle);

                    Rect rgbaRect = new Rect(metaLeft, hexRect.yMax + metaLineGap, metaWidth, monoLine);
                    GUI.color = textMuted;
                    GUI.Label(RectSnap.Snap(rgbaRect), rgbaLine, valStyle);

                    GUI.color = savedColor;
                }

                int rowsInGroup = Mathf.CeilToInt((float)slots.Length / columns);
                y += rowsInGroup * cardHeight + Mathf.Max(0, rowsInGroup - 1) * cardGap;
                if (g < SlotGroups.Length - 1) {
                    y += groupGap;
                }
            }
        };

        return node;
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
