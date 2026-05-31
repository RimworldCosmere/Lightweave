using Cosmere.Lightweave.Data;
using Cosmere.Lightweave.Hooks;
using Cosmere.Lightweave.Icons;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Navigation;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Surfaces;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Button = Cosmere.Lightweave.Input.Button;
using Eyebrow = Cosmere.Lightweave.Typography.Eyebrow;
using Glyph = Cosmere.Lightweave.Typography.Glyph;
using Text = Cosmere.Lightweave.Typography.Typography.Text;

namespace Cosmere.Lightweave.Redesign.NewColony;

public static class WorldTab {
    private static readonly string[] SubtabIds = ["world", "site"];

    public static LightweaveNode Build(
        Hooks.Hooks.StateHandle<WorldParams> world,
        Hooks.Hooks.StateHandle<int> pickedTile
    ) {
        Hooks.Hooks.StateHandle<string> subtab = Hooks.Hooks.UseState<string>("world");

        return HStack.Create(SpacingScale.None, h => {
            h.AddFlex(BuildMap(world, pickedTile));
            h.AddHug(Divider.Vertical());
            h.Add(BuildSide(world, pickedTile, subtab), new Rem(26f).ToPixels());
        }, style: new Style { Width = Length.Stretch, Height = Length.Stretch });
    }

    private static LightweaveNode BuildMap(
        Hooks.Hooks.StateHandle<WorldParams> world,
        Hooks.Hooks.StateHandle<int> pickedTile
    ) {
        return Stack.Create(SpacingScale.None, s => {
            s.AddFlex(WorldPreview.Create(
                onPick: tile => pickedTile.Set(tile.tileId),
                loading: () => NewColonyLauncher.Generating,
                loadingText: "CL_NewColony_World_Generating".Translate(),
                restoreRenderModeOnUnmount: false,
                style: new Style { Width = Length.Stretch, Height = Length.Stretch }));
            s.Add(BuildOverlay(world, pickedTile));
        }, style: new Style {
            Position = Position.Relative,
            Width = Length.Stretch,
            Height = Length.Stretch,
            Padding = EdgeInsets.All(new Rem(1f / 16f)),
        });
    }

    private static LightweaveNode BuildOverlay(
        Hooks.Hooks.StateHandle<WorldParams> world,
        Hooks.Hooks.StateHandle<int> pickedTile
    ) {
        return HStack.Create(SpacingScale.Lg, h => {
            h.AddHug(OverlayStat("CL_NewColony_World_Stat_Seed".Translate(), SeedLabel(world.Value)));
            h.AddHug(OverlayStat("CL_NewColony_World_Stat_Tiles".Translate(), TileCountLabel()));
            h.AddHug(OverlayStat("CL_NewColony_World_Stat_Landing".Translate(), LandingLabel(pickedTile.Value)));
            h.AddFlex(Spacer.Flex());
            h.AddHug(Button.Create(
                "CL_NewColony_World_Seed_Randomize".Translate(),
                () => {
                    WorldParams next = world.Value;
                    next.Seed = GenText.RandomSeedString();
                    world.Set(next);
                    NewColonyLauncher.GenerateWorld(next);
                },
                ghost: true,
                leading: Glyph.Create(Icons.Phosphor.ArrowsClockwise, style: new Style {
                    TextColor = ThemeSlot.TextSecondary,
                })));
        }, style: new Style {
            Position = Position.Absolute,
            Top = new Rem(0f),
            Left = new Rem(0f),
            Right = new Rem(0f),
            Padding = new EdgeInsets(new Rem(0.75f), new Rem(1f), new Rem(0.75f), new Rem(1f)),
            Background = BackgroundSpec.Of(ThemeSlot.WindowGlass),
            Border = BorderSpec.All(new Rem(1f / 16f), ThemeSlot.BorderSubtle),
            Radius = RadiusSpec.All(RadiusScale.Md),
        });
    }

    private static LightweaveNode OverlayStat(string label, string value) {
        return Stack.Create(SpacingScale.Xxs, s => {
            s.Add(Eyebrow.Create(label, style: new Style {
                FontFamily = FontRole.Mono,
                FontSize = new Rem(0.625f),
                LetterSpacing = Tracking.Of(0.2f),
                TextColor = ThemeSlot.TextMuted,
            }));
            s.Add(Text.Create(value, style: new Style {
                FontFamily = FontRole.Mono,
                FontSize = new Rem(13f / 16f),
                TextColor = ThemeSlot.TextPrimary,
            }));
        });
    }

    private static LightweaveNode BuildSide(
        Hooks.Hooks.StateHandle<WorldParams> world,
        Hooks.Hooks.StateHandle<int> pickedTile,
        Hooks.Hooks.StateHandle<string> subtab
    ) {
        string active = subtab.Value == "site" ? "site" : "world";

        return Stack.Create(SpacingScale.Md, s => {
            s.Add(Segmented.Create(
                active,
                SubtabIds,
                SubtabLabel,
                (string id) => subtab.Set(id)));
            if (active == "site") {
                s.AddFlex(WorldSiteTab.Build(pickedTile));
            }
            else {
                s.AddFlex(BuildWorldConfig(world));
            }
        }, style: new Style {
            Width = Length.Stretch,
            Height = Length.Stretch,
            Padding = new EdgeInsets(new Rem(1.25f), new Rem(1.5f), new Rem(1.25f), new Rem(1.5f)),
        });
    }

    private static string SubtabLabel(string id) {
        return id == "site"
            ? (string)"CL_NewColony_World_Subtab_Site".Translate()
            : (string)"CL_NewColony_World_Subtab_World".Translate();
    }

    private static LightweaveNode BuildWorldConfig(Hooks.Hooks.StateHandle<WorldParams> world) {
        return Stack.Create(SpacingScale.Md, outer => {
            outer.AddFlex(ScrollArea.Create(Stack.Create(SpacingScale.Lg, s => {
                s.Add(BuildCoverage(world));
                s.Add(BuildRainfall(world));
                s.Add(BuildTemperature(world));
                s.Add(BuildPopulation(world));
                s.Add(BuildMapSize(world));
            }, style: new Style { Width = Length.Stretch }),
                style: new Style { Width = Length.Stretch, Height = Length.Stretch }));
            outer.Add(BuildRegenerateButton(world));
        }, style: new Style { Width = Length.Stretch, Height = Length.Stretch });
    }

    private static LightweaveNode BuildRegenerateButton(Hooks.Hooks.StateHandle<WorldParams> world) {
        return Button.Create(
            "CL_NewColony_World_Regenerate".Translate(),
            () => NewColonyLauncher.GenerateWorld(world.Value),
            leading: Glyph.Create(Icons.Phosphor.ArrowsClockwise),
            style: new Style { Width = Length.Stretch });
    }

    private static string SeedLabel(WorldParams world) {
        if (!string.IsNullOrEmpty(NewColonyLauncher.LastSeed)) {
            return NewColonyLauncher.LastSeed;
        }
        return string.IsNullOrEmpty(world.Seed)
            ? (string)"CL_NewColony_World_Seed_Random".Translate()
            : world.Seed;
    }

    private static string TileCountLabel() {
        if (NewColonyLauncher.Generating || !NewColonyLauncher.WorldReady) {
            return "—";
        }
        return Find.WorldGrid.TilesCount.ToString("N0");
    }

    private static string LandingLabel(int tileId) {
        if (tileId < 0 || NewColonyLauncher.Generating || !NewColonyLauncher.WorldReady) {
            return "CL_NewColony_World_Landing_None".Translate();
        }
        PlanetTile tile = new PlanetTile(tileId);
        if (!tile.Valid) {
            return "CL_NewColony_World_Landing_None".Translate();
        }
        Tile ws = Find.WorldGrid[tile];
        return ws.PrimaryBiome.LabelCap;
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
