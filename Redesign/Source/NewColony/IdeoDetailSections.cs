namespace Cosmere.Lightweave.Redesign.NewColony;

// Pure mapping between the rail's IdeoDetailSection and the legacy IdeoEditorTab the belief gallery
// machinery is keyed on. Belief sections map 1:1; preference sections (Xenotypes/Apparel/Appearance)
// have no IdeoEditorTab and report null. Kept free of Verse/UI types so it is unit-testable.
public static class IdeoDetailSections {
    public static bool IsBelief(IdeoDetailSection section) {
        return BeliefTab(section).HasValue;
    }

    public static IdeoEditorTab? BeliefTab(IdeoDetailSection section) {
        return section switch {
            IdeoDetailSection.Memes => IdeoEditorTab.Memes,
            IdeoDetailSection.Precepts => IdeoEditorTab.Precepts,
            IdeoDetailSection.Rituals => IdeoEditorTab.Rituals,
            IdeoDetailSection.Roles => IdeoEditorTab.Roles,
            IdeoDetailSection.Relics => IdeoEditorTab.Relics,
            IdeoDetailSection.Buildings => IdeoEditorTab.Buildings,
            IdeoDetailSection.Animals => IdeoEditorTab.Animals,
            _ => null,
        };
    }

    public static IdeoDetailSection FromBeliefTab(IdeoEditorTab tab) {
        return tab switch {
            IdeoEditorTab.Memes => IdeoDetailSection.Memes,
            IdeoEditorTab.Precepts => IdeoDetailSection.Precepts,
            IdeoEditorTab.Rituals => IdeoDetailSection.Rituals,
            IdeoEditorTab.Roles => IdeoDetailSection.Roles,
            IdeoEditorTab.Relics => IdeoDetailSection.Relics,
            IdeoEditorTab.Buildings => IdeoDetailSection.Buildings,
            IdeoEditorTab.Animals => IdeoDetailSection.Animals,
            _ => IdeoDetailSection.Memes,
        };
    }

    // Stable string ids for SideNav rows (the rail addresses rows by id, not by enum).
    public static string Id(IdeoDetailSection section) {
        return section.ToString();
    }

    public static IdeoDetailSection Parse(string id) {
        return System.Enum.TryParse(id, out IdeoDetailSection parsed) ? parsed : IdeoDetailSection.Memes;
    }
}
