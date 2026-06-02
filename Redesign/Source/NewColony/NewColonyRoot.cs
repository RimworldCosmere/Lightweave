using System;
using System.Collections.Generic;
using Cosmere.Lightweave.Hooks;
using Cosmere.Lightweave.Icons;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Navigation;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Button = Cosmere.Lightweave.Input.Button;
using Display = Cosmere.Lightweave.Typography.Display;
using IconButton = Cosmere.Lightweave.Input.IconButton;
using Eyebrow = Cosmere.Lightweave.Typography.Eyebrow;
using Glyph = Cosmere.Lightweave.Typography.Glyph;
using Text = Cosmere.Lightweave.Typography.Typography.Text;

namespace Cosmere.Lightweave.Redesign.NewColony;

public static class NewColonyRoot {
    public static LightweaveNode Build(Action onClose) {
        bool anomaly = Verse.ModsConfig.AnomalyActive;

        Hooks.Hooks.StateHandle<string> tab = Hooks.Hooks.UseState<string>("scenario");
        Hooks.Hooks.StateHandle<string?> scenarioName = Hooks.Hooks.UseState<string?>(DefaultScenarioName());
        Hooks.Hooks.StateHandle<string?> tellerDefName = Hooks.Hooks.UseState<string?>(DefaultTellerDefName());
        Hooks.Hooks.StateHandle<string?> diffDefName = Hooks.Hooks.UseState<string?>(DefaultDifficultyDefName());
        Hooks.Hooks.StateHandle<WorldParams> world = Hooks.Hooks.UseState<WorldParams>(WorldParams.Default());
        Hooks.Hooks.StateHandle<AnomalyParams> anomalyParams = Hooks.Hooks.UseState<AnomalyParams>(AnomalyParams.Default());
        Hooks.Hooks.StateHandle<int> pickedTile = Hooks.Hooks.UseState<int>(-1);

        List<string> order = TabOrder(anomaly);
        string activeTab = order.Contains(tab.Value) ? tab.Value : "scenario";
        bool onLastTab = activeTab == order[order.Count - 1];

        // Teardown is handled uniformly in NewColonyWindow.PostClose so every close path
        // (Escape, click-away, this button) cleans up; here we just dismiss the window.
        Action close = onClose;

        Hooks.Hooks.UseEffect(() => {
            if (activeTab == "world") {
                NewColonyLauncher.PrepareProvision(scenarioName.Value, tellerDefName.Value, diffDefName.Value, anomalyParams.Value);
                if (!NewColonyLauncher.WorldReady) {
                    NewColonyLauncher.GenerateWorld(world.Value);
                }
            }
            return null;
        }, [activeTab]);

        return Stack.Create(SpacingScale.None, root => {
            root.Add(BuildHeadline(close));
            root.Add(BuildTabs(activeTab, anomaly, scenarioName, tellerDefName, diffDefName, world, anomalyParams, tab));
            root.AddFlex(BuildBody(activeTab, scenarioName, tellerDefName, diffDefName, world, anomalyParams, pickedTile));
            root.Add(BuildCommit(activeTab, order, onLastTab, tab, close, scenarioName, tellerDefName, diffDefName, world, anomalyParams, pickedTile));
            if (NewColonyLauncher.Generating) {
                root.Add(BuildDisabledScrim());
            }
        }, style: new Style { Position = Position.Relative });
    }

    private static LightweaveNode BuildDisabledScrim() {
        return Stack.Create(SpacingScale.None, _ => { }, style: new Style {
            Position = Position.Absolute,
            Top = new Rem(0f),
            Right = new Rem(0f),
            Bottom = new Rem(0f),
            Left = new Rem(0f),
            Background = BackgroundSpec.Of(ThemeSlot.OverlayDim),
        });
    }

    private static LightweaveNode BuildHeadline(Action onClose) {
        return HStack.Create(SpacingScale.Lg, h => {
            h.AddFlex(HStack.Create(SpacingScale.Md, title => {
                title.AddHug(Display.Create("CL_NewColony_Title".Translate(), level: 1, style: new Style {
                    FontFamily = FontRole.Display,
                    FontSize = new Rem(2.25f),
                    LetterSpacing = Tracking.Of(0.04f),
                    TextColor = ThemeSlot.TextPrimary,
                }));
                title.AddHug(Text.Create("CL_NewColony_Subtitle".Translate(), style: new Style {
                    FontFamily = FontRole.Display,
                    FontWeight = UnityEngine.FontStyle.Italic,
                    FontSize = new Rem(18f / 16f),
                    TextColor = ThemeSlot.TextMuted,
                }));
            }));
            h.AddHug(IconButton.Create(
                Glyph.Create(Icons.Phosphor.X, style: new Style {
                    FontSize = new Rem(1f),
                    TextColor = ThemeSlot.TextMuted,
                }),
                onClose,
                variant: Variant.Secondary,
                tooltipKey: "CL_NewColony_Close_Tooltip"));
        }, style: new Style {
            Padding = new EdgeInsets(new Rem(1.5f), new Rem(1.75f), new Rem(0.5f), new Rem(1.75f)),
        });
    }

    private static LightweaveNode BuildTabs(
        string activeTab,
        bool anomaly,
        Hooks.Hooks.StateHandle<string?> scenarioName,
        Hooks.Hooks.StateHandle<string?> tellerDefName,
        Hooks.Hooks.StateHandle<string?> diffDefName,
        Hooks.Hooks.StateHandle<WorldParams> world,
        Hooks.Hooks.StateHandle<AnomalyParams> anomalyParams,
        Hooks.Hooks.StateHandle<string> tab
    ) {
        List<TabItem> items = [
            new TabItem("scenario", "CL_NewColony_Tab_Scenario".Translate(), number: 1,
                subtitle: ScenarioSummary(scenarioName.Value)),
            new TabItem("storyteller", "CL_NewColony_Tab_Storyteller".Translate(), number: 2,
                subtitle: TellerName(tellerDefName.Value)),
        ];
        if (anomaly) {
            items.Add(new TabItem("anomaly", "CL_NewColony_Tab_Anomaly".Translate(), number: 3,
                subtitle: AnomalySummary(anomalyParams.Value)));
            items.Add(new TabItem("world", "CL_NewColony_Tab_World".Translate(), number: 4,
                subtitle: WorldSummary(world.Value)));
        }
        else {
            items.Add(new TabItem("world", "CL_NewColony_Tab_World".Translate(), number: 3,
                subtitle: WorldSummary(world.Value)));
        }

        return Stack.Create(SpacingScale.None, s => s.Add(Tabs.Create(items, activeTab, (string id) => tab.Set(id), TabsVariant.Dossier)),
            style: new Style {
                Padding = new EdgeInsets(Right: new Rem(1.75f), Left: new Rem(1.75f)),
            });
    }

    private static LightweaveNode BuildBody(
        string activeTab,
        Hooks.Hooks.StateHandle<string?> scenarioName,
        Hooks.Hooks.StateHandle<string?> tellerDefName,
        Hooks.Hooks.StateHandle<string?> diffDefName,
        Hooks.Hooks.StateHandle<WorldParams> world,
        Hooks.Hooks.StateHandle<AnomalyParams> anomalyParams,
        Hooks.Hooks.StateHandle<int> pickedTile
    ) {
        LightweaveNode content = activeTab switch {
            "storyteller" => StorytellerTab.Build(tellerDefName, diffDefName),
            "world" => WorldTab.Build(world, pickedTile),
            "anomaly" => AnomalyTab.Build(anomalyParams),
            _ => ScenarioTab.Build(scenarioName),
        };

        bool flush = activeTab == "world";
        EdgeInsets padding = flush
            ? new EdgeInsets(new Rem(0f), new Rem(0f), new Rem(0f), new Rem(0f))
            : new EdgeInsets(new Rem(1.25f), new Rem(1.5f), new Rem(1.25f), new Rem(1.5f));

        return Stack.Create(SpacingScale.None, s => s.AddFlex(content),
            style: new Style {
                Width = Length.Stretch,
                Height = Length.Stretch,
                Margin = new EdgeInsets(new Rem(0.75f), new Rem(1.75f), new Rem(0.75f), new Rem(1.75f)),
                Padding = padding,
                Background = BackgroundSpec.Of(ThemeSlot.ShelfTint),
                Border = BorderSpec.All(new Rem(1f / 16f), ThemeSlot.BorderSubtle),
                Radius = RadiusSpec.All(RadiusScale.Md),
            });
    }

    private static LightweaveNode BuildCommit(
        string activeTab,
        List<string> order,
        bool onLastTab,
        Hooks.Hooks.StateHandle<string> tab,
        Action onClose,
        Hooks.Hooks.StateHandle<string?> scenarioName,
        Hooks.Hooks.StateHandle<string?> tellerDefName,
        Hooks.Hooks.StateHandle<string?> diffDefName,
        Hooks.Hooks.StateHandle<WorldParams> world,
        Hooks.Hooks.StateHandle<AnomalyParams> anomalyParams,
        Hooks.Hooks.StateHandle<int> pickedTile
    ) {
        int index = order.IndexOf(activeTab);

        Action back = index <= 0
            ? onClose
            : () => tab.Set(order[index - 1]);

        Action primary = onLastTab
            ? () => NewColonyLauncher.Commit(
                world.Value,
                pickedTile.Value < 0 ? PlanetTile.Invalid : new PlanetTile(pickedTile.Value),
                onClose)
            : () => tab.Set(order[index + 1]);

        string backLabel = index <= 0
            ? "CL_NewColony_Back_MainMenu".Translate()
            : TabLabel(order[index - 1]);

        string primaryLabel = onLastTab
            ? "CL_NewColony_Generate".Translate()
            : "CL_NewColony_Continue".Translate();

        LightweaveNode stats = HStack.Create(SpacingScale.Xl, row => {
            row.AddHug(BuildStat("CL_NewColony_Stat_Scenario".Translate(), ScenarioSummary(scenarioName.Value)));
            row.AddHug(BuildStat("CL_NewColony_Stat_Storyteller".Translate(), TellerName(tellerDefName.Value)));
            row.AddHug(BuildStat("CL_NewColony_Stat_Difficulty".Translate(), DifficultyName(diffDefName.Value)));
            row.AddHug(BuildStat("CL_NewColony_Stat_MapSize".Translate(), world.Value.MapSize + "²"));
        });

        Style footerButtonStyle = new Style {
            FontFamily = FontRole.Mono,
            FontSize = new Rem(12f / 16f),
            LetterSpacing = Tracking.Of(0.22f),
        };

        LightweaveNode content = HStack.Create(SpacingScale.Xl, h => {
            h.AddHug(Button.Create(backLabel.ToUpperInvariant(), back, Variant.Solid,
                leading: Glyph.Create(Icons.Phosphor.ArrowLeft, style: new Style {
                    TextColor = ThemeSlot.TextMuted,
                }),
                style: footerButtonStyle));
            h.AddHug(stats);
            h.AddFlex(Spacer.Flex());
        });

        LightweaveNode actions = HStack.Create(SpacingScale.Md, a => {
            if (Verse.Steam.SteamManager.Initialized) {
                a.AddHug(Button.Create(((string)"CL_NewColony_BrowseWorkshop".Translate()).ToUpperInvariant(),
                    SteamUtility.OpenSteamWorkshopPage,
                    Variant.Secondary,
                    leading: Glyph.Create(Icons.Phosphor.ArrowUpRight, style: new Style {
                        TextColor = ThemeSlot.TextSecondary,
                    }),
                    style: footerButtonStyle));
            }
            a.AddHug(Button.Create(primaryLabel.ToUpperInvariant(), primary, Variant.Primary,
                trailing: Glyph.Create(onLastTab ? Icons.Phosphor.Play : Icons.Phosphor.ArrowRight, style: new Style {
                    TextColor = ThemeSlot.TextOnAccent,
                }),
                style: footerButtonStyle));
        });

        LightweaveNode footerCard = WindowFooter.Create(content: content, actions: actions, drawDivider: false, style: new Style {
            Width = Length.Stretch,
            Background = BackgroundSpec.Of(ThemeSlot.ShelfTint),
            Border = BorderSpec.All(new Rem(1f / 16f), ThemeSlot.BorderSubtle),
            Radius = RadiusSpec.All(RadiusScale.Md),
        });

        return Stack.Create(SpacingScale.None, s => s.Add(footerCard), style: new Style {
            Width = Length.Stretch,
            Padding = new EdgeInsets(new Rem(0.75f), new Rem(1.75f), new Rem(0.75f), new Rem(1.75f)),
        });
    }

    private static LightweaveNode BuildStat(string key, string value) {
        return Stack.Create(SpacingScale.Xxs, s => {
            s.Add(Eyebrow.Create(key, style: new Style {
                FontFamily = FontRole.Mono,
                FontSize = new Rem(0.625f),
                LetterSpacing = Tracking.Of(0.2f),
                TextColor = ThemeSlot.TextMuted,
            }));
            s.Add(Text.Create(value, style: new Style {
                FontFamily = FontRole.Display,
                FontSize = new Rem(1.5f),
                LetterSpacing = Tracking.Of(0.02f),
                TextColor = ThemeSlot.TextPrimary,
            }));
        });
    }

    private static string TabLabel(string id) {
        return id switch {
            "storyteller" => (string)"CL_NewColony_Tab_Storyteller".Translate(),
            "world" => (string)"CL_NewColony_Tab_World".Translate(),
            "anomaly" => (string)"CL_NewColony_Tab_Anomaly".Translate(),
            _ => (string)"CL_NewColony_Tab_Scenario".Translate(),
        };
    }

    private static List<string> TabOrder(bool anomaly) {
        List<string> order = ["scenario", "storyteller"];
        if (anomaly) {
            order.Add("anomaly");
        }
        order.Add("world");
        return order;
    }

    private static string? DefaultScenarioName() {
        List<Scenario> official = NewColonyData.OfficialScenarios();
        return official.Count > 0 ? official[0].name : null;
    }

    private static string? DefaultTellerDefName() {
        List<StorytellerDef> tellers = NewColonyData.Storytellers();
        foreach (StorytellerDef def in tellers) {
            if (def.defName == "Cassandra") {
                return def.defName;
            }
        }
        return tellers.Count > 0 ? tellers[0].defName : null;
    }

    private static string? DefaultDifficultyDefName() {
        List<DifficultyDef> diffs = NewColonyData.Difficulties();
        foreach (DifficultyDef def in diffs) {
            if (def.defName == "Rough") {
                return def.defName;
            }
        }
        return diffs.Count > 0 ? diffs[diffs.Count / 2].defName : null;
    }

    private static string ScenarioSummary(string? name) {
        return string.IsNullOrEmpty(name) ? "CL_NewColony_None".Translate() : name!;
    }

    private static string TellerName(string? defName) {
        StorytellerDef? def = NewColonyData.FindStoryteller(defName);
        return def != null ? def.label.CapitalizeFirst() : "CL_NewColony_None".Translate();
    }

    private static string DifficultyName(string? defName) {
        DifficultyDef? def = NewColonyData.FindDifficulty(defName);
        return def != null ? def.label.CapitalizeFirst() : "CL_NewColony_None".Translate();
    }

    private static string WorldSummary(WorldParams world) {
        return NewColonyFormat.CoverageLabel(world.Coverage) + " · " + world.MapSize + "²";
    }

    private static string AnomalySummary(AnomalyParams anomaly) {
        AnomalyPlaystyleDef? def = DefDatabase<AnomalyPlaystyleDef>.GetNamedSilentFail(anomaly.PlaystyleDefName);
        return def != null ? def.LabelCap.ToString() : "CL_NewColony_None".Translate();
    }
}
