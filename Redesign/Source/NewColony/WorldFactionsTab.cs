using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cosmere.Lightweave.Hooks;
using Cosmere.Lightweave.Icons;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Surfaces;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using RimWorld;
using UnityEngine;
using Verse;
using IconButton = Cosmere.Lightweave.Input.IconButton;
using Glyph = Cosmere.Lightweave.Typography.Glyph;
using Icon = Cosmere.Lightweave.Typography.Typography.Icon;
using Text = Cosmere.Lightweave.Typography.Typography.Text;
using Tooltip = Cosmere.Lightweave.Data.Tooltip;
using TooltipSide = Cosmere.Lightweave.Data.TooltipSide;
using OptionRow = Cosmere.Lightweave.Input.OptionRow;
using Cosmere.Lightweave.Navigation;

namespace Cosmere.Lightweave.Redesign.NewColony;

public static class WorldFactionsTab {
    public static LightweaveNode Build(Hooks.Hooks.StateHandle<WorldParams> world) {
        if (NewColonyLauncher.Generating || !NewColonyLauncher.WorldReady) {
            return BuildEmpty();
        }

        List<FactionDef> config = ConfigCopy(world.Value);
        List<Faction> live = DisplayFactions();
        Faction? player = Find.FactionManager.OfPlayer;
        List<LightweaveNode> rows = BuildRosterRows(world, config, live, player);

        return Stack.Create(SpacingScale.Md, outer => {
            outer.Add(BuildHeader(rows.Count));
            outer.AddFlex(ScrollArea.Create(Stack.Create(SpacingScale.Sm, list => {
                for (int i = 0; i < rows.Count; i++) {
                    list.Add(rows[i]);
                }
            }, style: new Style { Width = Length.Stretch }),
                style: new Style { Width = Length.Stretch, Height = Length.Stretch }));
            outer.Add(BuildAddPicker(world));
            outer.Add(BuildResetButton(world));
        }, style: new Style { Width = Length.Stretch, Height = Length.Stretch });
    }

    // The roster reflects the pending faction config so add/remove shows immediately, before any
    // regeneration. Each config entry backed by a live generated faction (matched by def, in view
    // order) renders the full row with its goodwill/relations; config entries beyond the generated
    // count render as PENDING rows that materialize on the next Regenerate, and pending removals
    // (config count below the generated count) simply drop out of the list.
    private static List<LightweaveNode> BuildRosterRows(
        Hooks.Hooks.StateHandle<WorldParams> world,
        List<FactionDef> config,
        List<Faction> live,
        Faction? player
    ) {
        Dictionary<FactionDef, int> want = [];
        for (int i = 0; i < config.Count; i++) {
            FactionDef d = config[i];
            if (d.hidden) {
                continue;
            }
            want[d] = (want.TryGetValue(d, out int c) ? c : 0) + 1;
        }

        List<LightweaveNode> rows = [];
        Dictionary<FactionDef, int> shown = [];
        for (int i = 0; i < live.Count; i++) {
            Faction faction = live[i];
            FactionDef d = faction.def;
            int cap = want.TryGetValue(d, out int w) ? w : 0;
            int already = shown.TryGetValue(d, out int s) ? s : 0;
            if (already >= cap) {
                continue;
            }
            shown[d] = already + 1;
            rows.Add(BuildRow(world, faction, player));
        }

        Dictionary<FactionDef, int> seen = [];
        for (int i = 0; i < config.Count; i++) {
            FactionDef d = config[i];
            if (d.hidden) {
                continue;
            }
            int occurrence = seen.TryGetValue(d, out int sv) ? sv : 0;
            seen[d] = occurrence + 1;
            int liveShown = shown.TryGetValue(d, out int s) ? s : 0;
            if (occurrence >= liveShown) {
                rows.Add(BuildPendingRow(world, d, occurrence));
            }
        }
        return rows;
    }

    private static LightweaveNode BuildEmpty() {
        return Stack.Create(SpacingScale.Lg, s => {
            s.AddFlex(Text.Create("CL_NewColony_World_Factions_Empty".Translate(), style: new Style {
                Width = Length.Stretch,
                FontSize = new Rem(13f / 16f),
                TextColor = ThemeSlot.TextMuted,
                TextAlign = TextAlign.Center,
            }));
        }, style: new Style {
            Width = Length.Stretch,
            Height = Length.Stretch,
            Padding = EdgeInsets.All(new Rem(1.5f)),
        });
    }

    private static LightweaveNode BuildHeader(int count) {
        return HStack.Create(SpacingScale.Sm, h => {
            h.AddFlex(Text.Create(
                ((string)"CL_NewColony_World_Factions".Translate()).ToUpperInvariant() + "  ·  " + count,
                style: new Style {
                    FontFamily = FontRole.Mono,
                    FontSize = new Rem(0.625f),
                    LetterSpacing = Tracking.Of(0.2f),
                    TextColor = ThemeSlot.TextMuted,
                }));
            h.AddHug(Input.Button.Create(
                ((string)"CL_NewColony_World_Factions_Relations".Translate()).ToUpperInvariant(),
                () => Find.WindowStack.Add(new Dialog_FactionRelations()),
                ghost: true,
                leading: Glyph.Create(Icons.Phosphor.Sword, style: new Style {
                    TextColor = ThemeSlot.SurfaceAccent,
                })));
        }, style: new Style { Width = Length.Stretch });
    }

    private static LightweaveNode BuildRow(
        Hooks.Hooks.StateHandle<WorldParams> world,
        Faction faction,
        Faction? player
    ) {
        FactionDef def = faction.def;
        bool removable = CanRemove(def);

        LightweaveNode row = HStack.Create(SpacingScale.Sm, h => {
            h.Add(BuildIconTile(faction), new Rem(2.5f).ToPixels());
            h.AddFlex(BuildNameBlock(faction));

            if (player != null && faction != player) {
                FactionRelationKind kind = faction.PlayerRelationKind;
                h.AddHug(Text.Create(RelationLabel(kind), style: new Style {
                    FontSize = new Rem(11f / 16f),
                    LetterSpacing = Tracking.Of(0.12f),
                    TextColor = RelationSlot(kind),
                }));
                // Reserved-width pill slot keeps the trailing trash column aligned across rows
                // whether or not a pill renders. Hidden factions (mechanoid hive, insect geneline,
                // anomaly entities) carry no diplomatic goodwill, so they show HOSTILE with an empty slot.
                h.Add(
                    BuildPillSlot(def.hidden ? null : BuildGoodwillPill(faction.PlayerGoodwill, kind)),
                    new Rem(3f).ToPixels());
            }

            h.AddHug(IconButton.Create(
                Glyph.Create(Icons.Phosphor.Trash),
                removable ? () => RemoveFaction(world, def) : null,
                disabled: !removable,
                tooltipKey: removable ? "CL_NewColony_World_Factions_Remove" : "FactionRemovalDisabled"));
        }, style: new Style {
            Width = Length.Stretch,
            Padding = new EdgeInsets(new Rem(0.55f), new Rem(0.7f), new Rem(0.55f), new Rem(0.7f)),
            Background = BackgroundSpec.Of(ThemeSlot.SurfaceRaised),
            Border = BorderSpec.All(new Rem(1f / 16f), ThemeSlot.BorderSubtle),
            Radius = RadiusSpec.All(RadiusScale.Md),
        });

        // The whole row is the faction's info affordance: hovering anywhere on it surfaces the faction
        // description (this replaced the old per-row info-card button). Left-anchored so the tooltip
        // opens over the globe instead of overlapping the rows below; the per-faction id keeps each
        // row's hover timer independent (siblings share this call site's line/file otherwise).
        return Tooltip.Create(
            row,
            FactionTooltip(faction),
            side: TooltipSide.Left,
            id: faction.GetUniqueLoadID());
    }

    private static string FactionTooltip(Faction faction) {
        string description = faction.def.description;
        return string.IsNullOrEmpty(description) ? (string)faction.def.LabelCap : description;
    }

    private static LightweaveNode BuildIconTile(Faction faction) {
        return FactionBadge(faction.def, faction.Color, new Rem(2.5f), new Rem(1.4f));
    }

    // Tinted faction-icon tile shared by the roster rows (40px) and the add-picker rows (30px).
    // Roster rows pass the live faction color; the picker has only a FactionDef, so it passes
    // def.DefaultColor.
    private static LightweaveNode FactionBadge(FactionDef def, Color color, Rem tile, Rem icon) {
        return Icon.Create(
            def.FactionIcon,
            size: icon,
            style: new Style {
                Width = tile,
                Height = tile,
                Background = BackgroundSpec.Of(ThemeSlot.SurfaceSunken),
                Radius = RadiusSpec.All(RadiusScale.Sm),
                TextColor = color,
            });
    }

    private static LightweaveNode BuildNameBlock(Faction faction) {
        return Stack.Create(SpacingScale.Xxs, s => {
            s.Add(Text.Create(faction.Name, style: new Style {
                FontFamily = FontRole.Display,
                FontSize = new Rem(17f / 16f),
                TextColor = ThemeSlot.TextPrimary,
            }));
            s.Add(Text.Create(SubtitleFor(faction.def), style: new Style {
                FontSize = new Rem(11f / 16f),
                LetterSpacing = Tracking.Of(0.08f),
                TextColor = ThemeSlot.TextMuted,
            }));
        });
    }

    // A faction queued for the next world generation. No live Faction exists yet, so there is no
    // rolled name or goodwill — the row shows the def's type name, a PENDING marker, and a trash
    // button to drop it from the config. The dashed accent border echoes the "+ ADD FACTION" trigger.
    private static LightweaveNode BuildPendingRow(
        Hooks.Hooks.StateHandle<WorldParams> world,
        FactionDef def,
        int occurrence
    ) {
        bool removable = CanRemove(def);
        LightweaveNode row = HStack.Create(SpacingScale.Sm, h => {
            h.Add(FactionBadge(def, def.DefaultColor, new Rem(2.5f), new Rem(1.4f)), new Rem(2.5f).ToPixels());
            h.AddFlex(BuildPendingNameBlock(def));
            h.AddHug(Text.Create(
                ((string)"CL_NewColony_World_Factions_Pending".Translate()).ToUpperInvariant(),
                style: new Style {
                    FontFamily = FontRole.Mono,
                    FontSize = new Rem(11f / 16f),
                    LetterSpacing = Tracking.Of(0.12f),
                    TextColor = ThemeSlot.SurfaceAccent,
                }));
            h.Add(BuildPillSlot(null), new Rem(3f).ToPixels());
            h.AddHug(IconButton.Create(
                Glyph.Create(Icons.Phosphor.Trash),
                removable ? () => RemoveFaction(world, def) : null,
                disabled: !removable,
                tooltipKey: removable ? "CL_NewColony_World_Factions_Remove" : "FactionRemovalDisabled"));
        }, style: new Style {
            Width = Length.Stretch,
            Padding = new EdgeInsets(new Rem(0.55f), new Rem(0.7f), new Rem(0.55f), new Rem(0.7f)),
            Background = BackgroundSpec.Of(ThemeSlot.SurfaceRaised),
            Border = BorderSpec.AllDashed(new Rem(1f / 16f), ThemeSlot.AccentGlow),
            Radius = RadiusSpec.All(RadiusScale.Md),
        });
        return Tooltip.Create(
            row,
            FactionTooltip(def),
            side: TooltipSide.Left,
            id: "pending:" + def.defName + ":" + occurrence);
    }

    private static LightweaveNode BuildPendingNameBlock(FactionDef def) {
        return Stack.Create(SpacingScale.Xxs, s => {
            s.Add(Text.Create((string)def.LabelCap, style: new Style {
                FontFamily = FontRole.Display,
                FontSize = new Rem(17f / 16f),
                TextColor = ThemeSlot.TextPrimary,
            }));
            s.Add(Text.Create(SubtitleFor(def), style: new Style {
                FontSize = new Rem(11f / 16f),
                LetterSpacing = Tracking.Of(0.08f),
                TextColor = ThemeSlot.TextMuted,
            }));
        });
    }

    private static string FactionTooltip(FactionDef def) {
        string description = def.description;
        return string.IsNullOrEmpty(description) ? (string)def.LabelCap : description;
    }

    private static string SubtitleFor(FactionDef def) {
        string type = ((string)def.LabelCap).ToUpperInvariant();
        (string Name, ThemeSlot Slot)? dlc = NewColonyData.FactionDlc(def);
        if (dlc.HasValue) {
            return type + "  ·  " + dlc.Value.Name.ToUpperInvariant();
        }
        return type;
    }

    private static LightweaveNode BuildGoodwillPill(int goodwill, FactionRelationKind kind) {
        ThemeSlot text = RelationSlot(kind);
        return Box.Create(
            children: c => c.Add(Text.Create(GoodwillText(goodwill), style: new Style {
                Width = Length.Stretch,
                FontFamily = FontRole.Mono,
                FontSize = new Rem(11f / 16f),
                TextColor = text,
                TextAlign = TextAlign.Center,
            })),
            // Fixed width so every pill reads as the same chip regardless of value width (the mock
            // keeps "0" and "-100" the same size); vertical padding sets the height.
            style: new Style {
                Width = new Rem(2.75f),
                Padding = new EdgeInsets(new Rem(0.15f), new Rem(0.2f), new Rem(0.15f), new Rem(0.2f)),
                Background = BackgroundSpec.Of(GoodwillBgSlot(kind)),
                Border = BorderSpec.All(new Rem(1f / 16f), text),
                Radius = RadiusSpec.All(RadiusScale.Sm),
            });
    }

    // Fixed-width slot the row reserves for the goodwill pill. The pill (when present) hugs the
    // left edge so neutral/ally/hostile labels right-align against the slot; an empty slot keeps
    // the trailing info/trash icons in a consistent column for hidden factions.
    private static LightweaveNode BuildPillSlot(LightweaveNode? pill) {
        return HStack.Create(SpacingScale.None, h => {
            if (pill != null) {
                h.AddHug(pill);
            }
        }, align: FlexAlign.Center, style: new Style { Width = Length.Stretch });
    }

    // Standard Menu dropdown that opens upward: a dashed-accent "+ ADD FACTION" trigger whose caret
    // flips when open, anchored to a Menu listing every configurable faction kind (tinted icon +
    // name + trailing count tag). Replaces the old in-flow Disclosure so the picker reads as native
    // dropdown chrome.
    private static LightweaveNode BuildAddPicker(
        Hooks.Hooks.StateHandle<WorldParams> world,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = ""
    ) {
        Hooks.Hooks.StateHandle<bool> open = Hooks.Hooks.UseState(false, line, file);
        Hooks.Hooks.StateHandle<Rect> anchor = Hooks.Hooks.UseState(Rect.zero, line + 1, file);

        LightweaveNode node = NodeBuilder.New("FactionAddPicker", line, file);
        node.ApplyStyling("faction-add-picker", null, null, null);

        LightweaveNode caret = Glyph.Create(
            open.Value ? Icons.Phosphor.CaretUp : Icons.Phosphor.CaretDown,
            style: new Style {
                FontSize = new Rem(0.6875f),
                TextColor = ThemeSlot.SurfaceAccent,
            });

        Style surf = open.Value ? AddToggleOpenStyle() : AddToggleClosedStyle();
        // All-caps label with a leading glyph reads high against the glyph's optical center; derive
        // the same proportional nudge Disclosure used so the "+ ADD FACTION" label sits inline.
        Rem? labelOffset = surf.FontSize.HasValue ? new Rem(surf.FontSize.Value.Value * 0.27f) : (Rem?)null;

        LightweaveNode trigger = OptionRow.Create(
            ((string)"CL_NewColony_World_Factions_Add".Translate()).ToUpperInvariant(),
            () => open.Set(!open.Value),
            leading: Glyph.Create("+", style: new Style {
                FontFamily = FontRole.Mono,
                FontSize = new Rem(11f / 16f),
                TextColor = ThemeSlot.SurfaceAccent,
            }),
            trailing: caret,
            gap: new Rem(10f / 16f),
            labelOffsetY: labelOffset,
            style: surf);

        LightweaveNode menu = Menu.Create(
            isOpen: open.Value,
            anchorRect: anchor.Value,
            items: BuildAddItems(world, () => open.Set(false)),
            onDismiss: () => open.Set(false),
            anchor: MenuAnchor.Left,
            direction: MenuDirection.Up,
            instanceKey: "faction-add-menu",
            size: new Vector2(anchor.Value.width, -1f));

        node.Children.Add(trigger);
        node.Children.Add(menu);

        node.MeasureWidth = () => trigger.MeasureWidth?.Invoke() ?? 0f;
        node.Measure = availableWidth => trigger.Measure?.Invoke(availableWidth) ?? trigger.PreferredHeight ?? 0f;

        node.Paint = (rect, _) => {
            if (anchor.Value != rect) {
                anchor.Set(rect);
            }
            trigger.MeasuredRect = rect;
            menu.MeasuredRect = rect;
            LightweaveRoot.PaintSubtree(trigger, rect);
            LightweaveRoot.PaintSubtree(menu, rect);
        };

        return node;
    }

    // Mock .nc-fac-add: full-width, Glass1->AccentSoft vertical gradient, 1px dashed accent-glow
    // border, mono 11px / 0.18em accent label.
    private static Style AddToggleClosedStyle() {
        return new Style {
            Width = Length.Stretch,
            Background = BackgroundSpec.VerticalGradient(ThemeSlot.Glass1, ThemeSlot.AccentSoft),
            Border = BorderSpec.AllDashed(new Rem(1f / 16f), ThemeSlot.AccentGlow),
            Padding = new EdgeInsets(new Rem(11f / 16f), new Rem(14f / 16f), new Rem(11f / 16f), new Rem(14f / 16f)),
            FontFamily = FontRole.Mono,
            FontSize = new Rem(11f / 16f),
            LetterSpacing = Tracking.Of(0.18f),
            TextColor = ThemeSlot.SurfaceAccent,
        };
    }

    // Open state: solid accent border + hover-tint fill (mock .nc-fac-add hover/open).
    private static Style AddToggleOpenStyle() {
        return AddToggleClosedStyle() with {
            Background = BackgroundSpec.Of(ThemeSlot.HoverTint),
            Border = BorderSpec.All(new Rem(1f / 16f), ThemeSlot.SurfaceAccent),
        };
    }

    // One MenuEntry per configurable faction kind: tinted faction icon, name, trailing count tag.
    // Kinds at their per-type cap (or blocked by the total cap) render disabled rather than hidden,
    // matching vanilla's faction picker. Selecting one adds it and closes the menu.
    private static IReadOnlyList<MenuEntry> BuildAddItems(Hooks.Hooks.StateHandle<WorldParams> world, Action onDismiss) {
        List<FactionDef> config = ConfigCopy(world.Value);
        List<MenuEntry> items = [];
        foreach (FactionDef def in FactionGenerator.ConfigurableFactions) {
            if (!def.displayInFactionSelection) {
                continue;
            }
            bool canAdd = CanAddFaction(config, def);
            string? tag = CountTagString(CountOf(config, def), def.maxConfigurableAtWorldCreation);
            FactionDef captured = def;
            items.Add(MenuEntry.Of(
                def.LabelCap,
                () => {
                    AddFaction(world, captured);
                    onDismiss();
                },
                icon: MenuIcon(def),
                disabled: !canAdd,
                hotkey: tag));
        }
        return items;
    }

    // Bare tinted faction icon for a menu row (the Menu renders icons in a fixed slot, so no tile
    // background is used — matching the dropdown's native chrome).
    private static LightweaveNode MenuIcon(FactionDef def) {
        return Icon.Create(def.FactionIcon, size: new Rem(1f), style: new Style { TextColor = def.DefaultColor });
    }

    // Trailing count tag for an add-dropdown row. A genuinely-capped kind (small per-type bound)
    // reads "max {N}" at its limit or "{count} / {max}" below it; an uncapped kind (the 9999
    // sentinel or unset) reads "×{count}" when any are present and nothing otherwise — so the
    // dropdown never shows a meaningless "1 / 9999".
    private static string? CountTagString(int count, int max) {
        bool capped = NewColonyThresholds.IsConfigurableCap(max);
        FactionCountForm form = NewColonyThresholds.CountForm(capped, count, max);
        switch (form) {
            case FactionCountForm.Max:
                return (string)"CL_NewColony_World_Factions_CountMax".Translate(max.Named("MAX"));
            case FactionCountForm.Ratio:
                return (string)"CL_NewColony_World_Factions_CountRatio".Translate(count.Named("COUNT"), max.Named("MAX"));
            case FactionCountForm.Multiple:
                return (string)"CL_NewColony_World_Factions_CountMultiple".Translate(count.Named("COUNT"));
            default:
                return null;
        }
    }

    private static LightweaveNode BuildResetButton(Hooks.Hooks.StateHandle<WorldParams> world) {
        return Input.Button.Create(
            "CL_NewColony_World_Factions_Reset".Translate(),
            () => ResetFactions(world),
            ghost: true,
            leading: Glyph.Create(Icons.Phosphor.ArrowCounterClockwise, style: new Style {
                TextColor = ThemeSlot.TextSecondary,
            }),
            style: new Style { Width = Length.Stretch });
    }

    private static List<Faction> DisplayFactions() {
        List<Faction> result = [];
        // AllFactionsVisibleInViewOrder excludes def.hidden factions (Anomaly void entities, etc.),
        // matching vanilla's display convention (FactionManager.AllFactionsVisible). Player and
        // temporary factions are visible but not part of the roster, so skip them explicitly.
        foreach (Faction faction in Find.FactionManager.AllFactionsVisibleInViewOrder) {
            if (faction.IsPlayer || faction.temporary) {
                continue;
            }
            result.Add(faction);
        }
        return result;
    }

    // The displayed roster count is the pending non-hidden config size (live-backed rows plus pending
    // additions, minus pending removals all net out to this), so the subtab badge tracks edits live.
    public static int DisplayCount(WorldParams world) {
        if (NewColonyLauncher.Generating || !NewColonyLauncher.WorldReady) {
            return 0;
        }
        return CountNonHidden(ConfigCopy(world));
    }

    // Faction edits only update the pending config; the world is not regenerated until the user hits
    // Regenerate (World tab) or commits via GENERATE WORLD, whose NeedsRegen check picks up the changed
    // faction set. This keeps adding/removing factions instant and avoids re-rolling the whole planet.
    private static void AddFaction(Hooks.Hooks.StateHandle<WorldParams> world, FactionDef def) {
        WorldParams next = world.Value;
        List<FactionDef> list = ConfigCopy(next);
        list.Add(def);
        next.Factions = list;
        world.Set(next);
    }

    private static void RemoveFaction(Hooks.Hooks.StateHandle<WorldParams> world, FactionDef def) {
        WorldParams next = world.Value;
        List<FactionDef> list = ConfigCopy(next);
        int index = list.LastIndexOf(def);
        if (index < 0) {
            return;
        }
        list.RemoveAt(index);
        next.Factions = list;
        world.Set(next);
    }

    private static void ResetFactions(Hooks.Hooks.StateHandle<WorldParams> world) {
        WorldParams next = world.Value;
        next.Factions = WorldParams.DefaultFactionConfig();
        world.Set(next);
    }

    private static List<FactionDef> ConfigCopy(WorldParams world) {
        return world.Factions != null ? new List<FactionDef>(world.Factions) : WorldParams.DefaultFactionConfig();
    }

    // Mirrors WorldFactionsUIUtility.CanAddFaction: a hard cap of 12 non-hidden factions, plus a
    // per-def cap from maxConfigurableAtWorldCreation. Reasons reuse the vanilla keys so the float
    // menu reads identically to vanilla's faction picker.
    // Wraps the pure NewColonyThresholds.CanAddFaction rule with the vanilla localized reasons so the
    // float menu reads identically to vanilla's faction picker.
    private static AcceptanceReport CanAddFaction(List<FactionDef> config, FactionDef def) {
        FactionAddRule rule = NewColonyThresholds.CanAddFaction(
            def.hidden, CountNonHidden(config), CountOf(config, def), def.maxConfigurableAtWorldCreation);
        switch (rule) {
            case FactionAddRule.TotalCapReached:
                return (string)"TotalFactionsAllowed".Translate(12).ToString().UncapitalizeFirst();
            case FactionAddRule.TypeCapReached:
                return (string)"MaxFactionsForType".Translate(def.maxConfigurableAtWorldCreation).ToString().UncapitalizeFirst();
            default:
                return true;
        }
    }

    // Mirrors WorldFactionsUIUtility.DoRow: removal is blocked when a scenario part pins the faction
    // (e.g. a scenario that requires a specific enemy faction to exist).
    private static bool CanRemove(FactionDef def) {
        Scenario? scenario = Current.Game?.Scenario;
        if (scenario?.AllParts != null) {
            foreach (ScenPart part in scenario.AllParts) {
                if (part.def.preventRemovalOfFaction == def) {
                    return false;
                }
            }
        }
        return true;
    }

    private static int CountOf(List<FactionDef> config, FactionDef def) {
        int count = 0;
        for (int i = 0; i < config.Count; i++) {
            if (config[i] == def) {
                count++;
            }
        }
        return count;
    }

    private static int CountNonHidden(List<FactionDef> config) {
        int count = 0;
        for (int i = 0; i < config.Count; i++) {
            if (!config[i].hidden) {
                count++;
            }
        }
        return count;
    }

    private static string GoodwillText(int goodwill) {
        return NewColonyThresholds.GoodwillText(goodwill);
    }

    private static string RelationLabel(FactionRelationKind kind) {
        return kind switch {
            FactionRelationKind.Ally => ((string)"CL_NewColony_World_Factions_Ally".Translate()).ToUpperInvariant(),
            FactionRelationKind.Hostile => ((string)"CL_NewColony_World_Factions_Hostile".Translate()).ToUpperInvariant(),
            _ => ((string)"CL_NewColony_World_Factions_Neutral".Translate()).ToUpperInvariant(),
        };
    }

    private static ThemeSlot RelationSlot(FactionRelationKind kind) {
        return kind switch {
            FactionRelationKind.Ally => ThemeSlot.StatusSuccess,
            FactionRelationKind.Hostile => ThemeSlot.StatusDanger,
            _ => ThemeSlot.TextMuted,
        };
    }

    private static ThemeSlot GoodwillBgSlot(FactionRelationKind kind) {
        return kind switch {
            FactionRelationKind.Ally => ThemeSlot.StatusSuccessSoft,
            FactionRelationKind.Hostile => ThemeSlot.StatusDangerSoft,
            _ => ThemeSlot.SurfaceSunken,
        };
    }
}
