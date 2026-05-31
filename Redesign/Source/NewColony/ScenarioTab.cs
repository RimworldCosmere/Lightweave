using System.Collections.Generic;
using Cosmere.Lightweave.Data;
using Cosmere.Lightweave.Hooks;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Surfaces;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using RimWorld;
using Verse;
using Chip = Cosmere.Lightweave.Data.Chip;
using Display = Cosmere.Lightweave.Typography.Display;
using Divider = Cosmere.Lightweave.Data.Divider;
using Eyebrow = Cosmere.Lightweave.Typography.Eyebrow;
using Text = Cosmere.Lightweave.Typography.Typography.Text;

namespace Cosmere.Lightweave.Redesign.NewColony;

public static class ScenarioTab {
    public static LightweaveNode Build(Hooks.Hooks.StateHandle<string?> scenarioName) {
        List<Scenario> official = NewColonyData.OfficialScenarios();
        List<Scenario> modded = NewColonyData.ModdedScenarios();
        List<Scenario> custom = NewColonyData.CustomScenarios();
        List<Scenario> workshop = NewColonyData.WorkshopScenarios();
        Scenario? selected = NewColonyData.FindScenario(scenarioName.Value);

        return HStack.Create(SpacingScale.Lg, h => {
            h.Add(ScrollArea.Create(BuildList(official, modded, custom, workshop, scenarioName)), new Rem(22f).ToPixels());
            h.AddFlex(ScrollArea.Create(BuildDetail(selected)));
        }, style: new Style {
            Width = Length.Stretch,
            Height = Length.Stretch,
        });
    }

    private static LightweaveNode BuildList(
        List<Scenario> official,
        List<Scenario> modded,
        List<Scenario> custom,
        List<Scenario> workshop,
        Hooks.Hooks.StateHandle<string?> scenarioName
    ) {
        return Stack.Create(SpacingScale.Sm, s => {
            s.Add(NewColonyControls.SectionLabel("CL_NewColony_Scenario_Official".Translate()));
            foreach (Scenario scen in official) {
                s.Add(BuildRow(scen, scenarioName));
            }
            if (modded.Count > 0) {
                s.Add(NewColonyControls.SectionLabel("CL_NewColony_Scenario_Modded".Translate()));
                foreach (Scenario scen in modded) {
                    s.Add(BuildRow(scen, scenarioName));
                }
            }
            if (custom.Count > 0) {
                s.Add(NewColonyControls.SectionLabel("CL_NewColony_Scenario_Custom".Translate()));
                foreach (Scenario scen in custom) {
                    s.Add(BuildRow(scen, scenarioName));
                }
            }
            if (workshop.Count > 0) {
                s.Add(NewColonyControls.SectionLabel("CL_NewColony_Scenario_Workshop".Translate()));
                foreach (Scenario scen in workshop) {
                    s.Add(BuildRow(scen, scenarioName));
                }
            }
        });
    }

    private static LightweaveNode BuildRow(Scenario scen, Hooks.Hooks.StateHandle<string?> scenarioName) {
        string name = scen.name;
        bool isSelected = name == scenarioName.Value;
        int colonists = ScenarioInfo.StartingColonists(scen);
        (string Name, ThemeSlot Slot)? dlc = NewColonyData.ScenarioDlc(scen);

        LightweaveNode child = Stack.Create(SpacingScale.Xxs, c => {
            c.Add(HStack.Create(SpacingScale.Xs, h => {
                h.AddFlex(Text.Create(name, truncate: true, style: new Style {
                    FontFamily = FontRole.Display,
                    FontSize = new Rem(1.2f),
                    LetterSpacing = Tracking.Of(0.03f),
                    TextColor = ThemeSlot.TextPrimary,
                }));
                if (dlc.HasValue) {
                    h.AddHug(Chip.Create(dlc.Value.Name, accent: dlc.Value.Slot, state: true, upper: false, bold: false, showDot: false));
                }
            }));
            if (colonists > 0) {
                c.Add(Eyebrow.Create("CL_NewColony_Scenario_Colonists".Translate(colonists.Named("COUNT"))));
            }
        });

        return SelectableSurface.Create(
            child: child,
            selected: isSelected,
            variant: SelectableSurfaceVariant.ListRow,
            trailingCaret: false,
            onSelect: () => scenarioName.Set(name),
            style: new Style { Width = Length.Stretch }
        );
    }

    private static LightweaveNode BuildDetail(Scenario? selected) {
        if (selected == null) {
            return Stack.Create(SpacingScale.None, s => s.Add(Text.Create(
                "CL_NewColony_Scenario_Empty".Translate(),
                style: new Style { TextColor = ThemeSlot.TextMuted }
            )));
        }

        List<(ThingDef def, int count)> loadout = ScenarioInfo.LoadoutEntries(selected);

        return Stack.Create(SpacingScale.Md, s => {
            s.Add(Display.Create(selected.name, level: 2));
            if (!selected.summary.NullOrEmpty()) {
                s.Add(Text.Create(selected.summary, wrap: true, style: new Style {
                    FontFamily = FontRole.Display,
                    FontWeight = UnityEngine.FontStyle.Italic,
                    FontSize = new Rem(1.25f),
                    LetterSpacing = Tracking.Of(0.02f),
                    TextColor = ThemeSlot.TextSecondary,
                }));
            }
            if (!selected.description.NullOrEmpty()) {
                s.Add(Text.Create(selected.description, wrap: true, style: new Style {
                    FontSize = new Rem(0.94f),
                    TextColor = ThemeSlot.TextSecondary,
                }));
            }
            LightweaveNode? statGrid = BuildStatGrid(selected);
            if (statGrid != null) {
                s.Add(Divider.Horizontal());
                s.Add(statGrid);
            }
            s.Add(Divider.Horizontal());
            s.Add(NewColonyControls.SectionLabel("CL_NewColony_Scenario_Loadout".Translate()));
            s.Add(Wrap.Create(SpacingScale.Xs, children: w => {
                if (loadout.Count == 0) {
                    w.Add(Chip.Create("CL_NewColony_Scenario_LoadoutEmpty".Translate(), ChipVariant.Error, size: ChipSize.Large, showDot: false, upper: false, bold: false));
                }
                else {
                    foreach ((ThingDef def, int count) in loadout) {
                        string label = count > 1
                            ? def.label.CapitalizeFirst() + " · " + count
                            : def.label.CapitalizeFirst();
                        LightweaveNode? icon = def.uiIcon != null
                            ? Image.Create(def.uiIcon, fit: ImageFit.Contain, tint: def.uiIconColor)
                            : null;
                        w.Add(Chip.Create(label, icon: icon, size: ChipSize.Large, showDot: false, upper: false, bold: false));
                    }
                }
            }, flow: true));
        });
    }

    private static LightweaveNode? BuildStatGrid(Scenario scen) {
        int colonists = ScenarioInfo.StartingColonists(scen);
        int items = ScenarioInfo.StartingItemsCount(scen);
        int animals = ScenarioInfo.StartingAnimalCount(scen);
        string? arrival = ScenarioInfo.ArrivalMethodLabel(scen);
        string? planetLayer = ScenarioInfo.PlanetLayerLabel(scen);

        List<(string Label, string Value)> cells = [];
        if (colonists > 0) {
            cells.Add(((string)"CL_NewColony_Scenario_StartingColonists".Translate(), colonists.ToString()));
        }
        if (items > 0) {
            cells.Add(((string)"CL_NewColony_Scenario_StartingItems".Translate(), items.ToString()));
        }
        if (animals > 0) {
            cells.Add(((string)"CL_NewColony_Scenario_StartingAnimals".Translate(), animals.ToString()));
        }
        if (!arrival.NullOrEmpty()) {
            cells.Add(((string)"CL_NewColony_Scenario_Arrival".Translate(), arrival!));
        }
        if (!planetLayer.NullOrEmpty()) {
            cells.Add(((string)"CL_NewColony_Scenario_PlanetLayer".Translate(), planetLayer!));
        }
        if (cells.Count == 0) {
            return null;
        }

        return HStack.Create(SpacingScale.Lg, w => {
            foreach ((string label, string value) in cells) {
                w.AddFlex(StatCell(label, value));
            }
        }, style: new Style { Width = Length.Stretch });
    }

    private static LightweaveNode StatCell(string label, string value) {
        return Stack.Create(SpacingScale.Xs, c => {
            c.Add(Eyebrow.Create(label, style: new Style {
                FontFamily = FontRole.Mono,
                FontSize = new Rem(10f / 16f),
                LetterSpacing = Tracking.Of(0.2f),
            }));
            c.Add(Text.Create(value, style: new Style {
                FontFamily = FontRole.Display,
                FontSize = new Rem(1.6f),
                LetterSpacing = Tracking.Of(0.02f),
                TextColor = ThemeSlot.TextPrimary,
            }));
        }, style: new Style { Width = Length.Stretch });
    }
}
