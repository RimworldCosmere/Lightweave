using System.Collections.Generic;
using Cosmere.Lightweave.Data;
using Cosmere.Lightweave.Hooks;
using Cosmere.Lightweave.Icons;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Button = Cosmere.Lightweave.Input.Button;
using Display = Cosmere.Lightweave.Typography.Display;
using Glyph = Cosmere.Lightweave.Typography.Glyph;
using Text = Cosmere.Lightweave.Typography.Typography.Text;

namespace Cosmere.Lightweave.Redesign.NewColony;

public static class WorldSiteTab {
    public static LightweaveNode Build(Hooks.Hooks.StateHandle<int> pickedTile) {
        int tileId = pickedTile.Value;
        if (tileId < 0 || NewColonyLauncher.Generating || !NewColonyLauncher.WorldReady) {
            return BuildEmpty(pickedTile);
        }

        PlanetTile tile = new PlanetTile(tileId);
        if (!tile.Valid) {
            return BuildEmpty(pickedTile);
        }

        Tile ws = Find.WorldGrid[tile];
        Vector2 longLat = Find.WorldGrid.LongLatOf(tile);

        return ScrollArea.Create(Stack.Create(SpacingScale.Md, s => {
            s.Add(BuildHeader(ws, longLat));
            if (!ws.PrimaryBiome.description.NullOrEmpty()) {
                s.Add(Text.Create(ws.PrimaryBiome.description, wrap: true, style: new Style {
                    Width = Length.Stretch,
                    FontFamily = FontRole.Body,
                    FontSize = new Rem(13f / 16f),
                    TextColor = ThemeSlot.TextSecondary,
                }));
            }
            s.Add(Divider.Horizontal());
            s.Add(BuildGrid(ws, tile, longLat));
            s.Add(BuildRandomButton(pickedTile));
        }, style: new Style {
            Width = Length.Stretch,
            Padding = new EdgeInsets(new Rem(1.25f), new Rem(1.5f), new Rem(1.25f), new Rem(1.5f)),
        }),
            style: new Style { Width = Length.Stretch, Height = Length.Stretch });
    }

    private static LightweaveNode BuildEmpty(Hooks.Hooks.StateHandle<int> pickedTile) {
        return Stack.Create(SpacingScale.Lg, s => {
            s.AddFlex(Text.Create("CL_NewColony_World_Site_Empty".Translate(), style: new Style {
                Width = Length.Stretch,
                FontSize = new Rem(13f / 16f),
                TextColor = ThemeSlot.TextMuted,
                TextAlign = TextAlign.Center,
            }));
            s.Add(BuildRandomButton(pickedTile));
        }, style: new Style {
            Width = Length.Stretch,
            Height = Length.Stretch,
            Padding = new EdgeInsets(new Rem(1.5f), new Rem(1.5f), new Rem(1.5f), new Rem(1.5f)),
        });
    }

    private static LightweaveNode BuildHeader(Tile ws, Vector2 longLat) {
        bool hot = ws.temperature >= 25f;
        string coords = longLat.y.ToStringLatitude() + " " + longLat.x.ToStringLongitude();

        return Stack.Create(SpacingScale.Xs, s => {
            s.Add(HStack.Create(SpacingScale.Sm, h => {
                h.AddFlex(Display.Create((string)ws.PrimaryBiome.LabelCap, level: 2, style: new Style {
                    FontFamily = FontRole.Display,
                    FontSize = new Rem(2f),
                    TextColor = ThemeSlot.TextPrimary,
                }));
                h.AddHug(Chip.Create(
                    GenTemperature.GetAverageTemperatureLabel(ws.tile),
                    variant: hot ? ChipVariant.Warn : ChipVariant.None,
                    upper: false,
                    bold: false));
            }, style: new Style { Width = Length.Stretch }));
            s.Add(Text.Create(coords, style: new Style {
                FontFamily = FontRole.Mono,
                FontSize = new Rem(12f / 16f),
                LetterSpacing = Tracking.Of(0.06f),
                TextColor = ThemeSlot.TextMuted,
            }));
        });
    }

    private static LightweaveNode BuildGrid(Tile ws, PlanetTile tile, Vector2 longLat) {
        return Stack.Create(SpacingScale.Md, outer => {
            outer.Add(Stack.Create(SpacingScale.Xs, geo => {
                geo.Add(SiteRow("CL_NewColony_World_Site_Terrain".Translate(), ws.hilliness.GetLabelCap()));
                string? river = RiverLabel(ws);
                if (river != null) {
                    geo.Add(SiteRow("CL_NewColony_World_Site_River".Translate(), river));
                }
                string? movement = MovementLabel(tile);
                if (movement != null) {
                    geo.Add(SiteRow("CL_NewColony_World_Site_Movement".Translate(), movement));
                }
                geo.Add(SiteRow("CL_NewColony_World_Site_Elevation".Translate(), ws.elevation.ToString("F0") + "m"));
                geo.Add(SiteRow("CL_NewColony_World_Site_Stone".Translate(), StoneTypes(tile)));
            }, style: new Style { Width = Length.Stretch }));

            outer.Add(Stack.Create(SpacingScale.Xs, clim => {
                clim.Add(SiteRow("CL_NewColony_World_Site_AvgTemp".Translate(), GenTemperature.GetAverageTemperatureLabel(tile)));
                clim.Add(SiteRow("CL_NewColony_World_Site_Growing".Translate(), Zone_Growing.GrowingQuadrumsDescription(tile, null)));
                clim.Add(SiteRow("CL_NewColony_World_Site_Rainfall".Translate(), ws.rainfall.ToString("F0") + "mm"));
                clim.Add(SiteRow("CL_NewColony_World_Site_Forageability".Translate(), Forageability(ws)));
                clim.Add(SiteRow("CL_NewColony_World_Site_Graze".Translate(), GrazeLabel(tile)));
                clim.Add(SiteRow("CL_NewColony_World_Site_Disease".Translate(), DiseaseRate(ws)));
            }, style: new Style { Width = Length.Stretch }));

            if (Verse.ModsConfig.BiotechActive) {
                outer.Add(Stack.Create(SpacingScale.Xs, pol => {
                    pol.Add(SiteRow("CL_NewColony_World_Site_Pollution".Translate(), ws.pollution.ToStringPercent()));
                    pol.Add(SiteRow("CL_NewColony_World_Site_NearbyPollution".Translate(), NearbyPollution(tile)));
                }, style: new Style { Width = Length.Stretch }));
            }

            outer.Add(SiteRow("CL_NewColony_World_Site_TimeZone".Translate(), TimeZoneLabel(longLat.x)));
        }, style: new Style { Width = Length.Stretch });
    }

    private static LightweaveNode SiteRow(string label, string value) {
        return HStack.Create(SpacingScale.Sm, h => {
            h.Add(Text.Create(label, style: new Style {
                FontSize = new Rem(13f / 16f),
                TextColor = ThemeSlot.TextMuted,
            }), new Rem(8f).ToPixels());
            h.AddFlex(Text.Create(value, style: new Style {
                FontSize = new Rem(13f / 16f),
                TextColor = ThemeSlot.TextPrimary,
            }));
        }, style: new Style {
            Width = Length.Stretch,
            Padding = new EdgeInsets(Top: new Rem(0.35f), Bottom: new Rem(0.35f)),
            Border = new BorderSpec(Bottom: new Rem(1f / 16f), Color: ThemeSlot.BorderSubtle),
        });
    }

    private static LightweaveNode BuildRandomButton(Hooks.Hooks.StateHandle<int> pickedTile) {
        return Button.Create(
            "CL_NewColony_World_Site_Random".Translate(),
            () => {
                PlanetTile tile = TileFinder.RandomStartingTile();
                pickedTile.Set(tile.tileId);
            },
            variant: Variant.Secondary,
            leading: Glyph.Create(Icons.Phosphor.Shuffle, style: new Style {
                TextColor = ThemeSlot.TextSecondary,
            }),
            style: new Style { Width = Length.Stretch });
    }

    private static string StoneTypes(PlanetTile tile) {
        List<string> rocks = [];
        foreach (ThingDef rock in Find.World.NaturalRockTypesIn(tile)) {
            rocks.Add(rock.LabelCap);
        }
        return rocks.Count > 0 ? string.Join(", ", rocks) : "—";
    }

    private static string DiseaseRate(Tile ws) {
        return NewColonyThresholds.DiseaseFrequencyLabel(ws.PrimaryBiome.diseaseMtbDays);
    }

    private static string TimeZoneLabel(float longitude) {
        return NewColonyThresholds.TimeZoneLabel(GenDate.TimeZoneAt(longitude));
    }

    private static string? RiverLabel(Tile ws) {
        if (ws is SurfaceTile st && st.Rivers != null && st.Rivers.Count > 0) {
            return st.Rivers.MaxBy(r => r.river.degradeThreshold).river.LabelCap;
        }
        return null;
    }

    private static string? MovementLabel(PlanetTile tile) {
        if (Find.World.Impassable(tile)) {
            return null;
        }
        float difficulty = WorldPathGrid.CalculatedMovementDifficultyAt(tile, false, null, null)
            * Find.WorldGrid.GetRoadMovementDifficultyMultiplier(tile, PlanetTile.Invalid, null);
        return difficulty.ToString("0.#");
    }

    private static string Forageability(Tile ws) {
        BiomeDef biome = ws.PrimaryBiome;
        if (biome.foragedFood != null && biome.forageability > 0f) {
            return biome.forageability.ToStringPercent() + " (" + biome.foragedFood.label + ")";
        }
        return "0%";
    }

    private static string GrazeLabel(PlanetTile tile) {
        return VirtualPlantsUtility.EnvironmentAllowsEatingVirtualPlantsNowAt(tile)
            ? (string)"Yes".Translate()
            : (string)"No".Translate();
    }

    private static string NearbyPollution(PlanetTile tile) {
        return WorldPollutionUtility.CalculateNearbyPollutionScore(tile).ToStringByStyle(ToStringStyle.FloatTwo);
    }
}
