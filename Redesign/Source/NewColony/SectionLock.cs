using System;

namespace Cosmere.Lightweave.Redesign.NewColony;

// Per-section locks for the ideoligion editor. A locked section is preserved when "Randomize all"
// runs and its own per-section randomize button is disabled. Stored in a single StateHandle<SectionLock>
// threaded through the editor in place of the old narrative-only bool.
[Flags]
public enum SectionLock {
    None = 0,
    Memes = 1 << 0,
    Narrative = 1 << 1,
    Styles = 1 << 2,
    Precepts = 1 << 3,
    Rituals = 1 << 4,
    Roles = 1 << 5,
    Relics = 1 << 6,
    Buildings = 1 << 7,
    Animals = 1 << 8,
    Structure = 1 << 9,
    Deities = 1 << 10,
    Name = 1 << 11,
    Adjective = 1 << 12,
    MemberNoun = 1 << 13,
    RitualRoom = 1 << 14,
    Culture = 1 << 15,
    Xenotypes = 1 << 16,
    Apparel = 1 << 17,
    Appearance = 1 << 18,
}
