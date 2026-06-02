using Cosmere.Lightweave.Data;
using Cosmere.Lightweave.Feedback;
using Cosmere.Lightweave.Hooks;
using Cosmere.Lightweave.Icons;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Navigation;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Surfaces;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Alert = Cosmere.Lightweave.Feedback.Alert;
using Button = Cosmere.Lightweave.Input.Button;
using IconButton = Cosmere.Lightweave.Input.IconButton;
using TextField = Cosmere.Lightweave.Input.TextField;
using Eyebrow = Cosmere.Lightweave.Typography.Eyebrow;
using Glyph = Cosmere.Lightweave.Typography.Glyph;
using Text = Cosmere.Lightweave.Typography.Typography.Text;

namespace Cosmere.Lightweave.Redesign.NewColony;

public static class WorldTab {
    private static readonly string[] SubtabIds = ["world", "factions", "site"];

    public static LightweaveNode Build(
        Hooks.Hooks.StateHandle<WorldParams> world,
        Hooks.Hooks.StateHandle<int> pickedTile
    ) {
        Hooks.Hooks.StateHandle<string> subtab = Hooks.Hooks.UseState<string>("world");

        return HStack.Create(SpacingScale.None, h => {
            h.AddFlex(BuildMap(world, pickedTile, subtab));
            h.AddHug(Divider.Vertical());
            h.Add(BuildSide(world, pickedTile, subtab), new Rem(52f).ToPixels());
        }, style: new Style { Width = Length.Stretch, Height = Length.Stretch });
    }

    private static LightweaveNode BuildMap(
        Hooks.Hooks.StateHandle<WorldParams> world,
        Hooks.Hooks.StateHandle<int> pickedTile,
        Hooks.Hooks.StateHandle<string> subtab
    ) {
        return Stack.Create(SpacingScale.None, s => {
            s.AddFlex(WorldPreview.Create(
                onPick: tile => {
                    pickedTile.Set(tile.tileId);
                    // Picking a landing site jumps the side panel to the Site subtab so its detail
                    // is immediately in view.
                    subtab.Set("site");
                },
                loading: () => NewColonyLauncher.Generating,
                loadingText: "CL_NewColony_World_Generating".Translate(),
                restoreRenderModeOnUnmount: false,
                style: new Style { Width = Length.Stretch, Height = Length.Stretch }));
            s.Add(BuildOverlay(world, pickedTile));
            s.Add(BuildControlsHint());
            if (Current.Game?.playSettings != null) {
                s.Add(BuildOverlayToggles(Current.Game.playSettings));
            }
        }, style: new Style {
            Position = Position.Relative,
            Width = Length.Stretch,
            Height = Length.Stretch,
            Padding = EdgeInsets.All(new Rem(1f / 16f)),
        });
    }

    private static LightweaveNode BuildControlsHint() {
        return HStack.Create(SpacingScale.Xs, h => {
            h.AddHug(Text.Create("CL_NewColony_World_Controls_Tilt".Translate(), style: new Style {
                FontFamily = FontRole.Mono,
                FontSize = new Rem(0.625f),
                LetterSpacing = Tracking.Of(0.15f),
                TextColor = ThemeSlot.TextMuted,
            }));
        }, style: new Style {
            Position = Position.Absolute,
            Bottom = new Rem(0.75f),
            Left = new Rem(0.75f),
            Padding = new EdgeInsets(new Rem(0.3f), new Rem(0.6f), new Rem(0.3f), new Rem(0.6f)),
            Background = BackgroundSpec.Of(ThemeSlot.WindowGlass),
            Border = BorderSpec.All(new Rem(1f / 16f), ThemeSlot.BorderSubtle),
            Radius = RadiusSpec.All(RadiusScale.Sm),
        });
    }

    private static LightweaveNode BuildOverlayToggles(PlaySettings ps) {
        // Mirrors the world-view toggle row vanilla floats over the planet during site selection
        // (PlaySettings.DoWorldViewControls). Our WorldPreview renders the real Find.World through the
        // world camera, so flipping these PlaySettings booleans changes our globe exactly as it does
        // vanilla's. We omit the colonist-bar toggle (Playing-only), the search button (not an
        // overlay), the lock-north toggle (our globe drives the camera directly and bypasses
        // WorldCameraDriver, so RotateSoNorthIsUp/lockNorthUp have no effect here), and the
        // showImportantExpandingIcons toggle (gates caravans/quest-sites/player objects that never
        // exist on a freshly-generated world before the colony starts, so it is inert on this surface),
        // keeping just the overlay toggles a new colony can actually act on.
        // captureHits: the row paints as one solid glass panel, so the whole rect (padding + the gaps
        // between toggles included) must claim the click - otherwise a click landing in a gap falls
        // through to the globe behind and selects the tile under it.
        return HStack.Create(SpacingScale.Xxs, h => {
            h.AddHug(OverlayToggle(Icons.Phosphor.Flag, ps.showBasesExpandingIcons,
                () => ps.showBasesExpandingIcons = !ps.showBasesExpandingIcons,
                "ShowBasesExpandingIconsToggleButton"));
            if (Verse.ModsConfig.OdysseyActive) {
                h.AddHug(OverlayToggle(Icons.Phosphor.Mountains, ps.showExpandingLandmarks,
                    () => ps.showExpandingLandmarks = !ps.showExpandingLandmarks,
                    "ShowExpandingLandmarksToggleButton"));
            }
            h.AddHug(OverlayToggle(Icons.Phosphor.Sun, ps.usePlanetDayNightSystem,
                () => ps.usePlanetDayNightSystem = !ps.usePlanetDayNightSystem,
                "UsePlanetDayNightSystemToggleButton"));
            h.AddHug(OverlayToggle(Icons.Phosphor.MapTrifold, ps.showWorldFeatures,
                () => ps.showWorldFeatures = !ps.showWorldFeatures,
                "ShowWorldFeaturesToggleButton"));
        }, captureHits: true, style: new Style {
            Position = Position.Absolute,
            Bottom = new Rem(0.75f),
            Right = new Rem(0.75f),
            Padding = new EdgeInsets(new Rem(0.3f), new Rem(0.4f), new Rem(0.3f), new Rem(0.4f)),
            Background = BackgroundSpec.Of(ThemeSlot.WindowGlass),
            Border = BorderSpec.All(new Rem(1f / 16f), ThemeSlot.BorderSubtle),
            Radius = RadiusSpec.All(RadiusScale.Md),
        });
    }

    private static LightweaveNode OverlayToggle(IconRef icon, bool value, System.Action toggle, string tooltipKey) {
        return IconButton.Create(
            Glyph.Create(icon),
            () => {
                toggle();
                // IconButton captures `active` at build, so bump the hook version to rebuild and re-read
                // the PlaySettings bool. The globe itself already reflects the change live each frame.
                RenderContext.Current.Hooks.Invalidate();
            },
            active: value,
            tooltipKey: tooltipKey,
            // Anchor above the row: the cluster sits in the bottom-right corner, so a bottom tooltip
            // would render off the pane edge.
            tooltipSide: TooltipSide.Top);
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
        string active = subtab.Value == "site" || subtab.Value == "factions" ? subtab.Value : "world";
        int factionCount = WorldFactionsTab.DisplayCount(world.Value);

        return Stack.Create(SpacingScale.None, s => {
            // The subtab strip sits flush against the pane's top/left/right borders (full-bleed),
            // with a divider beneath it; only the content below gets inset padding.
            s.Add(Segmented.Create(
                active,
                SubtabIds,
                id => SubtabStripLabel(id, factionCount),
                (string id) => subtab.Set(id),
                bordered: false,
                labelSize: new Rem(0.875f),
                style: new Style { Width = Length.Stretch, Height = new Rem(2.625f) }));
            s.Add(Divider.Horizontal());
            s.AddFlex(Stack.Create(SpacingScale.Md, content => {
                if (active == "site") {
                    content.AddFlex(WorldSiteTab.Build(pickedTile));
                }
                else if (active == "factions") {
                    content.AddFlex(WorldFactionsTab.Build(world));
                }
                else {
                    content.AddFlex(BuildWorldConfig(world));
                }
                // Regenerate lives in the shared side-panel footer so every subtab can apply pending
                // edits (faction adds/removes, slider changes, seed) to the world without leaving the tab.
                content.Add(BuildRegenerateButton(world));
            }, style: new Style {
                Width = Length.Stretch,
                Height = Length.Stretch,
                Padding = new EdgeInsets(new Rem(1.25f), new Rem(1.5f), new Rem(1.25f), new Rem(1.5f)),
            }));
        }, style: new Style { Width = Length.Stretch, Height = Length.Stretch });
    }

    private static string SubtabLabel(string id) {
        return id switch {
            "site" => (string)"CL_NewColony_World_Subtab_Site".Translate(),
            "factions" => (string)"CL_NewColony_World_Subtab_Factions".Translate(),
            _ => (string)"CL_NewColony_World_Subtab_World".Translate(),
        };
    }

    private static string SubtabStripLabel(string id, int factionCount) {
        string label = SubtabLabel(id).ToUpperInvariant();
        if (id == "factions" && factionCount > 0) {
            return label + " " + factionCount;
        }
        return label;
    }

    private static LightweaveNode BuildWorldConfig(Hooks.Hooks.StateHandle<WorldParams> world) {
        return ScrollArea.Create(Stack.Create(SpacingScale.Lg, s => {
            s.Add(BuildSeedField(world));
            s.Add(BuildCoverage(world));
            s.Add(BuildRainfall(world));
            s.Add(BuildTemperature(world));
            s.Add(BuildPopulation(world));
            s.Add(BuildMapSize(world));
            s.Add(BuildSeason(world));
            // Landmark density is an Odyssey planet-gen knob; pollution is a Biotech one. Vanilla
            // hides each slider when its DLC is inactive, so do the same rather than show a control
            // that feeds an ignored gen parameter.
            if (Verse.ModsConfig.OdysseyActive) {
                s.Add(BuildLandmark(world));
            }
            if (Verse.ModsConfig.BiotechActive) {
                s.Add(BuildPollution(world));
            }
        }, style: new Style { Width = Length.Stretch }),
            style: new Style { Width = Length.Stretch, Height = Length.Stretch });
    }

    private static LightweaveNode BuildSeedField(Hooks.Hooks.StateHandle<WorldParams> world) {
        // Editable seed: typing only mutates state; the world is not regenerated until the user hits
        // Regenerate (or the overlay's Randomize). An empty field means "random each generation", which
        // RunWorldGen turns into GenText.RandomSeedString — surfaced here as the placeholder.
        return Section("CL_NewColony_World_Seed".Translate(), TextField.Create(
            world.Value.Seed ?? string.Empty,
            v => {
                WorldParams next = world.Value;
                next.Seed = v;
                world.Set(next);
            },
            placeholder: (string)"CL_NewColony_World_Seed_Random".Translate(),
            style: new Style { Width = Length.Stretch }));
    }

    private static LightweaveNode BuildSeason(Hooks.Hooks.StateHandle<WorldParams> world) {
        Season current = world.Value.StartingSeason;
        LightweaveNode picker = Segmented.Create(
            current,
            NewColonyFormat.Seasons,
            v => NewColonyFormat.SeasonLabel(v).ToUpperInvariant(),
            v => {
                WorldParams next = world.Value;
                next.StartingSeason = v;
                world.Set(next);
            });
        if (current != Season.Winter) {
            return Section("CL_NewColony_World_Season".Translate(), picker);
        }
        // Winter starts every colony in the hardest season; vanilla flags this. Surface the same warning.
        return Stack.Create(SpacingScale.Sm, s => {
            s.Add(NewColonyControls.SectionLabel("CL_NewColony_World_Season".Translate()));
            s.Add(picker);
            s.Add(Alert.Create(
                "CL_NewColony_World_Season_WinterWarning".Translate(),
                variant: AlertVariant.Warning));
        });
    }

    private static LightweaveNode BuildPollution(Hooks.Hooks.StateHandle<WorldParams> world) {
        return NewColonyControls.LabeledSlider(
            "CL_NewColony_World_Pollution".Translate(),
            NewColonyFormat.PollutionLabel,
            world.Value.Pollution,
            v => {
                WorldParams next = world.Value;
                next.Pollution = Mathf.Clamp(Mathf.Round(v / 0.05f) * 0.05f, 0f, 1f);
                world.Set(next);
            },
            0f, 1f, 0.05f,
            key: "pollution"
        );
    }

    private static LightweaveNode BuildLandmark(Hooks.Hooks.StateHandle<WorldParams> world) {
        return NewColonyControls.LabeledSlider(
            "CL_NewColony_World_Landmark".Translate(),
            v => NewColonyFormat.LandmarkLabel(Mathf.RoundToInt(v)),
            world.Value.Landmark,
            v => {
                WorldParams next = world.Value;
                next.Landmark = Mathf.RoundToInt(v);
                world.Set(next);
            },
            0f, 6f, 1f,
            key: "landmark"
        );
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
        return NewColonyControls.LabeledSlider(
            "CL_NewColony_World_Coverage".Translate(),
            NewColonyFormat.CoverageLabel,
            world.Value.Coverage,
            v => {
                WorldParams next = world.Value;
                next.Coverage = Mathf.Clamp(Mathf.Round(v / 0.05f) * 0.05f, 0.05f, 1f);
                world.Set(next);
            },
            0.05f, 1f, 0.05f,
            key: "coverage"
        );
    }

    private static LightweaveNode BuildRainfall(Hooks.Hooks.StateHandle<WorldParams> world) {
        return NewColonyControls.LabeledSlider(
            "CL_NewColony_World_Rainfall".Translate(),
            v => NewColonyFormat.RainfallLabel(Mathf.RoundToInt(v)),
            world.Value.Rainfall,
            v => {
                WorldParams next = world.Value;
                next.Rainfall = Mathf.RoundToInt(v);
                world.Set(next);
            },
            0f, 6f, 1f,
            key: "rainfall"
        );
    }

    private static LightweaveNode BuildTemperature(Hooks.Hooks.StateHandle<WorldParams> world) {
        return NewColonyControls.LabeledSlider(
            "CL_NewColony_World_Temperature".Translate(),
            v => NewColonyFormat.TemperatureLabel(Mathf.RoundToInt(v)),
            world.Value.Temperature,
            v => {
                WorldParams next = world.Value;
                next.Temperature = Mathf.RoundToInt(v);
                world.Set(next);
            },
            0f, 6f, 1f,
            key: "temperature"
        );
    }

    private static LightweaveNode BuildPopulation(Hooks.Hooks.StateHandle<WorldParams> world) {
        return Section("CL_NewColony_World_Population".Translate(), Segmented.Create(
            world.Value.Population,
            NewColonyFormat.PopulationLevels,
            v => NewColonyFormat.PopulationLabel(v).ToUpperInvariant(),
            v => {
                WorldParams next = world.Value;
                next.Population = v;
                world.Set(next);
            }
        ));
    }

    private static LightweaveNode BuildMapSize(Hooks.Hooks.StateHandle<WorldParams> world) {
        int size = world.Value.MapSize;
        // Vanilla's selectable sizes top out at 325 (350/400 are dev-only). We open the upper end to 500
        // for players who want it, and the lower end to 50 — but tiny maps below 150 are a dev/testing
        // affordance, so they only unlock when dev mode is on.
        float min = Prefs.DevMode ? 50f : 150f;
        LightweaveNode slider = NewColonyControls.LabeledSlider(
            "CL_NewColony_World_MapSize".Translate(),
            v => NewColonyFormat.MapSizeLabel(Mathf.RoundToInt(v)),
            size,
            v => {
                WorldParams next = world.Value;
                next.MapSize = Mathf.RoundToInt(v);
                world.Set(next);
            },
            min, 500f, 25f,
            key: "mapsize"
        );
        // Maps grow with the square of the edge length, so anything past the 250 default starts to
        // cost real memory and per-tick CPU. Warn once the player pushes into the large tiers rather
        // than block the choice.
        if (size < 300) {
            return slider;
        }
        return Stack.Create(SpacingScale.Sm, s => {
            s.Add(slider);
            s.Add(Alert.Create(
                "CL_NewColony_World_MapSize_LargeWarning".Translate(),
                variant: AlertVariant.Warning));
        });
    }

    private static LightweaveNode Section(string label, LightweaveNode body) {
        return Stack.Create(SpacingScale.Sm, s => {
            s.Add(NewColonyControls.SectionLabel(label));
            s.Add(body);
        });
    }
}
