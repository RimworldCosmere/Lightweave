using System;
using System.Collections.Generic;
using System.Linq;
using Cosmere.Lightweave.Icons;
using Cosmere.Lightweave.Input;
using Cosmere.Lightweave.Layout;
using Cosmere.Lightweave.Feedback;
using Cosmere.Lightweave.Runtime;
using Cosmere.Lightweave.Tokens;
using Cosmere.Lightweave.Types;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Steam;
using Text = Cosmere.Lightweave.Typography.Typography.Text;

namespace Cosmere.Lightweave.Redesign.ModsConfig;

public enum ModsTab {
    Installed,
    LoadOrder,
}

public static class ModsConfigRoot {
    private static readonly Rem DetailPaneWidth = new Rem(38.75f);

    public static LightweaveNode Build(RimWorld.Page_ModsConfig page, Action onClose) {
        ConflictSnapshot.Invalidate();
        Hooks.Hooks.StateHandle<ModsTab> tab = Hooks.Hooks.UseState(ModsTab.Installed);
        Hooks.Hooks.StateHandle<string> query = Hooks.Hooks.UseState(string.Empty);
        List<ModMetaData> tabMods = ModsForTab(tab.Value);
        List<ModMetaData> visible = FilterByQuery(tabMods, query.Value);
        int activeCount = CountActive();
        int installedCount = CountInstalled();
        int conflictCount = CountConflicts();

        Hooks.Hooks.StateHandle<string?> selected = Hooks.Hooks.UseState<string?>(null);
        ModMetaData? activeMod = ResolveActive(visible, selected.Value);

        string statusLine = "CL_ModsConfig_Stat_ActiveCount".Translate(activeCount.Named("COUNT")).Resolve()
                          + "CL_ModsConfig_Stat_InstalledCount".Translate(installedCount.Named("COUNT")).Resolve();
        string? statusWarn = null;
        if (conflictCount > 0) {
            statusWarn = (conflictCount == 1
                ? "CL_ModsConfig_Stat_ConflictCount".Translate(conflictCount.Named("COUNT"))
                : "CL_ModsConfig_Stat_ConflictCountPlural".Translate(conflictCount.Named("COUNT"))).Resolve();
        }

        bool dirty = ModsConfigState.HasUnsavedChanges(page);

        LightweaveNode? footerNode = dirty
            ? WindowFooter.Create(
                actions: HStack.Create(SpacingScale.Xxs, a => {
                    a.AddHug(Button.Create(
                        (string)"CL_ModsConfig_Discard".Translate(),
                        () => DiscardAndClose(page, onClose),
                        Variant.Ghost
                    ));
                    a.AddHug(Button.Create(
                        (string)"CL_ModsConfig_Save".Translate(),
                        () => SaveAndClose(page, onClose),
                        Variant.Primary
                    ));
                })
            )
            : null;

        LightweaveNode statusContent = HStack.Create(SpacingScale.Xs, h => {
            h.AddHug(Text.Create(statusLine, style: new Style {
                FontFamily = FontRole.Mono,
                FontSize = new Rem(0.7f),
                LetterSpacing = Tracking.Of(0.08f),
                TextColor = ThemeSlot.TextMuted,
            }));
            if (!string.IsNullOrEmpty(statusWarn)) {
                h.AddHug(Text.Create(statusWarn!, style: new Style {
                    FontFamily = FontRole.Mono,
                    FontSize = new Rem(0.7f),
                    LetterSpacing = Tracking.Of(0.08f),
                    TextColor = ThemeSlot.StatusWarning,
                }));
            }
        });

        return Stack.Create(SpacingScale.None, root => {
            root.Add(WindowHeader.Create(
                title: "CL_ModsConfig_Title".Translate(),
                headerContent: statusContent,
                onClose: () => RequestClose(page, onClose),
                drawDivider: true
            ));
            root.AddFlex(HStack.Create(SpacingScale.None, h => {
                h.AddFlex(ModListPane.Create(
                    visible,
                    selected.Value,
                    name => selected.Set(name),
                    page,
                    tab.Value,
                    t => tab.Set(t),
                    query.Value,
                    q => query.Set(q)
                ));
                h.Add(ModDetailPane.Create(activeMod, page, onClose), DetailPaneWidth.ToPixels());
            }));
            if (footerNode != null) {
                root.Add(footerNode);
            }
        });
    }

    private static List<ModMetaData> ModsForTab(ModsTab tab) {
        switch (tab) {
            case ModsTab.LoadOrder:
                return Verse.ModsConfig.ActiveModsInLoadOrder.ToList();
            case ModsTab.Installed:
                List<ModMetaData> result = new List<ModMetaData>();
                foreach (ModMetaData m in Verse.ModsConfig.ActiveModsInLoadOrder) {
                    result.Add(m);
                }
                foreach (ModMetaData m in ModLister.AllInstalledMods) {
                    if (!m.Active) {
                        result.Add(m);
                    }
                }
                return result;
            default:
                return new List<ModMetaData>();
        }
    }


    private static List<ModMetaData> FilterByQuery(List<ModMetaData> mods, string query) {
        if (string.IsNullOrWhiteSpace(query)) {
            return mods;
        }
        List<ModMetaData> result = new List<ModMetaData>();
        foreach (ModMetaData m in mods) {
            string name = m.Name ?? string.Empty;
            string author = m.AuthorsString ?? string.Empty;
            if (name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                || author.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) {
                result.Add(m);
            }
        }
        return result;
    }

    private static int CountActive() {
        int count = 0;
        foreach (ModMetaData _ in Verse.ModsConfig.ActiveModsInLoadOrder) {
            count++;
        }
        return count;
    }

    private static int CountInstalled() {
        int count = 0;
        foreach (ModMetaData _ in ModLister.AllInstalledMods) {
            count++;
        }
        return count;
    }

    private static int CountConflicts() {
        int total = 0;
        foreach (ModMetaData mod in Verse.ModsConfig.ActiveModsInLoadOrder) {
            total += ModConflicts.CountFor(mod);
        }
        return total;
    }

    private static ModMetaData? ResolveActive(List<ModMetaData> mods, string? packageId) {
        if (string.IsNullOrEmpty(packageId)) {
            return mods.FirstOrDefault();
        }
        return mods.FirstOrDefault(m => string.Equals(m.PackageId, packageId, StringComparison.OrdinalIgnoreCase))
               ?? mods.FirstOrDefault();
    }

    private static void SaveAndClose(RimWorld.Page_ModsConfig page, Action onClose) {
        ModsConfigState.SetSaveChanges(page, true);
        onClose();
    }


    private static void DiscardAndClose(RimWorld.Page_ModsConfig page, Action onClose) {
        ModsConfigState.SetDiscardChanges(page, true);
        onClose();
    }

    private static void RequestClose(RimWorld.Page_ModsConfig page, Action onClose) {
        if (!ModsConfigState.HasUnsavedChanges(page)) {
            ModsConfigState.SetDiscardChanges(page, true);
            onClose();
            return;
        }
        Find.WindowStack.Add(new Dialog_ModsConfigConfirmClose(
            onSave: () => SaveAndClose(page, onClose),
            onDiscard: () => {
                ModsConfigState.SetDiscardChanges(page, true);
                onClose();
            }
        ));
    }

    

    

    

    
}
