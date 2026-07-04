namespace Cosmere.Lightweave.Redesign.NewColony;

// The Customize tab's detail rail merges the seven belief tabs (which still drive the existing
// IdeoEditorTab gallery machinery 1:1) with three preference sections that have their own pane
// renderers. The belief members map straight onto IdeoEditorTab; the preference members do not.
public enum IdeoDetailSection {
    Memes,
    Precepts,
    Rituals,
    Roles,
    Relics,
    Buildings,
    Animals,
    Xenotypes,
    Apparel,
    Appearance,
}
