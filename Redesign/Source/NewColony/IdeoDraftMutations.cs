using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.Sound;

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


    private static MemeDef? CurrentStructure(Ideo ideo) {
        for (int i = 0; i < ideo.memes.Count; i++) {
            if (ideo.memes[i].category == MemeCategory.Structure) {
                return ideo.memes[i];
            }
        }
        return null;
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

    public static void SetAdjective(Ideo ideo, string adjective) {
        if (string.IsNullOrEmpty(adjective)) {
            return;
        }
        ideo.adjective = adjective;
    }

    public static void SetMemberName(Ideo ideo, string memberName) {
        if (string.IsNullOrEmpty(memberName)) {
            return;
        }
        ideo.memberName = memberName;
    }

    public static void SetNarrative(Ideo ideo, string narrative) {
        if (narrative == null) {
            return;
        }
        ideo.description = narrative;
    }

    public static void SetCulture(Ideo ideo, CultureDef culture) {
        if (culture == null || ideo.culture == culture) {
            return;
        }
        ideo.culture = culture;
        RegenTextSymbols(ideo);
    }

    public static void RandomizeCulture(Ideo ideo) {
        RandomizeCulture(ideo, SectionLock.None);
    }

    // Re-rolling the culture also regenerates the name/adjective/member grammar, so honour those locks
    // by capturing and restoring them around the culture roll.
    public static void RandomizeCulture(Ideo ideo, SectionLock locks) {
        string keepName = ideo.name;
        string keepAdjective = ideo.adjective;
        string keepMember = ideo.memberName;

        // Pick from the full culture list rather than IdeoFoundation.RandomizeCulture: that path draws
        // from the player faction's allowedCultures, which is often a single entry, so the "random" roll
        // returns the same culture every time. Exclude the current culture so the roll is always visible.
        List<CultureDef> all = DefDatabase<CultureDef>.AllDefsListForReading;
        List<CultureDef> choices = new List<CultureDef>(all.Count);
        for (int i = 0; i < all.Count; i++) {
            if (all[i] != ideo.culture) {
                choices.Add(all[i]);
            }
        }
        if (choices.Count > 0) {
            ideo.culture = choices.RandomElement();
        }
        RegenTextSymbols(ideo);

        (ideo.name, ideo.adjective, ideo.memberName) = IdeologyDecisions.ApplyTextSymbolLocks(
            locks, keepName, keepAdjective, keepMember, ideo.name, ideo.adjective, ideo.memberName);
    }

    // Vanilla generates ideo name, adjective and member noun together from one grammar request
    // (GenerateTextSymbols), so the per-field dice in the editor re-rolls the whole text-symbol set.
    public static void RandomizeTextSymbols(Ideo ideo) {
        RandomizeTextSymbols(ideo, SectionLock.None);
    }

    // Name, adjective and member noun all come out of one grammar request, so they regenerate together.
    // Re-roll the set only when at least one of the three is unlocked, then restore each locked field so a
    // dice on the name never disturbs a locked member noun (or vice versa).
    public static void RandomizeTextSymbols(Ideo ideo, SectionLock locks) {
        if (IdeologyDecisions.TextSymbolsFullyLocked(locks)) {
            return;
        }

        string keepName = ideo.name;
        string keepAdjective = ideo.adjective;
        string keepMember = ideo.memberName;
        RegenTextSymbols(ideo);
        (ideo.name, ideo.adjective, ideo.memberName) = IdeologyDecisions.ApplyTextSymbolLocks(
            locks, keepName, keepAdjective, keepMember, ideo.name, ideo.adjective, ideo.memberName);
    }

    private static void RegenTextSymbols(Ideo ideo) {
        if (ideo.culture == null) {
            return;
        }
        ideo.foundation?.GenerateTextSymbols();
    }

    // No public single-deity factory exists; GenerateNewDeity is private. Reflect once and append one
    // freshly-generated deity to the existing list (vanilla regenerates the whole set otherwise).
    public static void AddDeity(Ideo ideo) {
        if (ideo.foundation is not IdeoFoundation_Deity deityFoundation) {
            return;
        }

        System.Reflection.MethodInfo? generate = GenerateNewDeityMethod;
        if (generate == null) {
            return;
        }

        object? created = generate.Invoke(deityFoundation, null);
        if (created is not IdeoFoundation_Deity.Deity deity) {
            return;
        }

        List<IdeoFoundation_Deity.Deity> next = new List<IdeoFoundation_Deity.Deity>(deityFoundation.DeitiesListForReading) { deity };
        deityFoundation.SetDeities(next);
        ideo.RegenerateDescription(force: true);
    }

    private static readonly System.Reflection.MethodInfo? GenerateNewDeityMethod =
        typeof(IdeoFoundation_Deity).GetMethod(
            "GenerateNewDeity",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

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

        if ((locks & SectionLock.Culture) == 0) {
            RandomizeCulture(ideo, locks);
        }

        bool structureLocked = (locks & SectionLock.Structure) != 0;
        if (!structureLocked) {
            RandomizeStructure(ideo);
        }
        if ((locks & SectionLock.Memes) == 0) {
            // RandomizeMemes re-rolls the whole meme set INCLUDING the structure meme, so a Structure
            // lock has to be re-applied afterward: capture the locked structure, roll the memes, then
            // swap the original structure back if the roll replaced it.
            MemeDef? keepStructure = structureLocked ? CurrentStructure(ideo) : null;
            RandomizeMemes(ideo);
            if (IdeologyDecisions.ShouldRestoreStructure(locks, keepStructure, CurrentStructure(ideo))) {
                SwapStructure(ideo, keepStructure!);
            }
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

        RandomizeTextSymbols(ideo, locks);

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

    // Vanilla caps the deity count per ideo via Ideo.DeityCountRange (max 4 for theist structures);
    // the editor disables "Add deity" once the list is at that ceiling. Non-deity foundations report
    // false so the affordance never shows.
    public static bool CanAddDeity(Ideo ideo) {
        if (ideo.foundation is not IdeoFoundation_Deity deityFoundation) {
            return false;
        }

        return deityFoundation.DeitiesListForReading.Count < ideo.DeityCountRange.max;
    }

    // Drop one god. Mirrors vanilla's per-deity remove: rebuild the list without the target via
    // SetDeities, then regenerate the description so any grammar references to that god refresh.
    public static void RemoveDeity(Ideo ideo, IdeoFoundation_Deity.Deity deity) {
        if (ideo.foundation is not IdeoFoundation_Deity deityFoundation) {
            return;
        }

        List<IdeoFoundation_Deity.Deity> current = deityFoundation.DeitiesListForReading;
        List<IdeoFoundation_Deity.Deity> next = new List<IdeoFoundation_Deity.Deity>(current.Count);
        for (int i = 0; i < current.Count; i++) {
            if (current[i] != deity) {
                next.Add(current[i]);
            }
        }

        deityFoundation.SetDeities(next);
        ideo.RegenerateDescription(force: true);
    }

    // Re-roll a single god in place. GenerateNewDeity dedups its name against the current list (which
    // still holds the old god at this point), so the replacement is guaranteed unique; we then swap it
    // in at the same index to preserve ordering and count.
    public static void RegenerateOneDeity(Ideo ideo, IdeoFoundation_Deity.Deity deity) {
        if (ideo.foundation is not IdeoFoundation_Deity deityFoundation) {
            return;
        }

        System.Reflection.MethodInfo? generate = GenerateNewDeityMethod;
        if (generate == null) {
            return;
        }

        List<IdeoFoundation_Deity.Deity> current = deityFoundation.DeitiesListForReading;
        int index = current.IndexOf(deity);
        if (index < 0) {
            return;
        }

        if (generate.Invoke(deityFoundation, null) is not IdeoFoundation_Deity.Deity replacement) {
            return;
        }

        List<IdeoFoundation_Deity.Deity> next = new List<IdeoFoundation_Deity.Deity>(current);
        next[index] = replacement;
        deityFoundation.SetDeities(next);
        ideo.RegenerateDescription(force: true);
    }


    public static void SetDeityFields(Ideo ideo, IdeoFoundation_Deity.Deity deity, string name, string title, Gender gender) {
        deity.name = name;
        deity.type = title;
        deity.gender = gender;
        ideo.RegenerateAllPreceptNames();
        ideo.RegenerateDescription(force: true);
    }

    public static void SetIcon(Ideo ideo, IdeoIconDef icon, ColorDef color) {
        ideo.SetIcon(icon, color, IdeologyDecisions.ShouldClearPrimaryFactionColor(ideo.colorDef, color));
    }

    // The deep per-god editor is vanilla's Dialog_EditDeity (name/type/gender/icon). Reuse it rather
    // than rebuilding the form; on close it has already mutated the live Deity in place.

    // The badge's icon + color popup is vanilla's Dialog_ChooseIdeoSymbols (icon grid + color swatches).
    // Reuse it; it mutates ideo.iconDef/colorDef in place, which the badge re-reads on the next render.


    // Start a one-frame-maintained preview of the ideo's ongoing-ritual sound. The sound is a sustainer
    // (vanilla returns RitualSustainer_Theist or a style's soundOngoingRitual), so it CANNOT be played
    // as a one-shot or on-camera the naive way - that throws "no on-camera subSounds" / "sustainer as a
    // one-shot". Mirror SoundDef.DoEditWidgets: spawn a test sustainer on camera and let the caller
    // Maintain() it each frame and End() it to stop. Returns null when there is no sound or no map for an
    // in-world sound (the New Colony screen often has no current map).
    public static Sustainer? StartAmbiencePreview(Ideo ideo) {
        SoundDef? sound = ideo.SoundOngoingRitual;
        if (sound == null) {
            return null;
        }

        sound.ResolveReferences();

        // Mirror SoundDef.DoEditWidgets: an in-world sound (a non-onCamera subsound) must be placed on a
        // map, NOT on camera (playing it on camera throws "has sub-sounds in the world"). New Colony has
        // no current map, so when an in-world sound would need one and there is none, skip the preview
        // rather than throw.
        SoundInfo info;
        if (sound.HasSubSoundsInWorld) {
            Map? map = Find.CurrentMap;
            if (map == null) {
                return null;
            }
            info = SoundInfo.InMap(new TargetInfo(map.Center, map), MaintenanceType.PerFrame);
        }
        else {
            info = SoundInfo.OnCamera(MaintenanceType.PerFrame);
        }
        info.testPlay = true;

        if (sound.sustain) {
            return sound.TrySpawnSustainer(info);
        }

        sound.PlayOneShot(info);
        return null;
    }

    // Appearance preferences (hair/beard and tattoo style frequencies) live behind vanilla's
    // Dialog_EditIdeoStyleItems, one dialog per StyleItemTab. Reuse it; it mutates ideo.style in place.
    public static void OpenAppearanceEditor(Ideo ideo, StyleItemTab category) {
        Find.WindowStack.Add(new Dialog_EditIdeoStyleItems(ideo, category, IdeoEditMode.GameStart));
    }

    // Xenotype and apparel "desires" are precepts on the ideo, so adding/removing one routes through
    // the existing AddPrecept/RemovePrecept levers. These enumerators surface the current ones so the
    // editor can render a chip per precept and a count.
    public static void CollectPrecepts<T>(Ideo ideo, List<T> into) where T : Precept {
        into.Clear();
        List<Precept> all = ideo.PreceptsListForReading;
        for (int i = 0; i < all.Count; i++) {
            if (all[i] is T typed) {
                into.Add(typed);
            }
        }
    }

    public static int CountPrecepts<T>(Ideo ideo) where T : Precept {
        int count = 0;
        List<Precept> all = ideo.PreceptsListForReading;
        for (int i = 0; i < all.Count; i++) {
            if (all[i] is T) {
                count++;
            }
        }

        return count;
    }
}
