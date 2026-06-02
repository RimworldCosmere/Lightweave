using System.Collections.Generic;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using RimWorld;
using UnityEngine;
using Verse;
using Icon = Cosmere.Lightweave.Typography.Typography.Icon;
using Text = Cosmere.Lightweave.Typography.Typography.Text;

namespace Cosmere.Lightweave.Redesign.NewColony;

// Pairwise goodwill grid for every faction on the generated world. Read-only: relations are set by
// world gen, so this surface only visualizes them. No mock exists for this dialog yet; the layout is
// a native compact matrix (icon column header, icon+name leading column, signed goodwill cells).
public class Dialog_FactionRelations : LightweaveWindow {
    private const float CellWidth = 56f;
    private const float CellHeight = 34f;
    private const float LabelWidth = 224f;
    // Estimated height of the title-row header (top pad + Display level 2 + bottom pad + divider).
    // Biased slightly high so the matrix never clips; a few px of slack reads as card background.
    private const float HeaderHeight = 84f;
    // Card border (1px) + card padding (1px) on each side; see LightweaveWindow.BuildRoot.
    private const float CardChrome = 4f;
    // Slim scroll-rail gutter (SlimRailWidth 4 + RailLeftGap 0.5rem) reserved only when the matrix
    // is taller than the viewport, so a scrolling matrix never also triggers a horizontal scrollbar.
    private const float ScrollGutter = 12f;

    private readonly List<Faction> factions;

    public Dialog_FactionRelations() {
        factions = Collect();
        absorbInputAroundWindow = true;
    }

    // Body wraps the matrix in a ScrollArea, then a Stack padded by Lg on every side. The pure sizing
    // math lives in FactionRelationsLayout so the window-size contract is unit testable.
    private float BodyPadding => SpacingScale.Lg.ToPixels() * 2f;

    private FactionRelationsSize Size => FactionRelationsLayout.Measure(
        factionCount: factions.Count,
        labelWidth: LabelWidth,
        cellWidth: CellWidth,
        cellHeight: CellHeight,
        headerHeight: HeaderHeight,
        bodyPadding: BodyPadding,
        cardChrome: CardChrome,
        scrollGutter: ScrollGutter,
        screenWidth: Verse.UI.screenWidth,
        screenHeight: Verse.UI.screenHeight);

    protected override float? CardWidth => Size.Width;
    protected override float? CardHeight => Size.Height;
    protected override BackgroundSpec? CardBackground => BackgroundSpec.Of(ThemeSlot.WindowSurface);

    protected override LightweaveNode Header() {
        return WindowHeader.Create(
            title: (string)"CL_NewColony_World_Factions_Relations_Title".Translate(),
            onClose: () => Close(),
            draggable: true);
    }

    protected override LightweaveNode Body() {
        return ScrollArea.Create(
            Stack.Create(SpacingScale.None, s => s.Add(BuildMatrix()), style: new Style {
                Width = Length.Stretch,
                Padding = EdgeInsets.All(SpacingScale.Lg),
            }),
            style: new Style { Width = Length.Stretch, Height = Length.Stretch });
    }

    private LightweaveNode BuildMatrix() {
        // Hug to the summed column width so each row's bottom divider spans only the data columns
        // (no overrun into empty space to the right of the last faction column).
        return Stack.Create(SpacingScale.None, rows => {
            rows.Add(BuildHeaderRow());
            for (int r = 0; r < factions.Count; r++) {
                rows.Add(BuildDataRow(factions[r]));
            }
        });
    }

    private LightweaveNode BuildHeaderRow() {
        return HStack.Create(SpacingScale.None, h => {
            // Zero-width, fixed-height spacer pins the row to CellHeight: HStack derives its height
            // from the tallest child's PreferredHeight and ignores Style.Height, so without this the
            // row would collapse to icon height (~20px) and the matrix would render cramped.
            h.AddHug(Spacer.Fixed(new Rem(CellHeight / 16f)));
            h.Add(Spacer.Fixed(SpacingScale.None), LabelWidth);
            for (int c = 0; c < factions.Count; c++) {
                h.Add(HeaderCell(factions[c]), CellWidth);
            }
        }, align: FlexAlign.Center, style: new Style {
            Border = new BorderSpec(Bottom: new Rem(1f / 16f), Color: ThemeSlot.BorderSubtle),
        });
    }

    private LightweaveNode HeaderCell(Faction faction) {
        return Icon.Create(
            faction.def.FactionIcon,
            size: new Rem(1.25f),
            style: new Style { TextColor = faction.Color });
    }

    private LightweaveNode BuildDataRow(Faction row) {
        return HStack.Create(SpacingScale.None, h => {
            h.AddHug(Spacer.Fixed(new Rem(CellHeight / 16f)));
            h.Add(RowLabel(row), LabelWidth);
            for (int c = 0; c < factions.Count; c++) {
                Faction col = factions[c];
                h.Add(row == col ? SelfCell() : GoodwillCell(row.GoodwillWith(col)), CellWidth);
            }
        }, align: FlexAlign.Center, style: new Style {
            Border = new BorderSpec(Bottom: new Rem(1f / 16f), Color: ThemeSlot.BorderSubtle),
        });
    }

    private LightweaveNode RowLabel(Faction faction) {
        return HStack.Create(SpacingScale.Xs, h => {
            h.AddHug(Icon.Create(
                faction.def.FactionIcon,
                size: new Rem(1.25f),
                style: new Style { TextColor = faction.Color }));
            h.AddFlex(Text.Create(faction.Name, style: new Style {
                FontSize = new Rem(12f / 16f),
                TextColor = ThemeSlot.TextSecondary,
            }));
        }, style: new Style { Width = Length.Stretch });
    }

    private LightweaveNode SelfCell() {
        return Text.Create("—", style: new Style {
            Width = Length.Stretch,
            FontFamily = FontRole.Mono,
            FontSize = new Rem(11f / 16f),
            TextColor = ThemeSlot.TextMuted,
            TextAlign = TextAlign.Center,
        });
    }

    private LightweaveNode GoodwillCell(int goodwill) {
        ThemeSlot color = goodwill > 0
            ? ThemeSlot.StatusSuccess
            : goodwill < 0 ? ThemeSlot.StatusDanger : ThemeSlot.TextMuted;
        return Text.Create(goodwill > 0 ? "+" + goodwill : goodwill.ToString(), style: new Style {
            Width = Length.Stretch,
            FontFamily = FontRole.Mono,
            FontSize = new Rem(11f / 16f),
            TextColor = color,
            TextAlign = TextAlign.Center,
        });
    }

    private static List<Faction> Collect() {
        List<Faction> result = [];
        Faction? player = Find.FactionManager.OfPlayer;
        if (player != null) {
            result.Add(player);
        }
        foreach (Faction faction in Find.FactionManager.AllFactionsInViewOrder) {
            if (faction.IsPlayer || faction.temporary) {
                continue;
            }
            result.Add(faction);
        }
        return result;
    }
}
