using Cosmere.Lightweave.Hooks;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Navigation;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Surfaces;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using UnityEngine;
using Verse;
using Button = Cosmere.Lightweave.Input.Button;
using Text = Cosmere.Lightweave.Typography.Typography.Text;

namespace Cosmere.Lightweave.Redesign.NewColony;

public static class WorldTab {
    public static LightweaveNode Build(Hooks.Hooks.StateHandle<WorldParams> world) {
        return ScrollArea.Create(Stack.Create(SpacingScale.Lg, s => {
            s.Add(BuildSeed(world));
            s.Add(BuildCoverage(world));
            s.Add(BuildRainfall(world));
            s.Add(BuildTemperature(world));
            s.Add(BuildPopulation(world));
            s.Add(BuildMapSize(world));
        }, style: new Style { Width = Length.Stretch }), style: new Style { Width = Length.Stretch });
    }

    private static LightweaveNode BuildSeed(Hooks.Hooks.StateHandle<WorldParams> world) {
        string seed = string.IsNullOrEmpty(world.Value.Seed)
            ? "CL_NewColony_World_Seed_Random".Translate()
            : world.Value.Seed;

        return Section("CL_NewColony_World_Seed".Translate(), HStack.Create(SpacingScale.Sm, h => {
            h.AddFlex(Text.Create(seed, style: new Style {
                FontFamily = FontRole.Mono,
                TextColor = ThemeSlot.TextPrimary,
            }));
            h.AddHug(Button.Create(
                "CL_NewColony_World_Seed_Randomize".Translate(),
                () => {
                    WorldParams next = world.Value;
                    next.Seed = GenText.RandomSeedString();
                    world.Set(next);
                },
                ghost: true
            ));
        }, style: new Style { Width = Length.Stretch }));
    }

    private static LightweaveNode BuildCoverage(Hooks.Hooks.StateHandle<WorldParams> world) {
        return Section("CL_NewColony_World_Coverage".Translate(), Segmented.Create(
            world.Value.Coverage,
            NewColonyFormat.Coverages,
            NewColonyFormat.CoverageLabel,
            v => {
                WorldParams next = world.Value;
                next.Coverage = v;
                world.Set(next);
            }
        ));
    }

    private static LightweaveNode BuildRainfall(Hooks.Hooks.StateHandle<WorldParams> world) {
        return NewColonyControls.LabeledSlider(
            "CL_NewColony_World_Rainfall".Translate(),
            NewColonyFormat.RainfallLabel(world.Value.Rainfall),
            world.Value.Rainfall,
            v => {
                WorldParams next = world.Value;
                next.Rainfall = Mathf.RoundToInt(v);
                world.Set(next);
            },
            0f, 6f, 1f
        );
    }

    private static LightweaveNode BuildTemperature(Hooks.Hooks.StateHandle<WorldParams> world) {
        return NewColonyControls.LabeledSlider(
            "CL_NewColony_World_Temperature".Translate(),
            NewColonyFormat.TemperatureLabel(world.Value.Temperature),
            world.Value.Temperature,
            v => {
                WorldParams next = world.Value;
                next.Temperature = Mathf.RoundToInt(v);
                world.Set(next);
            },
            0f, 6f, 1f
        );
    }

    private static LightweaveNode BuildPopulation(Hooks.Hooks.StateHandle<WorldParams> world) {
        return Section("CL_NewColony_World_Population".Translate(), Segmented.Create(
            world.Value.Population,
            NewColonyFormat.PopulationLevels,
            NewColonyFormat.PopulationLabel,
            v => {
                WorldParams next = world.Value;
                next.Population = v;
                world.Set(next);
            }
        ));
    }

    private static LightweaveNode BuildMapSize(Hooks.Hooks.StateHandle<WorldParams> world) {
        return Section("CL_NewColony_World_MapSize".Translate(), Segmented.Create(
            world.Value.MapSize,
            NewColonyFormat.MapSizes,
            NewColonyFormat.MapSizeLabel,
            v => {
                WorldParams next = world.Value;
                next.MapSize = v;
                world.Set(next);
            },
            countFn: size => size + "²"
        ));
    }

    private static LightweaveNode Section(string label, LightweaveNode body) {
        return Stack.Create(SpacingScale.Sm, s => {
            s.Add(NewColonyControls.SectionLabel(label));
            s.Add(body);
        });
    }
}
