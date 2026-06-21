using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Cosmere.Lightweave.Redesign.NewColony;

// The single seam between the Ideology editor and the deep Verse Ideo API. Each method mutates the
// live draft Ideo using the same lever sequence vanilla's Dialog_ChooseMemes.DoAcceptChanges uses
// (assign memes -> EnsurePreceptsCompatibleWithMemes -> RecachePrecepts -> RegenerateDescription),
// so precepts stay valid for the current memes. Callers bump the editor's rev hook afterward.
public static class IdeoDraftMutations {
    private static IdeoGenerationParms Parms() {
        return new IdeoGenerationParms(Faction.OfPlayer.def);
    }

    // A deity-based foundation whose deities haven't been rolled yet makes RegenerateDescription emit a
    // "Grammar unresolvable" error - the intro grammar references [deity0_name]/[deity0_type] that don't
    // exist. Roll them on demand so the description always resolves. No-ops for non-deity foundations
    // (animist, etc.) and for foundations that already have their deities.
    private static void EnsureDeities(Ideo ideo) {
        if (ideo.foundation is IdeoFoundation_Deity deityFoundation
            && deityFoundation.DeitiesListForReading.Count == 0) {
            deityFoundation.GenerateDeities();
        }
    }

    // Single funnel for every description regen: guarantee deities exist first so the intro grammar
    // never fails to resolve. Use this instead of calling ideo.RegenerateDescription directly.
    private static void RegenDescription(Ideo ideo) {
        EnsureDeities(ideo);
        ideo.RegenerateDescription(force: true);
    }

    private static void RecacheForMemeChange(Ideo ideo, List<MemeDef> oldMemes) {
        ideo.SortMemesInDisplayOrder();
        ideo.foundation.EnsurePreceptsCompatibleWithMemes(oldMemes, ideo.memes, Parms());
        ideo.RecachePrecepts();
        RegenDescription(ideo);
    }

    public static void SwapStructure(Ideo ideo, MemeDef newStructure) {
        List<MemeDef> oldMemes = new List<MemeDef>(ideo.memes);
        ideo.memes.RemoveAll(m => m.category == MemeCategory.Structure);
        ideo.memes.Insert(0, newStructure);
        RecacheForMemeChange(ideo, oldMemes);
    }

    public static void AddMeme(Ideo ideo, MemeDef meme) {
        if (ideo.memes.Contains(meme)) {
            return;
        }
        List<MemeDef> oldMemes = new List<MemeDef>(ideo.memes);
        ideo.memes.Add(meme);
        RecacheForMemeChange(ideo, oldMemes);
    }

    public static void RemoveMeme(Ideo ideo, MemeDef meme) {
        if (!ideo.memes.Contains(meme)) {
            return;
        }
        List<MemeDef> oldMemes = new List<MemeDef>(ideo.memes);
        ideo.memes.Remove(meme);
        RecacheForMemeChange(ideo, oldMemes);
    }

    // Strips every non-structure meme, leaving just the structure. Used to seed a minimal editable
    // ideo for the progressive wizard (structure kept, starting beliefs chosen by the player).
    public static void ClearNormalMemes(Ideo ideo) {
        List<MemeDef> oldMemes = new List<MemeDef>(ideo.memes);
        ideo.memes.RemoveAll(m => m.category != MemeCategory.Structure);
        RecacheForMemeChange(ideo, oldMemes);
    }

    // Sets a single starting meme: clears existing non-structure memes, then adds the chosen one.
    // The wizard's "starting belief" step (step 2) is single-select, so this mirrors that contract.
    public static void SetStartingMeme(Ideo ideo, MemeDef meme) {
        List<MemeDef> oldMemes = new List<MemeDef>(ideo.memes);
        ideo.memes.RemoveAll(m => m.category != MemeCategory.Structure);
        if (!ideo.memes.Contains(meme)) {
            ideo.memes.Add(meme);
        }
        RecacheForMemeChange(ideo, oldMemes);
    }

    public static void AddPrecept(Ideo ideo, PreceptDef def) {
        foreach (Precept existing in ideo.PreceptsListForReading) {
            if (existing.def == def) {
                return;
            }
        }
        ideo.AddPrecept(PreceptMaker.MakePrecept(def), init: true, Faction.OfPlayer.def, def.ritualPatternBase);
        ideo.RecachePrecepts();
        RegenDescription(ideo);
    }

    public static void RemovePrecept(Ideo ideo, Precept precept) {
        ideo.RemovePrecept(precept, replacing: false);
        ideo.RecachePrecepts();
        RegenDescription(ideo);
    }

    public static void ToggleStyle(Ideo ideo, StyleCategoryDef category) {
        List<ThingStyleCategoryWithPriority> styles = ideo.thingStyleCategories;
        int index = styles.FindIndex(s => s.category == category);
        if (index >= 0) {
            styles.RemoveAt(index);
        }
        else {
            styles.Add(new ThingStyleCategoryWithPriority(category, 1f));
        }
        ideo.style.RecalculateAvailableStyleItems();
    }

    // Re-rolls just the non-structure memes via vanilla's foundation lever (protected, so reached by
    // reflection), then re-runs the same compatibility recache as a manual meme edit.
    public static void RandomizeMemes(Ideo ideo) {
        List<MemeDef> oldMemes = new List<MemeDef>(ideo.memes);
        System.Reflection.MethodInfo method = HarmonyLib.AccessTools.Method(typeof(IdeoFoundation), "RandomizeMemes");
        method.Invoke(ideo.foundation, [Parms()]);
        RecacheForMemeChange(ideo, oldMemes);
    }

    public static void RandomizeStyles(Ideo ideo) {
        ideo.foundation.RandomizeStyles();
        ideo.style.RecalculateAvailableStyleItems();
    }

    // init: true is load-bearing - it runs vanilla's InitPrecepts, which assigns Precept_ThingDef.ThingDef
    // (and other per-precept setup). With init: false those stay null and RegenerateDescription NPEs in
    // IdeoDescriptionUtility.AddPreceptRules. Matches vanilla's own IdeoUIUtility RandomizePrecepts button.
    public static void RandomizePrecepts(Ideo ideo) {
        ideo.foundation.RandomizePrecepts(init: true, Parms());
        ideo.RecachePrecepts();
        RegenDescription(ideo);
    }

    // Renames the ideoligion in place. No description regen - the name is independent of the
    // generated scripture, and regenning here would clobber a player-edited description.
    public static void Rename(Ideo ideo, string name) {
        if (string.IsNullOrEmpty(name)) {
            return;
        }
        ideo.name = name;
    }

    // Re-rolls the structure meme to a random one, then recaches precept compatibility - a structure
    // swap can invalidate precepts the same way a manual structure pick does (handled by SwapStructure).
    public static void RandomizeStructure(Ideo ideo) {
        List<MemeDef> structures = new List<MemeDef>();
        foreach (MemeDef meme in DefDatabase<MemeDef>.AllDefsListForReading) {
            if (meme.category == MemeCategory.Structure) {
                structures.Add(meme);
            }
        }
        if (structures.Count == 0) {
            return;
        }
        SwapStructure(ideo, structures.RandomElement());
    }

    // Re-rolls one typed precept section (rituals/roles/relics/buildings/animals) in place: strips the
    // existing precepts of type T, then adds a random subset of the precept defs allowed for the current
    // memes - the SAME filter the typed picker uses (visible + preceptClass match + required-meme rule via
    // IdeoPreceptRules). Each add goes through AddPrecept's init:true path so per-precept setup runs.
    public static void RandomizeTypedPrecepts<T>(Ideo ideo, System.Type preceptClass) where T : Precept {
        List<Precept> existing = new List<Precept>();
        foreach (Precept precept in ideo.PreceptsListForReading) {
            if (precept is T) {
                existing.Add(precept);
            }
        }
        foreach (Precept precept in existing) {
            ideo.RemovePrecept(precept, replacing: false);
        }

        HashSet<string> present = new HashSet<string>();
        foreach (MemeDef meme in ideo.memes) {
            present.Add(meme.defName);
        }

        List<PreceptDef> allowed = new List<PreceptDef>();
        foreach (PreceptDef pd in DefDatabase<PreceptDef>.AllDefsListForReading) {
            if (!pd.visible || pd.preceptClass == null || !preceptClass.IsAssignableFrom(pd.preceptClass)) {
                continue;
            }
            List<string>? required = null;
            if (pd.requiredMemes != null && pd.requiredMemes.Count > 0) {
                required = new List<string>(pd.requiredMemes.Count);
                foreach (MemeDef req in pd.requiredMemes) {
                    required.Add(req.defName);
                }
            }
            if (IdeoPreceptRules.IsAllowed(required, present)) {
                allowed.Add(pd);
            }
        }

        if (allowed.Count > 0) {
            allowed.Shuffle();
            int cap = allowed.Count < 5 ? allowed.Count : 5;
            int take = Rand.RangeInclusive(1, cap);
            for (int i = 0; i < take; i++) {
                AddPrecept(ideo, allowed[i]);
            }
        }

        ideo.RecachePrecepts();
        RegenDescription(ideo);
    }

    public static void RandomizeRituals(Ideo ideo) {
        RandomizeTypedPrecepts<Precept_Ritual>(ideo, typeof(Precept_Ritual));
    }

    public static void RandomizeRoles(Ideo ideo) {
        RandomizeTypedPrecepts<Precept_Role>(ideo, typeof(Precept_Role));
    }

    public static void RandomizeRelics(Ideo ideo) {
        RandomizeTypedPrecepts<Precept_Relic>(ideo, typeof(Precept_Relic));
    }

    public static void RandomizeBuildings(Ideo ideo) {
        RandomizeTypedPrecepts<Precept_Building>(ideo, typeof(Precept_Building));
    }

    public static void RandomizeAnimals(Ideo ideo) {
        RandomizeTypedPrecepts<Precept_Animal>(ideo, typeof(Precept_Animal));
    }

    // Re-rolls every UNLOCKED section in place, leaving locked sections untouched. Symbols (icon)
    // have no lock and always re-roll. Done in place (not via a fresh GenerateIdeo) so a per-section
    // lock can actually be honored - you can keep your memes and reshuffle only precepts, etc.
    // Narrative is special: the meme/precept levers each call RegenerateDescription as a side effect,
    // so when narrative is locked we snapshot the text first and restore it last.
    public static void RandomizeAll(Ideo ideo, SectionLock locks) {
        string? keepDescription = (locks & SectionLock.Narrative) != 0 ? ideo.description : null;

        if ((locks & SectionLock.Structure) == 0) {
            RandomizeStructure(ideo);
        }
        if ((locks & SectionLock.Memes) == 0) {
            RandomizeMemes(ideo);
        }
        if ((locks & SectionLock.Precepts) == 0) {
            // Global precepts unlocked: vanilla re-rolls the whole precept set (issue stances AND the
            // typed sections) in one pass.
            RandomizePrecepts(ideo);
        }
        else {
            // Global precepts locked: keep the issue-stance precepts, but each unlocked typed section
            // still re-rolls independently.
            if ((locks & SectionLock.Rituals) == 0) {
                RandomizeRituals(ideo);
            }
            if ((locks & SectionLock.Roles) == 0) {
                RandomizeRoles(ideo);
            }
            if ((locks & SectionLock.Relics) == 0) {
                RandomizeRelics(ideo);
            }
            if ((locks & SectionLock.Buildings) == 0) {
                RandomizeBuildings(ideo);
            }
            if ((locks & SectionLock.Animals) == 0) {
                RandomizeAnimals(ideo);
            }
        }
        if ((locks & SectionLock.Styles) == 0) {
            RandomizeStyles(ideo);
        }
        if ((locks & SectionLock.Deities) == 0) {
            RegenerateDeities(ideo);
        }

        RandomizeIcon(ideo);

        if (keepDescription != null) {
            ideo.description = keepDescription;
        }
        else {
            RegenDescription(ideo);
        }
    }

    // Re-rolls only the icon + color (vanilla "randomize symbol"), leaving memes/precepts intact.
    public static void RandomizeIcon(Ideo ideo) {
        ideo.foundation.RandomizeIcon();
    }

    // Theist ideos store gods on an IdeoFoundation_Deity; non-deity foundations (e.g. animist) have
    // none, so this no-ops for them. GenerateDeities re-rolls each god's name/type/icon; regenerate the
    // description afterward so any deity grammar references refresh.
    public static void RegenerateDeities(Ideo ideo) {
        if (ideo.foundation is IdeoFoundation_Deity deityFoundation) {
            deityFoundation.GenerateDeities();
            ideo.RegenerateDescription(force: true);
        }
    }
}
