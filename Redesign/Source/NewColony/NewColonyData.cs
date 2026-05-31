using System.Collections.Generic;
using Cosmere.Lightweave.Tokens;
using RimWorld;
using Verse;

namespace Cosmere.Lightweave.Redesign.NewColony;

public static class NewColonyData {
    private static List<Scenario>? officialScenarios;
    private static List<Scenario>? moddedScenarios;
    private static List<Scenario>? customScenarios;
    private static List<Scenario>? workshopScenarios;
    private static Dictionary<Scenario, ModContentPack>? scenarioSources;
    private static List<StorytellerDef>? storytellers;
    private static List<DifficultyDef>? difficulties;
    private static List<AnomalyPlaystyleDef>? anomalyPlaystyles;

    public static List<Scenario> OfficialScenarios() {
        if (officialScenarios == null) {
            BuildFromDefScenarios();
        }
        return officialScenarios!;
    }

    public static List<Scenario> ModdedScenarios() {
        if (moddedScenarios == null) {
            BuildFromDefScenarios();
        }
        return moddedScenarios!;
    }

    private static void BuildFromDefScenarios() {
        officialScenarios = new List<Scenario>();
        moddedScenarios = new List<Scenario>();
        foreach (Scenario scen in ScenarioLister.ScenariosInCategory(ScenarioCategory.FromDef)) {
            ModContentPack? source = SourceFor(scen);
            if (source != null && !source.IsOfficialMod) {
                moddedScenarios.Add(scen);
            }
            else {
                officialScenarios.Add(scen);
            }
        }
    }

    private static ModContentPack? SourceFor(Scenario scen) {
        if (scenarioSources == null) {
            scenarioSources = new Dictionary<Scenario, ModContentPack>();
            foreach (ScenarioDef def in DefDatabase<ScenarioDef>.AllDefs) {
                if (def.scenario != null && def.modContentPack != null) {
                    scenarioSources[def.scenario] = def.modContentPack;
                }
            }
        }
        return scenarioSources.TryGetValue(scen, out ModContentPack mcp) ? mcp : null;
    }

    public static (string Name, ThemeSlot Slot)? ScenarioDlc(Scenario scen) {
        ModContentPack? source = SourceFor(scen);
        if (source == null || !source.IsOfficialMod || source.IsCoreMod) {
            return null;
        }
        return (source.Name, DlcSlotForPackageId(source.PackageId));
    }

    private static ThemeSlot DlcSlotForPackageId(string? packageId) {
        if (string.IsNullOrEmpty(packageId)) {
            return ThemeSlot.DlcRoyalty;
        }
        string id = packageId!.ToLowerInvariant();
        if (id.Contains("ideology")) {
            return ThemeSlot.DlcIdeology;
        }
        if (id.Contains("biotech")) {
            return ThemeSlot.DlcBiotech;
        }
        if (id.Contains("anomaly")) {
            return ThemeSlot.DlcAnomaly;
        }
        if (id.Contains("odyssey")) {
            return ThemeSlot.DlcOdyssey;
        }
        return ThemeSlot.DlcRoyalty;
    }

    public static List<Scenario> CustomScenarios() {
        if (customScenarios == null) {
            customScenarios = new List<Scenario>();
            foreach (Scenario scen in ScenarioLister.ScenariosInCategory(ScenarioCategory.CustomLocal)) {
                customScenarios.Add(scen);
            }
        }
        return customScenarios;
    }

    public static List<Scenario> WorkshopScenarios() {
        if (workshopScenarios == null) {
            workshopScenarios = new List<Scenario>();
            foreach (Scenario scen in ScenarioLister.ScenariosInCategory(ScenarioCategory.SteamWorkshop)) {
                workshopScenarios.Add(scen);
            }
        }
        return workshopScenarios;
    }

    public static List<StorytellerDef> Storytellers() {
        if (storytellers == null) {
            storytellers = new List<StorytellerDef>();
            foreach (StorytellerDef def in DefDatabase<StorytellerDef>.AllDefs) {
                if (def.listVisible) {
                    storytellers.Add(def);
                }
            }
            storytellers.SortBy(d => d.listOrder);
        }
        return storytellers;
    }

    public static List<DifficultyDef> Difficulties() {
        if (difficulties == null) {
            difficulties = new List<DifficultyDef>();
            foreach (DifficultyDef def in DefDatabase<DifficultyDef>.AllDefs) {
                if (!def.isCustom) {
                    difficulties.Add(def);
                }
            }
            difficulties.SortBy(d => d.threatScale);
        }
        return difficulties;
    }

    public static List<AnomalyPlaystyleDef> AnomalyPlaystyles() {
        if (anomalyPlaystyles == null) {
            anomalyPlaystyles = new List<AnomalyPlaystyleDef>();
            foreach (AnomalyPlaystyleDef def in DefDatabase<AnomalyPlaystyleDef>.AllDefs) {
                anomalyPlaystyles.Add(def);
            }
        }
        return anomalyPlaystyles;
    }

    public static Scenario? FindScenario(string? name) {
        if (string.IsNullOrEmpty(name)) {
            return null;
        }
        foreach (Scenario scen in OfficialScenarios()) {
            if (scen.name == name) {
                return scen;
            }
        }
        foreach (Scenario scen in ModdedScenarios()) {
            if (scen.name == name) {
                return scen;
            }
        }
        foreach (Scenario scen in CustomScenarios()) {
            if (scen.name == name) {
                return scen;
            }
        }
        foreach (Scenario scen in WorkshopScenarios()) {
            if (scen.name == name) {
                return scen;
            }
        }
        return null;
    }

    public static StorytellerDef? FindStoryteller(string? defName) {
        if (string.IsNullOrEmpty(defName)) {
            return null;
        }
        foreach (StorytellerDef def in Storytellers()) {
            if (def.defName == defName) {
                return def;
            }
        }
        return null;
    }

    public static DifficultyDef? FindDifficulty(string? defName) {
        if (string.IsNullOrEmpty(defName)) {
            return null;
        }
        foreach (DifficultyDef def in Difficulties()) {
            if (def.defName == defName) {
                return def;
            }
        }
        return null;
    }
}
